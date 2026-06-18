using System;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Quests
{
    /// <summary>
    /// Persistent quest service that tracks progress against one active quest definition.
    /// </summary>
    public sealed class QuestManager : MonoBehaviour
    {
        private static QuestManager instance;

        [SerializeField] private QuestData activeQuest;
        [SerializeField] private int currentProgress;
        [SerializeField] private bool isCompleted;

        public static QuestManager Instance => instance;
        public QuestData ActiveQuest => activeQuest;
        public int CurrentProgress => currentProgress;
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// Raised once when the active quest reaches its required objective progress.
        /// </summary>
        public event Action<QuestData> OnQuestCompleted;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            NormalizeProgress();
        }

        /// <summary>
        /// Adds progress to the active quest when the objective identifier matches its target.
        /// </summary>
        /// <param name="objectiveID">The objective identifier reported by gameplay, such as kill_slime.</param>
        /// <param name="amount">The amount of objective progress to add.</param>
        public void RegisterObjectiveProgress(string objectiveID, int amount = 1)
        {
            if (activeQuest == null || isCompleted || string.IsNullOrWhiteSpace(objectiveID) || amount <= 0)
            {
                return;
            }

            if (!string.Equals(objectiveID, activeQuest.TargetObjectiveID, StringComparison.Ordinal))
            {
                return;
            }

            currentProgress = Mathf.Min(currentProgress + amount, activeQuest.RequiredAmount);
            if (currentProgress < activeQuest.RequiredAmount)
            {
                return;
            }

            isCompleted = true;
            OnQuestCompleted?.Invoke(activeQuest);
        }

        /// <summary>
        /// Assigns a new active quest and resets runtime progress for it.
        /// </summary>
        /// <param name="quest">The quest definition to track.</param>
        public void SetActiveQuest(QuestData quest)
        {
            activeQuest = quest;
            currentProgress = 0;
            isCompleted = false;
            NormalizeProgress();
        }

        private void NormalizeProgress()
        {
            currentProgress = Mathf.Max(0, currentProgress);
            if (activeQuest == null)
            {
                isCompleted = false;
                return;
            }

            currentProgress = Mathf.Min(currentProgress, activeQuest.RequiredAmount);
            isCompleted = isCompleted || currentProgress >= activeQuest.RequiredAmount;
        }

        private void OnValidate()
        {
            NormalizeProgress();
        }
    }
}
