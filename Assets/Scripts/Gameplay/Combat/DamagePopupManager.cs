using UnityEngine;

namespace IdleOnDemo.Gameplay.Combat
{
    /// <summary>
    /// Spawns damage popup UI at positions derived from combat world coordinates.
    /// </summary>
    public sealed class DamagePopupManager : MonoBehaviour
    {
        private const float DefaultVerticalWorldOffset = 0.8f;

        [SerializeField] private DamagePopup popupPrefab;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 16f);
        [SerializeField] private Vector2 randomizationJitter = new Vector2(30f, 15f);

        private static DamagePopupManager instance;

        /// <summary>
        /// Spawns a damage popup for the given amount near the supplied world position.
        /// </summary>
        /// <param name="amount">The damage value to display.</param>
        /// <param name="worldPosition">The combat target position in world space.</param>
        public static void ShowDamage(int amount, Vector3 worldPosition)
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<DamagePopupManager>();
            }

            if (instance == null)
            {
                Debug.LogWarning("DamagePopupManager could not show damage because no manager exists in the scene.");
                return;
            }

            instance.ShowDamageInternal(amount, worldPosition + Vector3.up * DefaultVerticalWorldOffset);
        }

        /// <summary>
        /// Registers the singleton instance and caches nearby canvas references.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            targetCanvas ??= GetComponentInChildren<Canvas>();
            targetCanvas ??= FindAnyObjectByType<Canvas>();
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
        /// Instantiates the popup prefab and places it in the configured canvas.
        /// </summary>
        /// <param name="amount">The damage value to display.</param>
        /// <param name="worldPosition">The offset-adjusted world position to convert into UI space.</param>
        private void ShowDamageInternal(int amount, Vector3 worldPosition)
        {
            if (popupPrefab == null)
            {
                Debug.LogWarning("DamagePopupManager is missing a popup prefab reference.");
                return;
            }

            if (targetCanvas == null)
            {
                Debug.LogWarning("DamagePopupManager is missing a target canvas reference.");
                return;
            }

            Camera worldCamera = Camera.main;
            Vector3 screenPosition = worldCamera != null ? worldCamera.WorldToScreenPoint(worldPosition) : worldPosition;
            if (screenPosition.z < 0f)
            {
                return;
            }

            DamagePopup popup = Instantiate(popupPrefab, targetCanvas.transform);
            RectTransform popupTransform = popup.GetComponent<RectTransform>();
            RectTransform canvasTransform = targetCanvas.transform as RectTransform;
            Vector2 randomJitter = new Vector2(Random.Range(-randomizationJitter.x, randomizationJitter.x), Random.Range(-randomizationJitter.y, randomizationJitter.y));
            Vector2 finalOffset = screenOffset + randomJitter;

            if (popupTransform != null && canvasTransform != null)
            {
                Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasTransform,
                    (Vector2)screenPosition + finalOffset,
                    uiCamera,
                    out Vector2 anchoredPosition);

                popupTransform.anchoredPosition = anchoredPosition;
                popupTransform.localScale = Vector3.one;
            }
            else
            {
                popup.transform.position = screenPosition + (Vector3)finalOffset;
            }

            popup.Initialize(amount);
        }
    }
}
