using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardTile : MonoBehaviour
{
    [Header("")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None;

    [Header("")]
    public string tileName = "";
    public int tileID = 0;
    public int tileScale = 1; // 
    public int propertyPrice = 100; // 
    public int rentPrice = 10; // 
    public TileType tileType = TileType.Property;
    public bool isBuildable = false; // 

    [Header("Harvest 收获格设置")]
    [Tooltip("每次踩到必得的基础铜钱下限（含）")]
    public int harvestBaseMoneyMin = 5;
    [Tooltip("每次踩到必得的基础铜钱上限（含）")]
    public int harvestBaseMoneyMax = 10;
    [Tooltip("额外奖励抽到“卡牌”的概率(0~1)，其余概率改为给额外铜钱；默认0.5即55开")]
    [Range(0f, 1f)] public float harvestCardChance = 0.5f;
    [Tooltip("额外铜钱的随机下限（含）")]
    public int harvestExtraMoneyMin = 5;
    [Tooltip("额外铜钱的随机上限（含）")]
    public int harvestExtraMoneyMax = 10;

    [Header("")]
    public BuildingData currentBuildingData; // 
    public BuildingType currentBuildingType = BuildingType.None;
    public int buildingLevel = 0; // 
    public int buildingStartRound = 0; // 
    public GameObject currentBuilding; // 
    public Player ownerPlayer; // 

    [Header("")]
    [SerializeField] private List<BoardTile> linkedBuildingTiles; // 
    [SerializeField] private float incomeInterval = 5.0f; // 
    private Dictionary<BoardTile, float> lastIncomeTime = new Dictionary<BoardTile, float>(); // 
    [SerializeField] private bool enableLinkedIncome = true; // 

    [Header("")]
    [SerializeField] private bool enableAutoIncome = false; // 
    [SerializeField] private float autoIncomeInterval = 10.0f; // 
    private float lastAutoIncomeTime = 0f;

    [Header("")]
    public EventData[] eventDataArray; // 

    [Header("UI")]
    public TextMeshProUGUI tileNameText; // 
    public MeshRenderer tileRenderer; // 


    [Header("Buff")]
    public List<Player> buffedPlayers = new List<Player>(); // Buff

    // 
    public enum TileType
    {
        Start,          // 
        Property,       // 
        Railroad,       // 
        Utility,        // 
        Chance,         // 
        CommunityChest, // 
        Tax,            // ?
        Jail,           // 
        FreeParking,    // 
        GoToJail,       // 
        Buildable,      // 
        BuildingSite,   // 
        Event,          // 
        Normal,
        Harvest,      // 
        LoseMoney       // 
    }

    // 
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

    // 
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

        // 
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
        // 
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
        // 
        if (string.IsNullOrEmpty(tileName))
        {
            tileName = $"_{tileID}";
        }

        // 
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

    // 
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
                        UIManager.Instance.ShowToast($" {salary}", 2f);
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

            case TileType.Harvest:
                HandleHarvestTile(player);
                break;

            case TileType.LoseMoney:
                HandleLoseMoneyTile(player);
                break;
        }
    }

    // 
    private void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // 
            Debug.Log($"{tileName}   {propertyPrice} ");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // 
            Debug.Log($"{player.playerName}  {tileName}");
        }
        else
        {
            // 
            PayRent(player);
        }
    }

    // 
    private void PayRent(Player player)
    {
        int rent = CalculateRent();
        Debug.Log($"{player.playerName}  {ownerPlayer.playerName}  {rent} ");

        if (player.PayCash(rent))
        {
            ownerPlayer.ReceiveCash(rent);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($" {rent}  {ownerPlayer.playerName}", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} ");
        }
    }

    // 
    public int CalculateRent()
    {
        int baseRent = rentPrice;

        // 
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

        Debug.Log($"TriggerLinkedBuildingIncome: ={linkedBuildingTiles.Count}={incomeInterval}");
        
        // 
        for (int i = 0; i < linkedBuildingTiles.Count; i++)
        {
            BoardTile tile = linkedBuildingTiles[i];
            string dataName = tile.currentBuildingData?.buildingName ?? "";
            string ownerName = tile.ownerPlayer?.playerName ?? "";
            Debug.Log($"  [{i}]: {tile.name ?? "null"} - : {dataName}, : {ownerName}");
        }

        //  Income 
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

        //  Buff 
        if (hasIncomeBuilding)
        {
            Debug.Log($"TriggerLinkedBuildingIncome: ===  Buff  ===");
            for (int i = 0; i < linkedBuildingTiles.Count; i++)
            {
                BoardTile buildingTile = linkedBuildingTiles[i];
                Debug.Log($" {i}: {buildingTile.name ?? "null"}");
                if (buildingTile == null) continue;

                bool canGenerate = CanGenerateIncome(buildingTile, currentTime);
                Debug.Log($"  - CanGenerateIncome: {canGenerate}");

                if (!canGenerate) continue;

                if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != player)
                {
                    Debug.Log($"  - ");
                    continue;
                }

                if (buildingTile.currentBuildingData == null)
                {
                    Debug.Log($"  - ");
                    continue;
                }

                // BuffDiceEven
                if (buildingTile.currentBuildingData.functionType == BuildingData.BuildingFunctionType.Buff)
                {
                    Debug.Log($"  -  Buff ");
                    PlayBuildingEffect(buildingTile);
                    
                    if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
                    {
                        maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
                    }
                }
                else
                {
                    Debug.Log($"  -  {buildingTile.currentBuildingData.functionType} ");
                }
            }
        }

        //  Income  Mixed 
        Debug.Log($"TriggerLinkedBuildingIncome: ===  Income/Mixed  ===\n");
        foreach (BoardTile buildingTile in incomeTiles)
        {
            Debug.Log($": {buildingTile.name ?? "null"}");
            
            int baseIncome = buildingTile.currentBuildingData.GetIncomeAmountByTurns(buildingTile.GetBuildingTurnsOwned());
            int incomeAmount = player.GetIncomeWithMultiplier(baseIncome);
            Debug.Log($"  - : {baseIncome}, : {incomeAmount}");
            
            player.ReceiveCash(incomeAmount);
            totalIncome += incomeAmount;

            if (!lastIncomeTime.ContainsKey(buildingTile))
            {
                lastIncomeTime.Add(buildingTile, currentTime);
                Debug.Log($"  -  lastIncomeTime");
            }
            else
            {
                lastIncomeTime[buildingTile] = currentTime;
                Debug.Log($"  -  lastIncomeTime");
            }

            Debug.Log($"  - ");
            PlayBuildingEffect(buildingTile);
            
            if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
            {
                maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
            }
        }

        if (totalIncome > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"获得建筑收入 {totalIncome} 铜钱", 2f);
        }

        Debug.Log($"TriggerLinkedBuildingIncome: ={totalIncome}");

        return maxEffectDuration;
    }

    private void PlayBuildingEffect(BoardTile buildingTile)
    {
        if (buildingTile == null || buildingTile.currentBuildingData == null)
        {
            Debug.LogWarning($"PlayBuildingEffect: buildingTilecurrentBuildingData");
            return;
        }

        BuildingData data = buildingTile.currentBuildingData;
        
        Debug.Log($"PlayBuildingEffect:  {data.buildingName} ");
        Debug.Log($"  - effectIconPrefab: {(data.effectIconPrefab != null ? "" : "null")}");
        Debug.Log($"  - effectSound: {(data.effectSound != null ? "" : "null")}");
        
        if (data.effectIconPrefab != null || data.effectSound != null)
        {
            //  BuildingEffectSystem
            if (BuildingEffectSystem.Instance == null)
            {
                Debug.LogWarning("BuildingEffectSystem.Instance...");
                GameObject effectSystemObj = new GameObject("BuildingEffectSystem_AutoCreated");
                effectSystemObj.AddComponent<BuildingEffectSystem>();
                
                if (BuildingEffectSystem.Instance == null)
                {
                    Debug.LogError(" BuildingEffectSystem ");
                    return;
                }
                Debug.Log(" BuildingEffectSystem ");
            }
            
            Transform effectTransform = buildingTile.transform;
            if (buildingTile.currentBuilding != null)
            {
                effectTransform = buildingTile.currentBuilding.transform;
                Debug.Log($" transform: {buildingTile.currentBuilding.name}");
            }
            else
            {
                Debug.Log($" transform: {buildingTile.name}");
            }
            
            BuildingEffectSystem.Instance.QueueBuildingEffect(effectTransform, data);
        }
    }

    // 
    private bool CanGenerateIncome(BoardTile buildingTile, float currentTime)
    {
        if (!lastIncomeTime.ContainsKey(buildingTile))
            return true;

        float timeSinceLastIncome = currentTime - lastIncomeTime[buildingTile];
        return timeSinceLastIncome >= incomeInterval;
    }

    // ===  ===
    private void GenerateAutoIncome()
    {
        if (currentBuildingData == null || ownerPlayer == null) return;

        int baseIncome = currentBuildingData.GetIncomeAmountByTurns(GetBuildingTurnsOwned());
        int incomeAmount = ownerPlayer.GetIncomeWithMultiplier(baseIncome);
        if (incomeAmount > 0)
        {
            ownerPlayer.ReceiveCash(incomeAmount);
            Debug.Log($" {currentBuildingData.buildingName}  {incomeAmount}");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($" {incomeAmount}", 2f);
            }
        }
    }

    //  -  -  + 1
    public int GetBuildingTurnsOwned()
    {
        if (GameManager.Instance == null || currentBuildingData == null)
        {
            return 1;
        }
        int currentRound = GameManager.Instance.CurrentRound;
        //  =  -  + 11
        return Mathf.Max(1, currentRound - buildingStartRound + 1);
    }

    // 
    public void SetBuildingData(BuildingData data, int level = 1)
    {
        currentBuildingData = data;
        buildingLevel = level;
        // 
        buildingStartRound = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 0;

        if (data != null)
        {
            // 
            currentBuildingType = GetBuildingTypeFromData(data);

            Debug.Log($" {tileName}:  {data.buildingName}, : {currentBuildingType}, : {level}");

            // 
            if (data.functionType != BuildingData.BuildingFunctionType.Income &&
                data.functionType != BuildingData.BuildingFunctionType.Mixed)
            {
                Debug.LogWarning($" {data.functionType}  Income  Mixed ");
            }
        }
        else
        {
            currentBuildingType = BuildingType.None;
            Debug.Log($" {tileName}:  None");
        }
    }
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GetBuildingTypeFromData:  null");
            return BuildingType.None;
        }

        //  BuildingData  buildingType
        BoardTile.BuildingType type = data.buildingType;

        if (type == BuildingType.None)
        {
            // 
            Debug.LogWarning($" {data.buildingName}  buildingType");
            return InferBuildingTypeFromName(data.buildingName);
        }
        else
        {
            Debug.Log("");
            Debug.Log($"GetBuildingTypeFromData:  {data.buildingName} : {type}");
            return type;
        }
    }
    private BuildingType InferBuildingTypeFromName(string buildingName)
    {
        string name = buildingName.ToLower();
        //
        if (name.Contains("small") || name.Contains(""))
            return BuildingType.SmallHouse;
        else if (name.Contains("medium") || name.Contains(""))
            return BuildingType.MediumHouse;
        else if (name.Contains("large") || name.Contains(""))
            return BuildingType.LargeHouse;
        else
            return BuildingType.Special;
    }

    // 
    public int GetUpgradeCost()
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return 0;

        return currentBuildingData.nextLevelBuilding.purchasePrice;
    }

    // 
    public bool CanUpgradeBuilding(Player player)
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return false;

        if (ownerPlayer != player) return false;

        if (player.cash < GetUpgradeCost()) return false;

        // 
        if (!CheckScaleForUpgrade(currentBuildingData.nextLevelBuilding.requiredScale))
            return false;

        return true;
    }

    // 
    public bool CheckScaleForUpgrade(BuildingData.Scale requiredScale)
    {
        return tileScale >= (int)requiredScale;
    }

    // 
    public BuildingData GetNextUpgradeBuilding()
    {
        if (currentBuildingData == null) return null;
        return currentBuildingData.nextLevelBuilding;
    }

    // 
    public bool UpgradeBuilding(Player player)
    {
        if (!CanUpgradeBuilding(player)) return false;

        int upgradeCost = GetUpgradeCost();

        if (player.PayCash(upgradeCost))
        {
            // 
            BuildingData nextBuildingData = currentBuildingData.nextLevelBuilding;
            
            buildingLevel++;
            Debug.Log($"{player.playerName}  {tileName}  {buildingLevel}");

            // 
            if (nextBuildingData != null)
            {
                currentBuildingData = nextBuildingData;
                // 
                currentBuildingType = GetBuildingTypeFromData(nextBuildingData);
            }

            // 
            if (nextBuildingData != null && nextBuildingData.buildingPrefab != null)
            {
                // 
                if (currentBuilding != null)
                {
                    Destroy(currentBuilding);
                }

                // 
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

            // Buff
            ClearBuffs();
            if (player != null)
            {
                ApplyBuffToPlayer(player);
            }

            // 

            return true;
        }

        return false;
    }

    // 
    public int GetSellPrice()
    {
        if (currentBuildingData == null) return 0;

        float ratio = BuildingDataConfig.Instance != null ? BuildingDataConfig.Instance.GetSellPriceRatio() : 0.5f;

        //  =  + 
        if (currentBuildingData.functionType == BuildingData.BuildingFunctionType.Appreciation)
        {
            int roundsOwned = GetBuildingTurnsOwned();
            int appreciatedValue = currentBuildingData.GetAppreciatedValue(roundsOwned);
            return Mathf.RoundToInt(appreciatedValue * ratio);
        }

        //  =  * 
        int totalInvested = currentBuildingData.purchasePrice;
        
        BuildingData nextData = currentBuildingData.nextLevelBuilding;
        int tempLevel = buildingLevel;
        
        // 
        while (nextData != null && tempLevel > 1)
        {
            totalInvested += nextData.purchasePrice;
            nextData = nextData.nextLevelBuilding;
            tempLevel--;
        }

        //  =  * 
        return Mathf.RoundToInt(totalInvested * ratio);
    }

    // 
    public bool CanSellBuilding(Player player)
    {
        if (currentBuildingData == null) return false;
        if (ownerPlayer != player) return false;
        return true;
    }

    // 
    public bool SellBuilding(Player player)
    {
        if (!CanSellBuilding(player)) return false;

        int sellPrice = GetSellPrice();
        player.ReceiveCash(sellPrice);

        Debug.Log($"{player.playerName}  {tileName}  {sellPrice}");

        // 
        if (currentBuilding != null)
        {
            Destroy(currentBuilding);
            currentBuilding = null;
        }

        // 
        currentBuildingData = null;
        currentBuildingType = BuildingType.None;
        buildingLevel = 0;
        ownerPlayer = null;
        buildingStartRound = 0;

        return true;
    }

    // Buff
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

            // 
        }
    }

    // Buff
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

    // 
    private void DrawChanceCard(Player player)
    {
        // 
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(20, 101);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName}  {gain}");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int lose = Random.Range(20, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName}  {lose}");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);
                }
                break;

            case 3:
                // 
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomTileIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    BoardTile targetTile = BoardManager.Instance.allTiles[randomTileIndex];
                    player.MoveToTile(targetTile, true);
                    Debug.Log($"{player.playerName}  {targetTile.tileName}");
                }
                break;
        }
    }

    // 
    private void DrawCommunityChestCard(Player player)
    {
        // 
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(50, 201);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName}  {gain}");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int tax = Random.Range(50, 201);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName}  {tax}");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;

            case 3:
                Debug.Log($"{player.playerName} Buff");

                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlaySFX(SFXClip.EventBuffActivated);
                }
                break;
        }
    }

    // 
    private void PayTax(Player player)
    {
        int taxAmount = propertyPrice / 10; // 10%

        if (player.PayCash(taxAmount))
        {
            Debug.Log($"{player.playerName}  {taxAmount}");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
        }
    }

    // 
    private void SendToJail(Player player)
    {
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        Debug.Log($"{player.playerName}  {player.jailTurnsRemaining} ");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.EventGoToJail);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{player.playerName} ", 2f);
        }
    }

    // 
    private void TriggerRandomEvent(Player player)
    {
        if (eventDataArray != null && eventDataArray.Length > 0)
        {
            int randomIndex = Random.Range(0, eventDataArray.Length);
            Debug.Log($"[] : {tileName}, : {eventDataArray.Length}, : {randomIndex}");
            
            for (int i = 0; i < eventDataArray.Length; i++)
            {
                if (eventDataArray[i] != null)
                {
                    Debug.Log($"  - {i}: {eventDataArray[i].eventTitle}");
                }
                else
                {
                    Debug.Log($"  - {i}: ");
                }
            }
            
            EventData selectedEvent = eventDataArray[randomIndex];
            
            if (selectedEvent != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowEventPanel(selectedEvent, player);
                Debug.Log($"{player.playerName} : {selectedEvent.eventTitle}");
                return;
            }
        }

        // 
        TileEvent randomEvent = (TileEvent)Random.Range(1, 6);

        switch (randomEvent)
        {
            case TileEvent.GainMoney:
                int gain = Random.Range(50, 151);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName}  {gain}");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case TileEvent.LoseMoney:
                int lose = Random.Range(30, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName}  {lose}");

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
                    Debug.Log($"{player.playerName}  {targetTile.tileName}");
                }
                break;

            case TileEvent.GetOutOfJailFree:
                if (player.isInJail)
                {
                    player.isInJail = false;
                    player.jailTurnsRemaining = 0;
                    Debug.Log($"{player.playerName} ");
                }
                break;

            case TileEvent.PayTax:
                int tax = Random.Range(20, 81);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName}  {tax}");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;
        }
    }

    // 
    public void UpdateTileVisual()
    {
        if (tileRenderer == null) return;

       
    }

    // 
    public void AddLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }

        if (!linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Add(buildingTile);
            Debug.Log($" {tileName}  {buildingTile.tileName}");
        }
    }

    // 
    public void RemoveLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles != null && linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Remove(buildingTile);
            Debug.Log($" {tileName}  {buildingTile.tileName}");
        }
    }

    // 
    public void ClearAllLinkedBuildingTiles()
    {
        if (linkedBuildingTiles != null)
        {
            linkedBuildingTiles.Clear();
            Debug.Log($" {tileName} ");
        }
    }

    // 
    public List<BoardTile> GetLinkedBuildingTiles()
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }
        return linkedBuildingTiles;
    }

    // 
    public void SetLinkedIncomeEnabled(bool enabled)
    {
        enableLinkedIncome = enabled;
    }

    // 
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = Mathf.Max(1.0f, interval); // 1
    }

    // 
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

    // 
    public float GetLastIncomeTime(BoardTile buildingTile)
    {
        if (buildingTile == null) return 0f;

        if (lastIncomeTime.ContainsKey(buildingTile))
            return lastIncomeTime[buildingTile];

        return 0f; // 0
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

    private void HandleHarvestTile(Player player)
    {
        // 每次踩到必得的基础铜钱
        int baseAmount = Random.Range(harvestBaseMoneyMin, harvestBaseMoneyMax + 1);
        player.ReceiveCash(baseAmount);
        Debug.Log($"{player.playerName} 在 {tileName} 收获基础 {baseAmount} 铜钱");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);

        // 在基础奖励之上，按概率额外给“卡牌”或“额外铜钱”
        bool tryCard = Random.value < harvestCardChance;
        ItemData drawnCard = null;
        if (tryCard && ItemManager.Instance != null)
        {
            drawnCard = ItemManager.Instance.GiveRandomCardFromPool(player);
        }

        if (drawnCard != null)
        {
            // 抽到卡：GiveRandomCardFromPool 内部已弹“获得卡牌”提示，这里只补基础铜钱提示
            Debug.Log($"{player.playerName} 额外获得卡牌 {drawnCard.itemName}");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"收获 {baseAmount} 铜钱，还获得卡牌：{drawnCard.itemName}！", 2f);
            }
        }
        else
        {
            // 未抽卡或抽卡失败：给额外铜钱
            int extraAmount = Random.Range(harvestExtraMoneyMin, harvestExtraMoneyMax + 1);
            player.ReceiveCash(extraAmount);
            int total = baseAmount + extraAmount;
            Debug.Log($"{player.playerName} 在 {tileName} 额外获得 {extraAmount} 铜钱，共 {total}");

            if (UIManager.Instance != null)
            {
                string[] gainTexts = {
                    $"路遇商人掉落钱袋，共获得{total}铜钱",
                    $"捡到银子，共获得{total}铜钱",
                    $"路上发现遗落的铜钱，共获得{total}铜钱",
                    $"好心帮忙获得酬谢，共获得{total}铜钱",
                    $"运气不错，共捡到{total}铜钱"
                };
                int index = Random.Range(0, gainTexts.Length);
                UIManager.Instance.ShowToast(gainTexts[index], 2f);
            }
        }
    }

    private void HandleLoseMoneyTile(Player player)
    {
        int amount = Random.Range(5, 9);
        
        if (player.PayCash(amount))
        {
            Debug.Log($"{player.playerName} 在 {tileName} 损失 {amount} 铜钱");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);

            if (UIManager.Instance != null)
            {
                string[] loseTexts = {
                    $"遭遇小偷，丢失{amount}铜钱",
                    $"路遇强盗，损失{amount}铜钱",
                    $"被恶犬咬伤，医治花费{amount}铜钱",
                    $"踩到水坑弄脏衣物，清洗花费{amount}铜钱",
                    $"突发状况，损失{amount}铜钱"
                };
                int index = Random.Range(0, loseTexts.Length);
                UIManager.Instance.ShowToast(loseTexts[index], 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} 余额不足，无法支付 {amount} 铜钱");
        }
    }
}
