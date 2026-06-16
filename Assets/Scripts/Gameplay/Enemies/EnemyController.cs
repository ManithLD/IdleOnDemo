using System;
using IdleOnDemo.Core.Interfaces;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IdleOnDemo.Gameplay.Enemies
{
    /// <summary>
    /// Controls enemy health and platform-local patrol behavior.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        private const float PatrolWaitDuration = 5f;
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

        private enum EnemyState
        {
            PatrolWait,
            PatrolMove,
            Dead
        }

        [Header("Stats")]
        [SerializeField] private int maxHealth = 10;

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

        public int CurrentHealth => currentHealth;

        public int MaxHealth => maxHealth;

        public bool IsDead => currentState == EnemyState.Dead;

        public event Action<int, int> OnHealthChanged;

        public event Action OnDeath;

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

        private bool HasUsablePlatformBounds()
        {
            return platformMaxX > platformMinX;
        }

        private bool HasGroundAhead(float direction)
        {
            Bounds bounds = capsuleCollider.bounds;
            Vector2 origin = new Vector2(
                direction > 0f ? bounds.max.x + edgeCheckForwardDistance : bounds.min.x - edgeCheckForwardDistance,
                bounds.min.y + 0.05f);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDownDistance, groundLayer);
            return hit.collider != null && hit.normal.y >= minGroundNormalY;
        }

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

        private void EnterPatrolWait()
        {
            currentState = EnemyState.PatrolWait;
            waitTimer = 0f;
            SetHorizontalVelocity(0f);
            SetMoving(false);
        }

        private void SetHorizontalVelocity(float xVelocity)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = xVelocity;
            rb.linearVelocity = velocity;
        }

        private void SetFacingDirection(float direction)
        {
            if (spriteRenderer != null && !Mathf.Approximately(direction, 0f))
            {
                spriteRenderer.flipX = direction < 0f;
            }
        }

        private void SetMoving(bool isMoving)
        {
            if (animator != null)
            {
                animator.SetBool(IsMovingHash, isMoving);
            }
        }

        private void Die()
        {
            currentState = EnemyState.Dead;
            SetMoving(false);
            OnDeath?.Invoke();

            if (capsuleCollider != null)
            {
                capsuleCollider.enabled = false;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
        }

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
