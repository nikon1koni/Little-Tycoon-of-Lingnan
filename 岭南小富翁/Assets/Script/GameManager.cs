﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 
    public static GameManager Instance;

    [Header("")]
    public GameState currentState = GameState.Waiting;
    public int currentPlayerIndex = 0;
    public bool isGameStarted = false;
    public bool isPlayerTurn = true;
    public bool isMoving = false;

    [Header("")]
    public List<Player> players = new List<Player>();
    public Player currentPlayer;

    [Header("")]
    public DiceController diceController;
    public Dice3DController dice3DController;
    public int lastDiceValue = 0;

    [Header("UI")]
    public Text currentPlayerText;
    public Text playerCashText;
    public Text diceResultText;
    public Text currentTileText;
    public Button rollDiceButton;

    [Header("")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("")]
    public int startingCash = 1500;
    public int salaryAmount = 200;
    public int jailTurns = 3;

    [Header("")]
    public bool enablePressureSystem = true;

    private int diceRollCount = 0;          // 
    private int pressureInterval = 1;        // (?N)
    private int nextPressureAt = 1;          // 
    public float basePressureCost = 7f;   // 
    public float pressureMultiplier = 1.2f;
    
    [Header("")]
    public BuffData bankruptBuffData;        // Debuff(Inspector)
    public int bankruptGraceRounds = 3;     // 

    public int DiceRollCount => diceRollCount;
    public int CurrentRound => diceRollCount / 6;

    [Header("")]
    public bool enableDebugKeys = true;

    [Header("")]
    public bool enableBackgroundMusic = true;
    public MusicManager musicManager;

    [Header("")]
    public SFXConfig sfxConfig;
    public bool enableSFX = true;

    [Header("")]
    [Range(0f, 1f)]
    [Tooltip("")]
    public float eventSoundVolume = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("UI")]
    public float uiSoundVolume = 0.8f;
    [Range(0f, 1f)]
    [Tooltip("")]
    public float characterSoundVolume = 0.7f;
    [Range(0f, 1f)]
    [Tooltip("")]
    public float diceSoundVolume = 0.8f;

    [Header("")]
    [Range(0f, 10f)]
    public float diceCooldownTime = 0f; // 
    private float lastDiceRollTime = -1000f; // 

    // 
    public enum GameState
    {
        Waiting,           // 
        PlayerTurn,        // 
        RollingDice,       // 
        Moving,            // 
        ProcessingTile,    // 
        BuyingProperty,    // 
        BuildingSelection, // 
        BuildingPlacement, // 
        GameOver           // 
    }

    void Awake()
    {
        // 
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("===  ===");
        InitializeGame();
        // === 1:  ===
        StartCoroutine(StartInitialBuildingPhase());
    }

    // 
    void InitializeGame()
    {
        FindRequiredComponents();
        FindAllPlayers();
        CheckBoard();
        InitializePlayerPositions();

        if (players.Count > 0)
        {
            currentPlayer = players[currentPlayerIndex];
        }

        // 
        GiveStartingItemsToPlayers();

        currentState = GameState.Waiting;
        isGameStarted = true;

        UpdateUI();
        SetupButtonEvents();
        InitializeMusicSystem();
        InitializeSFXSystem();

        Debug.Log($": {players.Count}");
        Debug.Log($": {currentPlayer?.playerName ?? ""}");
    }

    void InitializeMusicSystem()
    {
        if (!enableBackgroundMusic)
        {
            Debug.Log("");
            return;
        }

        if (musicManager == null)
        {
            GameObject musicObj = GameObject.Find("MusicManager");
            if (musicObj != null)
            {
                musicManager = musicObj.GetComponent<MusicManager>();
            }
            else
            {
                musicObj = new GameObject("MusicManager");
                musicManager = musicObj.AddComponent<MusicManager>();
                Debug.Log("MusicManager ");
            }
        }

        if (musicManager != null && musicManager.GetTotalTracks() > 0)
        {
            musicManager.Play();
            Debug.Log("");
        }
        else
        {
            Debug.LogWarning("MusicManager ");
        }
    }

    void GiveStartingItemsToPlayers()
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("ItemManager ");
            return;
        }

        foreach (Player player in players)
        {
            ItemManager.Instance.GiveStartingItemsToPlayer(player);
            Debug.Log($" {player.playerName} ");
        }

        if (ItemHandManager.Instance != null && currentPlayer != null)
        {
            ItemHandManager.Instance.SetupHand(currentPlayer);
            Debug.Log("");
        }
    }

    void InitializeSFXSystem()
    {
        if (!enableSFX)
        {
            Debug.Log("");
            return;
        }

        if (SFXManager.Instance == null)
        {
            GameObject sfxObj = new GameObject("SFXManager");
            SFXManager sfxManager = sfxObj.AddComponent<SFXManager>();

            if (sfxConfig != null)
            {
                sfxManager.config = sfxConfig;
                sfxManager.ReloadClips();
            }
            else
            {
                sfxConfig = Resources.Load<SFXConfig>("SFXConfig");
                if (sfxConfig != null)
                {
                    sfxManager.config = sfxConfig;
                    sfxManager.ReloadClips();
                }
                else
                {
                    Debug.LogWarning("SFXConfig InspectorResources");
                }
            }

            // 
            sfxManager.SetCategoryVolume(SFXCategory.Event, eventSoundVolume);
            sfxManager.SetCategoryVolume(SFXCategory.UI, uiSoundVolume);
            sfxManager.SetCategoryVolume(SFXCategory.Character, characterSoundVolume);
            sfxManager.SetCategoryVolume(SFXCategory.Dice, diceSoundVolume);

            Debug.Log("SFXManager ");
        }
        else if (sfxConfig != null && SFXManager.Instance.config == null)
        {
            SFXManager.Instance.config = sfxConfig;
            SFXManager.Instance.ReloadClips();
        }
    }

    // ===  ===
    IEnumerator StartInitialBuildingPhase()
    {
        // UI
        yield return new WaitForSeconds(0.5f);

        if (currentPlayer != null)
        {
            Debug.Log($"=== : {currentPlayer.playerName}  ===");

            // 1. 
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;

            // 2. 
            SetRollDiceButtonInteractable(false);

            // 3. UI
            if (uiManager != null)
            {
                // TileUI
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager ");
                // UI
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === Tile ===
    private BoardTile startPurchaseTileCache = null;

    BoardTile CreateStartPurchaseTile()
    {
        if (startPurchaseTileCache == null)
        {
            GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
            startPurchaseTileCache = tempObj.AddComponent<BoardTile>();
        }

        startPurchaseTileCache.tileName = "";
        startPurchaseTileCache.tileType = BoardTile.TileType.Buildable;
        startPurchaseTileCache.propertyPrice = 100;
        startPurchaseTileCache.isBuildable = true;
        startPurchaseTileCache.tileScale = 3;
        startPurchaseTileCache.ownerPlayer = currentPlayer;

        return startPurchaseTileCache;
    }

    void FindRequiredComponents()
    {
        if (diceController == null)
            diceController = FindObjectOfType<DiceController>();

        if (boardManager == null)
            boardManager = FindObjectOfType<BoardManager>();

        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();

        Debug.Log($": DiceController={diceController != null}, BoardManager={boardManager != null}, UIManager={uiManager != null}");
    }

    void FindAllPlayers()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        players.Clear();
        players.AddRange(allPlayers);
        players.Sort((a, b) => a.playerID.CompareTo(b.playerID));

        if (players.Count == 0)
        {
            Debug.LogWarning("");
        }
    }

    void CheckBoard()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager ");
            return;
        }

        if (boardManager.allTiles == null || boardManager.allTiles.Count == 0)
        {
            Debug.LogWarning("");
        }
        else
        {
            Debug.Log($": {boardManager.allTiles.Count} ");
        }
    }

    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;

            // PlayerMovement
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                startPos.y = startTile.transform.position.y + playerMovement.heightOffset;
            }
            else
            {
                startPos.y = 0.875f; //  + 
            }

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} : {player.cash}");

            // === UI ===
            if (UIManager.Instance != null)
            {
                // 
                if (i == 0) // 
                {
                    UIManager.Instance.UpdateCashDisplay(player.cash);
                }
            }
            // === UI ===
        }
    }

    // 
    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        if (tile.ownerPlayer == null)
        {
            if (tile.tileType == BoardTile.TileType.Buildable)
            {
                currentState = GameState.BuildingSelection;
                Debug.Log($"{tile.tileName} : {tile.propertyPrice} ");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} : {tile.propertyPrice} ");

                if (uiManager != null)
                {
                    uiManager.ShowPropertyPurchasePanel(tile, currentPlayer);
                }
                else
                {
                    AutoDecidePurchase(tile);
                }
            }
        }
        else
        {
            StartCoroutine(EndMoveAfterDelay(1f));
        }
    }

    // 
    private void CheckLinkedBuildingIncome(BoardTile tile, Player player)
    {
        if (tile == null || player == null) return;

        if (tile.EnableLinkedIncome &&
            tile.LinkedBuildingTiles != null &&
            tile.LinkedBuildingTiles.Count > 0)
        {
            float currentTime = Time.time;
            int totalIncome = 0;

            foreach (BoardTile buildingTile in tile.LinkedBuildingTiles)
            {
                if (buildingTile == null) continue;

                if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != player)
                    continue;

                if (buildingTile.currentBuildingData == null)
                    continue;

                if (buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Income &&
                    buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Mixed)
                    continue;

                float lastTime = tile.GetLastIncomeTime(buildingTile);
                if (lastTime > 0 && (currentTime - lastTime) < buildingTile.IncomeInterval)
                    continue;

                int baseIncome = buildingTile.currentBuildingData.GetIncomeAmountByTurns(buildingTile.GetBuildingTurnsOwned());
                int income = player.GetIncomeWithMultiplier(baseIncome);
                if (income > 0)
                {
                    player.ReceiveCash(income);
                    totalIncome += income;
                    tile.SetLastIncomeTime(buildingTile, currentTime);
                }
            }

            if (totalIncome > 0 && uiManager != null)
            {
                uiManager.ShowToast($"获得建筑收入 {totalIncome} 铜钱", 2f);
            }
        }
    }

    BoardTile GetStartTile()
    {
        if (boardManager != null && boardManager.allTiles.Count > 0)
        {
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    return tile;
                }
            }
            return boardManager.allTiles[0];
        }
        return null;
    }

    void SetupButtonEvents()
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.RemoveAllListeners();
            rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
            Debug.Log("");
        }
        else
        {
            Debug.LogWarning("RollDiceButton Inspector");

            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("");
                }
            }
        }
    }

    // =================  =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("");

        // === 2:  ===
        if (!CanRollDice())
        {
            Debug.Log($": {currentState}");

            // 
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("ESC", 2f);
                }
            }
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} ");

        currentState = GameState.RollingDice;
        isPlayerTurn = false;

        if (dice3DController != null)
        {
            dice3DController.StartRollDice();
        }
        else if (diceController != null)
        {
            diceController.StartRollDice();
        }
        else
        {
            RollDiceSimple();
        }
    }

    void RollDiceSimple()
    {
        lastDiceValue = Random.Range(1, 7);
        Debug.Log($"{currentPlayer.playerName}  {lastDiceValue} ");

        if (diceResultText != null)
            diceResultText.text = lastDiceValue.ToString();

        if (uiManager != null)
            uiManager.UpdateDiceResult(lastDiceValue);

        StartMovePlayer();
    }

    public void OnDiceRolled(int value)
    {
        lastDiceValue = value;
        lastDiceRollTime = Time.time;
        Debug.Log($": {value}");

        int previousRound = CurrentRound;
        diceRollCount++;
        Debug.Log($": {diceRollCount}");

        // Buff
        if (CurrentRound != previousRound && BuffSystem.Instance != null)
        {
            BuffSystem.Instance.OnRoundChanged();
        }

        UpdateUI();

        if (uiManager != null)
        {
            uiManager.UpdatePressureSystemUI();
        }

        // 
        CheckDiceEvenBuildings(value);

        StartMovePlayer();
    }

    /// <summary>
    /// 
    /// </summary>
    private void CheckDiceEvenBuildings(int diceValue)
    {
        if (currentPlayer == null) return;

        Debug.Log($": ={diceValue}...");

        int totalReward = 0;
        int buildingCount = 0;

        // 
        foreach (BoardTile property in currentPlayer.ownedProperties)
        {
            if (property == null || property.currentBuildingData == null) continue;

            // DiceEven 
            if (property.currentBuildingData.functionType == BuildingData.BuildingFunctionType.DiceEven)
            {
                int reward = property.currentBuildingData.CalculateDiceReward(diceValue);
                if (reward <= 0) continue; // 

                currentPlayer.ReceiveCash(reward);
                totalReward += reward;
                buildingCount++;

                Debug.Log($": {property.tileName} ({property.currentBuildingData.buildingName})  {reward} ");

                // 
                Transform effectTransform = property.transform;
                if (property.currentBuilding != null)
                {
                    effectTransform = property.currentBuilding.transform;
                }
                if (BuildingEffectSystem.Instance != null)
                {
                    BuildingEffectSystem.Instance.QueueBuildingEffect(effectTransform, property.currentBuildingData);
                }
            }
        }

        // 
        if (totalReward > 0 && uiManager != null)
        {
            string message = $"共 {buildingCount} 座建筑触发，获得 {totalReward} 现金";
            uiManager.ShowToast(message, 3f);
            Debug.Log(message);
        }
    }

    // 
    private void CheckPressureTrigger()
    {
        if (!enablePressureSystem)
            return;

        int currentRound = diceRollCount / 6;
        
        Debug.Log($"CheckPressureTrigger: diceRollCount={diceRollCount}, currentRound={currentRound}, nextPressureAt={nextPressureAt}");

        // 
        if (currentRound >= nextPressureAt)
        {
            TriggerPressure(currentRound);
        }
    }

    private void TriggerPressure(int currentRound)
    {
        Debug.Log($":  {currentRound}");

        int cost = Mathf.RoundToInt(basePressureCost);

        nextPressureAt++;
        basePressureCost *= pressureMultiplier;

        bool hasBankruptPlayer = false;

        foreach (Player p in players)
        {
            if (p.isBankrupt)
                continue;

            bool success = p.PayCash(cost);

            if (!success || p.cash < 0)
            {
                ApplyBankruptDebuff(p);
                hasBankruptPlayer = true;
            }
        }

        string pressureText = GetRandomPressureText(cost);

        if (hasBankruptPlayer && UIManager.Instance != null)
        {
            UIManager.Instance.ShowTurnAnnouncement(
                $"{currentRound} - {pressureText}"
            );
        }
        else if (!hasBankruptPlayer && UIManager.Instance != null)
        {
            UIManager.Instance.ShowTurnAnnouncement(
                $"{currentRound} - {pressureText}"
            );
        }
    }

    private string GetRandomPressureText(int cost)
    {
        string[] pressureTexts = {
            $"路遇乞丐，心生怜悯，施舍了{cost}铜钱",
            $"家中失窃，损失{cost}铜钱",
            $"官府征税，缴纳{cost}铜钱",
            $"友人借钱，慷慨相助{cost}铜钱",
            $"赌坊输钱，白白损失{cost}铜钱",
            $"修缮房屋，花费{cost}铜钱"
        };

        int index = Random.Range(0, pressureTexts.Length);
        return pressureTexts[index];
    }
    
    public void ApplyBankruptDebuff(Player player)
    {
        // Debuff
        if (player.HasBankruptBuff())
        {
            Debug.Log($"{player.playerName} Debuff");
            return;
        }
        
        if (BuffSystem.Instance != null)
        {
            string buffId = $"bankrupt_{player.playerName}";
            
            // BuffData
            int durationRounds = bankruptGraceRounds;
            string sourceName = "";
            string description = $"{bankruptGraceRounds}";
            
            // BuffData
            if (bankruptBuffData != null)
            {
                // BuffData
                if (bankruptBuffData.durationRounds > 0)
                {
                    durationRounds = bankruptBuffData.durationRounds;
                }
                sourceName = string.IsNullOrEmpty(bankruptBuffData.sourceName) ? sourceName : bankruptBuffData.sourceName;
                description = string.IsNullOrEmpty(bankruptBuffData.description) ? description : bankruptBuffData.description;
                
                BuffSystem.Buff bankruptBuff = new BuffSystem.Buff(
                    buffId,
                    sourceName,
                    bankruptBuffData.effectType,
                    bankruptBuffData.value,
                    bankruptBuffData.GetDuration(),
                    durationRounds,
                    null,
                    description
                );
                BuffSystem.Instance.AddBuff(player, bankruptBuff);
                Debug.Log($"{player.playerName} DebuffBuffData {durationRounds} ");
            }
            else
            {
                // Buff
                BuffSystem.Buff bankruptBuff = new BuffSystem.Buff(
                    buffId,
                    sourceName,
                    BuildingData.BuffEffect.Bankrupt,
                    0f,
                    0f,
                    durationRounds,
                    null,
                    description
                );
                BuffSystem.Instance.AddBuff(player, bankruptBuff);
                Debug.Log($"{player.playerName} Debuff {durationRounds} ");
            }
        }
        
        if (UIManager.Instance != null)
        {
            // BuffData
            string toastMessage = string.Empty;
            if (bankruptBuffData != null && !string.IsNullOrEmpty(bankruptBuffData.notificationMessage))
            {
                toastMessage = bankruptBuffData.notificationMessage.Replace("{PlayerName}", player.playerName);
            }
            else
            {
                toastMessage = $"{player.playerName} ";
            }
            UIManager.Instance.ShowToast(toastMessage, 3f);
        }
    }

    // === 3:  ===
    public bool CanRollDice()
    {
        // 
        float timeSinceLastRoll = Time.time - lastDiceRollTime;
        bool cooldownFinished = timeSinceLastRoll >= diceCooldownTime;
        
        bool canRoll = isGameStarted &&
                       currentState == GameState.PlayerTurn && // 
                       !isMoving &&
                       currentPlayer != null &&
                       !currentPlayer.isInJail &&
                       !currentPlayer.isBankrupt &&
                       cooldownFinished;

        string cooldownText = !cooldownFinished ? $"{diceCooldownTime - timeSinceLastRoll:F1}s" : "OK";
        Debug.Log($"CanRollDice: {canRoll} | State: {currentState} | isMoving: {isMoving} | Player: {currentPlayer.playerName} | Bankrupt: {currentPlayer.isBankrupt} | Cooldown: {cooldownText}");

        return canRoll;
    }

    void StartMovePlayer()
    {
        if (currentPlayer == null) return;

        // 
        float multiplier = currentPlayer.GetNextRollMultiplier();
        int finalDiceValue = Mathf.RoundToInt(lastDiceValue * multiplier);
        
        if (multiplier != 1f)
        {
            Debug.Log($"{currentPlayer.playerName} : {lastDiceValue} * {multiplier} = {finalDiceValue}");
        }

        // 
        int stepsModifier = currentPlayer.GetStepsModifier();
        if (stepsModifier != 0)
        {
            finalDiceValue += stepsModifier;
            Debug.Log($"{currentPlayer.playerName} : {finalDiceValue} + {stepsModifier} = {finalDiceValue}");
        }

        // 1
        finalDiceValue = Mathf.Max(1, finalDiceValue);

        Debug.Log($"{currentPlayer.playerName}  {finalDiceValue} ");

        currentState = GameState.Moving;
        isMoving = true;

        PlayerMovement movement = currentPlayer.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError($"{currentPlayer.playerName}  PlayerMovement ");
            EndMove();
            return;
        }

        movement.MoveSteps(finalDiceValue);
        StartCoroutine(WaitForMoveComplete(movement));
    }

    IEnumerator WaitForMoveComplete(PlayerMovement movement)
    {
        while (movement.isMoving)
        {
            yield return null;
        }

        Debug.Log($"{currentPlayer.playerName} ");

        // === 4:  ===
        CheckPassingStart();

        ProcessCurrentTile();
    }

    // === 5:  ===
    void CheckPassingStart()
    {
        if (boardManager == null || currentPlayer == null) return;

        int currentIndex = currentPlayer.currentTileIndex;
        int startIndex = 0;

        for (int i = 0; i < boardManager.allTiles.Count; i++)
        {
            if (boardManager.allTiles[i].tileType == BoardTile.TileType.Start)
            {
                startIndex = i;
                break;
            }
        }

        // (tileID == 0  tileType == Start)
        bool isOnStartTile = (currentPlayer.currentTile.tileID == 0 ||
                             currentPlayer.currentTile.tileType == BoardTile.TileType.Start);

        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        // ()
        if (!isOnStartTile && previousIndex > currentIndex)
        {
            Debug.Log($"{currentPlayer.playerName} ");

            // 1. 
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName}  {salary} ");

            if (uiManager != null)
            {
                uiManager.ShowToast($"获得工资 {salary} 铜钱", 2f);
            }

            // 2. 
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. 
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
        else if (isOnStartTile)
        {
            Debug.Log($"{currentPlayer.playerName} ");

            // (BoardTile.OnLanded)
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // ===  ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // UI
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} UIManager");
            // 
            OnBuildingPurchaseCompleted();
        }
    }

    // 
    void ProcessCurrentTile()
    {
        if (currentPlayer == null || currentPlayer.currentTile == null)
        {
            EndMove();
            return;
        }

        currentState = GameState.ProcessingTile;

        Debug.Log($"{currentPlayer.playerName}  {currentPlayer.currentTile.tileName}");

        BoardTile currentTile = currentPlayer.currentTile;

        if (currentTile.tileType == BoardTile.TileType.Start)
        {
            currentTile.OnLanded(currentPlayer);
        }
        else if (currentTile.tileType == BoardTile.TileType.Property ||
                 currentTile.tileType == BoardTile.TileType.Railroad ||
                 currentTile.tileType == BoardTile.TileType.Utility)
        {
            HandlePropertyTile();
        }
        else if (currentTile.tileType == BoardTile.TileType.Event)
        {
            currentTile.OnLanded(currentPlayer);
        }
        else if (currentTile.tileType == BoardTile.TileType.GainMoney ||
                 currentTile.tileType == BoardTile.TileType.LoseMoney)
        {
            currentTile.OnLanded(currentPlayer);
            StartCoroutine(EndMoveAfterDelay(0.2f));
        }
        else
        {
            StartCoroutine(EndMoveAfterDelay(0.2f));
        }
    }

    public void OnPlayerMoveComplete()
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = true;
        }

        if (uiManager != null)
        {
            uiManager.SetRollDiceButtonInteractable(true);
        }
    }

    void AutoDecidePurchase(BoardTile tile)
    {
        if (currentPlayer.cash >= tile.propertyPrice)
        {
            if (currentPlayer.BuyProperty(tile))
            {
                Debug.Log($"{currentPlayer.playerName}  {tile.tileName}");
            }
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName}  {tile.tileName}");
        }

        StartCoroutine(EndMoveAfterDelay(1f));
    }

    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($": {(purchased ? "" : "")}");
        StartCoroutine(EndMoveAfterDelay(0.5f));
    }

    IEnumerator EndMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMove();
    }

    void EndMove()
    {
        Debug.Log($"{currentPlayer.playerName} ");

        isMoving = false;

        bool shouldEndTurn = true;

        CheckPressureTrigger();

        if (currentPlayer.isBankrupt)
        {
            shouldEndTurn = false;
        }
        else if (currentPlayer.cash < 0)
        {
            // Debuff
            Debug.Log($"{currentPlayer.playerName} Debuff");
            ApplyBankruptDebuff(currentPlayer);
        }

        if (shouldEndTurn)
        {
            EndTurn();
        }
    }

    public void EndTurn()
    {
        Debug.Log($"{currentPlayer.playerName} ");

        ProcessPlayerEndTurnEffects(currentPlayer);

        SwitchToNextPlayer();
        StartCoroutine(StartNextTurnAfterDelay(1f));
    }

    private void ProcessPlayerEndTurnEffects(Player player)
    {
        if (player == null) return;

        player.ProcessLoanRepayment();
        player.ProcessReceivableRepayment();

        float incomeReduction = player.GetIncomeReduction();
        if (incomeReduction > 0)
        {
            Debug.Log($"{player.playerName} : {incomeReduction * 100}%");
        }

        float taxReduction = player.GetTaxReduction();
        if (taxReduction > 0)
        {
            Debug.Log($"{player.playerName} : {taxReduction * 100}%");
        }
    }

    void SwitchToNextPlayer()
    {
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            currentPlayer = players[currentPlayerIndex];
        }
        while (currentPlayer.isBankrupt);
    }

    IEnumerator StartNextTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentPlayer.isInJail)
        {
            HandleJailTurn();
        }
        else
        {
            StartPlayerTurn();
        }
    }

    void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        //  buildingStartRound == CurrentRound

        Debug.Log($"=== {currentPlayer.playerName}  ===");
        UpdateUI();

        // === UI ===
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
        }
        // === UI ===

        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = true;
        }

        if (uiManager != null)
        {
            uiManager.SetRollDiceButtonInteractable(true);
            uiManager.UpdateRollDiceButtonText("");
        }
    }

    void HandleJailTurn()
    {
        currentPlayer.jailTurnsRemaining--;

        if (currentPlayer.jailTurnsRemaining <= 0)
        {
            currentPlayer.isInJail = false;
            Debug.Log($"{currentPlayer.playerName} ");
            StartPlayerTurn();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName}  {currentPlayer.jailTurnsRemaining} ");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} {currentPlayer.jailTurnsRemaining}", 2f);
            }

            EndTurn();
        }
    }

    void HandlePlayerBankrupt(Player player)
    {
        Debug.Log($"=== : {player.playerName} ===");

        player.isBankrupt = true;

        foreach (BoardTile property in player.ownedProperties)
        {
            property.ownerPlayer = null;
            Debug.Log($": {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} ");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} !", 3f);
        }
    }

    void GameOver()
    {
        currentState = GameState.GameOver;
        isGameStarted = false;

        if (players.Count == 1)
        {
            Player player = players[0];
            bool isWinner = !player.isBankrupt;
            Debug.Log($"=== ! {player.playerName}: {(isWinner ? "" : "")} ===");

            if (uiManager != null)
            {
                uiManager.ShowGameOverPanel(player.playerName, isWinner);
            }
        }
        else
        {
            Player winner = players.Find(p => !p.isBankrupt);
            if (winner != null)
            {
                Debug.Log($"=== ! : {winner.playerName} ===");
                if (uiManager != null)
                {
                    uiManager.ShowGameOverPanel(winner.playerName, true);
                }
            }
            else
            {
                Debug.Log("===  ===");
            }
        }
    }

    // ================= UI  =================

    public void UpdateUI()
    {
        if (currentPlayer == null) return;

        if (currentPlayerText != null)
            currentPlayerText.text = $": {currentPlayer.playerName}";

        if (playerCashText != null)
            playerCashText.text = $": {currentPlayer.cash}";

        if (diceResultText != null)
            diceResultText.text = $": {lastDiceValue}";

        if (currentTileText != null && currentPlayer.currentTile != null)
            currentTileText.text = $": {currentPlayer.currentTile.tileName}";

        if (uiManager != null)
        {
            uiManager.UpdateCurrentPlayerInfo(currentPlayer);
        }
    }

    public void SetCurrentPlayer(Player player)
    {
        if (player == null || !players.Contains(player)) return;

        currentPlayerIndex = players.IndexOf(player);
        currentPlayer = player;
        UpdateUI();
    }

    public void SetRollDiceButtonInteractable(bool interactable)
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = interactable;
        }
    }

    // === 6:  ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("");

        isMoving = false;//

        // 
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // UI
        UpdateUI();
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = true;
        }

        if (uiManager != null)
        {
            uiManager.SetRollDiceButtonInteractable(true);
            uiManager.UpdateRollDiceButtonText("");
        }
        
        // ()
        // 
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} ");
            return;
        }
        
        // 
        CheckPressureTrigger();
        
        // ()
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} ");
            return;
        }
        
        // 
        EndTurn();
    }

    // =================  =================

    void Update()
    {
        if (!enableDebugKeys) return;

        Debug_Test();
    }

    private void Debug_Test()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestRollDice();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestMovePlayer(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestMovePlayer(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestMovePlayer(3);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("");
            EndTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            DebugGameState();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            int currentRound = diceRollCount / 6;
            TriggerPressure(currentRound);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMusic();
        }

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            PreviousMusic();
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            NextMusic();
        }
    }

    void ToggleMusic()
    {
        if (musicManager != null)
        {
            musicManager.TogglePlayPause();
        }
    }

    void PreviousMusic()
    {
        if (musicManager != null)
        {
            musicManager.PlayPreviousTrack();
        }
    }

    void NextMusic()
    {
        if (musicManager != null)
        {
            musicManager.PlayNextTrack();
        }
    }

    void TestRollDice()
    {
        Debug.Log("");
        OnRollDiceButtonClicked();
    }

    public void TestMovePlayer(int steps)
    {
        if (currentPlayer == null || isMoving) return;

        Debug.Log($": {currentPlayer.playerName}  {steps} ");

        lastDiceValue = steps;
        StartMovePlayer();
    }

    void DebugGameState()
    {
        Debug.Log("===  ===");
        Debug.Log($": {currentState}");
        Debug.Log($": {currentPlayer.playerName}");
        Debug.Log($": {players.Count}");
        Debug.Log($": {currentPlayerIndex}");
        Debug.Log($": {isGameStarted}");
        Debug.Log($": {isPlayerTurn}");
        Debug.Log($": {isMoving}");
        Debug.Log($": {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($": {currentPlayer.cash}");
            Debug.Log($": {currentPlayer.currentTile.tileName}");
            Debug.Log($": {currentPlayer.isInJail}");
            Debug.Log($": {currentPlayer.jailTurnsRemaining}");
        }
    }

    public void RestartFromGameOver()
    {
        Debug.Log("");
        
        currentState = GameState.PlayerTurn;
        isGameStarted = true;
        isPlayerTurn = true;
        isMoving = false;
        currentPlayerIndex = 0;
        diceRollCount = 0;
        nextPressureAt = 1;
        basePressureCost = 7f;
        
        // 
        ResetDiceCooldown();
        
        // 
        ClearAllBuildings();
        startPurchaseTileCache = null;
        
        BoardTile startTile = GetStartTile();
        
        foreach (Player p in players)
        {
            p.isBankrupt = false;
            p.cash = startingCash;
            p.ownedProperties.Clear();
            p.isInJail = false;
            p.jailTurnsRemaining = 0;
            
            // 
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.ResetPlayerInventory(p);
                ItemManager.Instance.GiveStartingItemsToPlayer(p);
            }
            
            if (startTile != null)
            {
                p.MoveToTile(startTile, false);
                Debug.Log($": {p.playerName} ");
            }
        }
        
        if (players.Count > 0)
        {
            currentPlayer = players[0];
        }
        
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = true;
        }
        
        if (uiManager != null)
        {
            uiManager.SetRollDiceButtonInteractable(true);
            uiManager.SwitchToGameUI();
            uiManager.UpdateAllPlayerInfo();
            uiManager.UpdateCashDisplay(currentPlayer.cash);
            uiManager.UpdatePressureSystemUI();
        }
        
        UpdateUI();
        
        // 
        StartCoroutine(DelayedShowBuildingPanelAfterRestart());
        
        Debug.Log("");
    }
    
    // 
    IEnumerator DelayedShowBuildingPanelAfterRestart()
    {
        yield return new WaitForSeconds(2f);
        
        if (currentPlayer == null) yield break;
        
        BoardTile startTile = GetStartTile();
        bool isOnStart = (currentPlayer.currentTile == startTile ||
                         currentPlayer.currentTile.tileType == BoardTile.TileType.Start ||
                         currentPlayer.currentTileIndex == 0);
        
        if (isOnStart)
        {
            Debug.Log($"{currentPlayer.playerName} ");
            
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);
            
            if (uiManager != null)
            {
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
        }
    }

    // 
    private void ClearAllBuildings()
    {
        Debug.Log("...");
        
        if (boardManager == null || boardManager.allTiles == null)
        {
            Debug.LogWarning("BoardManager  allTiles ");
            return;
        }
        
        foreach (BoardTile tile in boardManager.allTiles)
        {
            if (tile == null) continue;
            
            // 
            tile.currentBuildingData = null;
            tile.currentBuildingType = BoardTile.BuildingType.None;
            tile.buildingLevel = 0;
            tile.ownerPlayer = null;
            
            // 
            if (tile.currentBuilding != null)
            {
                Destroy(tile.currentBuilding);
                tile.currentBuilding = null;
            }
        }
        
        Debug.Log("");
    }

    public void ResetGame()
    {
        Debug.Log("");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void AddPlayer(Player player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($": {player.playerName}");
        }
    }

    public void RemovePlayer(Player player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($": {player.playerName}");

            if (players.Count > 0 && currentPlayer == player)
            {
                SwitchToNextPlayer();
            }
        }
    }

    public List<Player> GetAllPlayers()
    {
        return new List<Player>(players);
    }

    public Player GetPlayerByID(int id)
    {
        foreach (Player player in players)
        {
            if (player.playerID == id)
                return player;
        }
        return null;
    }

    public bool IsGameOver()
    {
        int activePlayers = 0;
        foreach (Player player in players)
        {
            if (!player.isBankrupt)
                activePlayers++;
        }
        return activePlayers <= 1;
    }

    public Player GetWinner()
    {
        if (players.Count == 1 && !players[0].isBankrupt)
            return players[0];

        foreach (Player player in players)
        {
            if (!player.isBankrupt)
                return player;
        }
        return null;
    }
    
    // Debuff
    public void CheckGameOverAfterBankrupt()
    {
        Debug.Log("...");
        
        Player winner = GetWinner();
        if (winner != null)
        {
            Debug.Log($": {winner.playerName}");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverPanel(winner.playerName, true);
            }
            GameOver();
        }
        else
        {
            Debug.Log("");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverPanel("", false);
            }
            GameOver();
        }
    }

    public void OnEventPanelClosed()
    {
        // 
        Debug.Log("");
        
        // Debuff
        if (currentPlayer != null && currentPlayer.cash < 0 && !currentPlayer.HasBankruptBuff())
        {
            Debug.Log($"{currentPlayer.playerName} ");
            ApplyBankruptDebuff(currentPlayer);
            return;
        }
        
        // 
        SetRollDiceButtonInteractable(true);
        
        // 
        if (currentState == GameState.BuildingSelection)
        {
            Debug.Log("");
            return;
        }
        
        // 
        StartCoroutine(EndMoveAfterDelay(0.1f));
        UpdateUI();
    }

    void CheckStartTiles()
    {
        if (boardManager != null)
        {
            Debug.Log("===  ===");
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    Debug.Log($": {tile.tileName}, ID: {tile.tileID}");

                    // 
                    if (tile.isBuildable)
                    {
                        Debug.LogError($": {tile.tileName} ");
                    }

                    // 
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($": {tile.tileName} ");
                    }
                }
            }
        }
    }

    // 
    public void SetDiceRollSpeed(float multiplier)
    {
        if (dice3DController != null)
        {
            dice3DController.SetRollSpeedMultiplier(multiplier);
        }
        if (diceController != null)
        {
            // DiceController
        }
        Debug.Log($"GameManager:  {multiplier}x");
    }

    // 
    public void SetDiceCooldown(float cooldownSeconds)
    {
        diceCooldownTime = Mathf.Max(0f, cooldownSeconds);
        Debug.Log($"GameManager:  {diceCooldownTime}");
    }

    // 
    public float GetDiceCooldown()
    {
        return diceCooldownTime;
    }

    // 
    public float GetDiceCooldownRemaining()
    {
        float timeSinceLastRoll = Time.time - lastDiceRollTime;
        return Mathf.Max(0f, diceCooldownTime - timeSinceLastRoll);
    }

    // 
    public void ResetDiceCooldown()
    {
        lastDiceRollTime = -1000f;
        Debug.Log("GameManager: ");
    }

    // 
    public void DisableDiceCooldown()
    {
        diceCooldownTime = 0f;
        ResetDiceCooldown();
        Debug.Log("GameManager: ");
    }
}
