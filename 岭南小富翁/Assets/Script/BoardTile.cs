using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardTile : MonoBehaviour
{
    [Header("地块基本信息")]
    public string tileName = "地块";
    public int tileID = 0;
    public int tileScale = 1; // 地块规模
    public int propertyPrice = 100; // 地块价格
    public int rentPrice = 10; // 租金
    public TileType tileType = TileType.Property;
    public bool isBuildable = false; // 是否可建造

    [Header("建筑系统")]
    public BuildingData currentBuildingData; // 当前建筑数据
    public BuildingType currentBuildingType = BuildingType.None;
    public int buildingLevel = 0; // 建筑等级
    public GameObject currentBuilding; // 当前建筑模型
    public Player ownerPlayer; // 地块所有者

    [Header("建筑关联系统 - 新增功能")]
    [SerializeField] private List<BoardTile> linkedBuildingTiles; // 关联的建筑地块
    [SerializeField] private float incomeInterval = 5.0f; // 收入间隔（秒）
    private Dictionary<BoardTile, float> lastIncomeTime = new Dictionary<BoardTile, float>(); // 上次产生收入的时间
    [SerializeField] private bool enableLinkedIncome = true; // 是否启用关联收入

    [Header("自动收入系统")]
    [SerializeField] private bool enableAutoIncome = false; // 是否启用自动收入
    [SerializeField] private float autoIncomeInterval = 10.0f; // 自动收入间隔
    private float lastAutoIncomeTime = 0f;

    [Header("UI显示")]
    public TextMeshProUGUI tileNameText; // 地块名称文本
    public MeshRenderer tileRenderer; // 地块渲染器
    public Color defaultColor = Color.white; // 默认颜色
    public Color ownedColor = Color.blue; // 被拥有时的颜色
    public Color buildableColor = Color.green; // 可建造时的颜色

    [Header("事件系统")]
    public List<Player> buffedPlayers = new List<Player>(); // 受影响的玩家
    public float buffDuration = 0f; // Buff持续时间

    // 地块类型枚举
    public enum TileType
    {
        Start,          // 起点
        Property,       // 地产
        Railroad,       // 铁路
        Utility,        // 公用事业
        Chance,         // 机会卡
        CommunityChest, // 社区基金
        Tax,            // 税
        Jail,           // 监狱
        FreeParking,    // 免费停车
        GoToJail,       // 进监狱
        Buildable,      // 可建造地皮
        BuildingSite,   // 建筑地块
        Event,           // 事件
        Normal
    }

    // 建筑类型枚举
    public enum BuildingType
    {
        None,
        SmallHouse,
        MediumHouse,
        LargeHouse,
        Shop,
        Inn,
        Temple,
        Special
    }

    // 地块事件枚举
    public enum TileEvent
    {
        None,
        GainMoney,
        LoseMoney,
        MoveToTile,
        GetOutOfJailFree,
        PayTax
    }

    void Start()
    {
        InitializeTile();

        // 尝试获取组件
        if (tileRenderer == null)
        {
            tileRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (tileNameText == null)
        {
            tileNameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        UpdateTileVisual();
    }

    void Update()
    {
        // 自动收入系统
        if (enableAutoIncome &&
            currentBuildingData != null &&
            ownerPlayer != null &&
            (currentBuildingData.functionType == BuildingData.BuildingFunctionType.Income ||
             currentBuildingData.functionType == BuildingData.BuildingFunctionType.Mixed))
        {
            if (Time.time - lastAutoIncomeTime >= autoIncomeInterval)
            {
                GenerateAutoIncome();
                lastAutoIncomeTime = Time.time;
            }
        }

        // 更新Buff持续时间
        if (buffDuration > 0)
        {
            buffDuration -= Time.deltaTime;
            if (buffDuration <= 0)
            {
                ClearBuffs();
            }
        }
    }

    void InitializeTile()
    {
        // 设置默认值
        if (string.IsNullOrEmpty(tileName))
        {
            tileName = $"地块_{tileID}";
        }

        // 更新显示
        if (tileNameText != null)
        {
            tileNameText.text = tileName;
        }
    }

    // 玩家降落在地块上
    public virtual void OnLanded(Player player)
    {
        Debug.Log($"玩家 {player.playerName} 降落在 {tileName} 上");

        switch (tileType)
        {
            case TileType.Start:
                Debug.Log($"{player.playerName} 到达起点");
                // 起点可能触发一些事件，比如获得资金
                if (GameManager.Instance != null)
                {
                    int salary = GameManager.Instance.salaryAmount;
                    player.ReceiveCash(salary);
                    Debug.Log($"{player.playerName} 获得起点工资: {salary} 元");

                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast($"经过起点，获得{salary}元工资", 2f);
                    }
                }
                break;

            case TileType.Property:
            case TileType.Railroad:
            case TileType.Utility:
                HandlePropertyLanding(player);
                break;

            case TileType.Chance:
                DrawChanceCard(player);
                break;

            case TileType.CommunityChest:
                DrawCommunityChestCard(player);
                break;

            case TileType.Tax:
                PayTax(player);
                break;

            case TileType.Jail:
                // 只是访问监狱
                Debug.Log($"{player.playerName} 访问监狱");
                break;

            case TileType.GoToJail:
                SendToJail(player);
                break;

            case TileType.FreeParking:
                Debug.Log($"{player.playerName} 在免费停车");
                break;

            case TileType.Buildable:
                // 可建造地块
                Debug.Log($"{player.playerName} 到达可建造地块: {tileName}");
                if (ownerPlayer == null)
                {
                    // 显示购买界面
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowBuildingSelectionUI(this, player);
                    }
                }
                break;

            case TileType.BuildingSite:
                // 建筑地块
                Debug.Log($"{player.playerName} 到达建筑地块: {tileName}");
                if (ownerPlayer != null && ownerPlayer != player)
                {
                    // 支付租金
                    PayRent(player);
                }
                else
                {
                    // 自己的地块，可以升级
                    if (ownerPlayer == player && currentBuildingData != null)
                    {
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.ShowBuildingUpgradeUI(this, player);
                        }
                    }
                }
                break;

            case TileType.Event:
                TriggerRandomEvent(player);
                break;

            default:
                Debug.Log($"玩家 {player.playerName} 降落在 {tileName} 上，类型: {tileType}");
                break;
        }

        Debug.Log($"准备进入检查Debug");
        // === 新增：检查关联建筑并触发收入 ===
        if (enableLinkedIncome && linkedBuildingTiles != null && linkedBuildingTiles.Count > 0)
        {
            Debug.Log($"检查地块 {tileName} 的 {linkedBuildingTiles.Count} 个关联建筑");
            TriggerLinkedBuildingIncome(player);
        }
    }

    // 处理属性地块着陆
    private void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 无主之地，可以购买
            Debug.Log($"{tileName} 可购买，价格: {propertyPrice} 元");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // 自己的地块
            Debug.Log($"{player.playerName} 访问自己的地块: {tileName}");
        }
        else
        {
            // 别人的地块，支付租金
            PayRent(player);
        }
    }

    // 支付租金
    private void PayRent(Player player)
    {
        int rent = CalculateRent();
        Debug.Log($"{player.playerName} 需要支付租金 {rent} 元给 {ownerPlayer.playerName}");

        if (player.PayCash(rent))
        {
            ownerPlayer.ReceiveCash(rent);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"支付租金 {rent} 元给 {ownerPlayer.playerName}", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} 无法支付租金，可能需要破产处理");
        }
    }

    // 计算租金
    public int CalculateRent()
    {
        int baseRent = rentPrice;

        // 如果有建筑，增加租金
        if (currentBuildingData != null)
        {
            baseRent += currentBuildingData.GetIncomeAmount(buildingLevel);
        }

        return baseRent;
    }

    // === 新增：触发关联建筑收入 ===
    // 触发关联建筑的收入功能 - 调试版本
    private void TriggerLinkedBuildingIncome(Player player)
    {
        Debug.Log($"=== [调试] 开始为玩家 [{player.playerName}] 检查地块 [{tileName}] 的关联收入 ===");

        // 1. 检查总开关和列表
        if (!enableLinkedIncome)
        {
            Debug.LogWarning($"  [失败] 总开关 'enableLinkedIncome' 为 false，功能已禁用。");
            return;
        }

        if (linkedBuildingTiles == null || linkedBuildingTiles.Count == 0)
        {
            Debug.LogWarning($"  [失败] 关联建筑列表为空或长度为0，直接返回。");
            return;
        }
        Debug.Log($"  [信息] 关联建筑地块数量: {linkedBuildingTiles.Count}");

        // 2. 记录当前时间
        float currentTime = Time.time;
        bool anyIncomeGenerated = false;
        int totalIncome = 0;

        // 3. 遍历所有关联建筑
        for (int i = 0; i < linkedBuildingTiles.Count; i++)
        {
            BoardTile buildingTile = linkedBuildingTiles[i];
            Debug.Log($"  -- 检查关联建筑 [{i + 1}/{linkedBuildingTiles.Count}]: {buildingTile?.tileName ?? "NULL"} --");

            if (buildingTile == null)
            {
                Debug.LogWarning($"    [跳过] 数组中的第 {i + 1} 个建筑地块为 null。");
                continue;
            }

            // 4. 检查冷却时间
            if (CanGenerateIncome(buildingTile, currentTime))
            {
                Debug.Log($"    [通过] 冷却时间检查。");

                // 5. 检查建筑所有者
                if (buildingTile.ownerPlayer != null)
                {
                    Debug.Log($"    [信息] 建筑所有者: {buildingTile.ownerPlayer.playerName}， 当前玩家: {player.playerName}");

                    if (buildingTile.ownerPlayer == player)
                    {
                        Debug.Log($"    [通过] 建筑所有者是当前玩家。");

                        // 6. 检查建筑数据
                        if (buildingTile.currentBuildingData != null)
                        {
                            Debug.Log($"    [信息] 建筑数据: {buildingTile.currentBuildingData.buildingName}, 功能类型: {buildingTile.currentBuildingData.functionType}");

                            // 7. 检查是否具有收入功能
                            if (buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Income ||
                                buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Mixed)
                            {
                                // 8. 计算收入
                                int incomeAmount = buildingTile.currentBuildingData.GetIncomeAmount(buildingTile.buildingLevel);
                                Debug.Log($"    [信息] 建筑等级: {buildingTile.buildingLevel}, 计算收入: {incomeAmount}");

                                if (incomeAmount > 0)
                                {
                                    // 9. 给玩家钱
                                    player.ReceiveCash(incomeAmount);
                                    totalIncome += incomeAmount;
                                    Debug.Log($"    [成功] √ 为玩家 {player.playerName} 发放建筑 {buildingTile.currentBuildingData.buildingName} 的收入: {incomeAmount} 元");

                                    // 10. 更新上次产生收入的时间
                                    if (!lastIncomeTime.ContainsKey(buildingTile))
                                    {
                                        lastIncomeTime.Add(buildingTile, currentTime);
                                    }
                                    else
                                    {
                                        lastIncomeTime[buildingTile] = currentTime;
                                    }
                                    anyIncomeGenerated = true;
                                }
                                else
                                {
                                    Debug.LogWarning($"    [失败] 计算出的收入金额为 0 或负数。");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"    [失败] 建筑功能类型不是 Income 或 Mixed。当前类型: {buildingTile.currentBuildingData.functionType}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"    [失败] 建筑地块上没有建筑数据 (currentBuildingData 为 null)。");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"    [失败] 建筑所有者不是当前玩家。");
                    }
                }
                else
                {
                    Debug.LogWarning($"    [失败] 建筑地块没有所有者 (ownerPlayer 为 null)。");
                }
            }
            else
            {
                Debug.Log($"    [跳过] 未通过冷却时间检查。");
            }
        }

        // 11. 最终结果汇总
        if (anyIncomeGenerated && totalIncome > 0)
        {
            Debug.Log($"=== [调试] 关联收入检查结束，总计为玩家 {player.playerName} 发放: {totalIncome} 元 ===");

            // 显示UI提示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"关联建筑收入: {totalIncome} 元", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"=== [调试] 关联收入检查结束，未产生任何收入。 ===");
        }
    }

    // 检查建筑是否可以产生收入 - 调试版本
    private bool CanGenerateIncome(BoardTile buildingTile, float currentTime)
    {
        if (!lastIncomeTime.ContainsKey(buildingTile))
        {
            Debug.Log($"    [信息] 建筑 {buildingTile.tileName} 从未产生过收入，允许产生。");
            return true;
        }

        float timeSinceLastIncome = currentTime - lastIncomeTime[buildingTile];
        bool canGenerate = timeSinceLastIncome >= incomeInterval;

        Debug.Log($"    [信息] 建筑 {buildingTile.tileName} 上次收入于 {timeSinceLastIncome:F1} 秒前，间隔要求 {incomeInterval} 秒，允许产生: {canGenerate}");
        return canGenerate;
    }

    // === 新增：生成自动收入 ===
    private void GenerateAutoIncome()
    {
        if (currentBuildingData == null || ownerPlayer == null) return;

        int incomeAmount = currentBuildingData.GetIncomeAmount(buildingLevel);
        if (incomeAmount > 0)
        {
            ownerPlayer.ReceiveCash(incomeAmount);
            Debug.Log($"建筑 {currentBuildingData.buildingName} 自动产生收入: {incomeAmount} 元");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"建筑自动收入: {incomeAmount} 元", 2f);
            }
        }
    }

    // 设置建筑数据
    public void SetBuildingData(BuildingData data, int level = 1)
    {
        currentBuildingData = data;
        buildingLevel = level;

        if (data != null)
        {
            Debug.Log($"为地块 {tileName} 设置建筑: {data.buildingName} 等级 {level}");
        }
    }

    // 获取升级成本
    public int GetUpgradeCost()
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return 0;

        return currentBuildingData.nextLevelBuilding.purchasePrice;
    }

    // 检查是否可以升级
    public bool CanUpgradeBuilding(Player player)
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return false;

        if (ownerPlayer != player) return false;

        if (player.cash < GetUpgradeCost()) return false;

        // 检查规模限制
        if (!CheckScaleForUpgrade(currentBuildingData.nextLevelBuilding.requiredScale))
            return false;

        return true;
    }

    // 检查规模是否满足升级要求
    public bool CheckScaleForUpgrade(BuildingData.Scale requiredScale)
    {
        return tileScale >= (int)requiredScale;
    }

    // 获取下一级建筑
    public BuildingData GetNextUpgradeBuilding()
    {
        if (currentBuildingData == null) return null;
        return currentBuildingData.nextLevelBuilding;
    }

    // 升级建筑
    public bool UpgradeBuilding(Player player)
    {
        if (!CanUpgradeBuilding(player)) return false;

        int upgradeCost = GetUpgradeCost();

        if (player.PayCash(upgradeCost))
        {
            buildingLevel++;
            Debug.Log($"{player.playerName} 升级了 {tileName} 上的建筑到等级 {buildingLevel}");

            // 更新建筑模型
            if (currentBuildingData.nextLevelBuilding != null &&
                currentBuildingData.nextLevelBuilding.buildingPrefab != null)
            {
                // 销毁旧建筑
                if (currentBuilding != null)
                {
                    Destroy(currentBuilding);
                }

                // 创建新建筑
                GameObject newBuilding = Instantiate(
                    currentBuildingData.nextLevelBuilding.buildingPrefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
                newBuilding.transform.SetParent(transform);
                currentBuilding = newBuilding;
            }

            return true;
        }

        return false;
    }

    // 应用Buff效果
    public void ApplyBuffToPlayer(Player player)
    {
        if (currentBuildingData == null) return;

        if (currentBuildingData.functionType == BuildingData.BuildingFunctionType.Buff ||
            currentBuildingData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            float buffValue = currentBuildingData.GetBuffValue(buildingLevel);
            BuildingData.BuffEffect effect = currentBuildingData.buffEffect;

            switch (effect)
            {
                case BuildingData.BuffEffect.MoveSpeedBoost:
                    player.moveSpeedMultiplier += buffValue;
                    Debug.Log($"{player.playerName} 获得移动速度加成: {buffValue * 100}%");
                    break;

                case BuildingData.BuffEffect.DiceBoost:
                    player.hasDiceBoost = true;
                    player.diceBoostValue = Mathf.RoundToInt(buffValue);
                    Debug.Log($"{player.playerName} 获得骰子加成: +{player.diceBoostValue}");
                    break;

                case BuildingData.BuffEffect.IncomeMultiplier:
                    player.incomeMultiplier += buffValue;
                    Debug.Log($"{player.playerName} 获得收入倍率: {buffValue * 100}%");
                    break;

                case BuildingData.BuffEffect.LuckBoost:
                    player.luckBoost += buffValue;
                    Debug.Log($"{player.playerName} 获得幸运加成: {buffValue * 100}%");
                    break;
            }

            buffedPlayers.Add(player);
            buffDuration = currentBuildingData.buffDuration;
        }
    }

    // 清除Buff效果
    private void ClearBuffs()
    {
        foreach (Player player in buffedPlayers)
        {
            if (currentBuildingData != null)
            {
                BuildingData.BuffEffect effect = currentBuildingData.buffEffect;

                switch (effect)
                {
                    case BuildingData.BuffEffect.MoveSpeedBoost:
                        player.moveSpeedMultiplier = 1.0f;
                        break;

                    case BuildingData.BuffEffect.DiceBoost:
                        player.hasDiceBoost = false;
                        player.diceBoostValue = 0;
                        break;

                    case BuildingData.BuffEffect.IncomeMultiplier:
                        player.incomeMultiplier = 1.0f;
                        break;

                    case BuildingData.BuffEffect.LuckBoost:
                        player.luckBoost = 0f;
                        break;
                }
            }
        }

        buffedPlayers.Clear();
        buffDuration = 0f;
    }

    // 绘制机会卡
    private void DrawChanceCard(Player player)
    {
        // 简单的机会卡实现
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(20, 101);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 抽到机会卡: 获得 {gain} 元");
                break;

            case 2:
                int lose = Random.Range(20, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} 抽到机会卡: 损失 {lose} 元");
                }
                break;

            case 3:
                // 移动到随机地块
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomTileIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    BoardTile targetTile = BoardManager.Instance.allTiles[randomTileIndex];
                    player.MoveToTile(targetTile, true);
                    Debug.Log($"{player.playerName} 抽到机会卡: 移动到 {targetTile.tileName}");
                }
                break;
        }
    }

    // 绘制社区基金卡
    private void DrawCommunityChestCard(Player player)
    {
        // 简单的社区基金卡实现
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(50, 201);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 抽到社区基金卡: 获得 {gain} 元");
                break;

            case 2:
                int tax = Random.Range(50, 201);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} 抽到社区基金卡: 缴税 {tax} 元");
                }
                break;

            case 3:
                player.AddBuff(this);
                Debug.Log($"{player.playerName} 抽到社区基金卡: 获得临时Buff");
                break;
        }
    }

    // 支付税款
    private void PayTax(Player player)
    {
        int taxAmount = propertyPrice / 10; // 税为地产价值的10%

        if (player.PayCash(taxAmount))
        {
            Debug.Log($"{player.playerName} 支付税款: {taxAmount} 元");
        }
    }

    // 送进监狱
    private void SendToJail(Player player)
    {
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        Debug.Log($"{player.playerName} 被送进监狱，剩余 {player.jailTurnsRemaining} 回合");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{player.playerName} 进监狱了！", 2f);
        }
    }

    // 触发随机事件
    private void TriggerRandomEvent(Player player)
    {
        TileEvent randomEvent = (TileEvent)Random.Range(1, 6);

        switch (randomEvent)
        {
            case TileEvent.GainMoney:
                int gain = Random.Range(50, 151);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 触发事件: 捡到 {gain} 元");
                break;

            case TileEvent.LoseMoney:
                int lose = Random.Range(30, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} 触发事件: 丢失 {lose} 元");
                }
                break;

            case TileEvent.MoveToTile:
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    BoardTile targetTile = BoardManager.Instance.allTiles[randomIndex];
                    player.MoveToTile(targetTile, true);
                    Debug.Log($"{player.playerName} 触发事件: 传送到 {targetTile.tileName}");
                }
                break;

            case TileEvent.GetOutOfJailFree:
                if (player.isInJail)
                {
                    player.isInJail = false;
                    player.jailTurnsRemaining = 0;
                    Debug.Log($"{player.playerName} 触发事件: 获得出狱卡");
                }
                break;

            case TileEvent.PayTax:
                int tax = Random.Range(20, 81);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} 触发事件: 支付额外税款 {tax} 元");
                }
                break;
        }
    }

    // 更新地块视觉
    public void UpdateTileVisual()
    {
        if (tileRenderer == null) return;

        if (ownerPlayer != null)
        {
            // 有所有者，使用所有者颜色
            tileRenderer.material.color = ownerPlayer.playerColor;
        }
        else if (isBuildable)
        {
            // 可建造，使用可建造颜色
            tileRenderer.material.color = buildableColor;
        }
        else
        {
            // 默认颜色
            tileRenderer.material.color = defaultColor;
        }
    }

    // 添加关联建筑地块
    public void AddLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }

        if (!linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Add(buildingTile);
            Debug.Log($"地块 {tileName} 关联了建筑地块 {buildingTile.tileName}");
        }
    }

    // 移除关联建筑地块
    public void RemoveLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles != null && linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Remove(buildingTile);
            Debug.Log($"地块 {tileName} 移除了建筑地块关联 {buildingTile.tileName}");
        }
    }

    // 清空所有关联
    public void ClearAllLinkedBuildingTiles()
    {
        if (linkedBuildingTiles != null)
        {
            linkedBuildingTiles.Clear();
            Debug.Log($"地块 {tileName} 清空了所有建筑关联");
        }
    }

    // 获取所有关联建筑地块
    public List<BoardTile> GetLinkedBuildingTiles()
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }
        return linkedBuildingTiles;
    }

    // 启用/禁用关联收入
    public void SetLinkedIncomeEnabled(bool enabled)
    {
        enableLinkedIncome = enabled;
    }

    // 设置收入间隔
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = Mathf.Max(1.0f, interval); // 最小1秒
    }

    // 启用/禁用自动收入
    public void SetAutoIncomeEnabled(bool enabled, float interval = 10.0f)
    {
        enableAutoIncome = enabled;
        autoIncomeInterval = interval;
    }
}