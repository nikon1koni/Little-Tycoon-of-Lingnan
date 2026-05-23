using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardTile : MonoBehaviour
{
    [Header("建筑类型")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None; //建筑类型

    [Header("基础属性")]
    public string tileName = "格子";
    public int tileID = 0;
    public int tileScale = 1; // 格子规模
    public int propertyPrice = 100; // 房产价格
    public int rentPrice = 10; // 租金
    public TileType tileType = TileType.Property;
    public bool isBuildable = false; // 是否可建造

    [Header("建筑数据")]
    public BuildingData currentBuildingData; // 当前建筑数据
    public BuildingType currentBuildingType = BuildingType.None;
    public int buildingLevel = 0; // 建筑等级
    public int buildingStartRound = 0; // 建筑开始回合（为了计算6级和1级）
    public GameObject currentBuilding; // 当前建筑物体
    public Player ownerPlayer; // 建筑所有者

    [Header("关联建筑设置")]
    [SerializeField] private List<BoardTile> linkedBuildingTiles; // 关联的建筑格子
    [SerializeField] private float incomeInterval = 5.0f; // 收益间隔(秒)
    private Dictionary<BoardTile, float> lastIncomeTime = new Dictionary<BoardTile, float>(); // 记录每个建筑的上次收益时间
    [SerializeField] private bool enableLinkedIncome = true; // 是否启用关联收益

    [Header("自动收益设置")]
    [SerializeField] private bool enableAutoIncome = false; // 是否启用自动收益
    [SerializeField] private float autoIncomeInterval = 10.0f; // 自动收益间隔
    private float lastAutoIncomeTime = 0f;

    [Header("事件数据")]
    public EventData[] eventDataArray; // 事件数据数组

    [Header("UI引用")]
    public TextMeshProUGUI tileNameText; // 格子名称文本
    public MeshRenderer tileRenderer; // 格子渲染器


    [Header("Buff玩家列表")]
    public List<Player> buffedPlayers = new List<Player>(); // 获得Buff的玩家

    // 格子类型枚举
    public enum TileType
    {
        Start,          // 起点
        Property,       // 地产
        Railroad,       // 铁路
        Utility,        // 公共设施
        Chance,         // 机会
        CommunityChest, // 公共宝箱
        Tax,            // 税务
        Jail,           // 监狱
        FreeParking,    // 免费停车
        GoToJail,       // 去监狱
        Buildable,      // 可建造
        BuildingSite,   // 建筑工地
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

    // 格子事件枚举
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

        // 获取渲染器
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
        // 自动收益逻辑
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
            tileName = $"格子_{tileID}";
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

    // 玩家落在格子上
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
                        UIManager.Instance.ShowToast($"经过起点获得{salary}金币", 2f);
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
            // 地产未被购买，显示购买面板
            Debug.Log($"{tileName} 地产未被购买，价格 {propertyPrice} 金币");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // 玩家自己的地产
            Debug.Log($"{player.playerName} 落在自己的地产 {tileName}");
        }
        else
        {
            // 其他玩家的地产，支付租金
            PayRent(player);
        }
    }

    // 支付租金
    private void PayRent(Player player)
    {
        int rent = CalculateRent();
        Debug.Log($"{player.playerName} 向 {ownerPlayer.playerName} 支付租金 {rent} 金币");

        if (player.PayCash(rent))
        {
            ownerPlayer.ReceiveCash(rent);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"支付 {rent} 金币给 {ownerPlayer.playerName}", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} 没有足够的金币支付租金");
        }
    }

    // 计算租金
    public int CalculateRent()
    {
        int baseRent = rentPrice;

        // 如果有建筑，增加建筑带来的收益
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

        Debug.Log($"TriggerLinkedBuildingIncome: 开始处理关联建筑收益，关联建筑数量={linkedBuildingTiles.Count}，收益间隔={incomeInterval}秒");
        
        // 打印所有关联建筑信息
        for (int i = 0; i < linkedBuildingTiles.Count; i++)
        {
            BoardTile tile = linkedBuildingTiles[i];
            string dataName = tile?.currentBuildingData?.buildingName ?? "无建筑数据";
            string ownerName = tile?.ownerPlayer?.playerName ?? "无所有者";
            Debug.Log($"  关联建筑[{i}]: {tile?.name ?? "null"} - 建筑数据: {dataName}, 建筑所有者: {ownerName}");
        }

        // 先遍历所有关联建筑，找出 Income 类型的建筑
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

        // 如果有收益建筑，先播放所有 Buff 建筑的特效，然后再播放 Income 建筑的特效
        if (hasIncomeBuilding)
        {
            Debug.Log($"TriggerLinkedBuildingIncome: === 开始处理 Buff 建筑特效 ===");
            for (int i = 0; i < linkedBuildingTiles.Count; i++)
            {
                BoardTile buildingTile = linkedBuildingTiles[i];
                Debug.Log($"建筑 {i}: {buildingTile?.name ?? "null"}");
                if (buildingTile == null) continue;

                bool canGenerate = CanGenerateIncome(buildingTile, currentTime);
                Debug.Log($"  - CanGenerateIncome: {canGenerate}");

                if (!canGenerate) continue;

                if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != player)
                {
                    Debug.Log($"  - 所有者不是当前玩家");
                    continue;
                }

                if (buildingTile.currentBuildingData == null)
                {
                    Debug.Log($"  - 没有建筑数据，跳过");
                    continue;
                }

                // 只对 Buff 类型的建筑播放特效，DiceEven 类型的建筑会在 CheckDiceEvenBuildings 中处理
                if (buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Buff)
                {
                    Debug.Log($"  - 触发 Buff 类型建筑，播放特效");
                    PlayBuildingEffect(buildingTile);
                    
                    if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
                    {
                        maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
                    }
                }
                else
                {
                    Debug.Log($"  - 跳过 {buildingTile.currentBuildingData.functionType} 类型");
                }
            }
        }

        // 现在处理 Income 或 Mixed 类型的建筑并播放特效
        Debug.Log($"TriggerLinkedBuildingIncome: === 开始处理 Income/Mixed 建筑 ===\n");
        foreach (BoardTile buildingTile in incomeTiles)
        {
            Debug.Log($"建筑: {buildingTile?.name ?? "null"}");
            
            int baseIncome = buildingTile.currentBuildingData.GetIncomeAmountByTurns(buildingTile.GetBuildingTurnsOwned());
            int incomeAmount = player.GetIncomeWithMultiplier(baseIncome);
            Debug.Log($"  - 基础收益: {baseIncome}, 最终收益: {incomeAmount}");
            
            player.ReceiveCash(incomeAmount);
            totalIncome += incomeAmount;

            if (!lastIncomeTime.ContainsKey(buildingTile))
            {
                lastIncomeTime.Add(buildingTile, currentTime);
                Debug.Log($"  - 添加 lastIncomeTime记录");
            }
            else
            {
                lastIncomeTime[buildingTile] = currentTime;
                Debug.Log($"  - 更新 lastIncomeTime记录");
            }

            Debug.Log($"  - 播放建筑特效");
            PlayBuildingEffect(buildingTile);
            
            if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
            {
                maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
            }
        }

        if (totalIncome > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"关联建筑收益{totalIncome} 金币", 2f);
        }

        Debug.Log($"TriggerLinkedBuildingIncome: 总收益={totalIncome}");

        return maxEffectDuration;
    }

    private void PlayBuildingEffect(BoardTile buildingTile)
    {
        if (buildingTile == null || buildingTile.currentBuildingData == null)
        {
            Debug.LogWarning($"PlayBuildingEffect: buildingTile 或 currentBuildingData 为空");
            return;
        }

        BuildingData data = buildingTile.currentBuildingData;
        
        Debug.Log($"PlayBuildingEffect: 为 {data.buildingName} 播放特效");
        Debug.Log($"  - effectIconPrefab: {(data.effectIconPrefab != null ? "已设置" : "未设置")}");
        Debug.Log($"  - effectSound: {(data.effectSound != null ? "已设置" : "未设置")}");
        
        if (data.effectIconPrefab != null || data.effectSound != null)
        {
            // 确保 BuildingEffectSystem 存在
            if (BuildingEffectSystem.Instance == null)
            {
                Debug.LogWarning("BuildingEffectSystem.Instance 不存在，尝试创建...");
                GameObject effectSystemObj = new GameObject("BuildingEffectSystem_AutoCreated");
                effectSystemObj.AddComponent<BuildingEffectSystem>();
                
                if (BuildingEffectSystem.Instance == null)
                {
                    Debug.LogError("创建 BuildingEffectSystem 失败");
                    return;
                }
                Debug.Log("已自动创建 BuildingEffectSystem");
            }
            
            Transform effectTransform = buildingTile.transform;
            if (buildingTile.currentBuilding != null)
            {
                effectTransform = buildingTile.currentBuilding.transform;
                Debug.Log($"使用建筑物体的 transform: {buildingTile.currentBuilding.name}");
            }
            else
            {
                Debug.Log($"使用格子的 transform: {buildingTile.name}");
            }
            
            BuildingEffectSystem.Instance.QueueBuildingEffect(effectTransform, data);
        }
    }

    // 检查是否可以产生收益
    private bool CanGenerateIncome(BoardTile buildingTile, float currentTime)
    {
        if (!lastIncomeTime.ContainsKey(buildingTile))
            return true;

        float timeSinceLastIncome = currentTime - lastIncomeTime[buildingTile];
        return timeSinceLastIncome >= incomeInterval;
    }

    // === 自动收益方法 ===
    private void GenerateAutoIncome()
    {
        if (currentBuildingData == null || ownerPlayer == null) return;

        int baseIncome = currentBuildingData.GetIncomeAmountByTurns(GetBuildingTurnsOwned());
        int incomeAmount = ownerPlayer.GetIncomeWithMultiplier(baseIncome);
        if (incomeAmount > 0)
        {
            ownerPlayer.ReceiveCash(incomeAmount);
            Debug.Log($"建筑 {currentBuildingData.buildingName} 自动产生收益{incomeAmount} 金币");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"自动收益{incomeAmount} 金币", 2f);
            }
        }
    }

    // 获取建筑已经拥有的回合数 - 当前回合 - 开始回合 + 1
    public int GetBuildingTurnsOwned()
    {
        if (GameManager.Instance == null || currentBuildingData == null)
        {
            return 1;
        }
        int currentRound = GameManager.Instance.CurrentRound;
        // 拥有回合 = 当前回合 - 开始回合 + 1，至少为1回合
        return Mathf.Max(1, currentRound - buildingStartRound + 1);
    }

    // 设置建筑数据
    public void SetBuildingData(BuildingData data, int level = 1)
    {
        currentBuildingData = data;
        buildingLevel = level;
        // 记录建筑开始回合（为了计算6级和1级）
        buildingStartRound = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 0;

        if (data != null)
        {
            // 确定建筑类型
            currentBuildingType = GetBuildingTypeFromData(data);

            Debug.Log($"格子 {tileName}: 设置建筑 {data.buildingName}, 类型{currentBuildingType}, 等级{level}");

            // 验证建筑类型
            if (data.functionType != BuildingData.BuildingFunctionType.Income &&
                data.functionType != BuildingData.BuildingFunctionType.Mixed)
            {
                Debug.LogWarning($"注意：此建筑功能类型为 {data.functionType}，可能不会产生 Income 或 Mixed 收益");
            }
        }
        else
        {
            currentBuildingType = BuildingType.None;
            Debug.Log($"格子 {tileName}: 移除建筑，设置类型为 None");
        }
    }
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GetBuildingTypeFromData: 建筑数据为 null");
            return BuildingType.None;
        }

        // 优先使用 BuildingData 中的 buildingType 字段
        BoardTile.BuildingType type = data.buildingType;

        if (type == BuildingType.None)
        {
            // 如果没有设置，尝试从名字推断
            Debug.LogWarning($"建筑 {data.buildingName} 的 buildingType 未设置，尝试从名字推断");
            return InferBuildingTypeFromName(data.buildingName);
        }
        else
        {
            Debug.Log("建筑数据中已有类型，直接使用");
            Debug.Log($"GetBuildingTypeFromData: 从 {data.buildingName} 得到类型{type}");
            return type;
        }
    }
    private BuildingType InferBuildingTypeFromName(string buildingName)
    {
        string name = buildingName.ToLower();
        //尝试推断类型
        if (name.Contains("small") || name.Contains("小"))
            return BuildingType.SmallHouse;
        else if (name.Contains("medium") || name.Contains("中"))
            return BuildingType.MediumHouse;
        else if (name.Contains("large") || name.Contains("大"))
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

    // 检查是否可以升级
    public bool CanUpgradeBuilding(Player player)
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return false;

        if (ownerPlayer != player) return false;

        if (player.cash < GetUpgradeCost()) return false;

        // 检查规模
        if (!CheckScaleForUpgrade(currentBuildingData.nextLevelBuilding.requiredScale))
            return false;

        return true;
    }

    // 检查格子规模是否符合升级要求
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
            // 获取下一级建筑数据
            BuildingData nextBuildingData = currentBuildingData.nextLevelBuilding;
            
            buildingLevel++;
            Debug.Log($"{player.playerName} 在格子 {tileName} 将建筑升级到 {buildingLevel} 级");

            // 更新建筑数据
            if (nextBuildingData != null)
            {
                currentBuildingData = nextBuildingData;
                // 更新建筑类型
                currentBuildingType = GetBuildingTypeFromData(nextBuildingData);
            }

            // 更新建筑预制体
            if (nextBuildingData != null && nextBuildingData.buildingPrefab != null)
            {
                // 销毁旧建筑
                if (currentBuilding != null)
                {
                    Destroy(currentBuilding);
                }

                // 创建新建筑
                GameObject newBuilding = Instantiate(
                    nextBuildingData.buildingPrefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.identity
                );
                newBuilding.transform.SetParent(transform);
                currentBuilding = newBuilding;
            }

            // 清除旧Buff并应用新Buff
            ClearBuffs();
            if (player != null)
            {
                ApplyBuffToPlayer(player);
            }

            // 升级后不需要立即播放特效，会在TriggerLinkedBuildingIncome中处理

            return true;
        }

        return false;
    }

    // 获取出售价格
    public int GetSellPrice()
    {
        if (currentBuildingData == null) return 0;
        
        // 计算总投资
        int totalInvested = currentBuildingData.purchasePrice;
        
        BuildingData nextData = currentBuildingData.nextLevelBuilding;
        int tempLevel = buildingLevel;
        
        // 累加各级升级费用
        while (nextData != null && tempLevel > 1)
        {
            totalInvested += nextData.purchasePrice;
            nextData = nextData.nextLevelBuilding;
            tempLevel--;
        }
        
        // 获取出售价格比例
        float ratio = BuildingDataConfig.Instance != null ? BuildingDataConfig.Instance.GetSellPriceRatio() : 0.5f;
        
        // 出售价格 = 总投资 × 出售比例
        return Mathf.RoundToInt(totalInvested * ratio);
    }

    // 检查是否可以出售建筑
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

        Debug.Log($"{player.playerName} 在格子 {tileName}出售建筑，获得 {sellPrice} 金币");

        // 销毁建筑物体
        if (currentBuilding != null)
        {
            Destroy(currentBuilding);
            currentBuilding = null;
        }

        // 清除建筑数据
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

            // 应用Buff后不需要立即播放特效，会在TriggerLinkedBuildingIncome中处理
        }
    }

    // 清除Buff效果
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

    // 抽取机会卡
    private void DrawChanceCard(Player player)
    {
        // 简化版机会卡
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(20, 101);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 抽到机会卡，获得 {gain} 金币");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int lose = Random.Range(20, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} 抽到机会卡，失去 {lose} 金币");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);
                }
                break;

            case 3:
                // 移动到随机位置
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomTileIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    BoardTile targetTile = BoardManager.Instance.allTiles[randomTileIndex];
                    player.MoveToTile(targetTile, true);
                    Debug.Log($"{player.playerName} 抽到机会卡，移动到 {targetTile.tileName}");
                }
                break;
        }
    }

    // 抽取公共宝箱卡
    private void DrawCommunityChestCard(Player player)
    {
        // 简化版公共宝箱
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(50, 201);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 抽到公共宝箱卡，获得 {gain} 金币");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int tax = Random.Range(50, 201);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} 抽到公共宝箱卡，缴纳税金 {tax} 金币");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;

            case 3:
                Debug.Log($"{player.playerName} 抽到公共宝箱卡，获得Buff");

                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlaySFX(SFXClip.EventBuffActivated);
                }
                break;
        }
    }

    // 支付税金
    private void PayTax(Player player)
    {
        int taxAmount = propertyPrice / 10; // 收取房产价格的10%

        if (player.PayCash(taxAmount))
        {
            Debug.Log($"{player.playerName} 支付税金{taxAmount} 金币");

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
            UIManager.Instance.ShowToast($"{player.playerName} 被送进监狱", 2f);
        }
    }

    // 触发随机事件
    private void TriggerRandomEvent(Player player)
    {
        if (eventDataArray != null && eventDataArray.Length > 0)
        {
            EventData selectedEvent = eventDataArray[Random.Range(0, eventDataArray.Length)];
            
            if (selectedEvent != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowEventPanel(selectedEvent, player);
                Debug.Log($"{player.playerName} 触发事件: {selectedEvent.eventTitle}");
                return;
            }
        }

        // 如果没有设置事件数据，使用旧的随机事件
        TileEvent randomEvent = (TileEvent)Random.Range(1, 6);

        switch (randomEvent)
        {
            case TileEvent.GainMoney:
                int gain = Random.Range(50, 151);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} 触发获得金币事件，获得 {gain} 金币");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case TileEvent.LoseMoney:
                int lose = Random.Range(30, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} 触发失去金币事件，失去 {lose} 金币");

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
                    Debug.Log($"{player.playerName} 触发移动事件，移动到 {targetTile.tileName}");
                }
                break;

            case TileEvent.GetOutOfJailFree:
                if (player.isInJail)
                {
                    player.isInJail = false;
                    player.jailTurnsRemaining = 0;
                    Debug.Log($"{player.playerName} 触发出狱事件");
                }
                break;

            case TileEvent.PayTax:
                int tax = Random.Range(20, 81);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} 触发纳税事件，支付 {tax} 金币");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;
        }
    }

    // 更新格子外观
    public void UpdateTileVisual()
    {
        if (tileRenderer == null) return;

       
    }

    // 添加关联建筑格子
    public void AddLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }

        if (!linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Add(buildingTile);
            Debug.Log($"格子 {tileName} 添加关联建筑格子 {buildingTile.tileName}");
        }
    }

    // 移除关联建筑格子
    public void RemoveLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles != null && linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Remove(buildingTile);
            Debug.Log($"格子 {tileName} 移除关联建筑格子 {buildingTile.tileName}");
        }
    }

    // 清除所有关联建筑格子
    public void ClearAllLinkedBuildingTiles()
    {
        if (linkedBuildingTiles != null)
        {
            linkedBuildingTiles.Clear();
            Debug.Log($"格子 {tileName} 清除所有关联建筑格子");
        }
    }

    // 获取关联建筑格子列表
    public List<BoardTile> GetLinkedBuildingTiles()
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }
        return linkedBuildingTiles;
    }

    // 设置/取消关联收益
    public void SetLinkedIncomeEnabled(bool enabled)
    {
        enableLinkedIncome = enabled;
    }

    // 设置收益间隔
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = Mathf.Max(1.0f, interval); // 最少1秒
    }

    // 设置/取消自动收益
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

    // 获取上次收益时间（用于调试）
    public float GetLastIncomeTime(BoardTile buildingTile)
    {
        if (buildingTile == null) return 0f;

        if (lastIncomeTime.ContainsKey(buildingTile))
            return lastIncomeTime[buildingTile];

        return 0f; // 如果没有记录返回0
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
