using UnityEngine;

namespace NocturneAnnex.Inventory
{
    public enum ItemKind { Key, Note, Collectible }

    /// <summary>Authored data for one item. Id is the stable key used by saves and doors.</summary>
    [CreateAssetMenu(menuName = "Nocturne Annex/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string Id = "";
        public string DisplayName = "Item";
        public ItemKind Kind = ItemKind.Collectible;
        [TextArea(2, 10)] public string Body = "";   // note text / flavour
    }
}
