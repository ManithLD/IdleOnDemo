using IdleOnDemo.Gameplay.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Displays player level, XP progress, and HP from a persistent UI prefab.
    /// </summary>
    public sealed class PlayerProgressionUI : MonoBehaviour
    {
        [SerializeField] private Slider xpSlider;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpProgressText;
        [SerializeField] private TMP_Text hpProgressText;

        private PlayerStats playerStats;

        private void Start()
        {
            playerStats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogWarning("PlayerProgressionUI could not find a PlayerStats instance. Progression UI will not update.");
                return;
            }

            playerStats.OnLevelUp += HandleLevelUp;
            playerStats.OnXPUpdated += HandleXPUpdated;
            playerStats.OnHPUpdated += HandleHPUpdated;

            HandleLevelUp(playerStats.CurrentLevel);
            HandleXPUpdated(playerStats.CurrentXP, playerStats.GetRequiredXP());
            HandleHPUpdated(playerStats.CurrentHP, playerStats.MaxHP);
        }

        private void OnDestroy()
        {
            if (playerStats == null)
            {
                return;
            }

            playerStats.OnLevelUp -= HandleLevelUp;
            playerStats.OnXPUpdated -= HandleXPUpdated;
            playerStats.OnHPUpdated -= HandleHPUpdated;
        }

        private void HandleLevelUp(int newLevel)
        {
            levelText.text = $"LV. {newLevel}";
        }

        private void HandleXPUpdated(int currentXP, int requiredXP)
        {
            xpSlider.maxValue = requiredXP;
            xpSlider.value = currentXP;
            xpProgressText.text = $"{currentXP}/{requiredXP}";
        }

        private void HandleHPUpdated(int currentHP, int maxHP)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
            hpProgressText.text = $"{currentHP}/{maxHP}";
        }
    }
}
