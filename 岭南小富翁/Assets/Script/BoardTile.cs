using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static BuildingData;

public class BoardTile : MonoBehaviour
{
    [Header("地块信息")]
    public int tileID;
    public string tileName = "未命名地块";
    public TileType tileType = TileType.Property;

    [Header("地产信息")]
    public int propertyPrice = 100;
    public Color propertyColor = Color.white;
    [HideInInspector] public Player ownerPlayer;  // 用于存储Player的引用

    [Header("事件")]
    public UnityEvent onPlayerLanded;

    [Header("建筑系统")]
    public bool isBuildable = false;
    public GameObject currentBuilding = null;  // 当前建筑模型
    public List<GameObject> availableBuildings = new List<GameObject>(); // 可选择的建筑预制体

    [Header("规模系统")]
    public int tileScale = 1; // 1:小建筑, 2:中建筑, 3:大建筑
    public BuildingScale allowedScale = BuildingScale.Small;

    [Header("当前建筑信息")]
    public BuildingType currentBuildingType = BuildingType.None;
    public int buildingLevel = 0; // 建筑等级

    [Header("升级系统")]
    public bool canBeUpgraded = true; // 是否可升级
    public int maxUpgradeLevel = 3; // 最大可升级等级
    public float upgradeCheckInterval = 5.0f; // 升级检查间隔

    [Header("建筑链系统")]
    public BuildingData currentBuildingData; // 当前建筑的配置数据
    public BuildingData nextBuildingData;    // 可升级到的下一级建筑

    [Header("Buff效果")]
    public Player currentBuffOwner;          // buff拥有者
    public float buffExpireTime = 0f;        // buff过期时间
    public float currentBuffValue = 0f;      // 当前buff数值
    public BuildingData.BuffEffect currentBuffEffect = BuildingData.BuffEffect.None;

    // 建筑规模枚举
    public enum BuildingScale
    {
        Small = 1,    // 小建筑
        Medium = 2,   // 中建筑
        Large = 3     // 大建筑
    }

    // 建筑类型枚举
    public enum BuildingType
    {
        None,
        SmallHouse,    // 小房屋
        MediumHouse,   // 中房屋
        LargeHouse,    // 大房屋
        SmallShop,     // 小商店
        MediumShop,    // 中商店
        LargeShop,     // 大商店
    }

    // 地块类型枚举
    public enum TileType
    {
        Start,
        Property,
        Railroad,
        Utility,
        Event,
        CommunityChest,
        Tax,
        Jail,
        GoToJail,
        FreeParking,
        Normal,
        Buildable,         // 可建造的地块，不一定是地产
        BuildingSite       // 已放置建筑的地块
    }

    void Start()
    {
        //UpdateTileVisual(); // 更新地块颜色
    }

    void Update()
    {
        // 检查buff过期
        if (currentBuffOwner != null && buffExpireTime > 0 && Time.time > buffExpireTime)
        {
            RemoveBuffFromPlayer();
        }
    }

