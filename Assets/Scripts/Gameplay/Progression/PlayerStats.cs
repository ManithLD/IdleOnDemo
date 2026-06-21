using System;
using IdleOnDemo.UI;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Progression
{
    /// <summary>
    /// Tracks player RPG progression, health, combat stats, and wallet totals.
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP = 100;
        [SerializeField] private int damage = 25;

        [Header("Currency")]
        [SerializeField] private int coins;

        public int CurrentLevel { get; private set; } = 0;
        public int CurrentXP { get; private set; } = 0;
        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public int Damage => damage;
        public int Coins => coins;

        public event Action<int> OnLevelUp;
        public event Action<int, int> OnXPUpdated;
        public event Action<int, int> OnHPUpdated;
        public event Action<int> OnCoinsUpdated;

        private void Awake()
        {
            NormalizeStats();
        }

        public int GetRequiredXP() => 10 + (CurrentLevel * 10);

        /// <summary>
        /// Adds experience and processes any level-ups earned by the new total.
        /// </summary>
        /// <param name="amount">The amount of experience to add.</param>
        public void AddXP(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentXP += amount;
            Debug.Log($"[PlayerStats] Gained {amount} XP. Total: {CurrentXP}/{GetRequiredXP()}");

            while (CurrentXP >= GetRequiredXP())
            {
                CurrentXP -= GetRequiredXP();
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
                Debug.Log($"[PlayerStats] Leveled up! Now Level {CurrentLevel}. XP towards next: {CurrentXP}/{GetRequiredXP()}");
            }

            OnXPUpdated?.Invoke(CurrentXP, GetRequiredXP());
        }

        /// <summary>
        /// Applies incoming damage and notifies health UI listeners.
        /// </summary>
        /// <param name="amount">The amount of health to subtract.</param>
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentHP -= amount;
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
            OnHPUpdated?.Invoke(currentHP, maxHP);
        }

        /// <summary>
        /// Adds coins to the player wallet.
        /// </summary>
        /// <param name="amount">The amount of coins to add.</param>
        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coins += amount;
            OnCoinsUpdated?.Invoke(coins);
            LootNotificationManager.Instance?.ShowCoins(amount);
        }

        private void NormalizeStats()
        {
            maxHP = Mathf.Max(1, maxHP);
            currentHP = currentHP <= 0 ? maxHP : Mathf.Clamp(currentHP, 0, maxHP);
            damage = Mathf.Max(1, damage);
            coins = Mathf.Max(0, coins);
        }

        private void OnValidate()
        {
            NormalizeStats();
        }
    }
}
