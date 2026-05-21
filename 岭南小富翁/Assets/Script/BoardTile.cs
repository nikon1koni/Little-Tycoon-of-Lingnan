using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardTile : MonoBehaviour
{
    [Header("????????")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None; //??????????

    [Header("?????????")]
    public string tileName = "???";
    public int tileID = 0;
    public int tileScale = 1; // ?????
    public int propertyPrice = 100; // ?????
    public int rentPrice = 10; // ???
    public TileType tileType = TileType.Property;
    public bool isBuildable = false; // ???????

    [Header("??????")]
    public BuildingData currentBuildingData; // ???????????
    public BuildingType currentBuildingType = BuildingType.None;
    public int buildingLevel = 0; // ???????
    public GameObject currentBuilding; // ??????????
    public Player ownerPlayer; // ????????

    [Header("?????????? - ????????")]
    [SerializeField] private List<BoardTile> linkedBuildingTiles; // ????????????
    [SerializeField] private float incomeInterval = 5.0f; // ??????(??)
    private Dictionary<BoardTile, float> lastIncomeTime = new Dictionary<BoardTile, float>(); // ??¦Ë??????????
    [SerializeField] private bool enableLinkedIncome = true; // ??????¨´???????

    [Header("?????????")]
    [SerializeField] private bool enableAutoIncome = false; // ??????????????
    [SerializeField] private float autoIncomeInterval = 10.0f; // ?????????
    private float lastAutoIncomeTime = 0f;

    [Header("?????")]
    public EventData[] eventDataArray; // ???????????

    [Header("UI???")]
    public TextMeshProUGUI tileNameText; // ??????????
    public MeshRenderer tileRenderer; // ????????
  
 

    [Header("Buff§¹????")]
    public List<Player> buffedPlayers = new List<Player>(); // ?????????
    public float buffDuration = 0f; // Buff???????

    // ??????????
    public enum TileType
    {
        Start,          // ???
        Property,       // ???
        Railroad,       // ??¡¤
        Utility,        // ???????
        Chance,         // ????
        CommunityChest, // ????????
        Tax,            // ?
        Jail,           // ????
        FreeParking,    // ??????
        GoToJail,       // ??????
        Buildable,      // ???????
        BuildingSite,   // ???????
        Event,           // ???
        Normal
    }

    // ???????????
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

    // ?????????
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

        // ?????????
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
        // ?????????
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

        // ???Buff???????
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
        // ????????
        if (string.IsNullOrEmpty(tileName))
        {
            tileName = $"???_{tileID}";
        }

        // ???????
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

    // ?????????????????????
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
                        UIManager.Instance.ShowToast($"??????????{salary}?????", 2f);
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

    // ???????????????
    private void HandlePropertyLanding(Player player)
    {
        if (ownerPlayer == null)
        {
            // ????????????????
            Debug.Log($"{tileName} ?????????: {propertyPrice} ?");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPropertyPurchasePanel(this, player);
            }
        }
        else if (ownerPlayer == player)
        {
            // ???????
            Debug.Log($"{player.playerName} ??????????: {tileName}");
        }
        else
        {
            // ???????????????
            PayRent(player);
        }
    }

    // ??????
    private void PayRent(Player player)
    {
        int rent = CalculateRent();
        Debug.Log($"{player.playerName} ????????? {rent} ??? {ownerPlayer.playerName}");

        if (player.PayCash(rent))
        {
            ownerPlayer.ReceiveCash(rent);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"?????? {rent} ??? {ownerPlayer.playerName}", 2f);
            }
        }
        else
        {
            Debug.LogWarning($"{player.playerName} ??????????????????");
        }
    }

    // ???????
    public int CalculateRent()
    {
        int baseRent = rentPrice;

        // ????§ß????????????????
        if (currentBuildingData != null)
        {
            baseRent += currentBuildingData.GetIncomeAmount(buildingLevel);
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

                PlayBuildingEffect(buildingTile);
                
                if (buildingTile.currentBuildingData.effectDuration > maxEffectDuration)
                {
                    maxEffectDuration = buildingTile.currentBuildingData.effectDuration;
                }
            }
        }

        if (totalIncome > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"????????????: {totalIncome} ?", 2f);
        }

        return maxEffectDuration;
    }

    private void PlayBuildingEffect(BoardTile buildingTile)
    {
        if (buildingTile == null || buildingTile.currentBuildingData == null)
            return;

        BuildingData data = buildingTile.currentBuildingData;
        
        if (data.effectIconPrefab != null || data.effectSound != null)
        {
            if (BuildingEffectSystem.Instance != null)
            {
                Transform effectTransform = buildingTile.transform;
                if (buildingTile.currentBuilding != null)
                {
                    effectTransform = buildingTile.currentBuilding.transform;
                }
                BuildingEffectSystem.Instance.PlayBuildingEffectImmediate(effectTransform, data);
            }
            else
            {
                Debug.LogWarning("BuildingEffectSystem ¦Ä???????????????????? BuildingEffectSystem ????");
            }
        }
    }

    // ????????????????
    private bool CanGenerateIncome(BoardTile buildingTile, float currentTime)
    {
        if (!lastIncomeTime.ContainsKey(buildingTile))
            return true;

        float timeSinceLastIncome = currentTime - lastIncomeTime[buildingTile];
        return timeSinceLastIncome >= incomeInterval;
    }

    // === ??????????? ===
    private void GenerateAutoIncome()
    {
        if (currentBuildingData == null || ownerPlayer == null) return;

        int incomeAmount = currentBuildingData.GetIncomeAmount(buildingLevel);
        if (incomeAmount > 0)
        {
            ownerPlayer.ReceiveCash(incomeAmount);
            Debug.Log($"???? {currentBuildingData.buildingName} ???????????: {incomeAmount} ?");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"???????????: {incomeAmount} ?", 2f);
            }
        }
    }

    // ???????????
    public void SetBuildingData(BuildingData data, int level = 1)
    {
        currentBuildingData = data;
        buildingLevel = level;

        if (data != null)
        {
            // ??????????????
            currentBuildingType = GetBuildingTypeFromData(data);

            Debug.Log($"??? {tileName}: ??????? {data.buildingName}, ????: {currentBuildingType}, ???: {level}");

            // ??ýn??????
            if (data.functionType != BuildingData.BuildingFunctionType.Income &&
                data.functionType != BuildingData.BuildingFunctionType.Mixed)
            {
                Debug.LogWarning($"?????????????????? {data.functionType}??????????????? Income ?? Mixed ????");
            }
        }
        else
        {
            currentBuildingType = BuildingType.None;
            Debug.Log($"??? {tileName}: ????????????????????? None");
        }
    }
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GetBuildingTypeFromData: ????????????? null");
            return BuildingType.None;
        }

        // ????? BuildingData ?§Ö? buildingType ???
        BoardTile.BuildingType type = data.buildingType;

        if (type == BuildingType.None)
        {
            // ??????¦Ä?????????????????
            Debug.LogWarning($"????? {data.buildingName} ?? buildingType ???¦Ä?????????????????");
            return InferBuildingTypeFromName(data.buildingName);
        }
        else
        {
            Debug.Log("?????????????????");
            Debug.Log($"GetBuildingTypeFromData: ?? {data.buildingName} ?????????????: {type}");
            return type;
        }
    }
    private BuildingType InferBuildingTypeFromName(string buildingName)
    {
        string name = buildingName.ToLower();
        //??????
        if (name.Contains("small") || name.Contains("§³????"))
            return BuildingType.SmallHouse;
        else if (name.Contains("medium") || name.Contains("?§Ù???"))
            return BuildingType.MediumHouse;
        else if (name.Contains("large") || name.Contains("????"))
            return BuildingType.LargeHouse;
        else
            return BuildingType.Special;
    }

    // ??????????????
    public int GetUpgradeCost()
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return 0;

        return currentBuildingData.nextLevelBuilding.purchasePrice;
    }

    // ?????????????????
    public bool CanUpgradeBuilding(Player player)
    {
        if (currentBuildingData == null || currentBuildingData.nextLevelBuilding == null)
            return false;

        if (ownerPlayer != player) return false;

        if (player.cash < GetUpgradeCost()) return false;

        // ???????
        if (!CheckScaleForUpgrade(currentBuildingData.nextLevelBuilding.requiredScale))
            return false;

        return true;
    }

    // ?????????????????????
    public bool CheckScaleForUpgrade(BuildingData.Scale requiredScale)
    {
        return tileScale >= (int)requiredScale;
    }

    // ????????????
    public BuildingData GetNextUpgradeBuilding()
    {
        if (currentBuildingData == null) return null;
        return currentBuildingData.nextLevelBuilding;
    }

    // ????????
    public bool UpgradeBuilding(Player player)
    {
        if (!CanUpgradeBuilding(player)) return false;

        int upgradeCost = GetUpgradeCost();

        if (player.PayCash(upgradeCost))
        {
            // ?????????????????
            BuildingData nextBuildingData = currentBuildingData.nextLevelBuilding;
            
            buildingLevel++;
            Debug.Log($"{player.playerName} ?????? {tileName} ??????????? {buildingLevel}");

            // ???????????
            if (nextBuildingData != null)
            {
                currentBuildingData = nextBuildingData;
                // ???????????
                currentBuildingType = GetBuildingTypeFromData(nextBuildingData);
            }

            // ??????????
            if (nextBuildingData != null && nextBuildingData.buildingPrefab != null)
            {
                // ????????
                if (currentBuilding != null)
                {
                    Destroy(currentBuilding);
                }

                // ?????????
                GameObject newBuilding = Instantiate(
                    nextBuildingData.buildingPrefab,
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

    // ???Buff§¹??
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
                    Debug.Log($"{player.playerName} ???????????: {buffValue * 100}%");
                    break;

                case BuildingData.BuffEffect.DiceBoost:
                    player.hasDiceBoost = true;
                    player.diceBoostValue = Mathf.RoundToInt(buffValue);
                    Debug.Log($"{player.playerName} ?????????: +{player.diceBoostValue}");
                    break;

                case BuildingData.BuffEffect.IncomeMultiplier:
                    player.incomeMultiplier += buffValue;
                    Debug.Log($"{player.playerName} ?????????: {buffValue * 100}%");
                    break;

                case BuildingData.BuffEffect.LuckBoost:
                    player.luckBoost += buffValue;
                    Debug.Log($"{player.playerName} ?????????: {buffValue * 100}%");
                    break;
            }

            buffedPlayers.Add(player);
            buffDuration = currentBuildingData.buffDuration;
        }
    }

    // ???Buff§¹??
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

    // ???????
    private void DrawChanceCard(Player player)
    {
        // ?????????
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(20, 101);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} ?ùz????: ??? {gain} ?");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int lose = Random.Range(20, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} ?ùz????: ??? {lose} ?");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventLoseMoney);
                }
                break;

            case 3:
                // ???????????
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomTileIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    BoardTile targetTile = BoardManager.Instance.allTiles[randomTileIndex];
                    player.MoveToTile(targetTile, true);
                    Debug.Log($"{player.playerName} ?ùz????: ????? {targetTile.tileName}");
                }
                break;
        }
    }

    // ?????????????
    private void DrawCommunityChestCard(Player player)
    {
        // ?????????????
        int random = Random.Range(1, 4);

        switch (random)
        {
            case 1:
                int gain = Random.Range(50, 201);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} ?ùz????????: ??? {gain} ?");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case 2:
                int tax = Random.Range(50, 201);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} ?ùz????????: ??? {tax} ?");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;

            case 3:
                player.AddBuff(this);
                Debug.Log($"{player.playerName} ?ùz????????: ??????Buff");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventBuffActivated);
                break;
        }
    }

    // ??????
    private void PayTax(Player player)
    {
        int taxAmount = propertyPrice / 10; // ??????????10%

        if (player.PayCash(taxAmount))
        {
            Debug.Log($"{player.playerName} ??????: {taxAmount} ?");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
        }
    }

    // ???????
    private void SendToJail(Player player)
    {
        player.isInJail = true;
        player.jailTurnsRemaining = 3;

        Debug.Log($"{player.playerName} ?????????????? {player.jailTurnsRemaining} ???");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.EventGoToJail);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{player.playerName} ?????????", 2f);
        }
    }

    // ??????????
    private void TriggerRandomEvent(Player player)
    {
        if (eventDataArray != null && eventDataArray.Length > 0)
        {
            // ???????????????????????????
            EventData selectedEvent = eventDataArray[Random.Range(0, eventDataArray.Length)];
            
            if (selectedEvent != null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowEventPanel(selectedEvent);
                Debug.Log($"{player.playerName} ???????: {selectedEvent.eventTitle}");
                return;
            }
        }

        // ???????????????????????????
        TileEvent randomEvent = (TileEvent)Random.Range(1, 6);

        switch (randomEvent)
        {
            case TileEvent.GainMoney:
                int gain = Random.Range(50, 151);
                player.ReceiveCash(gain);
                Debug.Log($"{player.playerName} ???????: ??? {gain} ?");

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
                break;

            case TileEvent.LoseMoney:
                int lose = Random.Range(30, 101);
                if (player.PayCash(lose))
                {
                    Debug.Log($"{player.playerName} ???????: ??? {lose} ?");

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
                    Debug.Log($"{player.playerName} ???????: ????? {targetTile.tileName}");
                }
                break;

            case TileEvent.GetOutOfJailFree:
                if (player.isInJail)
                {
                    player.isInJail = false;
                    player.jailTurnsRemaining = 0;
                    Debug.Log($"{player.playerName} ???????: ???????");
                }
                break;

            case TileEvent.PayTax:
                int tax = Random.Range(20, 81);
                if (player.PayCash(tax))
                {
                    Debug.Log($"{player.playerName} ???????: ?????????? {tax} ?");

                    if (SFXManager.Instance != null)
                        SFXManager.Instance.PlaySFX(SFXClip.EventTaxPaid);
                }
                break;
        }
    }

    // ?????????§¹??
    public void UpdateTileVisual()
    {
        if (tileRenderer == null) return;

       
    }

    // ??????????????
    public void AddLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }

        if (!linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Add(buildingTile);
            Debug.Log($"??? {tileName} ???????????????? {buildingTile.tileName}");
        }
    }

    // ??????????????
    public void RemoveLinkedBuildingTile(BoardTile buildingTile)
    {
        if (linkedBuildingTiles != null && linkedBuildingTiles.Contains(buildingTile))
        {
            linkedBuildingTiles.Remove(buildingTile);
            Debug.Log($"??? {tileName} ??????????????? {buildingTile.tileName}");
        }
    }

    // ??????§Û???
    public void ClearAllLinkedBuildingTiles()
    {
        if (linkedBuildingTiles != null)
        {
            linkedBuildingTiles.Clear();
            Debug.Log($"??? {tileName} ????????§Û???????");
        }
    }

    // ??????§Û??????????
    public List<BoardTile> GetLinkedBuildingTiles()
    {
        if (linkedBuildingTiles == null)
        {
            linkedBuildingTiles = new List<BoardTile>();
        }
        return linkedBuildingTiles;
    }

    // ????/???????????????
    public void SetLinkedIncomeEnabled(bool enabled)
    {
        enableLinkedIncome = enabled;
    }

    // ??????????
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = Mathf.Max(1.0f, interval); // ??§³1??
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

    // ???????????????¡Â???
    public float GetLastIncomeTime(BoardTile buildingTile)
    {
        if (buildingTile == null) return 0f;

        if (lastIncomeTime.ContainsKey(buildingTile))
            return lastIncomeTime[buildingTile];

        return 0f; // ??¦Ä??????????
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