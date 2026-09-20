using UnityEngine;

namespace YesterdayMap.Prologue
{
    // The broad item categories used by the prologue scavenging map.
    public enum ItemKind
    {
        Food,
        Water,
        Medicine,
        Weapon,
        Repair,
        Mental,
        Clothing
    }

    // Data-only item definition so pickups, UI, and inventory can share one source of truth.
    [CreateAssetMenu(fileName = "ItemData", menuName = "YesterdayMap/Prologue/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "item_id";
        [SerializeField] private string displayName = "Item";
        [SerializeField] private ItemKind itemKind = ItemKind.Food;
        [SerializeField, TextArea] private string description = "Temporary prologue item.";
        [SerializeField, Min(1)] private int inventorySpace = 1;
        [SerializeField] private bool canPickup = true;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ItemKind ItemKind => itemKind;
        public string Description => description;
        public int InventorySpace => Mathf.Max(1, inventorySpace);
        public bool CanPickup => canPickup;

        // Editor and generator helper for creating the temporary blockout item assets.
        public void Configure(string id, string itemName, ItemKind kind, string itemDescription, int space, bool pickupable)
        {
            itemId = id;
            displayName = itemName;
            itemKind = kind;
            description = itemDescription;
            inventorySpace = Mathf.Max(1, space);
            canPickup = pickupable;
        }
    }
}
