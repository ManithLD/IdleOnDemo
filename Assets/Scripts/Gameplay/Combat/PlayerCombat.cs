using System.Collections;
using System.Collections.Generic;
using IdleOnDemo.Core.Interfaces;
using IdleOnDemo.Gameplay.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;
using IdleOnDemo.Gameplay.Progression;

namespace IdleOnDemo.Gameplay.Player
{
    /// <summary>
    /// Coordinates player attacks, including manual mouse-directed attacks and optional auto-attack steering.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        /// <summary>
        /// Controls whether the player automatically approaches and attacks the nearest enemy.
        /// </summary>
        /// <value><c>true</c> to use auto attack; <c>false</c> to require manual left-click attacks.</value>
        public bool autoAttackEnabled = false;

        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private float closeTargetGraceDistance = 0.25f;
        [SerializeField] private int attackDamage = 25;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private LayerMask attackLayerMask;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Animator animator;

        private readonly List<Collider2D> attackHits = new();
        private Coroutine activeAttackRoutine;
        private float nextAttackTime;

        /// <summary>
        /// Caches required player references and initializes the default attack target layer.
        /// </summary>
        private void Awake()
        {
            playerController ??= GetComponent<PlayerController>();
            playerStats ??= GetComponent<PlayerStats>();
            animator ??= GetComponentInChildren<Animator>();

            if (attackLayerMask.value == 0)
            {
                attackLayerMask = LayerMask.GetMask("Enemy");
            }
        }

