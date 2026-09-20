using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace YesterdayMap.Prologue
{
    [Serializable]
    public sealed class InventoryItemEvent : UnityEvent<ItemData> { }

    // Lightweight inventory for the house prologue. It tracks slot usage, not stack counts.
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 8;
        [SerializeField] private List<ItemData> startingItems = new();
        [SerializeField] private InventoryItemEvent onItemAdded = new();
        [SerializeField] private InventoryItemEvent onItemRemoved = new();

        private readonly List<ItemData> items = new();

        public int Capacity => capacity;
        public int UsedSpace { get; private set; }
        public int RemainingSpace => Mathf.Max(0, capacity - UsedSpace);
        public IReadOnlyList<ItemData> Items => items;
        public InventoryItemEvent OnItemAdded => onItemAdded;
        public InventoryItemEvent OnItemRemoved => onItemRemoved;

        private void Awake()
        {
            // Seed optional starting items while respecting capacity.
            foreach (ItemData item in startingItems)
            {
                if (item != null)
                {
                    TryAddItem(item);
                }
            }
        }

        public bool CanAdd(ItemData item)
        {
            return item != null && item.CanPickup && UsedSpace + item.InventorySpace <= capacity;
        }

        public bool TryAddItem(ItemData item)
        {
            if (!CanAdd(item))
            {
                Debug.Log($"[Inventory] Cannot add {(item != null ? item.DisplayName : "null item")}. Used {UsedSpace}/{capacity}.");
                return false;
            }

            items.Add(item);
            UsedSpace += item.InventorySpace;
            onItemAdded.Invoke(item);
            Debug.Log($"[Inventory] Added {item.DisplayName}. Used {UsedSpace}/{capacity}.");
            return true;
        }

        public bool RemoveItem(ItemData item)
        {
            if (item == null || !items.Remove(item))
            {
                return false;
            }

            UsedSpace = Mathf.Max(0, UsedSpace - item.InventorySpace);
            onItemRemoved.Invoke(item);
            Debug.Log($"[Inventory] Removed {item.DisplayName}. Used {UsedSpace}/{capacity}.");
            return true;
        }

        public string GetItemListText()
        {
            if (items.Count == 0)
            {
                return "Inventory is empty.";
            }

            List<string> names = new();
            foreach (ItemData item in items)
            {
                if (item != null)
                {
                    names.Add($"{item.DisplayName}({item.InventorySpace})");
                }
            }

            return string.Join(", ", names);
        }
    }
}
