using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "Game/Event Data")]
public class EventData : ScriptableObject
{
    [Header("事件信息")]
    public string eventTitle;
    public Sprite eventImage;
    [TextArea(3, 10)]
    public string eventDescription;

    [Header("选项")]
    public EventOption[] options;

    [Header("效果参数")]
    [Tooltip("花费金币，0表示不花费")]
    public int costAmount = 0;

    [Tooltip("奖励金币，0表示无奖励")]
    public int rewardAmount = 0;

    [Tooltip("收入提升倍率，0.4表示+40%，0表示无提升")]
    public float incomeBoost = 0f;

    [Tooltip("Buff持续回合数，0表示永久")]
    public int buffDurationRounds = 0;

    [System.Serializable]
    public class EventOption
    {
        public string optionText;
        public UnityEngine.Events.UnityEvent onOptionSelected;
        
        [Header("选项特殊效果参数（覆盖全局）")]
        public int optionCostAmount = 0;
        public int optionRewardAmount = 0;
        public float optionIncomeBoost = 0f;
        public int optionBuffDurationRounds = 0;

        [Header("特殊效果")]
        public EventEffectType effectType = EventEffectType.None;
        
        public int stepsModifier = 0;
        public int buildingDowngradeCount = 0;
        public float incomeReductionPercent = 0f;
        public int incomeReductionRounds = 0;
        public float taxReductionPercent = 0f;
        public int taxReductionRounds = 0;
        public int roundsImmuneToNegativeEvents = 0;
        public float nextRollMultiplier = 1f;
        public float propertyCompensationPercent = 0f;
        public int loanAmount = 0;
        public float loanRepayMultiplier = 1f;
        public int loanRepayRounds = 0;
    }

    public enum EventEffectType
    {
        None,
        GainMoney,
        LoseMoney,
        IncomeBoost,
        StepsModifier,
        BuildingDowngrade,
        IncomeReduction,
        TaxReduction,
        ImmuneToNegativeEvents,
        NextRollMultiplier,
        PropertyCompensation,
        Loan
    }
}
