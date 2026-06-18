using System;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Progression
{
    /// <summary>
    /// Tracks player RPG progression, health, combat stats, and wallet totals.
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("Progression")]
        [SerializeField] private int level = 1;
        [SerializeField] private int currentXP;
        [SerializeField] private int xpToNextLevel;
        [SerializeField] private int baseXPToNextLevel = 100;

        [Header("Combat")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int currentHP = 100;
        [SerializeField] private int damage = 25;

        [Header("Currency")]
        [SerializeField] private int coins;

        /// <summary>
        /// Raised when experience changes. Args are current XP and XP required for the active level.
        /// </summary>
        public event Action<int, int> OnExperienceChanged;

        /// <summary>
        /// Raised after a level is gained. Arg is the new level.
        /// </summary>
        public event Action<int> OnLevelUp;

        /// <summary>
        /// Raised when the coin total changes. Arg is the new coin total.
        /// </summary>
        public event Action<int> OnCoinsChanged;

        public int Level => level;
        public int CurrentXP => currentXP;
        public int XPToNextLevel => xpToNextLevel;
        public int MaxHP => maxHP;
        public int CurrentHP => currentHP;
        public int Damage => damage;
        public int Coins => coins;

        private void Awake()
        {
            NormalizeStats();
        }

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

            currentXP += amount;
            while (currentXP >= xpToNextLevel)
            {
                currentXP -= xpToNextLevel;
                LevelUp();
            }

            OnExperienceChanged?.Invoke(currentXP, xpToNextLevel);
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
            OnCoinsChanged?.Invoke(coins);
        }

        private void LevelUp()
        {
            level++;
            xpToNextLevel = CalculateXPToNextLevel(level);
            currentHP = maxHP;
            OnLevelUp?.Invoke(level);
        }

        private int CalculateXPToNextLevel(int targetLevel)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseXPToNextLevel * Mathf.Pow(Mathf.Max(1, targetLevel), 1.5f)));
        }

        private void NormalizeStats()
        {
            level = Mathf.Max(1, level);
            baseXPToNextLevel = Mathf.Max(1, baseXPToNextLevel);
            xpToNextLevel = CalculateXPToNextLevel(level);
            currentXP = Mathf.Clamp(currentXP, 0, xpToNextLevel - 1);
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
