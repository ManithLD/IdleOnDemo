using UnityEngine;

namespace IdleOnDemo.Gameplay.Inventory
{
    /// <summary>
    /// Displays a dropped item stack in the world for future pickup interactions.
    /// </summary>
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity = 1;
        [SerializeField] private SpriteRenderer iconRenderer;

        public ItemData Item => item;
        public int Quantity => quantity;

        /// <summary>
        /// Assigns the item stack represented by this world pickup and refreshes its display.
        /// </summary>
        /// <param name="item">The item data asset represented by this pickup.</param>
        /// <param name="quantity">The number of items represented by this pickup.</param>
        public void Initialize(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = Mathf.Max(1, quantity);
            RefreshVisuals();
            UpdateObjectName();
        }

        /// <summary>
        /// Caches the pickup's sprite renderer before runtime initialization.
        /// </summary>
        private void Awake()
        {
            CacheIconRenderer();
            quantity = Mathf.Max(1, quantity);
            RefreshVisuals();
        }

        /// <summary>
        /// Keeps serialized pickup values valid while editing prefabs or scene instances.
        /// </summary>
        private void OnValidate()
        {
            CacheIconRenderer();
            quantity = Mathf.Max(1, quantity);
            RefreshVisuals();
        }

        /// <summary>
        /// Finds a child or same-object sprite renderer when one has not been assigned.
        /// </summary>
        private void CacheIconRenderer()
        {
            if (iconRenderer == null)
            {
                iconRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Updates the pickup sprite from its assigned item data.
        /// </summary>
        private void RefreshVisuals()
        {
            if (iconRenderer != null)
            {
                iconRenderer.sprite = item != null ? item.Icon : null;
            }
        }

        /// <summary>
        /// Gives the runtime pickup object a readable name for scene debugging.
        /// </summary>
        private void UpdateObjectName()
        {
            if (item == null)
            {
                gameObject.name = $"Pickup_Unassigned_x{quantity}";
                return;
            }

            string itemName = !string.IsNullOrWhiteSpace(item.DisplayName) ? item.DisplayName : item.name;
            gameObject.name = $"Pickup_{itemName}_x{quantity}";
        }
    }
}
