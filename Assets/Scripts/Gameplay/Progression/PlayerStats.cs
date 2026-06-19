using System;
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
        }

        private void NormalizeStats()
        {
            maxHP = Mathf.Max(1, maxHP);
            currentHP = currentHP <= 0 ? maxHP : Mathf.Clamp(currentHP, 1, maxHP);
            damage = Mathf.Max(1, damage);
            coins = Mathf.Max(0, coins);
        }

        private void OnValidate()
        {
            NormalizeStats();
        }
    }
}
