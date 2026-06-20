using IdleOnDemo.Gameplay.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Renders the current inventory contents into a fixed set of UI slots.
    /// </summary>
    public sealed class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private Transform[] uiSlots;
        [SerializeField] private GameObject itemUIPrefab;

        /// <summary>
        /// Subscribes to inventory changes and draws the current inventory state.
        /// </summary>
        private void OnEnable()
        {
            if (InventoryService.Instance == null)
            {
                Debug.LogWarning("InventoryUIController could not find an InventoryService instance.");
                return;
            }

            InventoryService.Instance.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }

        /// <summary>
        /// Unsubscribes from inventory changes when the UI is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (InventoryService.Instance != null)
            {
                InventoryService.Instance.OnInventoryChanged -= RefreshUI;
            }
        }

        /// <summary>
        /// Clears every slot, then recreates item visuals from the inventory service data.
        /// </summary>
        private void RefreshUI()
        {
            ClearSlots();

            if (InventoryService.Instance == null || itemUIPrefab == null || uiSlots == null)
            {
                return;
            }

            int slotIndex = 0;
            foreach (var entry in InventoryService.Instance.Items)
            {
                if (entry.Key == null || slotIndex >= uiSlots.Length)
                {
                    continue;
                }

                Transform slot = uiSlots[slotIndex];
                if (slot == null)
                {
                    slotIndex++;
                    continue;
                }

                GameObject itemUI = Instantiate(itemUIPrefab, slot);
                Image itemImage = itemUI.GetComponentInChildren<Image>();
                if (itemImage != null)
                {
                    itemImage.sprite = entry.Key.Icon;
                }

                TextMeshProUGUI quantityText = itemUI.GetComponentInChildren<TextMeshProUGUI>();
                if (quantityText != null)
                {
                    quantityText.text = entry.Value.ToString();
                }

                slotIndex++;
            }
        }

        /// <summary>
        /// Removes all instantiated item visuals from every configured slot.
        /// </summary>
        private void ClearSlots()
        {
            if (uiSlots == null)
            {
                return;
            }

            foreach (Transform slot in uiSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                for (int i = slot.childCount - 1; i >= 0; i--)
                {
                    Destroy(slot.GetChild(i).gameObject);
                }
            }
        }
    }
}
