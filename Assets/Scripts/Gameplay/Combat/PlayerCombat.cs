using System.Collections;
using IdleOnDemo.Core.Interfaces;
using IdleOnDemo.Gameplay.Enemies;
using IdleOnDemo.Gameplay.Progression;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Player
{
    /// <summary>
    /// Coordinates player attacks, using click-selected targets or optional nearest-enemy auto targeting.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerTargeting))]
    public class PlayerCombat : MonoBehaviour
    {
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");

        /// <summary>
        /// Controls whether the player automatically approaches and attacks the nearest enemy.
        /// </summary>
        /// <value><c>true</c> to use auto attack; <c>false</c> to use click-selected targets.</value>
        public bool autoAttackEnabled = false;

        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private float autoTargetYTolerance = 1.5f;
        [SerializeField] private float directionFlipDeadZone = 0.1f;
        [SerializeField] private int attackDamage = 25;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private LayerMask attackLayerMask;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerTargeting playerTargeting;
        [SerializeField] private Animator animator;

        private Coroutine activeAttackRoutine;
        private float nextAttackTime;
        private EnemyController currentAutoTarget;

        public bool IsAutoAttackEnabled => autoAttackEnabled;
        public AttackType CurrentAttackType { get; private set; } = AttackType.Default;

        public void ToggleAutoAttack()
        {
            autoAttackEnabled = !autoAttackEnabled;

            if (!autoAttackEnabled)
            {
                ClearAutoTarget();
            }
        }

        /// <summary>
        /// Sets the attack animation variant used by the next attack trigger.
        /// </summary>
        /// <param name="type">The attack animation variant to play on future attacks.</param>
        public void SetAttackType(AttackType type)
        {
            CurrentAttackType = type;
        }

        /// <summary>
        /// Caches required player references and initializes the default attack target layer.
        /// </summary>
        private void Awake()
        {
            playerController ??= GetComponent<PlayerController>();
            playerStats ??= GetComponent<PlayerStats>();
            playerTargeting ??= GetComponent<PlayerTargeting>();
            animator ??= GetComponentInChildren<Animator>();

            if (playerTargeting == null)
            {
                playerTargeting = gameObject.AddComponent<PlayerTargeting>();
            }

            if (attackLayerMask.value == 0)
            {
                attackLayerMask = LayerMask.GetMask("Enemy");
            }

            playerTargeting.SetTargetLayerMask(attackLayerMask);
        }

        /// <summary>
        /// Releases simulated movement and attack state if combat is disabled mid-sequence.
        /// </summary>
        private void OnDisable()
        {
            if (activeAttackRoutine != null)
            {
                StopCoroutine(activeAttackRoutine);
                activeAttackRoutine = null;
            }

            if (playerController != null)
            {
                playerController.SetSimulatedInput(0f);
                playerController.IsAttacking = false;
            }

            ClearAutoTarget();
        }

        /// <summary>
        /// Chooses a target source, then routes both manual and auto attack through one pursue-and-attack path.
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

            EnemyController target;
            if (autoAttackEnabled)
            {
                if (!IsValidAutoTarget(currentAutoTarget))
                {
                    currentAutoTarget = FindNearestLivingEnemy();
                }

                target = currentAutoTarget;
            }
            else
            {
                ClearAutoTarget();
                target = playerTargeting != null ? playerTargeting.CurrentTarget : null;
            }

            if (target == null || target.IsDead)
            {
                playerController.SetSimulatedInput(0f);
                if (autoAttackEnabled)
                {
                    ClearAutoTarget();
                }

                return;
            }

            PursueAndAttack(target);
        }

        /// <summary>
        /// Moves toward the target until in range, then starts attacks on cooldown.
        /// </summary>
        /// <param name="target">The live enemy to pursue and attack.</param>
        private void PursueAndAttack(EnemyController target)
        {
            float direction = GetDirectionToTarget(target);
            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance > attackRange)
            {
                playerController.SetSimulatedInput(direction);
                return;
            }

            playerController.SetSimulatedInput(0f);
            playerController.FaceDirection(direction);
            TryStartAttack(target, direction);
        }

        /// <summary>
        /// Starts the attack coroutine if cooldown, grounded state, and current attack state allow it.
        /// </summary>
        /// <param name="target">The enemy locked for this swing.</param>
        /// <param name="direction">The horizontal attack direction, where negative is left and positive is right.</param>
        private void TryStartAttack(EnemyController target, float direction)
        {
            if (target == null || target.IsDead || activeAttackRoutine != null || Time.time < nextAttackTime)
            {
                return;
            }

            if (!playerController.IsGrounded || playerController.IsAttacking)
            {
                return;
            }

            direction = Mathf.Approximately(direction, 0f) ? playerController.FacingDirection : Mathf.Sign(direction);
            nextAttackTime = Time.time + attackCooldown;
            activeAttackRoutine = StartCoroutine(AttackRoutine(target, direction));
        }

        /// <summary>
        /// Locks movement, triggers the attack animation, damages the locked target, and releases the player after cooldown.
        /// </summary>
        /// <param name="target">The enemy locked when the swing starts.</param>
        /// <param name="direction">The horizontal direction used for facing and knockback.</param>
        /// <returns>An IEnumerator used by Unity's coroutine scheduler.</returns>
        private IEnumerator AttackRoutine(EnemyController target, float direction)
        {
            playerController.SetSimulatedInput(0f);
            playerController.FaceDirection(direction);
            playerController.IsAttacking = true;

            if (animator != null)
            {
                animator.ResetTrigger(AttackHash);
                animator.SetFloat(AttackTypeHash, (float)CurrentAttackType);
                animator.SetTrigger(AttackHash);
            }

            TryDamageTarget(target, direction);

            yield return new WaitForSeconds(attackCooldown);

            playerController.IsAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Finds the nearest non-dead enemy on the player's current floor/platform.
        /// </summary>
        /// <returns>The closest living <see cref="EnemyController"/>, or <c>null</c> if none exist.</returns>
        private EnemyController FindNearestLivingEnemy()
        {
            if (IsValidAutoTarget(currentAutoTarget))
            {
                return currentAutoTarget;
            }

            EnemyController nearestEnemy = null;
            float nearestDistanceSqr = float.MaxValue;
            float playerY = transform.position.y;
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);

            foreach (EnemyController enemy in enemies)
            {
                if (!IsValidAutoTarget(enemy))
                {
                    continue;
                }

                if (Mathf.Abs(enemy.transform.position.y - playerY) > autoTargetYTolerance)
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

        private bool IsValidAutoTarget(EnemyController target)
        {
            return target != null && !target.IsDead;
        }

        private void ClearAutoTarget()
        {
            currentAutoTarget = null;
        }

        /// <summary>
        /// Applies direct damage to the already-selected target.
        /// </summary>
        /// <param name="target">The target locked for the current swing.</param>
        /// <param name="direction">The horizontal direction used for knockback.</param>
        private void TryDamageTarget(EnemyController target, float direction)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            IDamageable damageable = target;
            int damageAmount = playerStats != null ? playerStats.Damage : attackDamage;
            damageable.TakeDamage(damageAmount, Vector2.right * direction, 0f);
        }

        private float GetDirectionToTarget(EnemyController target)
        {
            float xDelta = target.transform.position.x - transform.position.x;
            if (Mathf.Abs(xDelta) <= directionFlipDeadZone)
            {
                return playerController.FacingDirection;
            }

            return Mathf.Sign(xDelta);
        }

        /// <summary>
        /// Keeps serialized combat tuning values in valid ranges when edited in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            attackRange = Mathf.Max(0.1f, attackRange);
            attackCooldown = Mathf.Max(0.01f, attackCooldown);
            autoTargetYTolerance = Mathf.Max(0f, autoTargetYTolerance);
            directionFlipDeadZone = Mathf.Max(0f, directionFlipDeadZone);
            attackDamage = Mathf.Max(1, attackDamage);
        }

        /// <summary>
        /// Draws the configured attack range for scene tuning.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            PlayerTargeting targeting = playerTargeting != null ? playerTargeting : GetComponent<PlayerTargeting>();
            EnemyController target = targeting != null ? targeting.CurrentTarget : null;
            if (target == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
}
