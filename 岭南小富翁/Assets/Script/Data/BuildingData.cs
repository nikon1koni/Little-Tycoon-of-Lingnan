﻿﻿﻿﻿﻿﻿﻿using UnityEngine;
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

    [Header("建筑类型")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None;

    [Header("升级信息")]
    public bool isFinalLevel = false;
    public int buildingLevel = 1;

    [Header("Buff数值")]
    public float[] buffValues;

    [Header("基本信息")]
    public string buildingName = "";
    public int purchasePrice = 100;
    public int upgradePrice = 50;
    public int minTileScale = 1;
    public int maxTileScale = 4;
    public Scale requiredScale = Scale.Small;

    [Header("功能类型")]
    public BuildingFunctionType functionType = BuildingFunctionType.Income;

    [Header("收入属性")]
    public int baseIncome = 10;
    public float incomeGrowthRate = 1.2f;
    public bool enableIncomeGrowth = false;

    [Header("Buff配置")]
    [Tooltip("建筑提供的Buff配置列表")]
    public List<BuildingBuffConfig> buffConfigs = new List<BuildingBuffConfig>();
    
    [Header("（旧版）Buff属性")]
    public BuffEffect buffEffect = BuffEffect.IncomeMultiplier;
    public float baseBuffValue = 0.1f;
    public float buffGrowthRate = 1.1f;
    public float buffDuration = 10f;

    [Header("混合收入属性")]
    public int mixedBaseIncome = 5;
    public float mixedIncomeGrowthRate = 1.1f;

    [Header("骰子奖励系统")]
    [Tooltip("满足条件时发放奖励的骰子点数，数组为空=所有点数1~6都算，例如[2,4,6]就是偶数，[1,6]就是最小最大")]
    public int[] targetDiceValues = new int[] { 2, 4, 6 };
    [Tooltip("发放奖励的方式是 固定值 还是 点数×倍数")]
    public DiceRewardMode diceRewardMode = DiceRewardMode.FixedValue;
    [Tooltip("如果是固定值方式，每次给多少")]
    public int diceFixedReward = 20;
    [Tooltip("如果是点数×倍数，乘多少倍")]
    public float diceMultiplier = 5f;

    // 奖励方式枚举
    public enum DiceRewardMode
    {
        FixedValue,     // 固定数值
        DiceMultiplier  // 点数 × 倍数
    }

    /// <summary> 根据骰子点数算这次奖励多少，不满足条件返回0 </summary>
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

    /// <summary> 检查骰子点数是否在目标数组中 </summary>
    public bool IsDiceValueMatch(int diceValue)
    {
        if (targetDiceValues == null || targetDiceValues.Length == 0)
            return true; // 数组为空=所有点数都算满足条件
        for (int i = 0; i < targetDiceValues.Length; i++)
        {
            if (targetDiceValues[i] == diceValue) return true;
        }
        return false;
    }

    /// <summary> 返回骰子规则的描述文本 </summary>
    public string GetDiceRuleDescription()
    {
        string targetDesc;
        if (targetDiceValues == null || targetDiceValues.Length == 0)
            targetDesc = "所有点数";
        else
            targetDesc = string.Join(",", targetDiceValues);

        switch (diceRewardMode)
        {
            case DiceRewardMode.FixedValue:
                return $"骰子点数 {targetDesc} 获得 {diceFixedReward}";
            case DiceRewardMode.DiceMultiplier:
                return $"骰子点数 {targetDesc} 获得 点数×{diceMultiplier}({diceMultiplier}~{diceMultiplier * 6})";
            default:
                return "";
        }
    }

    [Header("房产增值系统")]
    [Tooltip("房产每持有1回合，出售时增值多少")]
    public int appreciationPerRound = 0;

    // 计算房产增值后的价值 = 买入价 + 持有回合数 × 每回合增值
    public int GetAppreciatedValue(int roundsOwned)
    {
        return purchasePrice + (roundsOwned * appreciationPerRound);
    }

    // --- 建筑预制体和图标 ---
    public Sprite buildingIcon;
    public GameObject buildingPrefab;
    public BuildingData nextLevelBuilding;

    [Header("位置与旋转偏移")]
    [Tooltip("建筑放在地块上的位置偏移，修正对齐问题")]
    public Vector3 positionOffset = new Vector3(0, 0.5f, 0);
    [Tooltip("建筑放在地块上的旋转，使用Euler角度")]
    public Vector3 rotationEuler = Vector3.zero;

    [Header("视觉效果")]
    public GameObject effectIconPrefab;
    public AudioClip effectSound;
    public float effectDuration = 1.5f;

    [Header("描述")]
    [TextArea(3, 5)]
    public string description = "";

    // 建筑规模
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
        DiceEven,  // 骰子偶数奖励
        Appreciation  // 房产会随着持有时间增值
    }

    // Buff效果枚举
    public enum BuffEffect
    {
        MoveSpeedBoost,
        DiceBoost,
        IncomeMultiplier,
        DefenseBoost,
        LuckBoost,
        AllIncomeBoost,
        Bankrupt,
        IncomeReduction,
        TaxReduction,
        Immune,
        NextRollMultiplier
    }

    // 获取收入金额
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

        // 根据回合数获取收入金额
        public int GetIncomeAmountByTurns(int turns)
        {
            if (!enableIncomeGrowth)
            {
                // 不启用增长
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

            // base income * Income Growth Rate * (回合数 - 1)
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

    // 获取Buff值(旧版)
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

    // 检查地块规模
    public bool CheckTileScale(int tileScale)
    {
        return tileScale >= minTileScale && tileScale <= maxTileScale;
    }

    // 获取描述
    public string GetDescription(int level = 1)
    {
        string desc = $"{buildingName}\n";
        desc += $"价格: {purchasePrice}\n";
        desc += $"规模: {minTileScale}-{maxTileScale}\n";

        switch (functionType)
        {
            case BuildingFunctionType.Income:
                desc += $"功能:  每回合 {GetIncomeAmount(1)} \n";
                if (level > 1)
                {
                    desc += $"(当前{level}级): {GetIncomeAmount(level)} ";
                }
                break;

            case BuildingFunctionType.Buff:
                desc += $"功能:  提供 Buff\n";
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
                        desc += $"   持续 {config.durationRounds} 回合\n";
                    }
                    else if (config.duration > 0)
                    {
                        desc += $"   持续 {config.duration:F1} 秒\n";
                    }
                }
                break;

            case BuildingFunctionType.Mixed:
                desc += $"功能: 收入(+Buff)\n";
                desc += $"收入: {GetIncomeAmount(1)} \n";
                List<BuildingBuffConfig> mixedConfigs = GetBuffConfigs();
                foreach (var config in mixedConfigs)
                {
                    float value = GetBuffValue(level, config);
                    desc += $"- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%";
                }
                break;

            case BuildingFunctionType.DiceEven:
                desc += $"功能: 骰子点数奖励\n";
                desc += $"规则: {GetDiceRuleDescription()}\n";
                break;

            case BuildingFunctionType.Appreciation:
                desc += $"功能: 房产增值\n";
                desc += $"规则: 每持有1回合，出售时+{appreciationPerRound}\n";
                break;
        }

        if (!string.IsNullOrEmpty(description))
        {
            desc += $"\n\n{description}";
        }

        return desc;
    }

    /// <summary> 悬停提示用：只含规模与功能信息（名称、价格已显示在面板上） </summary>
    public string GetTooltipDescription(int level = 1)
    {
        string desc = $"规模: {minTileScale}-{maxTileScale}\n";

        switch (functionType)
        {
            case BuildingFunctionType.Income:
                desc += $"功能: 每回合收入 {GetIncomeAmount(1)}";
                if (level > 1)
                {
                    desc += $"\n当前{level}级: {GetIncomeAmount(level)}";
                }
                break;

            case BuildingFunctionType.Buff:
                desc += "功能: 提供 Buff";
                foreach (var config in GetBuffConfigs())
                {
                    float value = GetBuffValue(level, config);
                    desc += $"\n- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%";
                    if (config.isPermanent)
                        desc += " (永久)";
                    else if (config.durationRounds > 0)
                        desc += $" 持续{config.durationRounds}回合";
                    else if (config.duration > 0)
                        desc += $" 持续{config.duration:F1}秒";
                }
                break;

            case BuildingFunctionType.Mixed:
                desc += $"功能: 收入+Buff\n收入: {GetIncomeAmount(1)}";
                foreach (var config in GetBuffConfigs())
                {
                    float value = GetBuffValue(level, config);
                    desc += $"\n- {GetBuffEffectName(config.effectType)}: +{value * 100:F1}%";
                }
                break;

            case BuildingFunctionType.DiceEven:
                desc += $"功能: 骰子点数奖励\n{GetDiceRuleDescription()}";
                break;

            case BuildingFunctionType.Appreciation:
                desc += $"功能: 房产增值\n每持有1回合，出售时+{appreciationPerRound}";
                break;
        }

        return desc;
    }

    // 获取Buff效果名称
    public static string GetBuffEffectName(BuffEffect effect)
    {
        switch (effect)
        {
            case BuffEffect.MoveSpeedBoost: return "移动速度提升";
            case BuffEffect.DiceBoost: return "骰子点数提升";
            case BuffEffect.IncomeMultiplier: return "收入提升";
            case BuffEffect.DefenseBoost: return "防御力提升";
            case BuffEffect.LuckBoost: return "运气提升";
            case BuffEffect.AllIncomeBoost: return "全玩家收入提升";
            case BuffEffect.Bankrupt: return "破产";
            case BuffEffect.IncomeReduction: return "收入下降";
            case BuffEffect.TaxReduction: return "税务减免";
            case BuffEffect.Immune: return "免疫负面事件";
            case BuffEffect.NextRollMultiplier: return "步数倍率";
            default: return "未知Buff效果";
        }
    }
}