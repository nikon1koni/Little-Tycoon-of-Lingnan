using UnityEngine;

public class EventEffectHandler : MonoBehaviour
{
    public static EventEffectHandler Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("EventEffectHandler initialized");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("EventEffectHandler instance already exists");
        }
    }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void ProcessOption(Player player, EventData eventData, int optionIndex)
    {
        Debug.Log($"Processing option: player={player?.playerName ?? "NULL"}, eventData={eventData?.eventTitle ?? "NULL"}, optionIndex={optionIndex}");
        
        if (player == null || eventData == null) 
        {
            Debug.LogError("Player or eventData is null");
            return;
        }

        if (optionIndex >= 0 && optionIndex < eventData.options.Length)
        {
            EventData.EventOption option = eventData.options[optionIndex];
            
            Debug.Log($"=== Processing option [{optionIndex}]: {option.optionText} ===");
            
            ApplyEventEffects(player, eventData, optionIndex);
            
            ShowResultToast(player, eventData, optionIndex);
        }
        else
        {
            Debug.LogError($"Invalid option index: {optionIndex}, total options: {eventData.options.Length}");
        }
    }

    private void ApplyEventEffects(Player player, EventData eventData, int optionIndex)
    {
        Debug.Log($"Applying event effects for optionIndex={optionIndex}");
        
        int costToPay = 0;
        int rewardToGive = 0;
        float incomeBoostToApply = 0f;
        int buffDurationToSet = 0;

        EventData.EventOption option = null;
        if (optionIndex >= 0 && optionIndex < eventData.options.Length)
        {
            option = eventData.options[optionIndex];
            
            costToPay = option.optionCostAmount > 0 ? option.optionCostAmount : eventData.costAmount;
            rewardToGive = option.optionRewardAmount > 0 ? option.optionRewardAmount : eventData.rewardAmount;
            incomeBoostToApply = option.optionIncomeBoost > 0 ? option.optionIncomeBoost : eventData.incomeBoost;
            buffDurationToSet = option.optionBuffDurationRounds > 0 ? option.optionBuffDurationRounds : eventData.buffDurationRounds;
        }
        else
        {
            costToPay = eventData.costAmount;
            rewardToGive = eventData.rewardAmount;
            incomeBoostToApply = eventData.incomeBoost;
            buffDurationToSet = eventData.buffDurationRounds;
        }

        Debug.Log($"Effect values: cost={costToPay}, reward={rewardToGive}, boost={incomeBoostToApply}, duration={buffDurationToSet}");
        Debug.Log($"Player cash before: {player.cash}");

        if (costToPay > 0)
        {
            bool success = player.PayCash(costToPay);
            if (success)
            {
                Debug.Log($"Paid {costToPay} cash. Player cash: {player.cash}");
            }
            else
            {
                Debug.LogWarning($"{player.playerName} cannot afford {costToPay}");
                if (UIManager.Instance != null)
                {
                    UIManager.ShowToastStatic("资金不足", 2f);
                }
                return;
            }
        }

        if (rewardToGive > 0)
        {
            player.ReceiveCash(rewardToGive);
            Debug.Log($"Received {rewardToGive} cash. Player cash: {player.cash}");
        }

        if (incomeBoostToApply > 0 && BuffSystem.Instance != null)
        {
            BuffSystem.Buff buff = BuffSystem.Instance.CreateEventBuff(eventData, optionIndex, this);
            BuffSystem.Instance.AddBuff(player, buff);
        }

        if (option != null)
        {
            ApplySpecialEffects(player, option);
        }
        
        Debug.Log("Event effects applied successfully");
    }

    private void ApplySpecialEffects(Player player, EventData.EventOption option)
    {
        switch (option.effectType)
        {
            case EventData.EventEffectType.StepsModifier:
                if (option.stepsModifier != 0)
                {
                    player.AddStepsModifier(option.stepsModifier);
                    Debug.Log($"{player.playerName} steps modifier changed by {option.stepsModifier}");
                    if (UIManager.Instance != null)
                    {
                        string directionText = option.stepsModifier > 0 ? "增加" : "减少";
                        UIManager.Instance.ShowToast($"移动步数{directionText} {Mathf.Abs(option.stepsModifier)}", 2f);
                    }
                }
                break;

            case EventData.EventEffectType.BuildingDowngrade:
                if (option.buildingDowngradeCount > 0)
                {
                    DowngradeRandomBuildings(player, option.buildingDowngradeCount);
                }
                break;

            case EventData.EventEffectType.IncomeReduction:
                if (option.incomeReductionPercent > 0 && option.incomeReductionRounds > 0)
                {
                    player.AddIncomeReductionDebuff(option.incomeReductionPercent, option.incomeReductionRounds);
                    Debug.Log($"{player.playerName} income reduced by {option.incomeReductionPercent * 100}% for {option.incomeReductionRounds} rounds");
                    if (UIManager.Instance != null)
                    {
                        float reductionPercent = option.incomeReductionPercent * 100;
                        UIManager.Instance.ShowToast($"收入减少{reductionPercent:0}%，持续{option.incomeReductionRounds}回合", 2f);
                    }
                }
                break;

            case EventData.EventEffectType.TaxReduction:
                if (option.taxReductionPercent > 0 && option.taxReductionRounds > 0)
                {
                    player.AddTaxReductionBuff(option.taxReductionPercent, option.taxReductionRounds);
                    Debug.Log($"{player.playerName} tax reduced by {option.taxReductionPercent * 100}% for {option.taxReductionRounds} rounds");
                    if (UIManager.Instance != null)
                    {
                        float reductionPercent = option.taxReductionPercent * 100;
                        UIManager.Instance.ShowToast($"税收减少{reductionPercent:0}%，持续{option.taxReductionRounds}回合", 2f);
                    }
                }
                break;

            case EventData.EventEffectType.ImmuneToNegativeEvents:
                if (option.roundsImmuneToNegativeEvents > 0)
                {
                    player.SetImmuneToNegativeEvents(option.roundsImmuneToNegativeEvents);
                    Debug.Log($"{player.playerName} immune to negative events for {option.roundsImmuneToNegativeEvents} rounds");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast($"获得{option.roundsImmuneToNegativeEvents}回合负面事件免疫", 2f);
                    }
                }
                break;

            case EventData.EventEffectType.NextRollMultiplier:
                if (option.nextRollMultiplier != 1f)
                {
                    player.SetNextRollMultiplier(option.nextRollMultiplier);
                    Debug.Log($"{player.playerName} next roll multiplier set to {option.nextRollMultiplier}x");
                    if (UIManager.Instance != null)
                    {
                        string modifierText = option.nextRollMultiplier > 1 ? "提升" : "降低";
                        UIManager.Instance.ShowToast($"下一次掷骰{modifierText}", 2f);
                    }
                }
                break;

            case EventData.EventEffectType.PropertyCompensation:
                if (option.propertyCompensationPercent > 0)
                {
                    CompensatePropertyLoss(player, option.propertyCompensationPercent);
                }
                break;

            case EventData.EventEffectType.Loan:
                if (option.loanAmount > 0)
                {
                    GiveLoan(player, option.loanAmount, option.loanRepayMultiplier, option.loanRepayRounds);
                }
                break;
        }
    }

    private void DowngradeRandomBuildings(Player player, int count)
    {
        if (player.ownedProperties == null || player.ownedProperties.Count == 0)
        {
            Debug.Log($"{player.playerName} has no buildings to downgrade");
            return;
        }

        System.Collections.Generic.List<BoardTile> upgradableBuildings = new System.Collections.Generic.List<BoardTile>();
        foreach (var property in player.ownedProperties)
        {
            if (property != null && property.currentBuildingData != null && 
                property.currentBuildingData.nextLevelBuilding != null)
            {
                upgradableBuildings.Add(property);
            }
        }

        if (upgradableBuildings.Count == 0)
        {
            Debug.Log($"{player.playerName} has no buildings that can be downgraded");
            return;
        }

        int downgradedCount = 0;
        for (int i = 0; i < count && upgradableBuildings.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, upgradableBuildings.Count);
            BoardTile targetBuilding = upgradableBuildings[randomIndex];
            upgradableBuildings.RemoveAt(randomIndex);

            Debug.Log($"Downgrading building: {targetBuilding.tileName} ({targetBuilding.currentBuildingData.buildingName})");
            downgradedCount++;
        }

        if (downgradedCount > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{downgradedCount}???????????", 2f);
        }
    }

    private void CompensatePropertyLoss(Player player, float compensationPercent)
    {
        if (player.ownedProperties == null || player.ownedProperties.Count == 0)
        {
            Debug.Log($"{player.playerName} has no properties to compensate");
            return;
        }

        int totalCompensation = 0;
        foreach (var property in player.ownedProperties)
        {
            if (property != null && property.currentBuildingData != null)
            {
                int compensation = Mathf.RoundToInt(property.currentBuildingData.purchasePrice * compensationPercent);
                totalCompensation += compensation;
            }
        }

        if (totalCompensation > 0)
        {
            player.ReceiveCash(totalCompensation);
            Debug.Log($"{player.playerName} received {totalCompensation} compensation");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"???{totalCompensation}??????", 2f);
            }
        }
    }

    private void GiveLoan(Player player, int amount, float repayMultiplier, int repayRounds)
    {
        player.ReceiveCash(amount);
        player.AddLoanDebt(amount, repayMultiplier, repayRounds);
        Debug.Log($"{player.playerName} took a loan of {amount} with {repayMultiplier}x repay in {repayRounds} rounds");
        
        if (UIManager.Instance != null)
        {
            int repayAmount = Mathf.RoundToInt(amount * repayMultiplier);
            UIManager.Instance.ShowToast($"???{amount}????{repayRounds}??????w??{repayAmount}??", 2f);
        }
    }

    private void ShowResultToast(Player player, EventData eventData, int optionIndex)
    {
        string message = "";

        if (eventData.costAmount > 0)
        {
            message += $"???? {eventData.costAmount} ??\n";
        }

        if (eventData.rewardAmount > 0)
        {
            message += $"??? {eventData.rewardAmount} ??\n";
        }

        if (eventData.incomeBoost > 0)
        {
            float boostPercent = eventData.incomeBoost * 100;
            message += $"????????({eventData.buffDurationRounds}?????+{boostPercent:0}%)";
        }

        if (!string.IsNullOrEmpty(message))
        {
            if (UIManager.Instance != null)
            {
                UIManager.ShowToastStatic(message.Trim(), 3f);
            }
        }
    }

    public bool CanAfford(Player player, int amount)
    {
        return player != null && player.cash >= amount;
    }
}