        /// <summary>
        /// Releases simulated movement and attack state if combat is disabled mid-sequence.
        /// </summary>
        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.SetSimulatedInput(0f);
                playerController.IsAttacking = false;
            }

            activeAttackRoutine = null;
        }

        /// <summary>
        /// Routes combat behavior between auto attack and manual attack while respecting grounded and attack-lock constraints.
        /// </summary>
        private void Update()
        {
            if (playerController == null)
            {
                return;
            }

            if (!playerController.IsGrounded || playerController.IsAttacking)
            {
                playerController.SetSimulatedInput(0f);
                return;
            }

            if (autoAttackEnabled)
            {
                UpdateAutoAttack();
                return;
            }

            UpdateManualAttack();
        }

        /// <summary>
        /// Finds the nearest live enemy, moves toward it, and starts an attack when in range.
        /// </summary>
        private void UpdateAutoAttack()
        {
            EnemyController target = FindNearestLivingEnemy();
            if (target == null)
            {
                playerController.SetSimulatedInput(0f);
                return;
            }

            float distance = Vector2.Distance(transform.position, target.transform.position);
            float direction = Mathf.Sign(target.transform.position.x - transform.position.x);
            if (Mathf.Approximately(direction, 0f))
            {
                direction = playerController.FacingDirection;
            }

            if (distance > attackRange)
            {
                playerController.SetSimulatedInput(direction);
                return;
            }

            playerController.SetSimulatedInput(0f);
            playerController.FaceDirection(direction);
            TryStartAttack(direction);
        }

        /// <summary>
        /// Handles left-click manual attacks and snaps the player to face the mouse before attacking.
        /// </summary>
        private void UpdateManualAttack()
        {
            playerController.SetSimulatedInput(0f);

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            float attackDirection = GetManualAttackDirection();
            playerController.FaceDirection(attackDirection);
            TryStartAttack(attackDirection);
        }

        /// <summary>
        /// Starts the attack coroutine if cooldown, grounded state, and current attack state allow it.
        /// </summary>
        /// <param name="direction">The horizontal attack direction, where negative is left and positive is right.</param>
        private void TryStartAttack(float direction)
        {
            if (activeAttackRoutine != null || Time.time < nextAttackTime)
            {
                return;
            }

            if (!playerController.IsGrounded || playerController.IsAttacking)
            {
                return;
            }

            direction = Mathf.Approximately(direction, 0f) ? playerController.FacingDirection : Mathf.Sign(direction);
            nextAttackTime = Time.time + attackCooldown;
            activeAttackRoutine = StartCoroutine(AttackRoutine(direction));
        }

        /// <summary>
        /// Locks movement, triggers the attack animation, applies damage, and releases the player after cooldown.
        /// </summary>
        /// <param name="direction">The horizontal direction used for facing and hit filtering.</param>
        /// <returns>An IEnumerator used by Unity's coroutine scheduler.</returns>
        private IEnumerator AttackRoutine(float direction)
        {
            playerController.SetSimulatedInput(0f);
            playerController.FaceDirection(direction);
            playerController.IsAttacking = true;

            if (animator != null)
            {
                animator.ResetTrigger(AttackHash);
                animator.SetTrigger(AttackHash);
            }

            TryDamageTarget(direction);

            yield return new WaitForSeconds(attackCooldown);

            playerController.IsAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Finds the nearest non-dead enemy in the active scene.
        /// </summary>
        /// <returns>The closest living <see cref="EnemyController"/>, or <c>null</c> if none exist.</returns>
        private EnemyController FindNearestLivingEnemy()
        {
            EnemyController nearestEnemy = null;
            float nearestDistanceSqr = float.MaxValue;
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float distanceSqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy;
            }

            return nearestEnemy;
        }

        /// <summary>
        /// Resolves manual attack direction from the mouse position in world space.
        /// </summary>
        /// <returns><c>1</c> when the mouse is right of the player, <c>-1</c> when left, or the current facing if no camera exists.</returns>
        private float GetManualAttackDirection()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || Mouse.current == null)
            {
                return playerController.FacingDirection;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 screenPoint = new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                Mathf.Abs(mainCamera.transform.position.z - transform.position.z));
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(screenPoint);

            if (Mathf.Approximately(mouseWorldPosition.x, transform.position.x))
            {
                return playerController.FacingDirection;
            }

            return mouseWorldPosition.x > transform.position.x ? 1f : -1f;
        }

        /// <summary>
        /// Finds and damages the nearest damageable target in front of the player.
        /// </summary>
        /// <param name="direction">The horizontal facing direction used to filter targets.</param>
        private void TryDamageTarget(float direction)
        {
            Vector2 origin = transform.position;
            ContactFilter2D attackFilter = new ContactFilter2D { useTriggers = false };
            attackFilter.SetLayerMask(attackLayerMask);
            attackHits.Clear();
            int hitCount = Physics2D.OverlapCircle(origin, attackRange, attackFilter, attackHits);

            IDamageable target = null;
            float nearestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = attackHits[i];
                if (hit == null)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead)
                {
                    continue;
                }

                if (!IsValidAttackHit(hit, origin, direction, out float distanceSqr))
                {
                    continue;
                }

                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr = distanceSqr;
                target = damageable;
            }

            int damageAmount = playerStats != null ? playerStats.Damage : attackDamage;
            target?.TakeDamage(damageAmount, Vector2.right * direction, 0f);
        }

        /// <summary>
        /// Checks whether a target collider is inside the active attack direction, including very close overlap cases.
        /// </summary>
        /// <param name="hit">The candidate target collider found by the attack range query.</param>
        /// <param name="origin">The player world position used as the attack origin.</param>
        /// <param name="direction">The horizontal attack direction, where negative is left and positive is right.</param>
        /// <param name="distanceSqr">The squared distance used to choose the nearest valid target.</param>
        /// <returns><c>true</c> when the collider should be considered a valid attack target.</returns>
        private bool IsValidAttackHit(Collider2D hit, Vector2 origin, float direction, out float distanceSqr)
        {
            Vector2 closestPoint = hit.bounds.ClosestPoint(origin);
            Vector2 toClosestPoint = closestPoint - origin;
            bool isCloseOrOverlapping = hit.bounds.Contains(origin) ||
                toClosestPoint.sqrMagnitude <= closeTargetGraceDistance * closeTargetGraceDistance;

            if (!isCloseOrOverlapping && toClosestPoint.x * direction <= 0.01f)
            {
                distanceSqr = float.MaxValue;
                return false;
            }

            Vector2 targetCenterOffset = (Vector2)hit.bounds.center - origin;
            distanceSqr = isCloseOrOverlapping ? targetCenterOffset.sqrMagnitude : toClosestPoint.sqrMagnitude;
            return true;
        }

        /// <summary>
        /// Keeps serialized combat tuning values in valid ranges when edited in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            attackRange = Mathf.Max(0.1f, attackRange);
            attackCooldown = Mathf.Max(0.01f, attackCooldown);
            closeTargetGraceDistance = Mathf.Max(0f, closeTargetGraceDistance);
            attackDamage = Mathf.Max(1, attackDamage);
        }

        /// <summary>
        /// Draws the configured attack range for scene tuning.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
