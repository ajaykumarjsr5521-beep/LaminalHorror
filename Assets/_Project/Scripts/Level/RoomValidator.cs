using System;
using System.Collections.Generic;
using System.Linq;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// Checks the room table (F-14f AC1-3): exactly one progression door per room, 4-6 doors, no softlock (every door the player needs
    /// can be opened with things they can reach), the room 5 key opens room 7, and difficulty only moves one way. Returns problems;
    /// an empty list means the table is sound. Pure C#, so a bad edit fails an EditMode test, not a playtest.
    /// </summary>
    public static class RoomValidator
    {
        public static List<string> Validate(IReadOnlyList<RoomDefinition> rooms)
        {
            var problems = new List<string>();
            var carried = new HashSet<string>(StringComparer.Ordinal);   // keys the player can hold when entering the next room
            var keyUsers = new Dictionary<string, string>(StringComparer.Ordinal);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            RoomDefinition previous = null;

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                string at = $"room {r.Index} ({r.Id})";
                if (r.Index != i + 1) problems.Add($"{at}: index should be {i + 1}");
                if (!seenIds.Add(r.Id)) problems.Add($"{at}: duplicate room id");

                CheckDoors(r, at, problems);
                CheckUnlocks(r, at, carried, keyUsers, problems);
                CheckReturnChange(r, at, problems);
                if (previous != null) CheckDifficulty(previous, r, at, problems);
                else if (r.Difficulty == null) problems.Add($"{at}: no difficulty");

                foreach (var k in ObtainableKeys(r, carried)) carried.Add(k);
                foreach (var k in r.Keys.Where(k => k.Placement == KeyPlacement.AfterReturn)) carried.Add(k.Id);   // reachable by coming back
                previous = r;
            }

            CheckRoomFiveKey(rooms, problems);
            return problems;
        }

        static void CheckDoors(RoomDefinition r, string at, List<string> problems)
        {
            int prog = r.Doors.Count(d => d.Kind == DoorKind.Progression);
            if (prog != 1) problems.Add($"{at}: needs exactly one progression door, has {prog}");
            if (r.Doors.Count < 4 || r.Doors.Count > 6) problems.Add($"{at}: has {r.Doors.Count} doors, expected 4-6");
            var ids = new HashSet<string>();
            foreach (var d in r.Doors)
            {
                if (string.IsNullOrEmpty(d.Id) || !ids.Add(d.Id)) problems.Add($"{at}: door id '{d.Id}' is empty or duplicated");
                if (d.Kind == DoorKind.Open && r.Index != 1) problems.Add($"{at}: door '{d.Id}' is Open; only room 1 has open passages");
                if (d.Kind == DoorKind.Back && (r.Index == 1 || d.Unlock != UnlockKind.None)) problems.Add($"{at}: bad back door '{d.Id}'");
                bool never = d.Kind == DoorKind.Fake || d.Kind == DoorKind.Locked || d.Kind == DoorKind.Blocked || d.Kind == DoorKind.Open || d.Kind == DoorKind.Back;
                if (never && d.Unlock != UnlockKind.None) problems.Add($"{at}: door '{d.Id}' is {d.Kind} but has an unlock");
                if (d.Kind == DoorKind.Key && d.Unlock == UnlockKind.None) problems.Add($"{at}: key door '{d.Id}' has no unlock");
            }
            if (r.Index > 1 && !r.Doors.Any(d => d.Kind == DoorKind.Back)) problems.Add($"{at}: no back door");
            if (r.HideSpots < 1) problems.Add($"{at}: needs at least one hide spot");
        }

        static void CheckUnlocks(RoomDefinition r, string at, HashSet<string> carried, Dictionary<string, string> keyUsers, List<string> problems)
        {
            bool usesCode = r.Doors.Any(d => d.Unlock == UnlockKind.Code);
            if (usesCode && (r.Code == null || string.IsNullOrEmpty(r.Code.Code))) problems.Add($"{at}: a door needs a code but the room has none");
            if (!usesCode && r.Code != null) problems.Add($"{at}: has a code but no door uses it");

            foreach (var d in r.Doors.Where(d => d.Unlock == UnlockKind.Key))
            {
                if (keyUsers.TryGetValue(d.UnlockId, out var other)) problems.Add($"{at}: key '{d.UnlockId}' is used by door '{d.Id}' and already by {other}");
                keyUsers[d.UnlockId] = $"{r.Id}/{d.Id}";
            }
            foreach (var d in r.Doors.Where(d => d.Unlock == UnlockKind.Switch))
                if (!r.Switches.Any(s => s.Id == d.UnlockId)) problems.Add($"{at}: door '{d.Id}' needs switch '{d.UnlockId}' that the room lacks");

            // fixed point: which doors can the player open, using only what they can reach
            var open = new HashSet<string>();
            var keys = new HashSet<string>(carried, StringComparer.Ordinal);
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var d in r.Doors)
                    if (!open.Contains(d.Id) && CanOpen(r, d, open, keys)) { open.Add(d.Id); changed = true; }
                foreach (var k in r.Keys)
                    if (k.Placement != KeyPlacement.AfterReturn && (k.Placement == KeyPlacement.Open || open.Contains(k.BehindDoorId)) && keys.Add(k.Id)) changed = true;
            }

            var prog = r.Progression;
            if (prog != null && !open.Contains(prog.Id)) problems.Add($"{at}: softlock, the progression door cannot be opened with reachable items");
            foreach (var d in r.Doors.Where(d => d.Kind == DoorKind.Key))
                if (!open.Contains(d.Id)) problems.Add($"{at}: key door '{d.Id}' can never be opened");
            foreach (var k in r.Keys)
            {
                if (k.Placement == KeyPlacement.BehindDoor && r.Door(k.BehindDoorId) == null) problems.Add($"{at}: key '{k.Id}' is behind unknown door '{k.BehindDoorId}'");
                if (k.Placement == KeyPlacement.BehindDoor && r.Doors.Any(d => d.Id == k.BehindDoorId && d.UnlockId == k.Id)) problems.Add($"{at}: key '{k.Id}' is behind the door it opens");
            }
        }

        static bool CanOpen(RoomDefinition r, DoorSpec d, HashSet<string> open, HashSet<string> keys)
        {
            switch (d.Kind)
            {
                case DoorKind.Fake: case DoorKind.Locked: case DoorKind.Blocked: return false;
                case DoorKind.Open: case DoorKind.Back: return true;
            }
            switch (d.Unlock)
            {
                case UnlockKind.None: return true;
                case UnlockKind.Key: return keys.Contains(d.UnlockId);
                case UnlockKind.Code:
                    return r.Code != null && (string.IsNullOrEmpty(r.Code.ClueBehindDoorId) || open.Contains(r.Code.ClueBehindDoorId));
                case UnlockKind.Switch:
                    var s = r.Switches.FirstOrDefault(x => x.Id == d.UnlockId);
                    return s != null
                        && (string.IsNullOrEmpty(s.BehindDoorId) || open.Contains(s.BehindDoorId))
                        && (string.IsNullOrEmpty(s.RequiredItemId) || keys.Contains(s.RequiredItemId));
            }
            return false;
        }

        /// <summary>Keys the player can pick up in this room during one visit.</summary>
        static IEnumerable<string> ObtainableKeys(RoomDefinition r, HashSet<string> carried)
        {
            var open = new HashSet<string>();
            var keys = new HashSet<string>(carried, StringComparer.Ordinal);
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var d in r.Doors)
                    if (!open.Contains(d.Id) && CanOpen(r, d, open, keys)) { open.Add(d.Id); changed = true; }
                foreach (var k in r.Keys)
                    if (k.Placement != KeyPlacement.AfterReturn && (k.Placement == KeyPlacement.Open || open.Contains(k.BehindDoorId)) && keys.Add(k.Id)) changed = true;
            }
            return keys.Where(k => !carried.Contains(k));
        }

        static void CheckReturnChange(RoomDefinition r, string at, List<string> problems)
        {
            if (!string.IsNullOrEmpty(r.ToyBoxKeyId) && !r.Keys.Any(k => k.Id == r.ToyBoxKeyId && k.Placement == KeyPlacement.Open))
                problems.Add($"{at}: toy box key '{r.ToyBoxKeyId}' must be an Open key of the room");
            bool reveals = r.ReturnChange == ReturnChangeKind.RevealKey;
            var after = r.Keys.Where(k => k.Placement == KeyPlacement.AfterReturn).ToList();
            if (reveals && !after.Any(k => k.Id == r.RevealedKeyId)) problems.Add($"{at}: RevealKey names '{r.RevealedKeyId}' which is not an AfterReturn key");
            if (!reveals && after.Count > 0) problems.Add($"{at}: has an AfterReturn key but its return change is {r.ReturnChange}");
        }

        static void CheckDifficulty(RoomDefinition a, RoomDefinition b, string at, List<string> problems)
        {
            var x = a.Difficulty; var y = b.Difficulty;
            if (y == null || x == null) { problems.Add($"{at}: no difficulty"); return; }
            if (y.Threat < 0f || y.Threat > 1f) problems.Add($"{at}: threat outside 0..1");
            if (y.Threat < x.Threat) problems.Add($"{at}: threat falls ({x.Threat} to {y.Threat})");
            if (y.InspectIntervalSeconds > x.InspectIntervalSeconds) problems.Add($"{at}: inspect interval rises");
            if (y.InspectRange < x.InspectRange) problems.Add($"{at}: inspect range falls");
            if (y.PatrolPoints < x.PatrolPoints) problems.Add($"{at}: patrol points fall");
        }

        static void CheckRoomFiveKey(IReadOnlyList<RoomDefinition> rooms, List<string> problems)
        {
            var r5 = rooms.FirstOrDefault(r => r.Index == 5);
            var r7 = rooms.FirstOrDefault(r => r.Index == 7);
            if (r5 == null || r7 == null) { problems.Add("rooms 5 and 7 are required"); return; }
            if (!r5.Keys.Any(k => k.Id == RoomCatalog.Room5KeyId)) problems.Add($"room 5 does not hold '{RoomCatalog.Room5KeyId}'");
            var prog = r7.Progression;
            if (prog == null || prog.Unlock != UnlockKind.Key || prog.UnlockId != RoomCatalog.Room5KeyId) problems.Add("room 7's progression door must need the room 5 key");
            foreach (var d in r7.Doors)
                if (d.Kind != DoorKind.Back && d.Kind != DoorKind.Progression && d.Kind != DoorKind.Fake && d.Kind != DoorKind.Locked && d.Kind != DoorKind.Blocked)
                    problems.Add($"room 7 door '{d.Id}' is not locked");
        }
    }
}
