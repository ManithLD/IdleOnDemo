using TMPro;
using UnityEngine;
using IdleOnDemo.Gameplay.Progression;

public class CurrencyUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.OnCoinsUpdated += UpdateCoinUI;
            UpdateCoinUI(playerStats.Coins);
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnCoinsUpdated -= UpdateCoinUI;
        }
    }

    private void UpdateCoinUI(int amount)
    {
        coinText.text = amount.ToString();
    }
}