using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Inventory
{
    /// <summary>
    /// Persistent inventory singleton that tracks collected item quantities.
    /// </summary>
    public sealed class InventoryService : MonoBehaviour
    {
        private static InventoryService instance;
        private readonly Dictionary<ItemData, int> items = new();

        public static InventoryService Instance => instance;
        public IReadOnlyDictionary<ItemData, int> Items => items;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        /// <summary>
        /// Adds an item quantity to inventory, respecting non-stackable item limits.
        /// </summary>
        /// <param name="item">The item asset to add.</param>
        /// <param name="amount">The quantity to add.</param>
        public void AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return;
            }

            if (!item.IsStackable && items.ContainsKey(item))
            {
                return;
            }

            int currentAmount = items.TryGetValue(item, out int existingAmount) ? existingAmount : 0;
            items[item] = item.IsStackable ? currentAmount + amount : 1;
            LogInventoryState();
        }

        public bool TryGetQuantity(ItemData item, out int quantity)
        {
            quantity = 0;
            return item != null && items.TryGetValue(item, out quantity);
        }

        private void LogInventoryState()
        {
            StringBuilder builder = new StringBuilder("[Inventory] ");
            bool hasEntry = false;

            foreach (KeyValuePair<ItemData, int> entry in items)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                if (hasEntry)
                {
                    builder.Append(" | ");
                }

                string itemName = !string.IsNullOrWhiteSpace(entry.Key.DisplayName) ? entry.Key.DisplayName : entry.Key.name;
                builder.Append(itemName);
                builder.Append(": ");
                builder.Append(entry.Value);
                hasEntry = true;
            }

            if (hasEntry)
            {
                Debug.Log(builder.ToString());
            }
        }
    }
}
