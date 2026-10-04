using System;
using System.Collections.Generic;
using UnityEngine;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Inventory
{
    /// <summary>Player-side inventory. Items must exist in the ItemDatabase; unknown ids are refused and logged.</summary>
    public class PlayerInventory : MonoBehaviour, IKeyProvider, IItemReceiver
    {
        public ItemDatabase Database;
        public int Capacity = 12;

        public event Action Changed;

        InventoryModel _model;
        InventoryModel Model => _model ??= new InventoryModel(Capacity);

        public IReadOnlyList<string> Ids => Model.Ids;
        public bool Has(string id) => Model.Has(id);

        public bool TryAdd(string itemId)
        {
            if (Database == null || !Database.TryGet(itemId, out _))
            {
                Debug.LogError($"PlayerInventory: refused unknown item id '{itemId}'.", this);
                return false;
            }
            if (!Model.TryAdd(itemId)) return false;
            Changed?.Invoke();
            return true;
        }

        public bool HasKey(string keyId) => IsKey(keyId) && Model.Has(keyId);

        public bool ConsumeKey(string keyId)
        {
            if (!IsKey(keyId) || !Model.Remove(keyId)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Collected notes, in pickup order, for the journal (UI in F-07).</summary>
        public List<ItemDefinition> GetJournalNotes()
        {
            var notes = new List<ItemDefinition>();
            foreach (var id in Model.Ids)
                if (Database.TryGet(id, out var def) && def.Kind == ItemKind.Note) notes.Add(def);
            return notes;
        }

        public string[] Snapshot() => Model.Snapshot();

        public void Restore(IEnumerable<string> ids)
        {
            int dropped = Model.Restore(ids);
            if (dropped > 0) Debug.LogWarning($"PlayerInventory: dropped {dropped} invalid saved item(s).", this);
            Changed?.Invoke();
        }

        bool IsKey(string id) => Database != null && Database.TryGet(id, out var def) && def.Kind == ItemKind.Key;
    }
}
