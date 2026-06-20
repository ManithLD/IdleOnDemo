using System;
using IdleOnDemo.Gameplay.Enemies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace IdleOnDemo.Gameplay.Player
{
    /// <summary>
    /// Resolves click-to-target enemy selection for manual combat.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerTargeting : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayerMask;
        [SerializeField] private float clickFallbackRadius = 0.05f;

        private readonly Collider2D[] clickHits = new Collider2D[8];
        private ContactFilter2D targetFilter;
        private EnemyController currentTarget;

        public EnemyController CurrentTarget
        {
            get
            {
                ClearInvalidTarget();
                return currentTarget;
            }
            private set => currentTarget = value;
        }

        public event Action<EnemyController> OnTargetChanged;

        private void Awake()
        {
            if (targetLayerMask.value == 0)
            {
                targetLayerMask = LayerMask.GetMask("Enemy");
            }

            ConfigureTargetFilter();
        }

        private void Update()
        {
            ClearInvalidTarget();

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            EnemyController clickedEnemy = FindEnemyUnderCursor();
            if (clickedEnemy != null)
            {
                SelectTarget(clickedEnemy);
                return;
            }

            ClearTarget();
        }

        public void SetTargetLayerMask(LayerMask layerMask)
        {
            targetLayerMask = layerMask;
            ConfigureTargetFilter();
        }

        public void SelectTarget(EnemyController target)
        {
            if (target == null || target.IsDead)
            {
                ClearTarget();
                return;
            }

            if (CurrentTarget == target)
            {
                return;
            }

            SetCurrentTarget(target);
        }

        public void ClearTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            SetCurrentTarget(null);
        }

        private void ConfigureTargetFilter()
        {
            targetFilter = new ContactFilter2D { useTriggers = false };
            targetFilter.SetLayerMask(targetLayerMask);
        }

        private EnemyController FindEnemyUnderCursor()
        {
            if (!TryGetCursorWorldPosition(out Vector2 worldPosition))
            {
                return null;
            }

            int hitCount = Physics2D.OverlapPoint(worldPosition, targetFilter, clickHits);
            if (hitCount == 0 && clickFallbackRadius > 0f)
            {
                hitCount = Physics2D.OverlapCircle(worldPosition, clickFallbackRadius, targetFilter, clickHits);
            }

            EnemyController closestEnemy = null;
            float closestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = clickHits[i];
                clickHits[i] = null;
                if (hit == null)
                {
                    continue;
                }

                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float distanceSqr = ((Vector2)hit.bounds.center - worldPosition).sqrMagnitude;
                if (distanceSqr >= closestDistanceSqr)
                {
                    continue;
                }

                closestDistanceSqr = distanceSqr;
                closestEnemy = enemy;
            }

            return closestEnemy;
        }

        private bool TryGetCursorWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;
            if (Mouse.current == null)
            {
                return false;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 screenPoint = new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                Mathf.Abs(mainCamera.transform.position.z - transform.position.z));
            worldPosition = mainCamera.ScreenToWorldPoint(screenPoint);
            return true;
        }

        private void ClearInvalidTarget()
        {
            if (currentTarget == null)
            {
                if (!ReferenceEquals(currentTarget, null))
                {
                    currentTarget = null;
                    OnTargetChanged?.Invoke(null);
                }

                return;
            }

            if (currentTarget.IsDead)
            {
                ClearTarget();
            }
        }

        private void SetCurrentTarget(EnemyController target)
        {
            if (currentTarget == target)
            {
                return;
            }

            if (currentTarget != null)
            {
                currentTarget.OnDeath -= HandleCurrentTargetDeath;
                currentTarget.SetSelected(false);
            }

            CurrentTarget = target;

            if (currentTarget != null)
            {
                currentTarget.SetSelected(true);
                currentTarget.OnDeath += HandleCurrentTargetDeath;
            }

            OnTargetChanged?.Invoke(currentTarget);
        }

        private void UnsubscribeFromCurrentTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.OnDeath -= HandleCurrentTargetDeath;
            }
        }

        private void HandleCurrentTargetDeath()
        {
            ClearTarget();
        }

        private void OnValidate()
        {
            clickFallbackRadius = Mathf.Max(0f, clickFallbackRadius);
        }
    }
}
