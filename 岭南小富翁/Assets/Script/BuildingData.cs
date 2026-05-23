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
    }

    [Header("建筑类型")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None;

    [Header("等级系统")]
    public bool isFinalLevel = false;
    public int buildingLevel = 1;

    [Header("Buff数值数组")]
    public float[] buffValues;

    [Header("建筑基本信息")]
    public string buildingName = "建筑";
    public int purchasePrice = 100;
    public int upgradePrice = 50;
    public int minTileScale = 1;
    public int maxTileScale = 4;
    public Scale requiredScale = Scale.Small;

    [Header("建筑功能")]
    public BuildingFunctionType functionType = BuildingFunctionType.Income;

    [Header("收入功能参数")]
    public int baseIncome = 10;
    public float incomeGrowthRate = 1.2f;
    public bool enableIncomeGrowth = false;

    [Header("Buff功能参数")]
    [Tooltip("建筑提供的 Buff 效果列表")]
    public List<BuildingBuffConfig> buffConfigs = new List<BuildingBuffConfig>();
    
    [Header("(旧版兼容) Buff功能参数")]
    public BuffEffect buffEffect = BuffEffect.IncomeMultiplier;
    public float baseBuffValue = 0.1f;
    public float buffGrowthRate = 1.1f;
    public float buffDuration = 10f;

    [Header("混合功能参数")]
    public int mixedBaseIncome = 5;
    public float mixedIncomeGrowthRate = 1.1f;

    [Header("双数奖励参数")]
    [Tooltip("掷骰子为偶数(2,4,6)时获得的奖金")]
    public int diceEvenReward = 20;

    [Header("视觉")]
    public Sprite buildingIcon;
    public GameObject buildingPrefab;
    public BuildingData nextLevelBuilding;

    [Header("效果动画与音效")]
    public GameObject effectIconPrefab;
    public AudioClip effectSound;
    public float effectDuration = 1.5f;

    [Header("描述")]
    [TextArea(3, 5)]
    public string description = "建筑描述";

    // 规模枚举
    public enum Scale
    {
        Small = 1,
        Medium = 2,
        Large = 3,
        ExtraLarge = 4
    }

    // 建筑功能类型
    public enum BuildingFunctionType
    {
        Income,
        Buff,
        Mixed,
        DiceEven  // 双数奖励：掷骰子为偶数时获得奖金
    }

    // Buff效果枚举
    public enum BuffEffect
    {
        MoveSpeedBoost,
        DiceBoost,
        IncomeMultiplier,
        DefenseBoost,
        LuckBoost,
        AllIncomeBoost
    }

    // 获取收入金额（按建筑等级）
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

        // 获取收入金额（按拥有回合数）
    public int GetIncomeAmountByTurns(int turns)
    {
        if (!enableIncomeGrowth)
        {
            // 不启用成长，直接返回基础收入
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

        // 启用成长，按回合数计算：base income * Income Growth Rate * (回合数 - 1)
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

    // 获取Buff值（旧版兼容）
    public float GetBuffValue(int level)
    {
        if (functionType == BuildingFunctionType.Buff || functionType == BuildingFunctionType.Mixed)
        {
            return baseBuffValue * Mathf.Pow(buffGrowthRate, level - 1);
        }
        return 0f;
    }

    // 计算收入
    private int CalculateIncome(int baseAmount, float growthRate, int level)
    {
        if (level <= 1) return baseAmount;
        return Mathf.RoundToInt(baseAmount * Mathf.Pow(growthRate, level - 1));
    }

    // 检查地块规模是否匹配
    public bool CheckTileScale(int tileScale)
    {
        return tileScale >= minTileScale && tileScale <= maxTileScale;
    }

    // 获取建筑描述
    public string GetDescription(int level = 1)
    {
        string desc = $"{buildingName}\n";
        desc += $"价格: {purchasePrice}金币\n";
        desc += $"规模要求: {minTileScale}-{maxTileScale}\n";

        switch (functionType)
        {
            case BuildingFunctionType.Income:
                desc += $"功能: 每回合收入 {GetIncomeAmount(1)} 金币\n";
                if (level > 1)
                {
                    desc += $"当前等级({level})收入: {GetIncomeAmount(level)} 金币";
                }
                break;

            case BuildingFunctionType.Buff:
                desc += $"功能: 提供 Buff 加成\n";
                List<BuildingBuffConfig> configs = GetBuffConfigs();
                foreach (var config in configs)
                {
                    float value = GetBuffValue(level, config);
                    desc += $"- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%\n";
                    if (config.isPermanent)
                    {
                        desc += "  (永久)\n";
                    }
                    else if (config.durationRounds > 0)
                    {
                        desc += $"  持续 {config.durationRounds} 回合\n";
                    }
                    else if (config.duration > 0)
                    {
                        desc += $"  持续 {config.duration:F1} 秒\n";
                    }
                }
                break;

            case BuildingFunctionType.Mixed:
                desc += $"功能: 混合(收入+Buff)\n";
                desc += $"收入: {GetIncomeAmount(1)} 金币\n";
                List<BuildingBuffConfig> mixedConfigs = GetBuffConfigs();
                foreach (var config in mixedConfigs)
                {
                    float value = GetBuffValue(level, config);
                    desc += $"- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%";
                }
                break;

            case BuildingFunctionType.DiceEven:
                desc += $"功能: 双数奖励\n";
                desc += $"偶数骰子奖励: {diceEvenReward} 金币";
                break;
        }

        if (!string.IsNullOrEmpty(description))
        {
            desc += $"\n\n{description}";
        }

        return desc;
    }

    // 获取Buff效果名称
    public static string GetBuffEffectName(BuffEffect effect)
    {
        switch (effect)
        {
            case BuffEffect.MoveSpeedBoost: return "移动速度";
            case BuffEffect.DiceBoost: return "骰子加成";
            case BuffEffect.IncomeMultiplier: return "收入倍率";
            case BuffEffect.DefenseBoost: return "防御加成";
            case BuffEffect.LuckBoost: return "幸运加成";
            case BuffEffect.AllIncomeBoost: return "全收入加成";
            default: return "未知效果";
        }
    }
}
