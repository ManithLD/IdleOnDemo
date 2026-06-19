namespace IdleOnDemo.Gameplay.Quests
{
    /// <summary>
    /// Runtime quest state tracked by QuestManager independently from quest definitions.
    /// </summary>
    public sealed class QuestRuntimeState
    {
        public QuestRuntimeState(string id)
        {
            QuestID = id;
            IsAccepted = false;
            IsCompleted = false;
            IsTurnedIn = false;
            CurrentProgress = 0;
        }

        public string QuestID { get; set; }
        public bool IsAccepted { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsTurnedIn { get; set; }
        public int CurrentProgress { get; set; }
    }
}
