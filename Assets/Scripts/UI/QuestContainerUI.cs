using IdleOnDemo.Gameplay.Quests;
using TMPro;
using UnityEngine;

namespace IdleOnDemo.UI
{
    /// <summary>
    /// Displays the currently active main-chain quest title and progress description.
    /// </summary>
    public sealed class QuestContainerUI : MonoBehaviour
    {
        [SerializeField] private GameObject questContainerRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        /// <summary>
        /// Locates the active quest manager, subscribes to quest-chain updates, and initializes the display.
        /// </summary>
        private void Start()
        {
            questContainerRoot ??= gameObject;

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("QuestContainerUI could not find a QuestManager instance. Quest UI will not update.");
                return;
            }

            QuestManager.Instance.OnActiveChainQuestChanged += HandleActiveChainQuestChanged;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            RefreshDisplay(QuestManager.Instance.CurrentChainQuest, QuestManager.Instance.CurrentChainQuestState);
        }

        /// <summary>
        /// Unsubscribes from quest manager events to avoid stale UI callbacks across scene loads.
        /// </summary>
        private void OnDestroy()
        {
            if (QuestManager.Instance == null)
            {
                return;
            }

            QuestManager.Instance.OnActiveChainQuestChanged -= HandleActiveChainQuestChanged;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
        }

        /// <summary>
        /// Refreshes the UI when the active main-chain quest changes.
        /// </summary>
        /// <param name="quest">The newly active quest definition.</param>
        /// <param name="state">The runtime state for the newly active quest.</param>
        private void HandleActiveChainQuestChanged(QuestData quest, QuestRuntimeState state)
        {
            RefreshDisplay(quest, state);
        }

        /// <summary>
        /// Refreshes the UI when the currently displayed main-chain quest receives progress.
        /// </summary>
        /// <param name="state">The quest runtime state that changed.</param>
        private void HandleQuestUpdated(QuestRuntimeState state)
        {
            if (QuestManager.Instance?.CurrentChainQuestState == null ||
                state == null ||
                !string.Equals(state.QuestID, QuestManager.Instance.CurrentChainQuestState.QuestID, System.StringComparison.Ordinal))
            {
                return;
            }

            RefreshDisplay(QuestManager.Instance.CurrentChainQuest, QuestManager.Instance.CurrentChainQuestState);
        }

        /// <summary>
        /// Shows or hides the quest container and writes active quest title and remaining-count text.
        /// </summary>
        /// <param name="quest">The quest definition to display.</param>
        /// <param name="state">The runtime state used for progress display.</param>
        private void RefreshDisplay(QuestData quest, QuestRuntimeState state)
        {
            bool hasActiveQuest = quest != null && state != null;
            questContainerRoot.SetActive(hasActiveQuest);
            if (!hasActiveQuest)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = quest.QuestTitle;
            }

            if (descriptionText != null)
            {
                int remaining = Mathf.Max(0, quest.RequiredAmount - state.CurrentProgress);
                descriptionText.text = string.Format(quest.QuestDescription, remaining);
            }
        }
    }
}
