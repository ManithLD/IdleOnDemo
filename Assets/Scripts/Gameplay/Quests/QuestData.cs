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
        [SerializeField] private string questTitle;
        [SerializeField] private string questDescription;
        [SerializeField] private string targetObjectiveID;
        [SerializeField] private int requiredAmount = 1;
        [SerializeField] private int coinReward;
        [SerializeField] private int xpReward;

        public string QuestID => questID;
        public string QuestTitle => questTitle;
        public string QuestDescription => questDescription;
        public string TargetObjectiveID => targetObjectiveID;
        public int RequiredAmount => requiredAmount;
        public int CoinReward => coinReward;
        public int XPReward => xpReward;

        private void OnValidate()
        {
            requiredAmount = Mathf.Max(1, requiredAmount);
            coinReward = Mathf.Max(0, coinReward);
            xpReward = Mathf.Max(0, xpReward);
        }
    }
}
