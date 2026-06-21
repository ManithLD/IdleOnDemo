using UnityEngine;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Spawns top-left HUD notifications for collected items and currency.
    /// </summary>
    public sealed class LootNotificationManager : MonoBehaviour
    {
        private static LootNotificationManager instance;

        [SerializeField] private LootNotification notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private bool newestOnTop = true;

        /// <summary>
        /// Gets the active loot notification manager instance when one exists in the scene.
        /// </summary>
        public static LootNotificationManager Instance => instance;

        /// <summary>
        /// Enforces a single active manager for simple gameplay calls.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            notificationContainer ??= transform;
        }

        /// <summary>
        /// Clears the singleton reference when this manager is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Shows a formatted item pickup notification.
        /// </summary>
        /// <param name="itemName">The display name of the collected item.</param>
        /// <param name="quantity">The collected quantity.</param>
        public void ShowLoot(string itemName, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0)
            {
                return;
            }

            ShowMessage($"+ {itemName} x {quantity}");
        }

        /// <summary>
        /// Shows a formatted coin pickup notification.
        /// </summary>
        /// <param name="amount">The number of coins collected.</param>
        public void ShowCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ShowMessage($"+ {amount} Coins");
        }

        /// <summary>
        /// Instantiates and initializes a notification under the configured layout container.
        /// </summary>
        /// <param name="message">The formatted notification message.</param>
        private void ShowMessage(string message)
        {
            if (notificationPrefab == null || notificationContainer == null)
            {
                Debug.LogWarning("LootNotificationManager is missing a notification prefab or container reference.");
                return;
            }

            LootNotification notification = Instantiate(notificationPrefab, notificationContainer);
            if (newestOnTop)
            {
                notification.transform.SetAsFirstSibling();
            }
            else
            {
                notification.transform.SetAsLastSibling();
            }

            notification.Initialize(message);
        }
    }
}
