using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardTile : MonoBehaviour
{
    [Header("建筑类型")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None;

    [Header("地块属性")]
    public string tileName = "";
    public int tileID = 0;
    public int tileScale = 1; // 地块规模
    public int propertyPrice = 100; // 地价
    public int rentPrice = 10; // 租金
    public TileType tileType = TileType.Property;
    public bool isBuildable = false; // 是否可建造

    [Header("建筑数据")]
    public BuildingData currentBuildingData; // 当前建筑数据
    public BuildingType currentBuildingType = BuildingType.None;
    public int buildingLevel = 0; // 建筑等级
    public int buildingStartRound = 0; // 建筑建造回合
    public GameObject currentBuilding; // 当前建筑对象
    public Player ownerPlayer; // 所有者

    [Header("联动收入")]
    [SerializeField] private List<BoardTile> linkedBuildingTiles; // 联动建筑地块列表
    [SerializeField] private float incomeInterval = 5.0f; // 收入间隔
    private Dictionary<BoardTile, float> lastIncomeTime = new Dictionary<BoardTile, float>(); // 上次收入时间
    [SerializeField] private bool enableLinkedIncome = true; // 是否启用联动收入

    [Header("自动收入")]
    [SerializeField] private bool enableAutoIncome = false; // 是否启用自动收入
    [SerializeField] private float autoIncomeInterval = 10.0f; // 自动收入间隔
    private float lastAutoIncomeTime = 0f;

    [Header("事件")]
    public EventData[] eventDataArray; // 事件数据数组

    [Header("UI")]
    public TextMeshProUGUI tileNameText; // 地块名称文本
    public MeshRenderer tileRenderer; // 地块渲染器


    [Header("Buff相关")]
    public List<Player> buffedPlayers = new List<Player>(); // Buff玩家列表

    // 地块类型枚举
    public enum TileType
    {
        Start,          // 起点
        Property,       // 地产
        Railroad,       // 铁路
        Utility,        // 公共设施
        Chance,         // 机会卡
        CommunityChest, // 社区福利
        Tax,            // 税
        Jail,           // 监狱
        FreeParking,    // 免费停车
        GoToJail,       // 去监狱
        Buildable,      // 可建造
        BuildingSite,   // 建筑用地
        Event,          // 事件
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

    // 地块事件
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

        // 查找渲染器
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
        // 自动收入生成
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
    }

    void InitializeTile()
    {
        // 设置默认名称
        if (string.IsNullOrEmpty(tileName))
        {
            tileName = $"地块_{tileID}";
        }

        // 更新名称显示
        if (tileNameText != null)
        {
            tileNameText.text = tileName;
        }
    }

    public virtual float OnPassed(Player player)
    {
        float maxEffectDuration = 0f;
        
        if (enableLinkedIncome && linkedBuildingTiles != null && linkedBuildingTiles.Count > 0)
        {
            maxEffectDuration = TriggerLinkedBuildingIncome(player);
        }
        
        return maxEffectDuration;
    }

    // 玩家落地时触发
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
                        UIManager.Instance.ShowToast($"获得工资 {salary}", 2f);
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

    // 处理地产落地
    private void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 未被拥有
            Debug.Log($"{tileName} 可购买 价格 {propertyPrice} 金币");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // 自己的地产
            Debug.Log($"{player.playerName} 到达自己的 {tileName}");
        }
        else
        {
            // 他人地产
            PayRent(player);
        }
    }

    // 支付租金
    private void PayRent(Player player)
    {
        int rent = CalculateRent();
        Debug.Log($"{player.playerName} 向 {ownerPlayer.playerName} 支付 {rent} 金币");

        if (player.PayCash(rent))
        {
            ownerPlayer.ReceiveCash(rent);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"支付租金 {rent} 给 {ownerPlayer.playerName}", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} 金币不足无法支付租金");
        }
    }

    // 计算租金
    public int CalculateRent()
    {
        int baseRent = rentPrice;

        // 如果有建筑则增加租金
        if (currentBuildingData != null)
        {
            baseRent += currentBuildingData.GetIncomeAmountByTurns(GetBuildingTurnsOwned());
        }

        return baseRent;
    }

    private float TriggerLinkedBuildingIncome(Player player)
    {
        if (!enableLinkedIncome)
            return 0f;

        if (linkedBuildingTiles == null || linkedBuildingTiles.Count == 0)
            return 0f;

        float currentTime = Time.time;
        int totalIncome = 0;
        float maxEffectDuration = 0f;

        Debug.Log($"TriggerLinkedBuildingIncome: 连接建筑数量={linkedBuildingTiles.Count}间隔={incomeInterval}");
        
        // 遍历所有连接的建筑
        for (int i = 0; i < linkedBuildingTiles.Count; i++)
        {
            BoardTile tile = linkedBuildingTiles[i];
            string dataName = tile.currentBuildingData?.buildingName ?? "";
            string ownerName = tile.ownerPlayer?.playerName ?? "";
            Debug.Log($"  [{i}]: {tile.name ?? "null"} - 建筑名: {dataName}, 拥有者: {ownerName}");
        }

        // 检查是否有 Income 建筑
        bool hasIncomeBuilding = false;
        List<BoardTile> incomeTiles = new List<BoardTile>();
        
        foreach (BoardTile buildingTile in linkedBuildingTiles)
        {
            if (buildingTile == null || buildingTile.currentBuildingData == null) continue;
            if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != player) continue;
            if (!CanGenerateIncome(buildingTile, currentTime)) continue;

            if (buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Income ||
                buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                int baseIncome = buildingTile.currentBuildingData.GetIncomeAmountByTurns(buildingTile.GetBuildingTurnsOwned());
                int incomeAmount = player.GetIncomeWithMultiplier(baseIncome);
                if (incomeAmount > 0)
                {
                    hasIncomeBuilding = true;
                    incomeTiles.Add(buildingTile);
                }
            }
        }

        // 检查 Buff 建筑并触发效果
        if (hasIncomeBuilding)
        {
            Debug.Log($"TriggerLinkedBuildingIncome: === 处理 Buff 建筑 ===");
            for (int i = 0; i < linkedBuildingTiles.Count; i++)
            {
                BoardTile buildingTile = linkedBuildingTiles[i];
                Debug.Log($"建筑 {i}: {buildingTile.name ?? "null"}");
                if (buildingTile == null) continue;

                bool canGenerate = CanGenerateIncome(buildingTile, currentTime);
                Debug.Log($"  - CanGenerateIncome: {canGenerate}");

                if (!canGenerate) continue;

                if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != player)
                {
                    Debug.Log($"  - 非当前玩家拥有");
                    continue;
                }

                if (buildingTile.currentBuildingData == null)
                {
                    Debug.Log($"  - 没有建筑数据");
                    continue;
                }

                // Buff效果在DiceEven检查后触发
                if (buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Buff)
                {
                    Debug.Log($"  - 触发 Buff 效果");
                    PlayBuildingEffect(buildingTile);
                    
                    if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
                    {
                        maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
                    }
                }
                else
                {
                    Debug.Log($"  - 触发 {buildingTile.currentBuildingData.functionType} 效果");
                }
            }
        }

        // 处理 Income 和 Mixed 建筑
        Debug.Log($"TriggerLinkedBuildingIncome: === 处理 Income/Mixed 建筑 ===\n");
        foreach (BoardTile buildingTile in incomeTiles)
        {
            Debug.Log($"建筑: {buildingTile.name ?? "null"}");
            
            int baseIncome = buildingTile.currentBuildingData.GetIncomeAmountByTurns(buildingTile.GetBuildingTurnsOwned());
            int incomeAmount = player.GetIncomeWithMultiplier(baseIncome);
            Debug.Log($"  - 基础收入: {baseIncome}, 实际收入: {incomeAmount}");
            
            player.ReceiveCash(incomeAmount);
            totalIncome += incomeAmount;

            if (!lastIncomeTime.ContainsKey(buildingTile))
            {
                lastIncomeTime.Add(buildingTile, currentTime);
                Debug.Log($"  - 添加 lastIncomeTime");
            }
            else
            {
                lastIncomeTime[buildingTile] = currentTime;
                Debug.Log($"  - 更新 lastIncomeTime");
            }

            Debug.Log($"  - 播放效果");
            PlayBuildingEffect(buildingTile);
            
            if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
            {
                maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
            }
        }

        if (totalIncome > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"获得收入 {totalIncome}", 2f);
        }

        Debug.Log($"TriggerLinkedBuildingIncome: 总收入={totalIncome}");

        return maxEffectDuration;
    }

    private void PlayBuildingEffect(BoardTile buildingTile)
    {
        if (buildingTile == null || buildingTile.currentBuildingData == null)
        {
            Debug.LogWarning($"PlayBuildingEffect: buildingTile或currentBuildingData为空");
            return;
        }

        BuildingData data = buildingTile.currentBuildingData;
        
        Debug.Log($"PlayBuildingEffect: 播放 {data.buildingName} 效果");
        Debug.Log($"  - effectIconPrefab: {(data.effectIconPrefab != null ? "已设置" : "null")}");
        Debug.Log($"  - effectSound: {(data.effectSound != null ? "已设置" : "null")}");
        
        if (data.effectIconPrefab != null || data.effectSound != null)
        {
            // 获取 BuildingEffectSystem
            if (BuildingEffectSystem.Instance == null)
            {
                Debug.LogWarning("BuildingEffectSystem.Instance不存在，正在创建...");
                GameObject effectSystemObj = new GameObject("BuildingEffectSystem_AutoCreated");
                effectSystemObj.AddComponent<BuildingEffectSystem>();
                
                if (BuildingEffectSystem.Instance == null)
                {
                    Debug.LogError("创建 BuildingEffectSystem 失败");
                    return;
                }
                Debug.Log("创建 BuildingEffectSystem 成功");
            }
            
            Transform effectTransform = buildingTile.transform;
            if (buildingTile.currentBuilding != null)
            {
                effectTransform = buildingTile.currentBuilding.transform;
                Debug.Log($"使用 transform: {buildingTile.currentBuilding.name}");
            }
            else
            {
                Debug.Log($"使用 transform: {buildingTile.name}");
            }
            
            BuildingEffectSystem.Instance.QueueBuildingEffect(effectTransform, data);
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

    // === 生成自动收入 ===
    private void GenerateAutoIncome()
    {
        if (currentBuildingData == null || ownerPlayer == null) return;

        int baseIncome = currentBuildingData.GetIncomeAmountByTurns(GetBuildingTurnsOwned());
        int incomeAmount = ownerPlayer.GetIncomeWithMultiplier(baseIncome);
        if (incomeAmount > 0)
        {
            ownerPlayer.ReceiveCash(incomeAmount);
            Debug.Log($"建筑 {currentBuildingData.buildingName} 自动获得收入 {incomeAmount}");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"获得收入 {incomeAmount}", 2f);
            }
        }
    }

    // 获取建筑拥有的回合数 - 当前回合 - 建造回合 + 1
    public int GetBuildingTurnsOwned()
    {
        if (GameManager.Instance == null || currentBuildingData == null)
        {
            return 1;
        }
        int currentRound = GameManager.Instance.CurrentRound;
        // 拥有回合数 = 当前回合 - 建造回合 + 1，最小为1
        return Mathf.Max(1, currentRound - buildingStartRound + 1);
    }

    // 设置建筑数据
    public void SetBuildingData(BuildingData data, int level = 1)
    {
        currentBuildingData = data;
        buildingLevel = level;
        // 记录建造回合
        buildingStartRound = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 0;

        if (data != null)
        {
            // 根据数据设置建筑类型
            currentBuildingType = GetBuildingTypeFromData(data);

            Debug.Log($"设置 {tileName}: 建筑 {data.buildingName}, 类型: {currentBuildingType}, 等级: {level}");

            // 如果不是收入型建筑
            if (data.functionType != BuildingData.BuildingFunctionType.Income &&
                data.functionType != BuildingData.BuildingFunctionType.Mixed)
            {
                Debug.LogWarning($"建筑 {data.functionType} 不是 Income 或 Mixed 类型");
            }
        }
        else
        {
            currentBuildingType = BuildingType.None;
            Debug.Log($"设置 {tileName}: 建筑类型为 None");
        }
    }
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GetBuildingTypeFromData: 数据为 null");
            return BuildingType.None;
        }

        // 从 BuildingData 获取 buildingType
        BoardTile.BuildingType type = data.buildingType;

        if (type == BuildingType.None)
        {
            // 需要推断类型
            Debug.LogWarning($"建筑 {data.buildingName} 的 buildingType未设置，尝试从名称推断");
            return InferBuildingTypeFromName(data.buildingName);
        }
        else
        {
            Debug.Log("使用已有类型");
            Debug.Log($"GetBuildingTypeFromData: 建筑 {data.buildingName} 类型: {type}");
            return type;
        }
    }
    private BuildingType InferBuildingTypeFromName(string buildingName)
    {
        string name = buildingName.ToLower();
        //从名称推断类型
        if (name.Contains("small") || name.Contains("小屋"))
            return BuildingType.SmallHouse;
        else if (name.Contains("medium") || name.Contains("中屋"))
            return BuildingType.MediumHouse;
        else if (name.Contains("large") || name.Contains("大屋"))
            return BuildingType.LargeHouse;
        else
            return BuildingType.Special;
    }

    // 获取升级费用
    public int GetUpgradeCost()
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return 0;

        return currentBuildingData.nextLevelBuilding.purchasePrice;
    }

    // 是否可以升级建筑
    public bool CanUpgradeBuilding(Player player)
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return false;

        if (ownerPlayer != player) return false;

        if (player.cash < GetUpgradeCost()) return false;

        // 检查地块等级
        if (!CheckScaleForUpgrade(currentBuildingData.nextLevelBuilding.requiredScale))
            return false;

        return true;
    }

    // 检查升级所需的地块等级
    public bool CheckScaleForUpgrade(BuildingData.Scale requiredScale)
    {
        return tileScale >= (int)requiredScale;
    }

    // 获取下一等级建筑
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
            // 获取下一等级数据
            BuildingData nextBuildingData = currentBuildingData.nextLevelBuilding;
            
            buildingLevel++;
            Debug.Log($"{player.playerName} 在 {tileName} 升级到等级 {buildingLevel}");

            // 更新建筑数据
            if (nextBuildingData != null)
            {
                currentBuildingData = nextBuildingData;
                // 更新建筑类型
                currentBuildingType = GetBuildingTypeFromData(nextBuildingData);
            }

            // 更新建筑对象
            if (nextBuildingData != null && nextBuildingData.buildingPrefab != null)
            {
                // 销毁旧建筑
                if (currentBuilding != null)
                {
                    Destroy(currentBuilding);
                }

                // 创建新建筑
                Vector3 pos = transform.position + nextBuildingData.positionOffset;
                Quaternion rot = Quaternion.Euler(nextBuildingData.rotationEuler);
                GameObject newBuilding = Instantiate(
                    nextBuildingData.buildingPrefab,
                    pos,
                    rot
                );
                newBuilding.transform.SetParent(transform);
                currentBuilding = newBuilding;
            }

            // 刷新Buff
            ClearBuffs();
            if (player != null)
            {
                ApplyBuffToPlayer(player);
            }

            // 触发连接建筑收入计算

            return true;
        }

        return false;
    }

    // 获取售价
    public int GetSellPrice()
    {
        if (currentBuildingData == null) return 0;

        float ratio = BuildingDataConfig.Instance != null ? BuildingDataConfig.Instance.GetSellPriceRatio() : 0.5f;

        // 增值建筑售价 = 购买价 + 增值
        if (currentBuildingData.functionType == BuildingData.BuildingFunctionType.Appreciation)
        {
            int roundsOwned = GetBuildingTurnsOwned();
            int appreciatedValue = currentBuildingData.GetAppreciatedValue(roundsOwned);
            return Mathf.RoundToInt(appreciatedValue * ratio);
        }

        // 普通建筑售价 = 总投资 * 比例
        int totalInvested = currentBuildingData.purchasePrice;
        
        BuildingData nextData = currentBuildingData.nextLevelBuilding;
        int tempLevel = buildingLevel;
        
        // 累加升级费用
        while (nextData != null && tempLevel > 1)
        {
            totalInvested += nextData.purchasePrice;
            nextData = nextData.nextLevelBuilding;
            tempLevel--;
        }

        // 售价 = 总投资 * 比例
        return Mathf.RoundToInt(totalInvested * ratio);
    }

    // 是否可以出售建筑
    public bool CanSellBuilding(Player player)
    {
        if (currentBuildingData == null) return false;
        if (ownerPlayer != player) return false;
        return true;
    }

    // 出售建筑
    public bool SellBuilding(Player player)
    {
        if (!CanSellBuilding(player)) return false;

        int sellPrice = GetSellPrice();
        player.ReceiveCash(sellPrice);

        Debug.Log($"{player.playerName} 出售 {tileName} 获得 {sellPrice}");

        // 销毁建筑对象
        if (currentBuilding != null)
        {
            Destroy(currentBuilding);
            currentBuilding = null;
        }

        // 重置建筑数据
        currentBuildingData = null;
        currentBuildingType = BuildingType.None;
        buildingLevel = 0;
        ownerPlayer = null;
        buildingStartRound = 0;

        return true;
    }

    // 应用Buff给玩家
    public void ApplyBuffToPlayer(Player player)
    {
        if (currentBuildingData == null || BuffSystem.Instance == null) return;

        if (currentBuildingData.functionType == BuildingData.BuildingFunctionType.Buff ||
            currentBuildingData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            List<BuildingData.BuildingBuffConfig> configs = currentBuildingData.GetBuffConfigs();
            
            foreach (var config in configs)
            {
                float buffValue = currentBuildingData.GetBuffValue(buildingLevel, config);
                string buffId = $"building_{GetInstanceID()}_{config.effectType}";
                
                BuffSystem.Buff buff = new BuffSystem.Buff(
                    buffId,
                    currentBuildingData.buildingName,
                    config.effectType,
                    buffValue,
                    config.isPermanent ? 0f : config.duration,
                    config.isPermanent ? 0 : config.durationRounds,
                    this,
                    config.customDescription
                );
                
                BuffSystem.Instance.AddBuff(player, buff);
                
                if (!buffedPlayers.Contains(player))
                {
                    buffedPlayers.Add(player);
                }
            }

            // 触发连接建筑收入计算
        }
    }

    // 清除Buff
    private void ClearBuffs()
    {
        if (BuffSystem.Instance != null)
        {
            foreach (Player player in buffedPlayers)
            {
                BuffSystem.Instance.RemoveAllBuffsFromSource(player, this);
            }
        }
        buffedPlayers.Clear();
    }

    // ???????
    private void DrawChanceCard(Player player)
    {
        // ?????
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(20, 101);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} ???? {gain} ?");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int lose = Random.Range(20, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} ??? {lose} ?");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);
                }
                break;

            case 3:
                // ???
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomTileIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    BoardTile targetTile = BoardManager.Instance.allTiles[randomTileIndex];
                    player.MoveToTile(targetTile, true);
                    Debug.Log($"{player.playerName} ????? {targetTile.tileName}");
                }
                break;
        }
    }

    // ?????????????
    private void DrawCommunityChestCard(Player player)
    {
        // ?????
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(50, 201);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} ??? {gain} ?");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int tax = Random.Range(50, 201);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} ??? {tax} ?");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;

            case 3:
                Debug.Log($"{player.playerName} ???Buff");

                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlaySFX(SFXClip.EventBuffActivated);
                }
                break;
        }
    }

    // ???
    private void PayTax(Player player)
    {
        int taxAmount = propertyPrice / 10; // 10%

        if (player.PayCash(taxAmount))
        {
            Debug.Log($"{player.playerName} ?????? {taxAmount}");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
        }
    }

    // ???????
    private void SendToJail(Player player)
    {
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        Debug.Log($"{player.playerName} ???? {player.jailTurnsRemaining} ???");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.EventGoToJail);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{player.playerName} ??????", 2f);
        }
    }

    // ??????????
    private void TriggerRandomEvent(Player player)
    {
        if (eventDataArray != null && eventDataArray.Length > 0)
        {
            EventData selectedEvent = eventDataArray[Random.Range(0, eventDataArray.Length)];
            
            if (selectedEvent != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowEventPanel(selectedEvent, player);
                Debug.Log($"{player.playerName} ???????: {selectedEvent.eventTitle}");
                return;
            }
        }

        // ?????
        TileEvent randomEvent = (TileEvent)Random.Range(1, 6);

        switch (randomEvent)
        {
            case TileEvent.GainMoney:
                int gain = Random.Range(50, 151);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} ??? {gain} ?");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case TileEvent.LoseMoney:
                int lose = Random.Range(30, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} ??? {lose} ?");

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
                    Debug.Log($"{player.playerName} ????? {targetTile.tileName}");
                }
                break;

            case TileEvent.GetOutOfJailFree:
                if (player.isInJail)
                {
                    player.isInJail = false;
                    player.jailTurnsRemaining = 0;
                    Debug.Log($"{player.playerName} ??????");
                }
                break;

            case TileEvent.PayTax:
                int tax = Random.Range(20, 81);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} ???? {tax} ?");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;
        }
    }

    // ???????
    public void UpdateTileVisual()
    {
        if (tileRenderer == null) return;

       
    }

    // ????????????
    public void AddLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }

        if (!linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Add(buildingTile);
            Debug.Log($"? {tileName} ???????? {buildingTile.tileName}");
        }
    }

    // ???????????
    public void RemoveLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles != null && linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Remove(buildingTile);
            Debug.Log($"?? {tileName} ??????? {buildingTile.tileName}");
        }
    }

    // ???????????
    public void ClearAllLinkedBuildingTiles()
    {
        if (linkedBuildingTiles != null)
        {
            linkedBuildingTiles.Clear();
            Debug.Log($"??? {tileName} ????");
        }
    }

    // ???????????
    public List<BoardTile> GetLinkedBuildingTiles()
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }
        return linkedBuildingTiles;
    }

    // ????/???????????
    public void SetLinkedIncomeEnabled(bool enabled)
    {
        enableLinkedIncome = enabled;
    }

    // ??????????
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = Mathf.Max(1.0f, interval); // ????1??
    }

    // ????/??????????
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

    // ?????????????
    public float GetLastIncomeTime(BoardTile buildingTile)
    {
        if (buildingTile == null) return 0f;

        if (lastIncomeTime.ContainsKey(buildingTile))
            return lastIncomeTime[buildingTile];

        return 0f; // ???0
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
