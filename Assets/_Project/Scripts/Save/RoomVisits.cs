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
    }

    /// <summary>
    /// Which room the player is in, how often each was entered, and which return changes have fired (F-14f AC4). The change for a room fires
    /// on its second entry and never again; the flag is saved, so loading the game keeps the changed room. Plain data, no scene.
    /// </summary>
    public sealed class RoomVisits
    {
        readonly Dictionary<int, int> _visits = new Dictionary<int, int>();
        readonly HashSet<int> _changed = new HashSet<int>();

        public int Current { get; private set; } = 1;

        public int VisitCount(int room) => _visits.TryGetValue(room, out var n) ? n : 0;
        public bool HasChanged(int room) => _changed.Contains(room);

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
            _visits.Clear(); _changed.Clear();
        }

        public RoomVisitsData Snapshot()
        {
            var keys = new List<int>(_visits.Keys); keys.Sort();
            var visits = new MemoryCount[keys.Count];
            for (int i = 0; i < visits.Length; i++) visits[i] = new MemoryCount { Key = keys[i].ToString(), Count = _visits[keys[i]] };
            var changed = new List<int>(_changed); changed.Sort();
            return new RoomVisitsData { Current = Current, Visits = visits, ChangedRooms = changed.ToArray() };
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
        }
    }
}
