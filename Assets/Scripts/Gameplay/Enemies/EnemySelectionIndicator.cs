using UnityEngine;

namespace IdleOnDemo.Gameplay.Enemies
{
    /// <summary>
    /// Shows an enemy selection indicator only while the owning enemy is the active player target.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemySelectionIndicator : MonoBehaviour
    {
        private EnemyController enemyController;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            enemyController = GetComponentInParent<EnemyController>();
            SetVisible(false);
        }

        private void Start()
        {
            if (enemyController == null)
            {
                Debug.LogWarning("EnemySelectionIndicator could not find an EnemyController parent.");
                return;
            }

            enemyController.OnSelectedChanged += HandleSelectedChanged;
            HandleSelectedChanged(enemyController.IsSelected);
        }

        private void OnDestroy()
        {
            if (enemyController == null)
            {
                return;
            }

            enemyController.OnSelectedChanged -= HandleSelectedChanged;
        }

        private void HandleSelectedChanged(bool selected)
        {
            SetVisible(selected);
        }

        private void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }
    }
}
