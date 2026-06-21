using System.Collections;
using IdleOnDemo.Gameplay.Quests;
using TMPro;
using UnityEngine;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Manages the visual display of the currently active main-chain quest.
    /// </summary>
    /// <remarks>
    /// This controller uses an event-driven architecture to listen for updates from the <see cref="QuestManager"/>.
    /// To prevent lifecycle issues, this script should be attached to an always-active parent Controller object, 
    /// while the visual hierarchy is assigned to the <see cref="questContainerRoot"/> field to be toggled safely.
    /// </remarks>
    public sealed class QuestContainerUI : MonoBehaviour
    {
        [Header("References")]

        /// <summary>
        /// The root visual GameObject containing the quest UI elements (e.g., backgrounds, borders).
        /// This object is toggled on/off based on quest availability.
        /// </summary>
        [Tooltip("The visual root to toggle on/off. Do NOT attach this script to this exact GameObject.")]
        [SerializeField] private GameObject questContainerRoot;

        /// <summary>
        /// The text component responsible for rendering the quest's title.
        /// </summary>
        [Tooltip("Text component for the quest title.")]
        [SerializeField] private TMP_Text titleText;

        /// <summary>
        /// The text component responsible for rendering the quest's progress and description.
        /// </summary>
        [Tooltip("Text component for the quest description and progress tracking.")]
        [SerializeField] private TMP_Text descriptionText;

        [Header("Settings")]

        /// <summary>
        /// The speed at which characters are revealed when a new quest is animated.
        /// Setting this to 0 or lower will disable the animation and show text instantly.
        /// </summary>
        [Tooltip("Characters revealed per second during the text animation.")]
        [SerializeField] private float charactersPerSecond = 30f;

        /// <summary>
        /// A reference to the currently running text reveal coroutine, used to safely interrupt 
        /// the animation if the quest updates or changes mid-reveal.
        /// </summary>
        private Coroutine activeRevealRoutine;

        /// <summary>
        /// Invoked when the GameObject becomes active and enabled. 
        /// Subscribes to global quest events and forces an initial state sync.
        /// </summary>
        private void OnEnable()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("[QuestContainerUI] QuestManager instance not found. UI will not update.");
                return;
            }

            // Subscribe to external state changes
            QuestManager.Instance.OnActiveChainQuestChanged += HandleActiveChainQuestChanged;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;

            // Force an immediate refresh to sync visuals with the current runtime state
            RefreshDisplay(QuestManager.Instance.CurrentChainQuest, QuestManager.Instance.CurrentChainQuestState, animate: true);
        }

        /// <summary>
        /// Invoked when the GameObject becomes disabled or inactive.
        /// Unsubscribes from global events to prevent memory leaks and stops active animations.
        /// </summary>
        private void OnDisable()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnActiveChainQuestChanged -= HandleActiveChainQuestChanged;
                QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            }

            StopActiveRevealRoutine();
        }

        /// <summary>
        /// Callback triggered when the player completes a quest and a new one in the chain begins.
        /// </summary>
        /// <param name="quest">The static data definition of the newly active quest.</param>
        /// <param name="state">The runtime progression state of the newly active quest.</param>
        private void HandleActiveChainQuestChanged(QuestData quest, QuestRuntimeState state)
        {
            RefreshDisplay(quest, state, animate: true);
        }

        /// <summary>
        /// Callback triggered when the currently active quest receives a progress update (e.g., an enemy is killed).
        /// </summary>
        /// <param name="state">The runtime progression state that was modified.</param>
        private void HandleQuestUpdated(QuestRuntimeState state)
        {
            // Validate that the update belongs to the current chain quest
            if (QuestManager.Instance?.CurrentChainQuestState == null || state == null ||
                !string.Equals(state.QuestID, QuestManager.Instance.CurrentChainQuestState.QuestID, System.StringComparison.Ordinal))
            {
                return;
            }

            // Refresh the display without re-animating the entire block of text
            RefreshDisplay(QuestManager.Instance.CurrentChainQuest, QuestManager.Instance.CurrentChainQuestState, animate: false);
        }

        /// <summary>
        /// Evaluates the current quest state to toggle the visual root and format the text displays.
        /// </summary>
        /// <param name="quest">The static data definition of the quest to display.</param>
        /// <param name="state">The runtime progression state used to calculate remaining objectives.</param>
        /// <param name="animate">If <c>true</c>, the text will reveal sequentially. If <c>false</c>, it updates instantly.</param>
        private void RefreshDisplay(QuestData quest, QuestRuntimeState state, bool animate)
        {
            bool hasActiveQuest = quest != null && state != null;

            if (questContainerRoot != null)
            {
                questContainerRoot.SetActive(hasActiveQuest);
            }

            // If there is no quest to display, ensure animations are killed and exit early
            if (!hasActiveQuest)
            {
                StopActiveRevealRoutine();
                return;
            }

            // Calculate remaining objectives and format the string allocation
            int remaining = Mathf.Max(0, quest.RequiredAmount - state.CurrentProgress);
            string description = string.Format(quest.QuestDescription, remaining);

            if (animate)
            {
                StopActiveRevealRoutine();

                // Unity strictly prohibits starting coroutines on inactive GameObjects. 
                // This check ensures the hierarchy is fully active before proceeding.
                if (gameObject.activeInHierarchy)
                {
                    activeRevealRoutine = StartCoroutine(RevealQuestText(quest.QuestTitle, description));
                }
                return;
            }

            // Handle instantaneous updates (e.g., rapid progress changes)
            StopActiveRevealRoutine();

            if (descriptionText != null)
            {
                descriptionText.text = description;
                descriptionText.maxVisibleCharacters = int.MaxValue;
            }

            if (titleText != null)
            {
                titleText.text = quest.QuestTitle;
                titleText.maxVisibleCharacters = int.MaxValue;
            }
        }

        /// <summary>
        /// Orchestrates the sequential text animation, revealing the title first, followed by the description.
        /// </summary>
        /// <param name="title">The formatted title string to display.</param>
        /// <param name="description">The formatted description string to display.</param>
        /// <returns>An IEnumerator to be processed by Unity's Coroutine system.</returns>
        private IEnumerator RevealQuestText(string title, string description)
        {
            // Sanitize inputs to prevent null reference exceptions during length checks
            title ??= string.Empty;
            description ??= string.Empty;

            // Pre-assign text but hide it to allow TextMeshPro to calculate layout boundaries 
            // without showing the characters to the player yet.
            if (titleText != null)
            {
                titleText.text = title;
                titleText.maxVisibleCharacters = 0;
            }

            if (descriptionText != null)
            {
                descriptionText.text = description;
                descriptionText.maxVisibleCharacters = 0;
            }

            // Yield operations sequentially
            yield return RevealText(titleText, title.Length);
            yield return RevealText(descriptionText, description.Length);

            activeRevealRoutine = null;
        }

        /// <summary>
        /// Modifies the TextMeshPro vertex visibility progressively to create a typing effect without string allocations.
        /// </summary>
        /// <param name="text">The specific TextMeshPro component to animate.</param>
        /// <param name="characterCount">The total number of characters to reveal.</param>
        /// <returns>An IEnumerator to be processed by Unity's Coroutine system.</returns>
        private IEnumerator RevealText(TMP_Text text, int characterCount)
        {
            if (text == null) yield break;

            if (charactersPerSecond <= 0f)
            {
                text.maxVisibleCharacters = characterCount;
                yield break;
            }

            // Cache the yield instruction to avoid garbage generation every loop iteration
            WaitForSeconds delay = new WaitForSeconds(1f / charactersPerSecond);

            for (int i = 0; i <= characterCount; i++)
            {
                text.maxVisibleCharacters = i;
                yield return delay;
            }
        }

        /// <summary>
        /// Safely terminates the currently running text reveal animation and clears its reference.
        /// </summary>
        private void StopActiveRevealRoutine()
        {
            if (activeRevealRoutine != null)
            {
                StopCoroutine(activeRevealRoutine);
                activeRevealRoutine = null;
            }
        }
    }
}