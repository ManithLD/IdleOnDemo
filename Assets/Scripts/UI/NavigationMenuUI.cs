using UnityEngine;
using UnityEngine.UI;

namespace IdleOnDemo.UI
{
    public sealed class NavigationMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button itemsButton;
        [SerializeField] private Button existButton;

        [Header("Containers")]
        [SerializeField] private GameObject inventoryContainer;

        [Header("Sprites")]
        [SerializeField] private Sprite buttonOffSprite;
        [SerializeField] private Sprite buttonOnSprite;

        private bool isInventoryOpen = false;

        private void Start()
        {
            if (itemsButton != null)
            {
                itemsButton.onClick.AddListener(OnItemsButtonClicked);
            }

            if (existButton != null)
            {
                existButton.onClick.AddListener(OnExistButtonClicked);
            }

            // Ensure inventory starts closed
            if (inventoryContainer != null)
            {
                inventoryContainer.SetActive(isInventoryOpen);
            }
        }

        private void OnItemsButtonClicked()
        {
            isInventoryOpen = !isInventoryOpen;

            if (inventoryContainer != null)
            {
                inventoryContainer.SetActive(isInventoryOpen);
            }

            itemsButton.image.sprite = isInventoryOpen ? buttonOnSprite : buttonOffSprite;
        }

        private void OnExistButtonClicked()
        {
            Debug.Log("Exiting Game...");
            Application.Quit();
        }

        private void OnDestroy()
        {
            if (itemsButton != null)
            {
                itemsButton.onClick.RemoveAllListeners();
            }
        }
    }
}