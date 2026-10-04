using System.Collections.Generic;

namespace NocturneAnnex.Inventory
{
    /// <summary>Plain-C# item id list with a capacity. No duplicates (no stacking in MVP).</summary>
    public class InventoryModel
    {
        readonly List<string> _ids = new List<string>();

        public int Capacity { get; }
        public int Count => _ids.Count;
        public IReadOnlyList<string> Ids => _ids;

        public InventoryModel(int capacity) { Capacity = capacity; }

        public bool Has(string id) => !string.IsNullOrEmpty(id) && _ids.Contains(id);

        /// <summary>False if id is empty, already held, or the inventory is full.</summary>
        public bool TryAdd(string id)
        {
            if (string.IsNullOrEmpty(id) || _ids.Contains(id) || _ids.Count >= Capacity) return false;
            _ids.Add(id);
            return true;
        }

        public bool Remove(string id) => _ids.Remove(id);

        public string[] Snapshot() => _ids.ToArray();

        /// <summary>Replaces contents. Entries beyond capacity, empty or duplicate are dropped; returns number dropped.</summary>
        public int Restore(IEnumerable<string> ids)
        {
            _ids.Clear();
            int dropped = 0;
            foreach (var id in ids ?? new string[0])
                if (!TryAdd(id)) dropped++;
            return dropped;
        }
    }
}
