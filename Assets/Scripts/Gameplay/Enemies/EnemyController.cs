using System;
using IdleOnDemo.Core.Interfaces;
using IdleOnDemo.Gameplay.Environment;
using UnityEngine;
using IdleOnDemo.Gameplay.Progression;
using IdleOnDemo.Gameplay.Quests;

namespace IdleOnDemo.Gameplay.Enemies
{
    /// <summary>
    /// Controls enemy health, death cleanup, animation state, and spawner-zone roaming behavior.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        private const float PatrolWaitDuration = 5f;
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int DieHash = Animator.StringToHash("die");

        /// <summary>
        /// Represents the enemy finite state machine used for bounded roaming and death.
        /// </summary>
        private enum EnemyState
        {
            PatrolWait,
            PatrolMove,
            Dead
        }

        [Header("Stats")]
        [SerializeField] private int maxHealth = 100;

        [Header("Rewards")]
        [SerializeField] private int xpReward = 25;
        [SerializeField] private int coinReward = 5;

        [Header("Quest")]
        [SerializeField] private string deathObjectiveID;

        [Header("Roaming")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float targetReachedDistance = 0.2f;
        [SerializeField] private float deathAnimationDuration = 1f;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D rb;
        private EnemyState currentState = EnemyState.PatrolWait;
        private EnemySpawnerZone homeZone;
        private Vector2 roamTarget;
        private float waitTimer;
        private int currentHealth;

        /// <summary>
        /// Gets the enemy's current health value.
        /// </summary>
        /// <value>The remaining health after damage has been applied.</value>
        public int CurrentHealth => currentHealth;

        /// <summary>
        /// Gets the enemy's maximum health value.
        /// </summary>
        /// <value>The health value assigned when the enemy initializes.</value>
        public int MaxHealth => maxHealth;

        /// <summary>
        /// Gets whether the enemy has entered the dead state.
        /// </summary>
        /// <value><c>true</c> after lethal damage is received.</value>
        public bool IsDead => currentState == EnemyState.Dead;
        public EnemySpawnerZone HomeZone => homeZone;

        public bool IsSelected { get; private set; }

        /// <summary>
        /// Raised whenever the enemy health total changes.
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// Raised when the enemy transitions into the dead state.
        /// </summary>
        public event Action OnDeath;

        public event Action<bool> OnSelectedChanged;

        /// <summary>
        /// Caches physics, animation, and health state required for roaming and combat.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator ??= GetComponent<Animator>();
            spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Advances the roaming finite state machine using physics-timestep movement.
        /// </summary>
        private void FixedUpdate()
        {
            if (IsDead)
            {
                return;
            }

            switch (currentState)
            {
                case EnemyState.PatrolWait:
                    TickPatrolWait();
                    break;
                case EnemyState.PatrolMove:
                    TickPatrolMove();
                    break;
            }
        }

        /// <summary>
        /// Assigns the spawner zone that owns this enemy's roaming range.
        /// </summary>
        /// <param name="homeZone">The zone used to pick future roaming target points.</param>
        public void InitializeSpawnerBounds(EnemySpawnerZone homeZone)
        {
            this.homeZone = homeZone;
            EnterPatrolWait();
        }

        public void SetSelected(bool selected)
        {
            if (IsSelected == selected)
            {
                return;
            }

            IsSelected = selected;
            OnSelectedChanged?.Invoke(IsSelected);
        }

        /// <summary>
        /// Applies incoming damage, raises health events, and destroys the enemy after its death animation.
        /// </summary>
        /// <param name="amount">The damage amount to subtract from current health.</param>
        /// <param name="knockbackDirection">The direction to apply optional knockback.</param>
        /// <param name="knockbackForce">The optional impulse force applied when the enemy survives.</param>
        public void TakeDamage(int amount, Vector2 knockbackDirection, float knockbackForce)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(currentHealth - amount, 0);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            if (knockbackForce > 0f && knockbackDirection.sqrMagnitude > 0f)
            {
                rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            }
        }

        /// <summary>
        /// Waits in place before selecting the next random point from the home zone.
        /// </summary>
        private void TickPatrolWait()
        {
            SetHorizontalVelocity(0f);
            SetMoving(false);

            if (homeZone == null)
            {
                waitTimer = 0f;
                return;
            }

            waitTimer += Time.fixedDeltaTime;
            if (waitTimer < PatrolWaitDuration)
            {
                return;
            }

            roamTarget = homeZone.GetRandomPointInZone();
            currentState = EnemyState.PatrolMove;
            waitTimer = 0f;
        }

