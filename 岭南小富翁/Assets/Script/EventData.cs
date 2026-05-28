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

    [Header("事件效果")]
    [Tooltip("事件基础消耗金额，0表示无消耗")]
    public int costAmount = 0;

    [Tooltip("事件基础奖励金额，0表示无奖励")]
    public int rewardAmount = 0;

    [Tooltip("收入提升比例，0.4表示+40%，0表示无提升")]
    public float incomeBoost = 0f;

    [Tooltip("Buff持续回合数，0表示立即生效")]
    public int buffDurationRounds = 0;

    [System.Serializable]
    public class EventOption
    {
        public string optionText;
        public UnityEngine.Events.UnityEvent onOptionSelected;
        
        [Header("选项效果（优先级高于事件基础效果）")]
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
