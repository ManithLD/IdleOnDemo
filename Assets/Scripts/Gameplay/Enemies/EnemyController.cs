using System;
using IdleOnDemo.Core.Interfaces;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Enemies
{
    /// <summary>
    /// Controls basic enemy health, physics movement, and state transitions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        private enum EnemyState
        {
            Idle,
            Chase,
            Attack,
            Dead
        }

        [Header("Stats")]
        [SerializeField] private int maxHealth = 10;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float aggroRange = 4f;
        [SerializeField] private float attackRange = 0.8f;

        [Header("References")]
        [SerializeField] private Transform target;

        private Rigidbody2D rb;
        private CapsuleCollider2D capsuleCollider;
        private SpriteRenderer spriteRenderer;
        private EnemyState currentState = EnemyState.Idle;
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
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            currentHealth = maxHealth;

            if (target == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsDead)
            {
                return;
            }

            UpdateState();
            ApplyStateMovement();
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

        private void UpdateState()
        {
            if (target == null)
            {
                currentState = EnemyState.Idle;
                return;
            }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget <= attackRange)
            {
                currentState = EnemyState.Attack;
            }
            else if (distanceToTarget <= aggroRange)
            {
                currentState = EnemyState.Chase;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
        }

        private void ApplyStateMovement()
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = currentState == EnemyState.Chase ? GetDirectionToTarget() * moveSpeed : 0f;
            rb.linearVelocity = velocity;
        }

        private float GetDirectionToTarget()
        {
            if (target == null)
            {
                return 0f;
            }

            float direction = Mathf.Sign(target.position.x - transform.position.x);
            if (spriteRenderer != null && !Mathf.Approximately(direction, 0f))
            {
                spriteRenderer.flipX = direction < 0f;
            }

            return direction;
        }

        private void Die()
        {
            currentState = EnemyState.Dead;
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
            aggroRange = Mathf.Max(0f, aggroRange);
            attackRange = Mathf.Clamp(attackRange, 0f, aggroRange);
        }
    }
}
