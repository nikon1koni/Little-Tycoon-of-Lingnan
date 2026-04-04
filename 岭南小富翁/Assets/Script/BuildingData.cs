using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "BoardGame/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("基础信息")]
    public string buildingName = "未命名建筑";
    public string buildingDescription = "建筑描述";
    public Sprite buildingIcon;
    public GameObject buildingPrefab; // 3D模型预制体
    public GameObject[] upgradePrefabs; // 升级后的建筑预制体数组

    [Header("规模限制")]
    public BuildingScale requiredScale = BuildingScale.Small;
    public int minTileScale = 1; // 需要的最小格子规模
    public int maxTileScale = 3; // 需要的最大格子规模

    [Header("经济属性")]
    public int purchasePrice = 100; // 购买价格
    public int[] upgradeCosts = new int[] { 100, 200 }; // 各级升级价格
    public int maxLevel = 3; // 最大等级

    [Header("功能类型")]
    public BuildingFunctionType functionType = BuildingFunctionType.Income;
    public float buffDuration = 0f; // buff持续时间（秒），0表示永久

    [Header("收益属性")]
    public bool providesIncome = true; // 是否提供收入
    public int[] incomeAmounts = new int[] { 10, 20, 40 }; // 各级收入
    public float incomeInterval = 1.0f; // 收入间隔（秒）

    [Header("Buff属性")]
    public BuffEffect buffEffect = BuffEffect.None;
    public float[] buffValues = new float[] { 0.1f, 0.2f, 0.3f }; // 各级buff数值

    [Header("视觉效果")]
    public Color highlightColor = Color.green;
    public Color normalColor = Color.white;

    [Header("建筑链")]
    public BuildingData nextLevelBuilding; // 下一级建筑（小->中->大）
    public bool isFinalLevel = false; // 是否为最终等级

    public enum BuildingScale
    {
        Small = 1,    // 小建筑
        Medium = 2,   // 中建筑
        Large = 3     // 大建筑
    }

    public enum BuildingFunctionType
    {
        Income,       // 提供资金
        Buff,         // 提供buff
        Mixed         // 混合功能
    }

    public enum BuffEffect
    {
        None,
        MoveSpeedBoost,     // 移动速度加成
        DiceBoost,          // 骰子点数加成
        IncomeMultiplier,   // 收入倍数加成
        DefenseBoost,       // 防御加成（减少被攻击损失）
        LuckBoost,          // 幸运加成（抽卡好运）
        AllIncomeBoost      // 所有建筑收入加成
    }

    // 获取指定等级的buff数值
    public float GetBuffValue(int level)
    {
        if (level < 0 || level >= buffValues.Length)
            return 0;
        return buffValues[level];
    }

    // 获取指定等级的收入
    public int GetIncomeAmount(int level)
    {
        if (level < 0 || level >= incomeAmounts.Length)
            return 0;
        return incomeAmounts[level];
    }

    // 获取指定等级的价格
    public int GetUpgradeCost(int targetLevel)
    {
        if (targetLevel < 1 || targetLevel > upgradeCosts.Length)
            return 0;
        return upgradeCosts[targetLevel - 1];
    }

    // 检查是否可以升级到下一个等级
    public bool CanUpgradeToNextLevel()
    {
        return nextLevelBuilding != null && !isFinalLevel;
    }
}