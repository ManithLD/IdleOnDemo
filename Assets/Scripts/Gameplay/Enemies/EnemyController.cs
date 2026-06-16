using System;
using IdleOnDemo.Core.Interfaces;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IdleOnDemo.Gameplay.Enemies
{
    /// <summary>
    /// Controls enemy health, death cleanup, animation state, and platform-local patrol behavior.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        private const float PatrolWaitDuration = 5f;
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

        /// <summary>
        /// Represents the enemy finite state machine used for platform patrol and death.
        /// </summary>
        private enum EnemyState
        {
            PatrolWait,
            PatrolMove,
            Dead
        }

        [Header("Stats")]
        [SerializeField] private int maxHealth = 100;

        [Header("Patrol")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float targetReachedDistance = 0.08f;
        [SerializeField] private float groundCheckDistance = 0.12f;
        [SerializeField] private float edgeCheckForwardDistance = 0.12f;
        [SerializeField] private float edgeCheckDownDistance = 0.35f;
        [SerializeField] private float wallCheckDistance = 0.08f;
        [SerializeField] private float platformEdgePadding = 0.05f;
        [SerializeField] private float minGroundNormalY = 0.65f;
        [SerializeField] private LayerMask groundLayer;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];
        private ContactFilter2D groundFilter;
        private Rigidbody2D rb;
        private CapsuleCollider2D capsuleCollider;
        private EnemyState currentState = EnemyState.PatrolWait;
        private Collider2D currentPlatformCollider;
        private float platformMinX;
        private float platformMaxX;
        private float patrolTargetX;
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

        /// <summary>
        /// Raised whenever the enemy health total changes.
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// Raised when the enemy transitions into the dead state.
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// Caches physics, animation, and health state required for patrol and combat.
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            capsuleCollider = GetComponent<CapsuleCollider2D>();
            animator ??= GetComponent<Animator>();
            spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
            currentHealth = maxHealth;

            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(groundLayer);
        }

        /// <summary>
        /// Advances the patrol finite state machine using physics-timestep movement.
        /// </summary>
        private void FixedUpdate()
        {
            if (IsDead)
            {
                return;
            }

            bool isGrounded = TryUpdateCurrentPlatform();
            switch (currentState)
            {
                case EnemyState.PatrolWait:
                    TickPatrolWait(isGrounded);
                    break;
                case EnemyState.PatrolMove:
                    TickPatrolMove(isGrounded);
                    break;
            }
        }

        /// <summary>
        /// Applies incoming damage, raises health events, and destroys the enemy on lethal damage.
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
        /// Waits on the current platform, then picks a random platform-local destination.
        /// </summary>
        /// <param name="isGrounded">Whether the enemy is currently standing on valid ground.</param>
        private void TickPatrolWait(bool isGrounded)
        {
            SetHorizontalVelocity(0f);
            SetMoving(false);

            if (!isGrounded || !HasUsablePlatformBounds())
            {
                waitTimer = 0f;
                return;
            }

            waitTimer += Time.fixedDeltaTime;
            if (waitTimer < PatrolWaitDuration)
            {
                return;
            }

            patrolTargetX = UnityEngine.Random.Range(platformMinX, platformMaxX);
            if (Mathf.Abs(patrolTargetX - transform.position.x) <= targetReachedDistance)
            {
                waitTimer = 0f;
                return;
            }

            currentState = EnemyState.PatrolMove;
            waitTimer = 0f;
        }

        /// <summary>
        /// Moves toward the selected platform-local destination while checking edges and walls.
        /// </summary>
        /// <param name="isGrounded">Whether the enemy is currently standing on valid ground.</param>
        private void TickPatrolMove(bool isGrounded)
        {
            if (!isGrounded || !HasUsablePlatformBounds())
            {
                EnterPatrolWait();
                return;
            }

            float distanceToTarget = patrolTargetX - transform.position.x;
            if (Mathf.Abs(distanceToTarget) <= targetReachedDistance)
            {
                EnterPatrolWait();
                return;
            }

            float direction = Mathf.Sign(distanceToTarget);
            if (!HasGroundAhead(direction) || IsWallAhead(direction))
            {
                EnterPatrolWait();
                return;
            }

            SetHorizontalVelocity(direction * moveSpeed);
            SetFacingDirection(direction);
            SetMoving(true);
        }

        /// <summary>
        /// Detects the current platform and caches its walkable X bounds.
        /// </summary>
        /// <returns><c>true</c> when the enemy is grounded on a valid platform collider.</returns>
        private bool TryUpdateCurrentPlatform()
        {
            if (!TryGetGroundHit(out RaycastHit2D groundHit))
            {
                return false;
            }

            if (groundHit.collider != currentPlatformCollider)
            {
                currentPlatformCollider = groundHit.collider;
                CachePlatformBounds(groundHit);
            }
            else
            {
                CachePlatformBounds(groundHit);
            }

            return true;
        }

        /// <summary>
        /// Casts downward from the capsule to find a ground contact with a usable normal.
        /// </summary>
        /// <param name="groundHit">The best ground hit when one is found.</param>
        /// <returns><c>true</c> when ground is detected beneath the enemy.</returns>
        private bool TryGetGroundHit(out RaycastHit2D groundHit)
        {
            groundHit = default;
            int hitCount = capsuleCollider.Cast(Vector2.down, groundFilter, hitBuffer, groundCheckDistance);
            for (int i = 0; i < hitCount; i++)
            {
                if (hitBuffer[i].normal.y >= minGroundNormalY)
                {
                    groundHit = hitBuffer[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Caches platform bounds from either the collider bounds or a contiguous Tilemap tile run.
        /// </summary>
        /// <param name="groundHit">The ground hit used to identify the current platform.</param>
        private void CachePlatformBounds(RaycastHit2D groundHit)
        {
            Bounds platformBounds = groundHit.collider.bounds;
            Tilemap tilemap = groundHit.collider.GetComponent<Tilemap>();
            if (tilemap != null && TryGetTileRunBounds(tilemap, groundHit.point, out Bounds tileRunBounds))
            {
                platformBounds = tileRunBounds;
            }

            float halfWidth = capsuleCollider.bounds.extents.x + platformEdgePadding;
            platformMinX = platformBounds.min.x + halfWidth;
            platformMaxX = platformBounds.max.x - halfWidth;
        }

        /// <summary>
        /// Calculates the contiguous horizontal tile run beneath the enemy on a Tilemap.
        /// </summary>
        /// <param name="tilemap">The tilemap containing the platform tiles.</param>
        /// <param name="hitPoint">The world-space contact point on the tilemap.</param>
        /// <param name="bounds">The calculated world-space bounds of the contiguous tile run.</param>
        /// <returns><c>true</c> when a tile run could be resolved from the hit point.</returns>
        private bool TryGetTileRunBounds(Tilemap tilemap, Vector2 hitPoint, out Bounds bounds)
        {
            Vector3Int cell = tilemap.WorldToCell(hitPoint + Vector2.down * 0.05f);
            if (!tilemap.HasTile(cell))
            {
                Vector3Int below = new Vector3Int(cell.x, cell.y - 1, cell.z);
                Vector3Int above = new Vector3Int(cell.x, cell.y + 1, cell.z);
                if (tilemap.HasTile(below))
                {
                    cell = below;
                }
                else if (tilemap.HasTile(above))
                {
                    cell = above;
                }
            }

            if (!tilemap.HasTile(cell))
            {
                bounds = default;
                return false;
            }

            int left = cell.x;
            int right = cell.x;
            while (tilemap.HasTile(new Vector3Int(left - 1, cell.y, cell.z)))
            {
                left--;
            }

            while (tilemap.HasTile(new Vector3Int(right + 1, cell.y, cell.z)))
            {
                right++;
            }

            Vector3 min = tilemap.CellToWorld(new Vector3Int(left, cell.y, cell.z));
            Vector3 max = tilemap.CellToWorld(new Vector3Int(right + 1, cell.y + 1, cell.z));
            bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return true;
        }

        /// <summary>
        /// Checks whether the cached platform bounds can support patrol movement.
        /// </summary>
        /// <returns><c>true</c> when the platform has a positive walkable width.</returns>
        private bool HasUsablePlatformBounds()
        {
            return platformMaxX > platformMinX;
        }

        /// <summary>
        /// Checks for ground just ahead of the enemy to avoid walking off platform edges.
        /// </summary>
        /// <param name="direction">The horizontal movement direction being tested.</param>
        /// <returns><c>true</c> when ground is detected in front of the enemy.</returns>
        private bool HasGroundAhead(float direction)
        {
            Bounds bounds = capsuleCollider.bounds;
            Vector2 origin = new Vector2(
                direction > 0f ? bounds.max.x + edgeCheckForwardDistance : bounds.min.x - edgeCheckForwardDistance,
                bounds.min.y + 0.05f);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDownDistance, groundLayer);
            return hit.collider != null && hit.normal.y >= minGroundNormalY;
        }

        /// <summary>
        /// Checks whether a wall blocks movement in the desired patrol direction.
        /// </summary>
        /// <param name="direction">The horizontal movement direction being tested.</param>
        /// <returns><c>true</c> when a blocking wall is directly ahead.</returns>
        private bool IsWallAhead(float direction)
        {
            int hitCount = capsuleCollider.Cast(Vector2.right * direction, groundFilter, hitBuffer, wallCheckDistance);
            for (int i = 0; i < hitCount; i++)
            {
                if (Vector2.Dot(hitBuffer[i].normal, Vector2.left * direction) > 0.5f)
                {
                    return true;
                }
            }

            return false;
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
            OnDeath?.Invoke();

            Destroy(gameObject);
        }

        /// <summary>
        /// Keeps serialized patrol and combat tuning values in valid ranges.
        /// </summary>
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            targetReachedDistance = Mathf.Max(0.01f, targetReachedDistance);
            groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
            edgeCheckForwardDistance = Mathf.Max(0f, edgeCheckForwardDistance);
            edgeCheckDownDistance = Mathf.Max(0.01f, edgeCheckDownDistance);
            wallCheckDistance = Mathf.Max(0.01f, wallCheckDistance);
            platformEdgePadding = Mathf.Max(0f, platformEdgePadding);
            minGroundNormalY = Mathf.Clamp01(minGroundNormalY);
        }
    }
}
