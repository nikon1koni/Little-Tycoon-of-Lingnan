using UnityEngine;

public class EventEffectHandler : MonoBehaviour
{
    public static EventEffectHandler Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("EventEffectHandler 初始化完成");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("EventEffectHandler 已存在，保留原有实例");
        }
    }

    void Start()
    {
        // 确保Instance正确设置
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// 处理事件选项效果（统一入口）
    /// </summary>
    public void ProcessOption(Player player, EventData eventData, int optionIndex)
    {
        Debug.Log($"? ProcessOption 被调用！player={player?.playerName ?? "NULL"}, eventData={eventData?.eventTitle ?? "NULL"}, optionIndex={optionIndex}");
        
        if (player == null || eventData == null) 
        {
            Debug.LogError("? ProcessOption: player 或 eventData 为空！");
            return;
        }

        if (optionIndex >= 0 && optionIndex < eventData.options.Length)
        {
            EventData.EventOption option = eventData.options[optionIndex];
            
            Debug.Log($"=== 处理事件选项 [{optionIndex}]: {option.optionText} ===");
            
            // 执行效果
            ApplyEventEffects(player, eventData, optionIndex);
            
            // 显示结果提示
            ShowResultToast(player, eventData, optionIndex);
        }
        else
        {
            Debug.LogError($"? 选项索引无效: {optionIndex}, 选项数量: {eventData.options.Length}");
        }
    }

    /// <summary>
    /// 应用事件效果（扣钱、给钱、加Buff等）
    /// </summary>
    private void ApplyEventEffects(Player player, EventData eventData, int optionIndex)
    {
        Debug.Log($"? ApplyEventEffects 开始执行，optionIndex={optionIndex}");
        
        // 获取选项的单独配置（如果有），否则使用全局配置
        int costToPay = 0;
        int rewardToGive = 0;
        float incomeBoostToApply = 0f;
        int buffDurationToSet = 0;

        if (optionIndex >= 0 && optionIndex < eventData.options.Length)
        {
            EventData.EventOption option = eventData.options[optionIndex];
            
            // 优先使用选项的单独配置
            costToPay = option.optionCostAmount > 0 ? option.optionCostAmount : eventData.costAmount;
            rewardToGive = option.optionRewardAmount > 0 ? option.optionRewardAmount : eventData.rewardAmount;
            incomeBoostToApply = option.optionIncomeBoost > 0 ? option.optionIncomeBoost : eventData.incomeBoost;
            buffDurationToSet = option.optionBuffDurationRounds > 0 ? option.optionBuffDurationRounds : eventData.buffDurationRounds;
        }
        else
        {
            // 使用全局配置
            costToPay = eventData.costAmount;
            rewardToGive = eventData.rewardAmount;
            incomeBoostToApply = eventData.incomeBoost;
            buffDurationToSet = eventData.buffDurationRounds;
        }

        Debug.Log($"? 效果参数: cost={costToPay}, reward={rewardToGive}, boost={incomeBoostToApply}, duration={buffDurationToSet}");
        Debug.Log($"? 玩家当前金币: {player.cash}");

        // 扣钱
        if (costToPay > 0)
        {
            bool success = player.PayCash(costToPay);
            if (success)
            {
                Debug.Log($"? 成功支付 {costToPay} 金币，剩余: {player.cash}");
            }
            else
            {
                Debug.LogWarning($"? {player.playerName} 金币不足，无法支付 {costToPay}");
                if (UIManager.Instance != null)
                {
                    UIManager.ShowToastStatic("金币不足！", 2f);
                }
                return;
            }
        }

        // 给钱
        if (rewardToGive > 0)
        {
            player.ReceiveCash(rewardToGive);
            Debug.Log($"? 获得 {rewardToGive} 金币，当前: {player.cash}");
        }

        // 添加收入Buff
        if (incomeBoostToApply > 0 && BuffSystem.Instance != null)
        {
            BuffSystem.Buff buff = BuffSystem.Instance.CreateEventBuff(eventData, optionIndex, this);
            BuffSystem.Instance.AddBuff(player, buff);
        }
        
        Debug.Log($"? ApplyEventEffects 执行完成！");
    }

    /// <summary>
    /// 显示结果提示
    /// </summary>
    private void ShowResultToast(Player player, EventData eventData, int optionIndex)
    {
        string message = "";

        if (eventData.costAmount > 0)
        {
            message += $"支付 {eventData.costAmount} 金币\n";
        }

        if (eventData.rewardAmount > 0)
        {
            message += $"获得 {eventData.rewardAmount} 金币\n";
        }

        if (eventData.incomeBoost > 0)
        {
            message += $"获得【商誉】({eventData.buffDurationRounds}圈内收益+{eventData.incomeBoost*100:0}%)";
        }

        if (!string.IsNullOrEmpty(message))
        {
            if (UIManager.Instance != null)
            {
                UIManager.ShowToastStatic(message.Trim(), 3f);
            }
        }
    }

    /// <summary>
    /// 检查玩家是否可以支付指定金额
    /// </summary>
    public bool CanAfford(Player player, int amount)
    {
        return player != null && player.cash >= amount;
    }
}
