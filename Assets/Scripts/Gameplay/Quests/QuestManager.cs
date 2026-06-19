using System;
using System.Collections.Generic;
using IdleOnDemo.Gameplay.Player;
using IdleOnDemo.Gameplay.Progression;
using UnityEngine;

namespace IdleOnDemo.Gameplay.Quests
{
    /// <summary>
    /// Persistent quest service that tracks runtime quest states and broadcasts updates to observers.
    /// </summary>
    public sealed class QuestManager : MonoBehaviour
    {
        private static QuestManager instance;

        [SerializeField] private List<QuestData> questDefinitions = new List<QuestData>();

        private Dictionary<string, QuestRuntimeState> questStates = new Dictionary<string, QuestRuntimeState>();
        private Dictionary<string, QuestData> questDefinitionsByID = new Dictionary<string, QuestData>();

        public static QuestManager Instance => instance;

        /// <summary>
        /// Raised whenever a quest runtime state changes.
        /// </summary>
        public event Action<QuestRuntimeState> OnQuestUpdated;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            RebuildQuestDefinitionRegistry();
        }

        public QuestRuntimeState GetQuestState(string questID)
        {
            if (string.IsNullOrWhiteSpace(questID))
            {
                return null;
            }

            if (!questStates.TryGetValue(questID, out QuestRuntimeState state))
            {
                state = new QuestRuntimeState(questID);
                questStates.Add(questID, state);
            }

            return state;
        }

        public void AcceptQuest(QuestData quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestID))
            {
                return;
            }

            RegisterQuestDefinition(quest);
            QuestRuntimeState state = GetQuestState(quest.QuestID);
            if (state == null)
            {
                return;
            }

            state.IsAccepted = true;
            Debug.Log("Quest Accepted!");
            OnQuestUpdated?.Invoke(state);
        }

        /// <summary>
        /// Adds progress to any accepted quest whose target objective identifier matches gameplay progress.
        /// </summary>
        /// <param name="objectiveID">The objective identifier reported by gameplay, such as kill_slime.</param>
        /// <param name="amount">The amount of objective progress to add.</param>
        public void RegisterObjectiveProgress(string objectiveID, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(objectiveID) || amount <= 0)
            {
                return;
            }

            List<QuestRuntimeState> updatedStates = new List<QuestRuntimeState>();
            foreach (QuestRuntimeState state in questStates.Values)
            {
                if (state == null || !state.IsAccepted || state.IsCompleted)
                {
                    continue;
                }

                if (!questDefinitionsByID.TryGetValue(state.QuestID, out QuestData quest) || quest == null)
                {
                    continue;
                }

                if (!string.Equals(objectiveID, quest.TargetObjectiveID, StringComparison.Ordinal))
                {
                    continue;
                }

                state.CurrentProgress = Mathf.Min(state.CurrentProgress + amount, quest.RequiredAmount);
                Debug.Log($"[DEBUG] QuestManager received progress for: {objectiveID}. Current: {state.CurrentProgress} / {quest.RequiredAmount}");

                if (state.CurrentProgress >= quest.RequiredAmount)
                {
                    state.IsCompleted = true;
                    Debug.Log("[DEBUG] Quest threshold reached!");
                }

                updatedStates.Add(state);
            }

            foreach (QuestRuntimeState state in updatedStates)
            {
                OnQuestUpdated?.Invoke(state);
            }
        }

        public bool TurnInQuest(string questID)
        {
            QuestRuntimeState state = GetQuestState(questID);
            if (state == null || !state.IsCompleted || state.IsTurnedIn)
            {
                return false;
            }

            if (!questDefinitionsByID.TryGetValue(questID, out QuestData questDefinition) || questDefinition == null)
            {
                return false;
            }

            state.IsTurnedIn = true;

            PlayerStats player = FindPlayerStats();
            if (player != null)
            {
                player.AddCoins(questDefinition.CoinReward);
                player.AddXP(questDefinition.XPReward);
            }

            Debug.Log($"Quest turned in: {questID}. Rewarded {questDefinition.CoinReward} coins.");
            OnQuestUpdated?.Invoke(state);
            return true;
        }

        private static PlayerStats FindPlayerStats()
        {
            PlayerStats player = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
            if (player != null)
            {
                return player;
            }

            PlayerController playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            return playerController != null ? playerController.GetComponent<PlayerStats>() : null;
        }

        private void RegisterQuestDefinition(QuestData quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestID))
            {
                return;
            }

            questDefinitionsByID[quest.QuestID] = quest;
            if (!questDefinitions.Contains(quest))
            {
                questDefinitions.Add(quest);
            }
        }

        private void RebuildQuestDefinitionRegistry()
        {
            questDefinitionsByID.Clear();
            foreach (QuestData quest in questDefinitions)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestID))
                {
                    continue;
                }

                questDefinitionsByID[quest.QuestID] = quest;
            }
        }

        private void OnValidate()
        {
            questDefinitions.RemoveAll(quest => quest == null);
        }
    }
}
