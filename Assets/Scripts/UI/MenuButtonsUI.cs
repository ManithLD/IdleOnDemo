using IdleOnDemo.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Wires HUD combat toggle buttons to player combat state and local menu visuals.
    /// </summary>
    public sealed class MenuButtonsUI : MonoBehaviour
    {
        [Header("Auto Button")]
        [SerializeField] private Button autoButton;
        [SerializeField] private TMP_Text autoButtonText;
        [SerializeField] private Image autoButtonBackgroundImage;
        [SerializeField] private Image autoIconImage;

        [Header("Attacks Button")]
        [SerializeField] private Button attacksButton;
        [SerializeField] private Image attacksButtonBackgroundImage;

        [Header("Sprites")]
        [SerializeField] private Sprite buttonOffSprite;
        [SerializeField] private Sprite buttonOnSprite;
        [SerializeField] private Sprite autoIconOffSprite;
        [SerializeField] private Sprite autoIconOnSprite;

        [Header("Containers")]
        [SerializeField] private GameObject attacksButtonContainer;
        [SerializeField] private GameObject mainMenuButtonContainer;

        private bool isAttacksEnabled = false;

        /// <summary>
        /// Locates the active <see cref="PlayerCombat"/> instance, binds button click handlers,
        /// and initializes both toggle buttons to their current visual state.
        /// </summary>
        private void Start()
        {
            PlayerCombat playerCombat = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            if (playerCombat == null)
            {
                Debug.LogWarning("MenuButtonsUI could not find a PlayerCombat instance. Combat buttons will not update.");
                return;
            }

            if (autoButton != null)
            {
                autoButton.onClick.AddListener(() => OnAutoButtonClicked(playerCombat));
            }

            if (attacksButton != null)
            {
                attacksButton.onClick.AddListener(OnAttacksButtonClicked);
            }

            UpdateAutoButtonVisuals(playerCombat.IsAutoAttackEnabled);
            UpdateAttacksButtonVisuals(isAttacksEnabled);
        }

        /// <summary>
        /// Toggles auto-attack on the player's combat component and refreshes the button visuals
        /// to match the resulting state.
        /// </summary>
        /// <param name="combat">The <see cref="PlayerCombat"/> instance to toggle auto-attack on.</param>
        private void OnAutoButtonClicked(PlayerCombat combat)
        {
            combat.ToggleAutoAttack();
            UpdateAutoButtonVisuals(combat.IsAutoAttackEnabled);
        }

        /// <summary>
        /// Flips the local attacks-button toggle state and updates its visuals.
        /// </summary>
        /// <remarks>
        /// This currently only drives the button's own sprite and does not call into
        /// <see cref="PlayerCombat"/> or any other gameplay system.
        /// </remarks>
        private void OnAttacksButtonClicked()
        {
            isAttacksEnabled = !isAttacksEnabled;
            UpdateAttacksButtonVisuals(isAttacksEnabled);
        }

        /// <summary>
        /// Updates the auto-attack button's label, background sprite, and icon to reflect whether
        /// auto-attack is currently enabled.
        /// </summary>
        /// <param name="isAuto"><c>true</c> if auto-attack is enabled; otherwise <c>false</c>.</param>
        private void UpdateAutoButtonVisuals(bool isAuto)
        {
            if (autoButtonText != null)
            {
                autoButtonText.text = isAuto ? "AUTO ON" : "AUTO OFF";
            }

            if (autoButtonBackgroundImage != null)
            {
                autoButtonBackgroundImage.sprite = isAuto ? buttonOnSprite : buttonOffSprite;
            }

            if (autoIconImage != null)
            {
                autoIconImage.sprite = isAuto ? autoIconOnSprite : autoIconOffSprite;
            }
        }

        /// <summary>
        /// Updates the attacks button's background sprite to reflect its toggled state.
        /// </summary>
        /// <param name="isToggled"><c>true</c> if the attacks button is currently toggled on; otherwise <c>false</c>.</param>
        private void UpdateAttacksButtonVisuals(bool isToggled)
        {
            if (attacksButtonBackgroundImage != null)
            {
                attacksButtonBackgroundImage.sprite = isToggled ? buttonOnSprite : buttonOffSprite;
            }

            if (attacksButtonContainer != null)
            {
                attacksButtonContainer.SetActive(isToggled);
            }

            if (mainMenuButtonContainer != null)
            {
                mainMenuButtonContainer.SetActive(!isToggled);
            }
        }

        /// <summary>
        /// Removes button click listeners to avoid leaking references when this component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (autoButton != null)
            {
                autoButton.onClick.RemoveAllListeners();
            }

            if (attacksButton != null)
            {
                attacksButton.onClick.RemoveAllListeners();
            }
        }
    }
}