        /// <summary>
        /// Moves horizontally toward the current home-zone target point.
        /// </summary>
        private void TickPatrolMove()
        {
            if (homeZone == null)
            {
                EnterPatrolWait();
                return;
            }

            float distanceToTarget = roamTarget.x - transform.position.x;
            if (Mathf.Abs(distanceToTarget) <= targetReachedDistance)
            {
                EnterPatrolWait();
                return;
            }

            float direction = Mathf.Sign(distanceToTarget);
            SetHorizontalVelocity(direction * moveSpeed);
            SetFacingDirection(direction);
            SetMoving(true);
        }

        /// <summary>
        /// Returns the finite state machine to the waiting state and clears movement.
        /// </summary>
        private void EnterPatrolWait()
        {
            currentState = EnemyState.PatrolWait;
            waitTimer = 0f;
            SetHorizontalVelocity(0f);
            SetMoving(false);
        }

        /// <summary>
        /// Sets horizontal Rigidbody2D velocity while preserving vertical velocity.
        /// </summary>
        /// <param name="xVelocity">The horizontal velocity to apply.</param>
        private void SetHorizontalVelocity(float xVelocity)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = xVelocity;
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Flips the sprite to face the active patrol direction.
        /// </summary>
        /// <param name="direction">The horizontal direction being faced.</param>
        private void SetFacingDirection(float direction)
        {
            if (spriteRenderer != null && !Mathf.Approximately(direction, 0f))
            {
                spriteRenderer.flipX = direction < 0f;
            }
        }

        /// <summary>
        /// Updates the enemy Animator movement parameter.
        /// </summary>
        /// <param name="isMoving">Whether the patrol state is currently moving.</param>
        private void SetMoving(bool isMoving)
        {
            if (animator != null)
            {
                animator.SetBool(IsMovingHash, isMoving);
            }
        }

        /// <summary>
        /// Marks the enemy dead, notifies listeners, and removes the enemy object from the scene.
        /// </summary>
        private void Die()
        {
            currentState = EnemyState.Dead;
            SetMoving(false);
            FreezeDeathPhysics();

            if (animator != null)
            {
                animator.SetTrigger(DieHash);
            }

            AwardDeathRewards();
            RegisterDeathObjectiveProgress();
            OnDeath?.Invoke();
            StartCoroutine(DestroyAfterDeathAnimation());
        }

        /// <summary>
        /// Grants configured XP and coin rewards to the active player.
        /// </summary>
        private void AwardDeathRewards()
        {
            PlayerStats playerStats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddXP(xpReward);
                playerStats.AddCoins(coinReward);
            }
        }

        /// <summary>
        /// Registers objective progress for enemies configured with a death objective identifier.
        /// </summary>
        private void RegisterDeathObjectiveProgress()
        {
            if (!string.IsNullOrWhiteSpace(deathObjectiveID) && QuestManager.Instance != null)
            {
                QuestManager.Instance.RegisterObjectiveProgress(deathObjectiveID);
            }
        }

        /// <summary>
        /// Stops all Rigidbody2D simulation so the death animation plays in place instead of falling through platforms.
        /// </summary>
        private void FreezeDeathPhysics()
        {
            if (rb == null)
            {
                return;
            }

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.simulated = false;
        }

        /// <summary>
        /// Delays enemy cleanup long enough for the death animation to finish playing.
        /// </summary>
        /// <returns>An IEnumerator used by Unity's coroutine scheduler.</returns>
        private System.Collections.IEnumerator DestroyAfterDeathAnimation()
        {
            yield return new WaitForSeconds(deathAnimationDuration);
            Destroy(gameObject);
        }

        /// <summary>
        /// Keeps serialized roaming and combat tuning values in valid ranges.
        /// </summary>
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            xpReward = Mathf.Max(0, xpReward);
            coinReward = Mathf.Max(0, coinReward);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            targetReachedDistance = Mathf.Max(0.01f, targetReachedDistance);
            deathAnimationDuration = Mathf.Max(0f, deathAnimationDuration);
        }
    }
}
