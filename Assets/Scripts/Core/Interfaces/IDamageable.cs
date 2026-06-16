using UnityEngine;

namespace IdleOnDemo.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for objects that can receive damage, report health state,
    /// and notify subscribers when health or death state changes.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Gets the object's current health value.
        /// </summary>
        int CurrentHealth { get; }

        /// <summary>
        /// Gets the object's maximum health value.
        /// </summary>
        int MaxHealth { get; }

        /// <summary>
        /// Gets a value indicating whether the object has reached its death state.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// Raised when health changes.
        /// The first argument is the current health, and the second argument is the maximum health.
        /// </summary>
        event System.Action<int, int> OnHealthChanged;

        /// <summary>
        /// Raised when the object enters its death state.
        /// </summary>
        event System.Action OnDeath;

        /// <summary>
        /// Applies damage and optional knockback to the object.
        /// </summary>
        /// <param name="amount">The amount of damage to apply.</param>
        /// <param name="knockbackDirection">The normalized direction of knockback force.</param>
        /// <param name="knockbackForce">The strength of the knockback force.</param>
        void TakeDamage(int amount, Vector2 knockbackDirection, float knockbackForce);
    }
}
