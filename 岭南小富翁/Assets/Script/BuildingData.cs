using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("等级系统")]
    public bool isFinalLevel = false; // 是否为最终等级

    [Header("Buff数值数组")]
    public float[] buffValues; // Buff数值数组，用于不同等级的Buff值

    [Header("建筑基本信息")]
    public string buildingName = "建筑";
    public int purchasePrice = 100;
    public int upgradePrice = 50;
    public int minTileScale = 1;
    public int maxTileScale = 4;
    public Scale requiredScale = Scale.Small;

    [Header("建筑功能")]
    public BuildingFunctionType functionType = BuildingFunctionType.Income;

    [Header("收入功能设置")]
    public int baseIncome = 10; // 基础收入
    public float incomeGrowthRate = 1.2f; // 每级收入增长率

    [Header("Buff功能设置")]
    public BuffEffect buffEffect = BuffEffect.IncomeMultiplier;
    public float baseBuffValue = 0.1f;
    public float buffGrowthRate = 1.1f;
    public float buffDuration = 10f; // Buff持续时间（秒）

    [Header("混合功能设置")]
    public int mixedBaseIncome = 5; // 混合功能的收入
    public float mixedIncomeGrowthRate = 1.1f;

    [Header("视觉")]
    public Sprite buildingIcon;
    public GameObject buildingPrefab;
    public BuildingData nextLevelBuilding;

    [Header("描述")]
    [TextArea(3, 5)]
    public string description = "建筑描述";

    // 建筑规模枚举
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
        Income,     // 收入
        Buff,       // 增益效果
        Mixed       // 混合
    }

    // Buff效果枚举
    public enum BuffEffect
    {
        MoveSpeedBoost,     // 移动速度
        DiceBoost,          // 骰子加成
        IncomeMultiplier,   // 收入倍率
        DefenseBoost,       // 防御加成
        LuckBoost,          // 幸运加成
        AllIncomeBoost      // 全收入加成
    }

    // 获取建筑收入
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

    // 获取Buff数值
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
                desc += $"功能: 每级产生 {GetIncomeAmount(1)} 金币收入\n";
                if (level > 1)
                {
                    desc += $"当前等级({level})收入: {GetIncomeAmount(level)} 金币";
                }
                break;

            case BuildingFunctionType.Buff:
                desc += $"功能: 提供 {GetBuffEffectName(buffEffect)} 加成\n";
                desc += $"基础值: {baseBuffValue * 100}%\n";
                if (buffDuration > 0)
                {
                    desc += $"持续时间: {buffDuration}秒";
                }
                break;

            case BuildingFunctionType.Mixed:
                desc += $"功能: 混合(收入+Buff)\n";
                desc += $"收入: {GetIncomeAmount(1)} 金币\n";
                desc += $"Buff: {GetBuffEffectName(buffEffect)} {baseBuffValue * 100}%";
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