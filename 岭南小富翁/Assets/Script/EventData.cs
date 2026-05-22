using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "Game/Event Data")]
public class EventData : ScriptableObject
{
    [Header("事件基本信息")]
    public string eventTitle;
    public Sprite eventImage;
    [TextArea(3, 10)]
    public string eventDescription;

    [Header("事件选项")]
    public EventOption[] options;

    [Header("效果配置")]
    [Tooltip("选择此事件时需要支付的金额（0表示不扣钱）")]
    public int costAmount = 0;

    [Tooltip("选择此事件后获得的奖励金额（0表示不给钱）")]
    public int rewardAmount = 0;

    [Tooltip("收入加成倍率（0.4表示+40%，0表示不加成）")]
    public float incomeBoost = 0f;

    [Tooltip("Buff持续回合数（0表示永久）")]
    public int buffDurationRounds = 0;

    [System.Serializable]
    public class EventOption
    {
        public string optionText;
        public UnityEngine.Events.UnityEvent onOptionSelected;
        
        [Header("单独的效果配置（可选，会覆盖全局配置）")]
        public int optionCostAmount = 0;
        public int optionRewardAmount = 0;
        public float optionIncomeBoost = 0f;
        public int optionBuffDurationRounds = 0;
    }
}
