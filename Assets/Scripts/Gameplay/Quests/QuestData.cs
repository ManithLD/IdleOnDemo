using UnityEngine;

namespace IdleOnDemo.Gameplay.Quests
{
    /// <summary>
    /// Scriptable quest definition used by the quest manager to track objective progress and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "RPG/Quest Data")]
    public sealed class QuestData : ScriptableObject
    {
        [SerializeField] private string questID;
        [SerializeField] private string targetObjectiveID;
        [SerializeField] private int requiredAmount = 1;
        [SerializeField] private int coinReward;

        public string QuestID => questID;
        public string TargetObjectiveID => targetObjectiveID;
        public int RequiredAmount => requiredAmount;
        public int CoinReward => coinReward;

        private void OnValidate()
        {
            requiredAmount = Mathf.Max(1, requiredAmount);
            coinReward = Mathf.Max(0, coinReward);
        }
    }
}
