using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Inventory
{
    /// <summary>Lookup of all items by id. Duplicate or empty ids are reported, not hidden.</summary>
    [CreateAssetMenu(menuName = "Nocturne Annex/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> Items = new List<ItemDefinition>();

        Dictionary<string, ItemDefinition> _byId;

        public bool TryGet(string id, out ItemDefinition item)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id ?? "", out item);
        }

        /// <summary>Returns problems found (empty/duplicate ids, null entries). Empty list means valid.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new HashSet<string>();
            for (int i = 0; i < Items.Count; i++)
            {
                var it = Items[i];
                if (it == null) { problems.Add($"Entry {i} is null"); continue; }
                if (string.IsNullOrEmpty(it.Id)) problems.Add($"'{it.name}' has an empty Id");
                else if (!seen.Add(it.Id)) problems.Add($"Duplicate Id '{it.Id}'");
            }
            return problems;
        }

        void Build()
        {
            _byId = new Dictionary<string, ItemDefinition>();
            foreach (var it in Items)
                if (it != null && !string.IsNullOrEmpty(it.Id)) _byId[it.Id] = it;
        }

        void OnValidate() => _byId = null;
    }
}
