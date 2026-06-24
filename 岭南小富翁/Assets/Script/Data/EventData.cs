using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "Game/Event Data")]
public class EventData : ScriptableObject
{
    [Header("??????")]
    public string eventTitle;
    public Sprite eventImage;
    [TextArea(3, 10)]
    public string eventDescription;

    [Header("???")]
    public EventOption[] options;

    [Header("????????")]
    [Tooltip("???????0?????????")]
    public int costAmount = 0;

    [Tooltip("????????0????????")]
    public int rewardAmount = 0;

    [Tooltip("?????????????0.4???+40%??0?????????")]
    public float incomeBoost = 0f;

    [Tooltip("Buff???????????0???????")]
    public int buffDurationRounds = 0;

    [System.Serializable]
    public class EventOption
    {
        public string optionText;
        public UnityEngine.Events.UnityEvent onOptionSelected;
        
        [Header("?????????????????????????")]
        public int optionCostAmount = 0;
        public int optionRewardAmount = 0;
        public float optionIncomeBoost = 0f;
        public int optionBuffDurationRounds = 0;

        [Header("????????")]
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