    // 更新地块视觉表现
    public void UpdateTileVisual()
    {
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            switch (tileType)
            {
                case TileType.Start:
                    renderer.material.color = Color.green;
                    break;
                case TileType.Property:
                    renderer.material.color = propertyColor;
                    break;
                case TileType.Event:
                    renderer.material.color = Color.yellow;
                    break;
                case TileType.CommunityChest:
                    renderer.material.color = new Color(0.5f, 0.8f, 1f); // 浅蓝色
                    break;
                case TileType.Tax:
                    renderer.material.color = Color.red;
                    break;
                case TileType.Jail:
                    renderer.material.color = new Color(0.3f, 0.3f, 0.3f); // 深灰色
                    break;
                case TileType.GoToJail:
                    renderer.material.color = new Color(0.5f, 0f, 0f); // 深红色
                    break;
                case TileType.FreeParking:
                    renderer.material.color = Color.white;
                    break;
                case TileType.Normal:
                    renderer.material.color = Color.gray;  // 灰色表示普通地块
                    break;
                case TileType.Buildable:
                    // 根据规模显示不同颜色
                    UpdateBuildableTileColor();
                    break;
                case TileType.BuildingSite:
                    // 建筑地块颜色
                    renderer.material.color = new Color(0.8f, 0.8f, 0.3f); // 土黄色
                    break;
                default:
                    renderer.material.color = Color.white;
                    break;
            }
        }
    }
    public void ForceUpdateVisual()
    {
        UpdateTileVisual();
    }

    // 更新可建造地块颜色（根据规模）
    void UpdateBuildableTileColor()
    {
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            switch (tileScale)
            {
                case 1: // 小规模 - 浅蓝色
                    renderer.material.color = new Color(0.7f, 0.9f, 1f);
                    break;
                case 2: // 中规模 - 浅绿色
                    renderer.material.color = new Color(0.7f, 1f, 0.7f);
                    break;
                case 3: // 大规模 - 浅黄色
                    renderer.material.color = new Color(1f, 1f, 0.7f);
                    break;
                default:
                    renderer.material.color = new Color(0.8f, 0.8f, 0.8f); // 默认灰色
                    break;
            }
        }
    }

    // 玩家停留时
    public void OnLanded(Player player)
    {
        if (player == null)
        {
            Debug.LogError("Player为null");
            return;
        }

        Debug.Log($"玩家 {player.playerName} 停留在 {tileName} 地块 (类型: {tileType})");

        // 触发UnityEvent（可以在Inspector中绑定）
        onPlayerLanded?.Invoke();

        // 执行地块对应逻辑
        switch (tileType)
        {
            case TileType.Property:
            case TileType.Railroad:
            case TileType.Utility:
                HandlePropertyLanding(player);
                break;

            case TileType.Event:
                DrawChanceCard(player);
                break;

            case TileType.CommunityChest:
                DrawCommunityChestCard(player);
                break;

            case TileType.Tax:
                HandleTaxTile(player);
                break;

            case TileType.GoToJail:
                SendToJail(player);
                break;

            case TileType.Start:
                HandleStartTile(player);
                break;

            case TileType.FreeParking:
                HandleFreeParking(player);
                break;

            case TileType.Normal:  // 普通地块不执行任何特殊操作
                Debug.Log($"{player.playerName} 停留在普通地块 [{tileName}]");
                break;

            //case TileType.Buildable:  // 可建造地块
            //    HandleBuildableTileLanding(player);
            //    break;

            case TileType.BuildingSite:  // 建筑地块
                HandleBuildingSiteLanding(player);
                break;
        }
    }

    // 处理可建造地块停留
    void HandleBuildableTileLanding(Player player)
    {
        Debug.Log($"{player.playerName} 停留在可建造地块 [{tileName}]，规模: {tileScale}");

        if (ownerPlayer == null)
        {
            // 1. 可建造地块 -> 显示购买/建造选项
            Debug.Log($"可建造地块 [{tileName}] 可购买，价格: {propertyPrice} 元");

            // 延迟显示建筑选择UI
            StartCoroutine(DelayedBuildingSelection(player));
        }
        else if (ownerPlayer == player)
        {
            Debug.Log($" {player.playerName} 已拥有此可建造地块 [{tileName}]");

            // 如果已经有建筑，检查是否可以升级
            if (currentBuildingData != null)
            {
                if (CanUpgradeBuilding(player))
                {
                    ShowUpgradeOption(player);
                }
                else
                {
                    // 应用建筑功能
                    ApplyBuildingFunction(player);
                }
            }
        }
        else
        {
            Debug.Log($"{player.playerName} 经过 {ownerPlayer.playerName} 拥有的地块 [{tileName}]");
        }
    }

    // 处理建筑地块停留
    void HandleBuildingSiteLanding(Player player)
    {
        Debug.Log($"{player.playerName} 停留在建筑地块 [{tileName}]，建筑: {currentBuildingData?.buildingName ?? "无"} 等级: {buildingLevel}");

        if (ownerPlayer == player)
        {
            // 玩家自己的建筑，检查是否可以升级
            if (currentBuildingData != null)
            {
                if (CanUpgradeBuilding(player))
                {
                    ShowUpgradeOption(player);
                }
                else
                {
                    // 应用建筑功能
                    ApplyBuildingFunction(player);
                }
            }
        }
        else if (ownerPlayer != null)
        {
            Debug.Log($"{player.playerName} 经过 {ownerPlayer.playerName} 的建筑 [{tileName}]");
        }
    }

    // 延迟显示建筑选择
    IEnumerator DelayedBuildingSelection(Player player)
    {
        // 等待一小段时间让骰子动画完成
        yield return new WaitForSeconds(0.5f);

        // 显示建筑选择UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBuildingSelectionUI(this, player);
        }
    }

    // 显示升级选项
    void ShowUpgradeOption(Player player)
    {
        StartCoroutine(DelayedUpgradeSelection(player));
    }

    IEnumerator DelayedUpgradeSelection(Player player)
    {
        yield return new WaitForSeconds(0.5f);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBuildingUpgradeUI(this, player);
        }
    }

    // 处理地产地块停留
    void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 可以购买
            Debug.Log($"可购买地产 [{tileName}] 可购买。价格: {propertyPrice} 元");

            // 显示UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // 玩家自己的地产
            Debug.Log($" {player.playerName} 拥有自己的地产 [{tileName}]");
        }
        else
        {
            // 其他玩家的地产 - 单机游戏无租金
            Debug.Log($"{player.playerName} 经过 {ownerPlayer.playerName} 拥有的地块 [{tileName}]");
        }
    }

    // 抽机会卡
    void DrawChanceCard(Player player)
    {
        Debug.Log($"{player.playerName} 抽取机会卡");
        // 实现抽卡逻辑
        // ChanceCardManager.Instance.DrawChanceCard(player);
    }

    // 抽社区宝箱卡
    void DrawCommunityChestCard(Player player)
    {
        Debug.Log($"{player.playerName} 抽取社区宝箱卡");
        // 实现抽卡逻辑
        // ChanceCardManager.Instance.DrawCommunityChestCard(player);
    }

    // 处理税务地块
    void HandleTaxTile(Player player)
    {
        int taxAmount = 200; // 示例税额
        Debug.Log($"{player.playerName} 需要缴税 {taxAmount} 元");

        if (player.PayCash(taxAmount))
        {
            Debug.Log("缴税成功");
        }
        else
        {
            Debug.LogWarning("缴税失败，资金不足");
        }
    }

    // 送进监狱
    void SendToJail(Player player)
    {
        Debug.Log($"{player.playerName} 被送进监狱");
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        // 移动到监狱地块
        BoardTile jailTile = FindJailTile();
        if (jailTile != null)
        {
            player.MoveToTile(jailTile, true);
        }
    }

    // 查找监狱地块
    BoardTile FindJailTile()
    {
        if (BoardManager.Instance == null) return null;

        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            if (tile.tileType == TileType.Jail)
            {
                return tile;
            }
        }
        return null;
    }

    // 处理起点地块
    void HandleStartTile(Player player)
    {
        int salary = 200; // 固定薪水
        Debug.Log($"{player.playerName} 经过起点，获得薪水 {salary} 元");
        player.ReceiveCash(salary);

        // 游戏开始时的购买逻辑
        if (GameManager.Instance != null)
        {
            // 延迟一帧执行，确保逻辑正确
            GameManager.Instance.StartCoroutine(DelayedStartTilePurchase(player));
        }
    }

    // 延迟起点购买
    IEnumerator DelayedStartTilePurchase(Player player)
    {
        yield return new WaitForSeconds(0.5f); // 延迟显示，让薪水信息先显示

        // 修改状态
        GameManager.Instance.currentState = GameManager.GameState.BuildingSelection;
        GameManager.Instance.isPlayerTurn = false;

        // 显示建筑选择
        if (UIManager.Instance != null)
        {
            // 创建一个临时的Tile用于购买
            BoardTile startShopTile = CreateStartShopTile();
            UIManager.Instance.ShowBuildingSelectionUI(startShopTile, player);
        }
    }

    // 创建临时的起点商店Tile
    BoardTile CreateStartShopTile()
    {
        GameObject tempObj = new GameObject("StartShopTile_FromBoardTile");
        BoardTile tile = tempObj.AddComponent<BoardTile>();
        tile.tileName = "建筑地块"; // 修改：不要使用"起点商店"
        tile.tileType = TileType.Buildable; // 修改：确保是可建造类型
        tile.propertyPrice = 100;
        tile.isBuildable = true;
        tile.tileScale = Random.Range(1, 4);

        // 新增：设置所有者，防止高亮
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            tile.ownerPlayer = GameManager.Instance.currentPlayer;
        }

        return tile;
    }

    // 处理免费停车
    void HandleFreeParking(Player player)
    {
        Debug.Log($"{player.playerName} 停在免费停车，休息一回合");
        // 实现停车奖金累积逻辑
    }

    // ================ 建筑升级链系统 ================

    // 获取可升级到的建筑
    public BuildingData GetNextUpgradeBuilding()
    {
        if (currentBuildingData == null) return null;
        return currentBuildingData.nextLevelBuilding;
    }

    // 检查是否可以升级
    public bool CanUpgradeBuilding(Player player)
    {
        if (!canBeUpgraded) return false;
        if (currentBuildingData == null) return false;
        if (buildingLevel >= currentBuildingData.maxLevel) return false;
        if (ownerPlayer != player) return false;

        // 检查是否有下一级建筑
        BuildingData nextBuilding = GetNextUpgradeBuilding();
        if (nextBuilding == null) return false;

        // 检查地块规模是否支持下一级建筑
        if (!CheckScaleForUpgrade(nextBuilding.requiredScale)) return false;

        // 检查资金
        int upgradeCost = GetUpgradeCost();
        if (upgradeCost <= 0 || player.cash < upgradeCost) return false;

        return true;
    }

    // 检查规模是否允许升级到指定规模
    public bool CheckScaleForUpgrade(BuildingData.BuildingScale requiredScale)
    {
        int requiredScaleInt = (int)requiredScale;
        return tileScale >= requiredScaleInt;
    }

    // 升级建筑
    public bool UpgradeBuilding(Player player)
    {
        if (!CanUpgradeBuilding(player))
        {
            Debug.LogWarning($"无法升级建筑：条件不满足");
            return false;
        }

        // 获取下一级建筑
        BuildingData nextBuilding = GetNextUpgradeBuilding();
        if (nextBuilding == null)
        {
            Debug.LogWarning("已到达最大等级，无法升级");
            return false;
        }

        // 检查规模限制
        if (!CheckScaleForUpgrade(nextBuilding.requiredScale))
        {
            Debug.LogWarning($"地块规模{tileScale}不支持升级到{nextBuilding.buildingName}");
            return false;
        }

        // 获取升级价格
        int upgradeCost = GetUpgradeCost();
        if (upgradeCost <= 0)
        {
            Debug.LogWarning("升级价格无效");
            return false;
        }

        // 扣款
        if (!player.PayCash(upgradeCost))
        {
            Debug.LogWarning("资金不足，无法升级");
            return false;
        }

        // 移除旧的buff效果
        if (currentBuffOwner != null)
        {
            RemoveBuffFromPlayer();
        }

        // 更新建筑数据
        currentBuildingData = nextBuilding;
        buildingLevel++;

        Debug.Log($"成功升级建筑到 {buildingLevel} 级: {currentBuildingData.buildingName}");

        // 更新建筑模型
        UpdateBuildingModel();

        // 应用建筑功能
        ApplyBuildingFunction(player);

        return true;
    }

    // 获取升级价格
    public int GetUpgradeCost()
    {
        if (currentBuildingData == null) return 0;

        if (buildingLevel <= currentBuildingData.upgradeCosts.Length)
        {
            return currentBuildingData.upgradeCosts[buildingLevel - 1];
        }

        // 基础价格 * 等级
        int basePrice = 0;

        switch (currentBuildingType)
        {
            case BuildingType.SmallHouse:
            case BuildingType.SmallShop:
                basePrice = 50;
                break;
            case BuildingType.MediumHouse:
            case BuildingType.MediumShop:
                basePrice = 100;
                break;
            case BuildingType.LargeHouse:
            case BuildingType.LargeShop:
                basePrice = 200;
                break;
        }

        return basePrice * buildingLevel;
    }

    // 更新建筑模型
    private void UpdateBuildingModel()
    {
        if (currentBuilding != null)
        {
            // 销毁当前模型
            Destroy(currentBuilding);
        }

        // 根据等级加载新的模型
        GameObject prefab = GetBuildingPrefabForLevel(buildingLevel);
        if (prefab != null)
        {
            currentBuilding = Instantiate(prefab, transform);
            currentBuilding.transform.localPosition = Vector3.up * 0.5f;
        }
    }

    // 根据等级获取建筑预制体
    private GameObject GetBuildingPrefabForLevel(int level)
    {
        if (currentBuildingData == null) return null;

        if (currentBuildingData.upgradePrefabs != null &&
            level - 1 < currentBuildingData.upgradePrefabs.Length)
        {
            return currentBuildingData.upgradePrefabs[level - 1];
        }

        // 备用方案：使用Resources加载
        string path = $"Buildings/{currentBuildingData.buildingName}_Level{level}";
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"找不到建筑预制体: {path}");
        }

        return prefab;
    }

    // ================ 建筑功能系统 ================

    // 应用建筑功能
    private void ApplyBuildingFunction(Player player)
    {
        if (currentBuildingData == null || player == null) return;

        switch (currentBuildingData.functionType)
        {
            case BuildingData.BuildingFunctionType.Income:
                // 提供收入
                ProvideIncome(player);
                break;

            case BuildingData.BuildingFunctionType.Buff:
                // 应用buff
                ApplyBuffToPlayer(player);
                break;

            case BuildingData.BuildingFunctionType.Mixed:
                // 混合功能
                ProvideIncome(player);
                ApplyBuffToPlayer(player);
                break;
        }

        Debug.Log($"应用建筑功能: {currentBuildingData.buildingName} - {currentBuildingData.functionType}");
    }

    // 提供收益
    public void ProvideIncome(Player player)
    {
        if (currentBuildingData == null) return;

        int income = currentBuildingData.GetIncomeAmount(buildingLevel);
        if (income > 0)
        {
            int finalIncome = player.GetIncomeWithMultiplier(income);
            player.ReceiveCash(finalIncome);
            Debug.Log($"{tileName} 为玩家提供 {finalIncome} 金币收益");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"{tileName} 提供 {finalIncome} 金币收益", 2f);
            }
        }
    }

    // 应用buff到玩家
    private void ApplyBuffToPlayer(Player player)
    {
        if (currentBuildingData == null ||
            currentBuildingData.buffEffect == BuildingData.BuffEffect.None)
            return;

        // 设置buff
        currentBuffOwner = player;
        currentBuffEffect = currentBuildingData.buffEffect;
        currentBuffValue = currentBuildingData.GetBuffValue(buildingLevel);

        if (currentBuildingData.buffDuration > 0)
        {
            buffExpireTime = Time.time + currentBuildingData.buffDuration;
        }
        else
        {
            buffExpireTime = 0f; // 永久buff
        }

        // 应用buff效果
        ApplyBuffEffect(player, true);

        // 添加到玩家的buff列表
        player.AddBuff(this);

        Debug.Log($"为玩家{player.playerName}应用{currentBuffEffect} buff: {currentBuffValue}");
    }

    // 应用buff效果
    private void ApplyBuffEffect(Player player, bool apply)
    {
        if (player == null) return;

        switch (currentBuffEffect)
        {
            case BuildingData.BuffEffect.MoveSpeedBoost:
                // 移动速度加成
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    // 注意：需要为PlayerMovement添加moveSpeedMultiplier字段
                    player.moveSpeedMultiplier += apply ? currentBuffValue : -currentBuffValue;
                }
                break;

            case BuildingData.BuffEffect.DiceBoost:
                // 骰子点数加成
                player.hasDiceBoost = apply;
                if (apply)
                    player.diceBoostValue = Mathf.RoundToInt(currentBuffValue * 6);
                else
                    player.diceBoostValue = 0;
                break;

            case BuildingData.BuffEffect.IncomeMultiplier:
                // 收入倍数加成
                player.incomeMultiplier += apply ? currentBuffValue : -currentBuffValue;
                break;

            case BuildingData.BuffEffect.LuckBoost:
                // 幸运加成
                player.luckBoost += apply ? currentBuffValue : -currentBuffValue;
                break;
        }
    }

    // 移除buff
    private void RemoveBuffFromPlayer()
    {
        if (currentBuffOwner == null) return;

        // 移除buff效果
        ApplyBuffEffect(currentBuffOwner, false);

        // 从玩家的buff列表中移除
        currentBuffOwner.RemoveBuff(this);

        Debug.Log($"从玩家{currentBuffOwner.playerName}移除{currentBuffEffect} buff");

        // 清空buff信息
        currentBuffOwner = null;
        currentBuffEffect = BuildingData.BuffEffect.None;
        currentBuffValue = 0f;
        buffExpireTime = 0f;
    }

    // ================ 辅助方法 ================

    // 设置建筑数据
    public void SetBuildingData(BuildingData buildingData, int level = 1)
    {
        currentBuildingData = buildingData;
        currentBuildingType = GetBuildingTypeFromData(buildingData);
        buildingLevel = level;
        tileType = TileType.BuildingSite;
        isBuildable = false;

        // 获取下一级建筑
        nextBuildingData = buildingData.nextLevelBuilding;

        // 更新模型
        UpdateBuildingModel();

        // 如果建筑属于当前玩家，应用功能
        if (ownerPlayer != null)
        {
            ApplyBuildingFunction(ownerPlayer);
        }
    }

    // 从BuildingData获取BuildingType
    private BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data == null) return BuildingType.None;

        // 根据建筑名称判断类型
        if (data.buildingName.Contains("小房屋") || data.buildingName.Contains("SmallHouse"))
            return BuildingType.SmallHouse;
        else if (data.buildingName.Contains("中房屋") || data.buildingName.Contains("MediumHouse"))
            return BuildingType.MediumHouse;
        else if (data.buildingName.Contains("大房屋") || data.buildingName.Contains("LargeHouse"))
            return BuildingType.LargeHouse;
        else if (data.buildingName.Contains("小商店") || data.buildingName.Contains("SmallShop"))
            return BuildingType.SmallShop;
        else if (data.buildingName.Contains("中商店") || data.buildingName.Contains("MediumShop"))
            return BuildingType.MediumShop;
        else if (data.buildingName.Contains("大商店") || data.buildingName.Contains("LargeShop"))
            return BuildingType.LargeShop;
        else
            return BuildingType.None;
    }

    // 获取建筑信息字符串
    public string GetBuildingInfo()
    {
        if (currentBuildingData == null)
        {
            return "无建筑";
        }

        return $"{currentBuildingData.buildingName} (等级 {buildingLevel})";
    }

    // 检查地块是否可放置指定规模的建筑
    public bool CanPlaceBuilding(int requiredScale)
    {
        if (!isBuildable) return false;
        if (currentBuildingData != null) return false;
        if (tileScale < requiredScale) return false;

        return true;
    }

    // 获取建筑功能描述
    public string GetBuildingFunctionDescription()
    {
        if (currentBuildingData == null) return "无功能";

        switch (currentBuildingData.functionType)
        {
            case BuildingData.BuildingFunctionType.Income:
                int income = currentBuildingData.GetIncomeAmount(buildingLevel);
                return $"每回合收入: {income}金币";

            case BuildingData.BuildingFunctionType.Buff:
                float buffValue = currentBuildingData.GetBuffValue(buildingLevel);
                string buffName = GetBuffEffectName(currentBuildingData.buffEffect);
                if (currentBuildingData.buffDuration > 0)
                {
                    return $"{buffName}: +{buffValue * 100}% (持续{currentBuildingData.buffDuration}秒)";
                }
                else
                {
                    return $"{buffName}: +{buffValue * 100}% (永久)";
                }

            case BuildingData.BuildingFunctionType.Mixed:
                income = currentBuildingData.GetIncomeAmount(buildingLevel);
                buffValue = currentBuildingData.GetBuffValue(buildingLevel);
                buffName = GetBuffEffectName(currentBuildingData.buffEffect);
                return $"收入: {income}金币 + {buffName}: +{buffValue * 100}%";

            default:
                return "未知功能";
        }
    }

    // 获取buff效果名称
    private string GetBuffEffectName(BuildingData.BuffEffect effect)
    {
        switch (effect)
        {
            case BuildingData.BuffEffect.MoveSpeedBoost: return "移动速度";
            case BuildingData.BuffEffect.DiceBoost: return "骰子加成";
            case BuildingData.BuffEffect.IncomeMultiplier: return "收入倍率";
            case BuildingData.BuffEffect.LuckBoost: return "幸运加成";
            default: return "未知效果";
        }
    }

    public void ClearHighlight()
    {
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            // 恢复原始颜色
            UpdateTileVisual(); // 调用更新视觉的方法
        }

        // 移除点击事件
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger != null)
        {
            Destroy(trigger);
        }
    }
}