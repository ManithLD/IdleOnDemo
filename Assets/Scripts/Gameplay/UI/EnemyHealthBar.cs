using IdleOnDemo.Core.Interfaces;
using UnityEngine;

/// <summary>
/// Displays a segmented overhead health bar for an enemy by swapping pre-authored HUD sprites.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer healthBarRenderer;
    [SerializeField] private Sprite fullHealthSprite;
    [SerializeField] private Sprite seventyFivePercentSprite;
    [SerializeField] private Sprite fiftyPercentSprite;
    [SerializeField] private Sprite twentyFivePercentSprite;
    [SerializeField] private Sprite zeroPercentSprite;

    private IDamageable damageable;

    /// <summary>
    /// Caches the parent enemy damage contract, initializes the full-health visual, and subscribes to health updates.
    /// </summary>
    private void Start()
    {
        healthBarRenderer ??= GetComponent<SpriteRenderer>();
        damageable = GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(damageable.CurrentHealth, damageable.MaxHealth);
            return;
        }

        SetSprite(fullHealthSprite);
    }

    /// <summary>
    /// Unsubscribes from the parent enemy event to avoid retaining destroyed health bar instances.
    /// </summary>
    private void OnDestroy()
    {
        if (damageable != null)
        {
            damageable.OnHealthChanged -= HandleHealthChanged;
        }
    }

    /// <summary>
    /// Selects the matching segmented sprite for the enemy's current health.
    /// </summary>
    /// <param name="currentHealth">The enemy health value after the latest damage event.</param>
    /// <param name="maxHealth">The enemy maximum health value used to calculate the remaining ratio.</param>
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0)
        {
            SetSprite(zeroPercentSprite);
            return;
        }

        float healthRatio = Mathf.Clamp01((float)currentHealth / maxHealth);
        if (currentHealth >= maxHealth)
        {
            SetSprite(fullHealthSprite);
        }
        else if (healthRatio >= 0.75f)
        {
            SetSprite(seventyFivePercentSprite);
        }
        else if (healthRatio >= 0.5f)
        {
            SetSprite(fiftyPercentSprite);
        }
        else if (healthRatio > 0f)
        {
            SetSprite(twentyFivePercentSprite);
        }
        else
        {
            SetSprite(zeroPercentSprite);
        }
    }

    /// <summary>
    /// Assigns a sprite to the configured health bar renderer when both references are valid.
    /// </summary>
    /// <param name="sprite">The sprite representing the current segmented health state.</param>
    private void SetSprite(Sprite sprite)
    {
        if (healthBarRenderer != null && sprite != null)
        {
            healthBarRenderer.sprite = sprite;
        }
    }
}
