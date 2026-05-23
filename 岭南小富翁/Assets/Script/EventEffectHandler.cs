using UnityEngine;

public class EventEffectHandler : MonoBehaviour
{
    public static EventEffectHandler Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("EventEffectHandler ????????");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("EventEffectHandler ????????????????");
        }
    }

    void Start()
    {
        // ???Instance???????
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// ??????????????????????
    /// </summary>
    public void ProcessOption(Player player, EventData eventData, int optionIndex)
    {
        Debug.Log($"? ProcessOption ???????player={player?.playerName ?? "NULL"}, eventData={eventData?.eventTitle ?? "NULL"}, optionIndex={optionIndex}");
        
        if (player == null || eventData == null) 
        {
            Debug.LogError("? ProcessOption: player ?? eventData ????");
            return;
        }

        if (optionIndex >= 0 && optionIndex < eventData.options.Length)
        {
            EventData.EventOption option = eventData.options[optionIndex];
            
            Debug.Log($"=== ?????????? [{optionIndex}]: {option.optionText} ===");
            
            // ???????
            ApplyEventEffects(player, eventData, optionIndex);
            
            // ?????????
            ShowResultToast(player, eventData, optionIndex);
        }
        else
        {
            Debug.LogError($"? ???????????: {optionIndex}, ???????: {eventData.options.Length}");
        }
    }

    /// <summary>
    /// ????????????????????????Buff???
    /// </summary>
    private void ApplyEventEffects(Player player, EventData eventData, int optionIndex)
    {
        Debug.Log($"? ApplyEventEffects ????????optionIndex={optionIndex}");
        
        // ??????????????????????????????????????
        int costToPay = 0;
        int rewardToGive = 0;
        float incomeBoostToApply = 0f;
        int buffDurationToSet = 0;

        if (optionIndex >= 0 && optionIndex < eventData.options.Length)
        {
            EventData.EventOption option = eventData.options[optionIndex];
            
            // ??????????????????
            costToPay = option.optionCostAmount > 0 ? option.optionCostAmount : eventData.costAmount;
            rewardToGive = option.optionRewardAmount > 0 ? option.optionRewardAmount : eventData.rewardAmount;
            incomeBoostToApply = option.optionIncomeBoost > 0 ? option.optionIncomeBoost : eventData.incomeBoost;
            buffDurationToSet = option.optionBuffDurationRounds > 0 ? option.optionBuffDurationRounds : eventData.buffDurationRounds;
        }
        else
        {
            // ??????????
            costToPay = eventData.costAmount;
            rewardToGive = eventData.rewardAmount;
            incomeBoostToApply = eventData.incomeBoost;
            buffDurationToSet = eventData.buffDurationRounds;
        }

        Debug.Log($"? ????????: cost={costToPay}, reward={rewardToGive}, boost={incomeBoostToApply}, duration={buffDurationToSet}");
        Debug.Log($"? ????????: {player.cash}");

        // ???
        if (costToPay > 0)
        {
            bool success = player.PayCash(costToPay);
            if (success)
            {
                Debug.Log($"? ?????? {costToPay} ???????: {player.cash}");
            }
            else
            {
                Debug.LogWarning($"? {player.playerName} ???????????? {costToPay}");
                if (UIManager.Instance != null)
                {
                    UIManager.ShowToastStatic("??????", 2f);
                }
                return;
            }
        }

        // ???
        if (rewardToGive > 0)
        {
            player.ReceiveCash(rewardToGive);
            Debug.Log($"? ??? {rewardToGive} ???????: {player.cash}");
        }

        // ????????Buff
        if (incomeBoostToApply > 0 && BuffSystem.Instance != null)
        {
            BuffSystem.Buff buff = BuffSystem.Instance.CreateEventBuff(eventData, optionIndex, this);
            BuffSystem.Instance.AddBuff(player, buff);
        }
        
        Debug.Log($"? ApplyEventEffects ???????");
    }

    /// <summary>
    /// ?????????
    /// </summary>
    private void ShowResultToast(Player player, EventData eventData, int optionIndex)
    {
        string message = "";

        if (eventData.costAmount > 0)
        {
            message += $"??? {eventData.costAmount} ???\n";
        }

        if (eventData.rewardAmount > 0)
        {
            message += $"??? {eventData.rewardAmount} ???\n";
        }

        if (eventData.incomeBoost > 0)
        {
            message += $"???????????({eventData.buffDurationRounds}???????+{eventData.incomeBoost*100:0}%)";
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
    /// ?????????????????????
    /// </summary>
    public bool CanAfford(Player player, int amount)
    {
        return player != null && player.cash >= amount;
    }
}
