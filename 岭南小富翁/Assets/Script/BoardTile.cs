using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardTile : MonoBehaviour
{
    [Header("建筑类型")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None; //自动获取建筑

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
    public Player ownerPlayer; // 地块拥有者

    [Header("关联收入系统 - 被动收入")]
    [SerializeField] private List<BoardTile> linkedBuildingTiles; // 关联的建筑地块
    [SerializeField] private float incomeInterval = 5.0f; // 收入间隔(秒)
    private Dictionary<BoardTile, float> lastIncomeTime = new Dictionary<BoardTile, float>(); // 上次获得收入的时间
    [SerializeField] private bool enableLinkedIncome = true; // 是否启用关联收入

    [Header("自动收入系统")]
    [SerializeField] private bool enableAutoIncome = false; // 是否启用自动收入
    [SerializeField] private float autoIncomeInterval = 10.0f; // 自动收入间隔
    private float lastAutoIncomeTime = 0f;

    [Header("事件系统")]
    public EventData[] eventDataArray; // 事件数据数组

    [Header("UI显示")]
    public TextMeshProUGUI tileNameText; // 地块名称文本
    public MeshRenderer tileRenderer; // 地块渲染器
  
 

    [Header("Buff效果系统")]
    public List<Player> buffedPlayers = new List<Player>(); // 受影响的玩家
    public float buffDuration = 0f; // Buff持续时间

    // 地块类型枚举
    public enum TileType
    {
        Start,          // 起点
        Property,       // 地产
        Railroad,       // 铁路
        Utility,        // 公共事业
        Chance,         // 机会卡
        CommunityChest, // 社区福利
        Tax,            // 税
        Jail,           // 监狱
        FreeParking,    // 免费停车
        GoToJail,       // 进监狱
        Buildable,      // 可建造地块
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

        // 检查Buff持续时间
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

    // 玩家经过地块（经过触发）
    public virtual void OnPassed(Player player)
    {
        if (enableLinkedIncome && linkedBuildingTiles != null && linkedBuildingTiles.Count > 0)
        {
            TriggerLinkedBuildingIncome(player);
        }
    }

    // 玩家站到地块上（停留触发）
    public virtual void OnLanded(Player player)
    {
        switch (tileType)
        {
            case TileType.Start:
                if (GameManager.Instance != null)
                {
                    int salary = GameManager.Instance.salaryAmount;
                    player.ReceiveCash(salary);

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);

                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast($"到达起点，获得{salary}元工资", 2f);
                    }
                }
                break;

            case TileType.Property:
            case TileType.Railroad:
            case TileType.Utility:
            case TileType.Normal:
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
                break;

            case TileType.GoToJail:
                SendToJail(player);
                break;

            case TileType.FreeParking:
                break;

            case TileType.Buildable:
                if (ownerPlayer == null && UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBuildingSelectionUI(this, player);
                }
                break;

            case TileType.BuildingSite:
                if (ownerPlayer != null && ownerPlayer != player)
                {
                    PayRent(player);
                }
                else if (ownerPlayer == player && currentBuildingData != null && UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBuildingUpgradeUI(this, player);
                }
                break;

            case TileType.Event:
                TriggerRandomEvent(player);
                break;
        }
    }

    // 处理玩家站到地产上
    private void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 无主地产，可以购买
            Debug.Log($"{tileName} 可以购买，价格: {propertyPrice} 元");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // 自己的地产
            Debug.Log($"{player.playerName} 站在自己的地产: {tileName}");
        }
        else
        {
            // 别人的地产，支付租金
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

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"支付租金 {rent} 元给 {ownerPlayer.playerName}", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} 无法支付租金，可能需要破产");
        }
    }

    // 计算租金
    public int CalculateRent()
    {
        int baseRent = rentPrice;

        // 如果有建筑，增加建筑收入
        if (currentBuildingData != null)
        {
            baseRent += currentBuildingData.GetIncomeAmount(buildingLevel);
        }

        return baseRent;
    }

    // === 关联收入触发函数 ===
    private void TriggerLinkedBuildingIncome(Player player)
    {
        if (!enableLinkedIncome)
            return;

        if (linkedBuildingTiles == null || linkedBuildingTiles.Count == 0)
            return;

        float currentTime = Time.time;
        int totalIncome = 0;

        for (int i = 0; i < linkedBuildingTiles.Count; i++)
        {
            BoardTile buildingTile = linkedBuildingTiles[i];
            if (buildingTile == null) continue;

            if (!CanGenerateIncome(buildingTile, currentTime)) continue;

            if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != player) continue;

            if (buildingTile.currentBuildingData == null) continue;

            if (buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Income &&
                buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Mixed)
                continue;

            int incomeAmount = buildingTile.currentBuildingData.GetIncomeAmount(buildingTile.buildingLevel);
            if (incomeAmount > 0)
            {
                player.ReceiveCash(incomeAmount);
                totalIncome += incomeAmount;

                if (!lastIncomeTime.ContainsKey(buildingTile))
                    lastIncomeTime.Add(buildingTile, currentTime);
                else
                    lastIncomeTime[buildingTile] = currentTime;
            }
        }

        if (totalIncome > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"关联建筑收入: {totalIncome} 元", 2f);
        }
    }

    // 检查是否可以产生收入
    private bool CanGenerateIncome(BoardTile buildingTile, float currentTime)
    {
        if (!lastIncomeTime.ContainsKey(buildingTile))
            return true;

        float timeSinceLastIncome = currentTime - lastIncomeTime[buildingTile];
        return timeSinceLastIncome >= incomeInterval;
    }

    // === 处理自动收入 ===
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
            // 自动获取建筑类型
            currentBuildingType = GetBuildingTypeFromData(data);

            Debug.Log($"地块 {tileName}: 设置建筑 {data.buildingName}, 类型: {currentBuildingType}, 等级: {level}");

            // 检查建筑类型
            if (data.functionType != BuildingData.BuildingFunctionType.Income &&
                data.functionType != BuildingData.BuildingFunctionType.Mixed)
            {
                Debug.LogWarning($"注意：该建筑的功能类型为 {data.functionType}，关联收入系统需要 Income 或 Mixed 类型");
            }
        }
        else
        {
            currentBuildingType = BuildingType.None;
            Debug.Log($"地块 {tileName}: 清除建筑数据，建筑类型为 None");
        }
    }
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GetBuildingTypeFromData: 传入的建筑数据为 null");
            return BuildingType.None;
        }

        // 直接读取 BuildingData 中的 buildingType 字段
        BoardTile.BuildingType type = data.buildingType;

        if (type == BuildingType.None)
        {
            // 如果字段未设置，尝试从名称匹配
            Debug.LogWarning($"该建筑 {data.buildingName} 的 buildingType 字段未设置，尝试从名称推断");
            return InferBuildingTypeFromName(data.buildingName);
        }
        else
        {
            Debug.Log("自动获取成功，字段匹配");
            Debug.Log($"GetBuildingTypeFromData: 从 {data.buildingName} 获取到建筑类型: {type}");
            return type;
        }
    }
    private BuildingType InferBuildingTypeFromName(string buildingName)
    {
        string name = buildingName.ToLower();
        //字段匹配
        if (name.Contains("small") || name.Contains("小房子"))
            return BuildingType.SmallHouse;
        else if (name.Contains("medium") || name.Contains("中房子"))
            return BuildingType.MediumHouse;
        else if (name.Contains("large") || name.Contains("大房子"))
            return BuildingType.LargeHouse;
        else
            return BuildingType.Special;
    }

    // 获取建筑升级成本
    public int GetUpgradeCost()
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return 0;

        return currentBuildingData.nextLevelBuilding.purchasePrice;
    }

    // 检查是否可以升级建筑
    public bool CanUpgradeBuilding(Player player)
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return false;

        if (ownerPlayer != player) return false;

        if (player.cash < GetUpgradeCost()) return false;

        // 检查地块规模
        if (!CheckScaleForUpgrade(currentBuildingData.nextLevelBuilding.requiredScale))
            return false;

        return true;
    }

    // 检查地块规模是否满足升级要求
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

    // 抽取机会卡
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

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int lose = Random.Range(20, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} 抽到机会卡: 损失 {lose} 元");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);
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

    // 抽取公共福利卡
    private void DrawCommunityChestCard(Player player)
    {
        // 简单的公共福利实现
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(50, 201);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 抽到公共福利: 获得 {gain} 元");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int tax = Random.Range(50, 201);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} 抽到公共福利: 纳税 {tax} 元");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;

            case 3:
                player.AddBuff(this);
                Debug.Log($"{player.playerName} 抽到公共福利: 获得临时Buff");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventBuffActivated);
                break;
        }
    }

    // 支付税金
    private void PayTax(Player player)
    {
        int taxAmount = propertyPrice / 10; // 税为地产价值的10%

        if (player.PayCash(taxAmount))
        {
            Debug.Log($"{player.playerName} 支付税金: {taxAmount} 元");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
        }
    }

    // 送进监狱
    private void SendToJail(Player player)
    {
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        Debug.Log($"{player.playerName} 被送进监狱，剩余 {player.jailTurnsRemaining} 回合");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.EventGoToJail);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{player.playerName} 进监狱了！", 2f);
        }
    }

    // 触发随机事件
    private void TriggerRandomEvent(Player player)
    {
        if (eventDataArray != null && eventDataArray.Length > 0)
        {
            // 从事件数据数组中随机选择一个事件
            EventData selectedEvent = eventDataArray[Random.Range(0, eventDataArray.Length)];
            
            if (selectedEvent != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowEventPanel(selectedEvent);
                Debug.Log($"{player.playerName} 触发事件: {selectedEvent.eventTitle}");
                return;
            }
        }

        // 如果没有设置事件数据，使用默认逻辑
        TileEvent randomEvent = (TileEvent)Random.Range(1, 6);

        switch (randomEvent)
        {
            case TileEvent.GainMoney:
                int gain = Random.Range(50, 151);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 触发事件: 获得 {gain} 元");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case TileEvent.LoseMoney:
                int lose = Random.Range(30, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} 触发事件: 损失 {lose} 元");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);
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
                    Debug.Log($"{player.playerName} 触发事件: 出狱了！");
                }
                break;

            case TileEvent.PayTax:
                int tax = Random.Range(20, 81);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} 触发事件: 支付额外税金 {tax} 元");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;
        }
    }

    // 更新地块视觉效果
    public void UpdateTileVisual()
    {
        if (tileRenderer == null) return;

       
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
            Debug.Log($"地块 {tileName} 添加了关联建筑地块 {buildingTile.tileName}");
        }
    }

    // 移除关联建筑地块
    public void RemoveLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles != null && linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Remove(buildingTile);
            Debug.Log($"地块 {tileName} 移除了关联建筑地块 {buildingTile.tileName}");
        }
    }

    // 清除所有关联
    public void ClearAllLinkedBuildingTiles()
    {
        if (linkedBuildingTiles != null)
        {
            linkedBuildingTiles.Clear();
            Debug.Log($"地块 {tileName} 清除了所有关联建筑");
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

    // 设置/获取关联收入启用
    public void SetLinkedIncomeEnabled(bool enabled)
    {
        enableLinkedIncome = enabled;
    }

    // 设置收入间隔
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = Mathf.Max(1.0f, interval); // 最小1秒
    }

    // 设置/获取自动收入
    public void SetAutoIncomeEnabled(bool enabled, float interval = 10.0f)
    {
        enableAutoIncome = enabled;
        autoIncomeInterval = interval;
    }

    public bool EnableLinkedIncome
    {
        get { return enableLinkedIncome; }
        set { enableLinkedIncome = value; }
    }

    public List<BoardTile> LinkedBuildingTiles
    {
        get
        {
            if (linkedBuildingTiles == null)
                linkedBuildingTiles = new List<BoardTile>();
            return linkedBuildingTiles;
        }
        set { linkedBuildingTiles = value; }
    }

    public float IncomeInterval
    {
        get { return incomeInterval; }
        set { incomeInterval = Mathf.Max(1.0f, value); }
    }

    // 提供公共的获取和设置方法
    public float GetLastIncomeTime(BoardTile buildingTile)
    {
        if (buildingTile == null) return 0f;

        if (lastIncomeTime.ContainsKey(buildingTile))
            return lastIncomeTime[buildingTile];

        return 0f; // 还未产生过收入
    }

    public void SetLastIncomeTime(BoardTile buildingTile, float time)
    {
        if (buildingTile == null) return;

        if (!lastIncomeTime.ContainsKey(buildingTile))
            lastIncomeTime.Add(buildingTile, time);
        else
            lastIncomeTime[buildingTile] = time;
    }

    public bool ContainsBuildingTile(BoardTile buildingTile)
    {
        return lastIncomeTime.ContainsKey(buildingTile);
    }
}