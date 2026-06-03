using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [System.Serializable]
    public class BuildingBuffConfig
    {
        public BuffEffect effectType = BuffEffect.IncomeMultiplier;
        public float baseValue = 0.1f;
        public float growthRate = 1.1f;
        public float duration = 0f;
        public int durationRounds = 0;
        public bool isPermanent = true;
        [TextArea(2, 4)] public string customDescription = "";
    }

    [Header("")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None;

    [Header("")]
    public bool isFinalLevel = false;
    public int buildingLevel = 1;

    [Header("Buff")]
    public float[] buffValues;

    [Header("")]
    public string buildingName = "";
    public int purchasePrice = 100;
    public int upgradePrice = 50;
    public int minTileScale = 1;
    public int maxTileScale = 4;
    public Scale requiredScale = Scale.Small;

    [Header("")]
    public BuildingFunctionType functionType = BuildingFunctionType.Income;

    [Header("")]
    public int baseIncome = 10;
    public float incomeGrowthRate = 1.2f;
    public bool enableIncomeGrowth = false;

    [Header("Buff??")]
    [Tooltip(" Buff ??")]
    public List<BuildingBuffConfig> buffConfigs = new List<BuildingBuffConfig>();
    
    [Header("() Buff")]
    public BuffEffect buffEffect = BuffEffect.IncomeMultiplier;
    public float baseBuffValue = 0.1f;
    public float buffGrowthRate = 1.1f;
    public float buffDuration = 10f;

    [Header("")]
    public int mixedBaseIncome = 5;
    public float mixedIncomeGrowthRate = 1.1f;

    [Header("骰子规则")]
    [Tooltip("触发收益的目标骰子点数（空=所有1~6，如[2,4,6]或[1,6]）")]
    public int[] targetDiceValues = new int[] { 2, 4, 6 };
    [Tooltip("骰子奖励模式：固定金额 或 点数倍率")]
    public DiceRewardMode diceRewardMode = DiceRewardMode.FixedValue;
    [Tooltip("固定金额模式的奖励数")]
    public int diceFixedReward = 20;
    [Tooltip("倍率模式：点数 × 此值")]
    public float diceMultiplier = 5f;

    // 骰子奖励模式
    public enum DiceRewardMode
    {
        FixedValue,     // 固定金额
        DiceMultiplier  // 点数 × 倍率
    }

    /// <summary> 根据骰子值计算收益，返回0表示不触发 </summary>
    public int CalculateDiceReward(int diceValue)
    {
        if (!IsDiceValueMatch(diceValue)) return 0;

        switch (diceRewardMode)
        {
            case DiceRewardMode.FixedValue:
                return diceFixedReward;
            case DiceRewardMode.DiceMultiplier:
                return Mathf.RoundToInt(diceValue * diceMultiplier);
            default:
                return 0;
        }
    }

    /// <summary> 判断骰子值是否匹配目标点数 </summary>
    public bool IsDiceValueMatch(int diceValue)
    {
        if (targetDiceValues == null || targetDiceValues.Length == 0)
            return true; // 空数组=所有点数都匹配
        for (int i = 0; i < targetDiceValues.Length; i++)
        {
            if (targetDiceValues[i] == diceValue) return true;
        }
        return false;
    }

    /// <summary> 获取骰子规则描述文本 </summary>
    public string GetDiceRuleDescription()
    {
        string targetDesc;
        if (targetDiceValues == null || targetDiceValues.Length == 0)
            targetDesc = "任意";
        else
            targetDesc = string.Join(",", targetDiceValues);

        switch (diceRewardMode)
        {
            case DiceRewardMode.FixedValue:
                return $"掷出 {targetDesc} 得 {diceFixedReward}";
            case DiceRewardMode.DiceMultiplier:
                return $"掷出 {targetDesc} 得 点数×{diceMultiplier}({diceMultiplier}~{diceMultiplier * 6})";
            default:
                return "";
        }
    }

    [Header("增值规则")]
    [Tooltip("每持有1圈，估值增加的金额")]
    public int appreciationPerRound = 0;

    // 增值估值计算：购买价 + 持有圈数 × 每圈增值
    public int GetAppreciatedValue(int roundsOwned)
    {
        return purchasePrice + (roundsOwned * appreciationPerRound);
    }

    // --- 以下是预制体和特效配置 ---
    public Sprite buildingIcon;
    public GameObject buildingPrefab;
    public BuildingData nextLevelBuilding;

    [Header("????????")]
    [Tooltip("?????????????????????????????")]
    public Vector3 positionOffset = new Vector3(0, 0.5f, 0);
    [Tooltip("??????????????????Euler??????")]
    public Vector3 rotationEuler = Vector3.zero;

    [Header("????")]
    public GameObject effectIconPrefab;
    public AudioClip effectSound;
    public float effectDuration = 1.5f;

    [Header("")]
    [TextArea(3, 5)]
    public string description = "";

    // 
    public enum Scale
    {
        Small = 1,
        Medium = 2,
        Large = 3,
        ExtraLarge = 4
    }

    // 
    public enum BuildingFunctionType
    {
        Income,
        Buff,
        Mixed,
        DiceEven,  // 骰子触发
        Appreciation  // 增值：持有每圈估值增加
    }

    // Buff??
    public enum BuffEffect
    {
        MoveSpeedBoost,
        DiceBoost,
        IncomeMultiplier,
        DefenseBoost,
        LuckBoost,
        AllIncomeBoost
    }

    // 
    public int GetIncomeAmount(int level)
    {
        switch (functionType)
        {
            case BuildingFunctionType.Income:
                return CalculateIncome(baseIncome, incomeGrowthRate, level);

            case BuildingFunctionType.Mixed:
                return CalculateIncome(mixedBaseIncome, mixedIncomeGrowthRate, level);

            default:
                return 0;
        }
    }

        // 
        public int GetIncomeAmountByTurns(int turns)
        {
            if (!enableIncomeGrowth)
            {
                // 
                switch (functionType)
                {
                    case BuildingFunctionType.Income:
                        return baseIncome;
                    case BuildingFunctionType.Mixed:
                        return mixedBaseIncome;
                    default:
                        return 0;
                }
            }

            // base income * Income Growth Rate * ( - 1)
            switch (functionType)
            {
                case BuildingFunctionType.Income:
                    int calculatedIncome = Mathf.RoundToInt(baseIncome * incomeGrowthRate * (turns - 1));
                    return Mathf.Max(calculatedIncome, baseIncome);

                case BuildingFunctionType.Mixed:
                    int calculatedMixedIncome = Mathf.RoundToInt(mixedBaseIncome * mixedIncomeGrowthRate * (turns - 1));
                    return Mathf.Max(calculatedMixedIncome, mixedBaseIncome);

                default:
                    return 0;
            }
        }

    public List<BuildingBuffConfig> GetBuffConfigs()
    {
        if (buffConfigs.Count > 0)
        {
            return buffConfigs;
        }
        
        List<BuildingBuffConfig> configs = new List<BuildingBuffConfig>();
        if (functionType == BuildingFunctionType.Buff || functionType == BuildingFunctionType.Mixed)
        {
            configs.Add(new BuildingBuffConfig
            {
                effectType = buffEffect,
                baseValue = baseBuffValue,
                growthRate = buffGrowthRate,
                duration = buffDuration,
                isPermanent = buffDuration <= 0f
            });
        }
        return configs;
    }

    public float GetBuffValue(int level, BuildingBuffConfig config)
    {
        return config.baseValue * Mathf.Pow(config.growthRate, level - 1);
    }

    // Buff()
    public float GetBuffValue(int level)
    {
        if (functionType == BuildingFunctionType.Buff || functionType == BuildingFunctionType.Mixed)
        {
            return baseBuffValue * Mathf.Pow(buffGrowthRate, level - 1);
        }
        return 0f;
    }

    // 
    private int CalculateIncome(int baseAmount, float growthRate, int level)
    {
        if (level <= 1) return baseAmount;
        return Mathf.RoundToInt(baseAmount * Mathf.Pow(growthRate, level - 1));
    }

    // 
    public bool CheckTileScale(int tileScale)
    {
        return tileScale >= minTileScale && tileScale <= maxTileScale;
    }

    // 
    public string GetDescription(int level = 1)
    {
        string desc = $"{buildingName}\n";
        desc += $": {purchasePrice}\n";
        desc += $"??: {minTileScale}-{maxTileScale}\n";

        switch (functionType)
        {
            case BuildingFunctionType.Income:
                desc += $":  {GetIncomeAmount(1)} \n";
                if (level > 1)
                {
                    desc += $"({level}): {GetIncomeAmount(level)} ";
                }
                break;

            case BuildingFunctionType.Buff:
                desc += $":  Buff ??\n";
                List<BuildingBuffConfig> configs = GetBuffConfigs();
                foreach (var config in configs)
                {
                    float value = GetBuffValue(level, config);
                    desc += $"- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%\n";
                    if (config.isPermanent)
                    {
                        desc += "  (??)\n";
                    }
                    else if (config.durationRounds > 0)
                    {
                        desc += $"   {config.durationRounds} \n";
                    }
                    else if (config.duration > 0)
                    {
                        desc += $"   {config.duration:F1} \n";
                    }
                }
                break;

            case BuildingFunctionType.Mixed:
                desc += $": (+Buff)\n";
                desc += $": {GetIncomeAmount(1)} \n";
                List<BuildingBuffConfig> mixedConfigs = GetBuffConfigs();
                foreach (var config in mixedConfigs)
                {
                    float value = GetBuffValue(level, config);
                    desc += $"- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%";
                }
                break;

            case BuildingFunctionType.DiceEven:
                desc += $": 骰子触发\n";
                desc += $": {GetDiceRuleDescription()}\n";
                break;

            case BuildingFunctionType.Appreciation:
                desc += $": 增值\n";
                desc += $": 每持有1圈, 估值+{appreciationPerRound}\n";
                break;
        }

        if (!string.IsNullOrEmpty(description))
        {
            desc += $"\n\n{description}";
        }

        return desc;
    }

    // Buff??
    public static string GetBuffEffectName(BuffEffect effect)
    {
        switch (effect)
        {
            case BuffEffect.MoveSpeedBoost: return "";
            case BuffEffect.DiceBoost: return "";
            case BuffEffect.IncomeMultiplier: return "";
            case BuffEffect.DefenseBoost: return "";
            case BuffEffect.LuckBoost: return "";
            case BuffEffect.AllIncomeBoost: return "";
            default: return "??Buff??";
        }
    }
}
