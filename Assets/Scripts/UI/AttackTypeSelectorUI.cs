using IdleOnDemo.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Wires a single-select HUD button group to the player's current attack animation type.
    /// </summary>
    public sealed class AttackTypeSelectorUI : MonoBehaviour
    {
        [System.Serializable]
        private sealed class AttackTypeButton
        {
            [SerializeField] private Button button;
            [SerializeField] private AttackType attackType;

            public Button Button => button;
            public AttackType AttackType => attackType;
        }

        [SerializeField] private AttackTypeButton[] attackButtons;
        [SerializeField] private Button defaultButton;
        [SerializeField] private AttackType defaultAttackType = AttackType.Default;
        [SerializeField] private Sprite buttonOffSprite;
        [SerializeField] private Sprite buttonOnSprite;

        private PlayerCombat playerCombat;
        private Button selectedButton;
        private AttackType selectedAttackType;

        /// <summary>
        /// Locates the active <see cref="PlayerCombat"/> instance, binds attack-type buttons,
        /// and initializes the selected attack type and button visuals.
        /// </summary>
        private void Start()
        {
            playerCombat = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
            if (playerCombat == null)
            {
                Debug.LogWarning("AttackTypeSelectorUI could not find a PlayerCombat instance. Attack type buttons will not update.");
                return;
            }

            BindAttackButtons();
            ResetButtonVisuals();
            SelectAttackType(defaultButton, defaultAttackType);
        }

        /// <summary>
        /// Registers click listeners for each configured attack-type button.
        /// </summary>
        private void BindAttackButtons()
        {
            if (attackButtons == null)
            {
                return;
            }

            foreach (AttackTypeButton attackButton in attackButtons)
            {
                if (attackButton?.Button == null)
                {
                    continue;
                }

                AttackTypeButton capturedButton = attackButton;
                attackButton.Button.onClick.AddListener(() => OnAttackTypeButtonClicked(capturedButton));
            }
        }

        /// <summary>
        /// Selects the clicked attack type unless it is already the active selection.
        /// </summary>
        /// <param name="attackButton">The clicked attack-type button mapping.</param>
        private void OnAttackTypeButtonClicked(AttackTypeButton attackButton)
        {
            if (attackButton == null)
            {
                return;
            }

            SelectAttackType(attackButton.Button, attackButton.AttackType);
        }

        /// <summary>
        /// Initializes all configured attack-type buttons to their unselected visual state.
        /// </summary>
        private void ResetButtonVisuals()
        {
            if (attackButtons == null)
            {
                return;
            }

            foreach (AttackTypeButton attackButton in attackButtons)
            {
                SetButtonSelected(attackButton?.Button, false);
            }
        }

        /// <summary>
        /// Applies a single active attack-type selection to the player combat component and button visuals.
        /// </summary>
        /// <param name="button">The button that should appear selected.</param>
        /// <param name="attackType">The attack type represented by the selected button.</param>
        private void SelectAttackType(Button button, AttackType attackType)
        {
            if (button == null)
            {
                return;
            }

            if (selectedButton == button && selectedAttackType == attackType)
            {
                return;
            }

            SetButtonSelected(selectedButton, false);
            selectedButton = button;
            selectedAttackType = attackType;
            SetButtonSelected(selectedButton, true);
            playerCombat.SetAttackType(attackType);
        }

        /// <summary>
        /// Swaps the configured on/off sprite onto a button's image.
        /// </summary>
        /// <param name="button">The button whose visual state should change.</param>
        /// <param name="isSelected"><c>true</c> if the button is selected; otherwise <c>false</c>.</param>
        private void SetButtonSelected(Button button, bool isSelected)
        {
            if (button == null || button.image == null)
            {
                return;
            }

            button.image.sprite = isSelected ? buttonOnSprite : buttonOffSprite;
        }

        /// <summary>
        /// Removes click listeners to avoid leaking player combat references across scene loads.
        /// </summary>
        private void OnDestroy()
        {
            if (attackButtons == null)
            {
                return;
            }

            foreach (AttackTypeButton attackButton in attackButtons)
            {
                if (attackButton?.Button != null)
                {
                    attackButton.Button.onClick.RemoveAllListeners();
                }
            }
        }
    }
}
