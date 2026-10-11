using System;
using System.Collections.Generic;

namespace NocturneAnnex.Save
{
    [Serializable]
    public class RoomVisitsData
    {
        public int Current = 1;
        public MemoryCount[] Visits = new MemoryCount[0];   // key = room index as text, count = how many times entered
        public int[] ChangedRooms = new int[0];             // rooms whose return change has fired
        public string[] OpenedDoors = new string[0];        // "<room>/<door>": unlocked for good (a used key is gone, so a reload must not relock it)
        public string[] TakenItems = new string[0];         // "<room>/<item>": picked up, must not reappear
    }

    /// <summary>
    /// Which room the player is in, how often each was entered, and which return changes have fired (F-14f AC4). The change for a room fires
    /// on its second entry and never again; the flag is saved, so loading the game keeps the changed room. Plain data, no scene.
    /// </summary>
    public sealed class RoomVisits
    {
        readonly Dictionary<int, int> _visits = new Dictionary<int, int>();
        readonly HashSet<int> _changed = new HashSet<int>();
        readonly HashSet<string> _opened = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _taken = new HashSet<string>(StringComparer.Ordinal);

        public int Current { get; private set; } = 1;

        /// <summary>The furthest room ever entered this run, 0 before any entry.</summary>
        public int HighestRoom { get { int h = 0; foreach (var k in _visits.Keys) if (k > h) h = k; return h; } }

        public int VisitCount(int room) => _visits.TryGetValue(room, out var n) ? n : 0;
        public bool HasChanged(int room) => _changed.Contains(room);

        public void MarkDoorOpened(int room, string doorId) => _opened.Add(room + "/" + doorId);
        public bool IsDoorOpened(int room, string doorId) => _opened.Contains(room + "/" + doorId);
        public void MarkItemTaken(int room, string itemId) => _taken.Add(room + "/" + itemId);
        public bool IsItemTaken(int room, string itemId) => _taken.Contains(room + "/" + itemId);

        /// <summary>Records entering a room. Returns true exactly once per room: on the entry that makes the return change fire.</summary>
        public bool Enter(int room)
        {
            Current = room;
            _visits[room] = VisitCount(room) + 1;
            if (_visits[room] >= 2 && _changed.Add(room)) return true;
            return false;
        }

        public void Clear()
        {
            Current = 1;
            _visits.Clear(); _changed.Clear(); _opened.Clear(); _taken.Clear();
        }

        public RoomVisitsData Snapshot()
        {
            var keys = new List<int>(_visits.Keys); keys.Sort();
            var visits = new MemoryCount[keys.Count];
            for (int i = 0; i < visits.Length; i++) visits[i] = new MemoryCount { Key = keys[i].ToString(), Count = _visits[keys[i]] };
            var changed = new List<int>(_changed); changed.Sort();
            var opened = new List<string>(_opened); opened.Sort(StringComparer.Ordinal);
            var taken = new List<string>(_taken); taken.Sort(StringComparer.Ordinal);
            return new RoomVisitsData { Current = Current, Visits = visits, ChangedRooms = changed.ToArray(), OpenedDoors = opened.ToArray(), TakenItems = taken.ToArray() };
        }

        /// <summary>Restores a snapshot. Null (older saves) means a fresh run in room 1.</summary>
        public void Restore(RoomVisitsData data)
        {
            Clear();
            if (data == null) return;
            Current = data.Current < 1 ? 1 : data.Current;
            if (data.Visits != null)
                foreach (var v in data.Visits)
                    if (v != null && int.TryParse(v.Key, out var room) && room >= 1 && v.Count > 0) _visits[room] = v.Count;
            if (data.ChangedRooms != null) foreach (var r in data.ChangedRooms) if (r >= 1) _changed.Add(r);
            if (data.OpenedDoors != null) foreach (var d in data.OpenedDoors) if (!string.IsNullOrEmpty(d)) _opened.Add(d);
            if (data.TakenItems != null) foreach (var t in data.TakenItems) if (!string.IsNullOrEmpty(t)) _taken.Add(t);
        }
    }
}
