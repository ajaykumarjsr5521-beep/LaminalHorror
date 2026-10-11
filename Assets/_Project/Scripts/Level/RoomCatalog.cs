using System.Collections.Generic;
using System.Linq;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// The ten rooms as data (F-14f). Room 1 is Level_B1 (hand-built); rooms 2-10 are generated greybox scenes from these rows.
    /// RoomValidator checks the table: one progression door, no softlock, the room 5 key opens room 7, difficulty only rises.
    /// </summary>
    public static class RoomCatalog
    {
        public const int Count = 10;
        public const string Room5KeyId = "key_room5";
        public const string FuseId = "fuse";

        static List<RoomDefinition> _all;
        public static IReadOnlyList<RoomDefinition> All => _all ??= Build();

        public static RoomDefinition Get(int index) => All.FirstOrDefault(r => r.Index == index);
        public static RoomDefinition ById(string id) => All.FirstOrDefault(r => r.Id == id);

        /// <summary>Every key and fuse id a room places, for the item database.</summary>
        public static IEnumerable<string> AllItemIds()
        {
            var ids = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (var r in All)
            {
                foreach (var k in r.Keys) ids.Add(k.Id);
                foreach (var s in r.Switches) if (!string.IsNullOrEmpty(s.RequiredItemId)) ids.Add(s.RequiredItemId);
            }
            return ids;
        }

        static RoomDefinition Room(int index, string title, DifficultySpec diff, int hide, int noise, ReturnChangeKind change, params DoorSpec[] doors)
        {
            var r = new RoomDefinition
            {
                Index = index, Id = index == 1 ? "B1" : "R" + index.ToString("00"), Title = title,
                Difficulty = diff, HideSpots = hide, NoiseProps = noise, ReturnChange = change
            };
            r.Doors.AddRange(doors);
            return r;
        }

        static DoorSpec Back() => new DoorSpec("back", DoorKind.Back);
        static DoorSpec Fake(string id) => new DoorSpec(id, DoorKind.Fake);
        static DoorSpec Blocked(string id) => new DoorSpec(id, DoorKind.Blocked);
        static DoorSpec Locked(string id) => new DoorSpec(id, DoorKind.Locked);
        static DoorSpec KeyDoor(string id, string key) => new DoorSpec(id, DoorKind.Key, UnlockKind.Key, key);
        static DoorSpec Prog(UnlockKind unlock = UnlockKind.None, string unlockId = "") => new DoorSpec("prog", DoorKind.Progression, unlock, unlockId);

        static List<RoomDefinition> Build()
        {
            var rooms = new List<RoomDefinition>();

            // 1 Intro (Level_B1): break room, hall, stacks, records, dock. The stamp opens the records door; the keypad opens the dock.
            var r1 = Room(1, "Break Room Wing", new DifficultySpec(0.20f, 4.0f, 8f, 5), 3, 4, ReturnChangeKind.None,
                new DoorSpec("break_hall", DoorKind.Open), new DoorSpec("hall_stacks", DoorKind.Open),
                KeyDoor("hall_records", "brass_stamp"), Prog(UnlockKind.Code));
            r1.Keys.Add(new KeySpec("brass_stamp"));
            r1.Code = new CodeSpec("1439");
            rooms.Add(r1);

            // 2 First threat: the entity patrols, the way on needs one key lying in the open.
            var r2 = Room(2, "Reading Room", new DifficultySpec(0.30f, 3.8f, 8.5f, 5), 3, 4, ReturnChangeKind.ChairMoved,
                Back(), Prog(UnlockKind.Key, "key_r2"), Fake("r02_fake"), Locked("r02_locked"), Blocked("r02_blocked"));
            r2.Keys.Add(new KeySpec("key_r2"));
            rooms.Add(r2);

            // 3 Psychological: a closet holds the key; the closet key is in the room. Quiet, wrong sounds.
            var r3 = Room(3, "Waiting Room", new DifficultySpec(0.35f, 3.6f, 9f, 5), 3, 4, ReturnChangeKind.NewSound,
                Back(), Prog(UnlockKind.Key, "key_r3"), KeyDoor("r03_closet", "key_r3a"), Fake("r03_fake"), Blocked("r03_blocked"));
            r3.Keys.Add(new KeySpec("key_r3a"));
            r3.Keys.Add(new KeySpec("key_r3", KeyPlacement.BehindDoor, "r03_closet"));
            rooms.Add(r3);

            // 4 Multi-key: three keys in a chain. A fourth key (for room 5) only appears when the player returns.
            var r4 = Room(4, "Archive Annex", new DifficultySpec(0.45f, 3.4f, 9.5f, 5), 4, 5, ReturnChangeKind.RevealKey,
                Back(), Prog(UnlockKind.Key, "key_r4c"), KeyDoor("r04_a", "key_r4a"), KeyDoor("r04_b", "key_r4b"), Fake("r04_fake"));
            r4.Keys.Add(new KeySpec("key_r4a"));
            r4.Keys.Add(new KeySpec("key_r4b", KeyPlacement.BehindDoor, "r04_a"));
            r4.Keys.Add(new KeySpec("key_r4c", KeyPlacement.BehindDoor, "r04_b"));
            r4.Keys.Add(new KeySpec("key_store", KeyPlacement.AfterReturn));
            r4.RevealedKeyId = "key_store";
            rooms.Add(r4);

            // 5 Backtracking: the store door needs the key that appeared in room 4; behind it is the key that opens room 7.
            var r5 = Room(5, "Maintenance Hall", new DifficultySpec(0.50f, 3.2f, 10f, 6), 4, 5, ReturnChangeKind.LightDead,
                Back(), Prog(UnlockKind.Key, "key_r5p"), KeyDoor("r05_store", "key_store"), Fake("r05_fake"), Blocked("r05_blocked"));
            r5.Keys.Add(new KeySpec("key_r5p"));
            r5.Keys.Add(new KeySpec(Room5KeyId, KeyPlacement.BehindDoor, "r05_store"));
            rooms.Add(r5);

            // 6 Combination and switch: a hidden switch opens the closet that holds the code clue.
            var r6 = Room(6, "Control Corridor", new DifficultySpec(0.55f, 3.0f, 10f, 6), 4, 5, ReturnChangeKind.ChairMoved,
                Back(), Prog(UnlockKind.Code), new DoorSpec("r06_side", DoorKind.Key, UnlockKind.Switch, "s6"), Fake("r06_fake"), Locked("r06_locked"));
            r6.Switches.Add(new SwitchSpec("s6"));
            r6.Code = new CodeSpec("6173", "r06_side");
            rooms.Add(r6);

            // 7 Major threat: every door is locked until the room 5 key.
            var r7 = Room(7, "Boiler Gallery", new DifficultySpec(0.75f, 2.4f, 11f, 6), 4, 5, ReturnChangeKind.NewSound,
                Back(), Prog(UnlockKind.Key, Room5KeyId), Locked("r07_locked"), Fake("r07_fake"), Blocked("r07_blocked"));
            rooms.Add(r7);

            // 8 Interconnected: the switch is inside a side room that needs its own key.
            var r8 = Room(8, "Switch Hall", new DifficultySpec(0.80f, 2.2f, 11.5f, 7), 4, 5, ReturnChangeKind.LightDead,
                Back(), Prog(UnlockKind.Switch, "s8"), KeyDoor("r08_a", "key_r8"), Fake("r08_fake"), Locked("r08_locked"));
            r8.Keys.Add(new KeySpec("key_r8"));
            r8.Switches.Add(new SwitchSpec("s8", behindDoor: "r08_a"));
            rooms.Add(r8);

            // 9 Final trials: a key for the side room, the code clue inside it, then the keypad.
            var r9 = Room(9, "Trial Chamber", new DifficultySpec(0.90f, 2.0f, 12f, 7), 4, 5, ReturnChangeKind.ChairMoved,
                Back(), Prog(UnlockKind.Code), KeyDoor("r09_a", "key_r9"), Fake("r09_fake"), Blocked("r09_blocked"), Locked("r09_locked"));
            r9.Keys.Add(new KeySpec("key_r9"));
            r9.Code = new CodeSpec("9052", "r09_a");
            rooms.Add(r9);

            // 10 Final escape: the fuse powers the switch, the switch opens the store, the store holds the exit key.
            var r10 = Room(10, "Exit Passage", new DifficultySpec(1.00f, 1.8f, 12f, 7), 4, 5, ReturnChangeKind.None,
                Back(), Prog(UnlockKind.Key, "key_exit"), new DoorSpec("r10_a", DoorKind.Key, UnlockKind.Switch, "s10"), Fake("r10_fake"), Blocked("r10_blocked"));
            r10.Keys.Add(new KeySpec(FuseId));
            r10.Keys.Add(new KeySpec("key_exit", KeyPlacement.BehindDoor, "r10_a"));
            r10.Switches.Add(new SwitchSpec("s10", FuseId));
            rooms.Add(r10);

            return rooms;
        }
    }
}
