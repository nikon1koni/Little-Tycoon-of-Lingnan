using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // ???????
    public static GameManager Instance;

    [Header("?????")]
    public GameState currentState = GameState.Waiting;
    public int currentPlayerIndex = 0;
    public bool isGameStarted = false;
    public bool isPlayerTurn = true;
    public bool isMoving = false;

    [Header("???????")]
    public List<Player> players = new List<Player>();
    public Player currentPlayer;

    [Header("??????")]
    public DiceController diceController;
    public Dice3DController dice3DController;
    public int lastDiceValue = 0;

    [Header("UI ????")]
    public Text currentPlayerText;
    public Text playerCashText;
    public Text diceResultText;
    public Text currentTileText;
    public Button rollDiceButton;

    [Header("??????????")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("???????")]
    public int startingCash = 1500;
    public int salaryAmount = 200;
    public int jailTurns = 3;

    [Header("?????")]
    public bool enablePressureSystem = true;

    private int diceRollCount = 0;          // ????????
    private int pressureInterval = 1;        // ???????????????????
    private int nextPressureAt = 1;          // ????????????????
    public float basePressureCost = 50f;   // ?????????????
    public float pressureMultiplier = 1.2f;

    public int DiceRollCount => diceRollCount;
    public int CurrentRound => diceRollCount / 6;

    [Header("????")]
    public bool enableDebugKeys = true;

    [Header("??????")]
    public bool enableBackgroundMusic = true;
    public MusicManager musicManager;

    [Header("????????")]
    public SFXConfig sfxConfig;
    public bool enableSFX = true;

    // ????????
    public enum GameState
    {
        Waiting,           // ?????
        PlayerTurn,        // ?????
        RollingDice,       // ???????
        Moving,            // ?????
        ProcessingTile,    // ??????????
        BuyingProperty,    // ??????
        BuildingSelection, // ?????????
        BuildingPlacement, // ??????????
        GameOver           // ???????
    }

    void Awake()
    {
        // ???????????
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
        Debug.Log("=== ?????????? ??????? ===");
        InitializeGame();
        // === ???1: ????????????? ===
        StartCoroutine(StartInitialBuildingPhase());
    }

    // ????????
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

        // ???????????????
        currentState = GameState.Waiting;
        isGameStarted = true;

        UpdateUI();
        SetupButtonEvents();
        InitializeMusicSystem();
        InitializeSFXSystem();

        Debug.Log($"???????: {players.Count}");
        Debug.Log($"??????: {currentPlayer?.playerName ?? ""}");
    }

    void InitializeMusicSystem()
    {
        if (!enableBackgroundMusic)
        {
            Debug.Log("?????????????");
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
                Debug.Log("MusicManager ?????");
            }
        }

        if (musicManager != null && musicManager.GetTotalTracks() > 0)
        {
            musicManager.Play();
            Debug.Log("??????????????");
        }
        else
        {
            Debug.LogWarning("MusicManager ??????????????");
        }
    }

    void InitializeSFXSystem()
    {
        if (!enableSFX)
        {
            Debug.Log("???????????");
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
                    Debug.LogWarning("SFXConfig ???????????Inspector??????????Resources?????");
                }
            }

            Debug.Log("SFXManager ????????");
        }
        else if (sfxConfig != null && SFXManager.Instance.config == null)
        {
            SFXManager.Instance.config = sfxConfig;
            SFXManager.Instance.ReloadClips();
        }
    }

    // === ????????????? ===
    IEnumerator StartInitialBuildingPhase()
    {
        // ??????????UI???????
        yield return new WaitForSeconds(0.5f);

        if (currentPlayer != null)
        {
            Debug.Log($"=== ???: {currentPlayer.playerName} ??????????? ===");

            // 1. ?????????????????????????
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;

            // 2. ??????????
            SetRollDiceButtonInteractable(false);

            // 3. ??????????UI
            if (uiManager != null)
            {
                // ????????????"??????"Tile????UI???
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager ?????????????????????");
                // ?????????????????
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === ?????????????Tile ===
    private BoardTile startPurchaseTileCache = null;

    BoardTile CreateStartPurchaseTile()
    {
        if (startPurchaseTileCache == null)
        {
            GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
            startPurchaseTileCache = tempObj.AddComponent<BoardTile>();
        }

        startPurchaseTileCache.tileName = "??????";
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

        Debug.Log($"???????: DiceController={diceController != null}, BoardManager={boardManager != null}, UIManager={uiManager != null}");
    }

    void FindAllPlayers()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        players.Clear();
        players.AddRange(allPlayers);
        players.Sort((a, b) => a.playerID.CompareTo(b.playerID));

        if (players.Count == 0)
        {
            Debug.LogWarning("?????????Player???????????????Player?????");
        }
    }

    void CheckBoard()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager ?????");
            return;
        }

        if (boardManager.allTiles == null || boardManager.allTiles.Count == 0)
        {
            Debug.LogWarning("????????????");
        }
        else
        {
            Debug.Log($"??????? {boardManager.allTiles.Count} ?????");
        }
    }

    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("??????????");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;

            // ???PlayerMovement????????????
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                startPos.y = startTile.transform.position.y + playerMovement.heightOffset;
            }
            else
            {
                startPos.y = 0.875f; // ???????? + ??????
            }

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} ??????: {player.cash}");

            // === ??????????????????UI ===
            if (UIManager.Instance != null)
            {
                // ???????????????????
                if (i == 0) // ????????
                {
                    UIManager.Instance.UpdateCashDisplay(player.cash);
                }
            }
            // === ????????? ===
        }
    }

    // ?????????????
    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        CheckLinkedBuildingIncome(tile, currentPlayer);

        if (tile.ownerPlayer == null)
        {
            if (tile.tileType == BoardTile.TileType.Buildable)
            {
                currentState = GameState.BuildingSelection;
                Debug.Log($"{tile.tileName} ??????????: {tile.propertyPrice} ???");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} ??????: {tile.propertyPrice} ???");

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

    // ??????????????
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

                int income = buildingTile.currentBuildingData.GetIncomeAmount(buildingTile.buildingLevel);
                if (income > 0)
                {
                    player.ReceiveCash(income);
                    totalIncome += income;
                    tile.SetLastIncomeTime(buildingTile, currentTime);
                }
            }

            if (totalIncome > 0 && uiManager != null)
            {
                uiManager.ShowToast($"????????: {totalIncome} ???", 2f);
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
            Debug.Log("????????????");
        }
        else
        {
            Debug.LogWarning("RollDiceButton ????Inspector??????????????????");

            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("?????????");
                }
            }
        }
    }

    // ================= ?????? =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("???????????");

        // === ????2: ????? ===
        if (!CanRollDice())
        {
            Debug.Log($"????????????????: {currentState}");

            // ??????
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("??????????????ESC???", 2f);
                }
            }
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("?????????");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} ??????");

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
        Debug.Log($"{currentPlayer.playerName} ???? {lastDiceValue} ??");

        if (diceResultText != null)
            diceResultText.text = lastDiceValue.ToString();

        if (uiManager != null)
            uiManager.UpdateDiceResult(lastDiceValue);

        StartMovePlayer();
    }

    public void OnDiceRolled(int value)
    {
        lastDiceValue = value;
        Debug.Log($"??????: {value}");

        diceRollCount++;
        Debug.Log($"?????????????: {diceRollCount}");

        UpdateUI();

        if (uiManager != null)
        {
            uiManager.UpdatePressureSystemUI();
        }

        StartMovePlayer();
    }

    // ????????????
    private void CheckPressureTrigger()
    {
        if (!enablePressureSystem)
            return;

        int currentRound = diceRollCount / 6;
        
        Debug.Log($"CheckPressureTrigger: diceRollCount={diceRollCount}, currentRound={currentRound}, nextPressureAt={nextPressureAt}");

        if (currentRound >= nextPressureAt)
        {
            TriggerPressure(currentRound);
        }
    }

    // ??????????
    private void TriggerPressure(int currentRound)
    {
        Debug.Log($"?? {currentRound} ????????????");

        int cost = Mathf.RoundToInt(basePressureCost);

        foreach (Player p in players)
        {
            if (p.isBankrupt)
                continue;

            bool success = p.PayCash(cost);

            if (!success || p.cash < 0)
            {
                p.isBankrupt = true;

                if (UIManager.Instance != null)
                {
                    if (players.Count == 1)
                    {
                        UIManager.Instance.ShowGameOverPanel(p.playerName, false);
                    }
                    else
                    {
                        Player winner = players.Find(p2 => !p2.isBankrupt);
                        if (winner != null)
                        {
                            UIManager.Instance.ShowGameOverPanel(winner.playerName, true);
                        }
                        else
                        {
                            UIManager.Instance.ShowGameOverPanel(p.playerName, false);
                        }
                    }
                }

                GameOver();
                return;
            }
        }

        // ??????????????
        nextPressureAt++;

        basePressureCost *= pressureMultiplier;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTurnAnnouncement(
                $"?? {currentRound} ???   ??? {cost} ???"
            );
        }
    }

    // === ????3: ????????? ===
    public bool CanRollDice()
    {
        bool canRoll = isGameStarted &&
                       currentState == GameState.PlayerTurn && // ?????????????
                       !isMoving &&
                       currentPlayer != null &&
                       !currentPlayer.isInJail &&
                       !currentPlayer.isBankrupt;

        Debug.Log($"CanRollDice: {canRoll} | State: {currentState} | isMoving: {isMoving} | Player: {currentPlayer?.playerName} | Bankrupt: {currentPlayer?.isBankrupt}");

        return canRoll;
    }

    void StartMovePlayer()
    {
        if (currentPlayer == null) return;

        Debug.Log($"{currentPlayer.playerName} ?????? {lastDiceValue} ??");

        currentState = GameState.Moving;
        isMoving = true;

        PlayerMovement movement = currentPlayer.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError($"{currentPlayer.playerName} ??? PlayerMovement ???");
            EndMove();
            return;
        }

        movement.MoveSteps(lastDiceValue);
        StartCoroutine(WaitForMoveComplete(movement));
    }

    IEnumerator WaitForMoveComplete(PlayerMovement movement)
    {
        while (movement.isMoving)
        {
            yield return null;
        }

        Debug.Log($"{currentPlayer.playerName} ??????");

        // === ????4: ?????????? ===
        CheckPassingStart();

        ProcessCurrentTile();
    }

    // === ????5: ????????? ===
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

        // ???n?????????????tileID == 0 ?? tileType == Start??
        bool isOnStartTile = (currentPlayer.currentTile.tileID == 0 ||
                             currentPlayer.currentTile.tileType == BoardTile.TileType.Start);

        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        // ???????????????????????????????????????????
        if (!isOnStartTile && previousIndex > currentIndex)
        {
            Debug.Log($"{currentPlayer.playerName} ??????????????");

            // 1. ???????
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} ??? {salary} ??????");

            if (uiManager != null)
            {
                uiManager.ShowToast($"?????????{salary}???!", 2f);
            }

            // 2. ???????????????
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. ?????????????
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
        else if (isOnStartTile)
        {
            Debug.Log($"{currentPlayer.playerName} ?????????BoardTile??");

            // ????????????????????????BoardTile.OnLanded????
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // === ???????????????? ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // ????????????????????
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} ???????????");
            // ??????UI?????????????????
            OnBuildingPurchaseCompleted();
        }
    }

    // ?????????????
    void ProcessCurrentTile()
    {
        if (currentPlayer == null || currentPlayer.currentTile == null)
        {
            EndMove();
            return;
        }

        currentState = GameState.ProcessingTile;

        Debug.Log($"{currentPlayer.playerName} ???? {currentPlayer.currentTile.tileName}");

        BoardTile currentTile = currentPlayer.currentTile;

        CheckLinkedBuildingIncome(currentTile, currentPlayer);

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
                Debug.Log($"{currentPlayer.playerName} ????????? {tile.tileName}");
            }
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} ??????? {tile.tileName}");
        }

        StartCoroutine(EndMoveAfterDelay(1f));
    }

    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($"???????: {(purchased ? "???" : "???")}");
        StartCoroutine(EndMoveAfterDelay(0.5f));
    }

    IEnumerator EndMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMove();
    }

    void EndMove()
    {
        Debug.Log($"{currentPlayer.playerName} ?????????");

        isMoving = false;

        bool shouldEndTurn = true;

        CheckPressureTrigger();

        if (currentPlayer.isBankrupt)
        {
            shouldEndTurn = false;
        }
        else if (currentPlayer.cash < 0)
        {
            Debug.Log($"{currentPlayer.playerName} ?????");
            HandlePlayerBankrupt(currentPlayer);

            if (players.Count <= 1)
            {
                GameOver();
                return;
            }
        }

        if (shouldEndTurn)
        {
            EndTurn();
        }
    }

    public void EndTurn()
    {
        Debug.Log($"{currentPlayer.playerName} ???????");

        SwitchToNextPlayer();
        StartCoroutine(StartNextTurnAfterDelay(1f));
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

        Debug.Log($"=== {currentPlayer.playerName} ???? ===");
        UpdateUI();

        // === ??????????UI ===
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
        }
        // === UI??????? ===

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
            Debug.Log($"{currentPlayer.playerName} ????");
            StartPlayerTurn();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} ???????? {currentPlayer.jailTurnsRemaining} ???");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} ??????????{currentPlayer.jailTurnsRemaining}???", 2f);
            }

            EndTurn();
        }
    }

    void HandlePlayerBankrupt(Player player)
    {
        Debug.Log($"=== ??????: {player.playerName} ===");

        player.isBankrupt = true;

        foreach (BoardTile property in player.ownedProperties)
        {
            property.ownerPlayer = null;
            Debug.Log($"?????: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} ?????");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} ?????!", 3f);
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
            Debug.Log($"=== ?????????! {player.playerName}: {(isWinner ? "???": "????")} ===");

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
                Debug.Log($"=== ???????! ???: {winner.playerName} ===");
                if (uiManager != null)
                {
                    uiManager.ShowGameOverPanel(winner.playerName, true);
                }
            }
            else
            {
                Debug.Log("=== ?????????????? ===");
            }
        }
    }

    // ================= UI ???? =================

    public void UpdateUI()
    {
        if (currentPlayer == null) return;

        if (currentPlayerText != null)
            currentPlayerText.text = $"??????: {currentPlayer.playerName}";

        if (playerCashText != null)
            playerCashText.text = $"???: {currentPlayer.cash}";

        if (diceResultText != null)
            diceResultText.text = $"????: {lastDiceValue}";

        if (currentTileText != null && currentPlayer.currentTile != null)
            currentTileText.text = $"????: {currentPlayer.currentTile.tileName}";

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

    // === ????6: ?????????????? ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("??????????????????????");

        isMoving = false;//?????????

        // ?????????????
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // ???UI????????
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
    }

    // ================= ??????? =================

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
            Debug.Log("??????????");
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
        Debug.Log("???????");
        OnRollDiceButtonClicked();
    }

    public void TestMovePlayer(int steps)
    {
        if (currentPlayer == null || isMoving) return;

        Debug.Log($"???????: {currentPlayer.playerName} ??? {steps} ??");

        lastDiceValue = steps;
        StartMovePlayer();
    }

    void DebugGameState()
    {
        Debug.Log("=== ????? ===");
        Debug.Log($"??: {currentState}");
        Debug.Log($"???: {currentPlayer?.playerName}");
        Debug.Log($"????: {players.Count}");
        Debug.Log($"???????: {currentPlayerIndex}");
        Debug.Log($"????: {isGameStarted}");
        Debug.Log($"?????: {isPlayerTurn}");
        Debug.Log($"??????: {isMoving}");
        Debug.Log($"?????: {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($"???: {currentPlayer.cash}");
            Debug.Log($"???????: {currentPlayer.currentTile?.tileName}");
            Debug.Log($"???????: {currentPlayer.isInJail}");
            Debug.Log($"???????: {currentPlayer.jailTurnsRemaining}");
        }
    }

    public void RestartFromGameOver()
    {
        Debug.Log("从游戏结束状态重启");
        
        currentState = GameState.PlayerTurn;
        isGameStarted = true;
        isPlayerTurn = true;
        isMoving = false;
        currentPlayerIndex = 0;
        diceRollCount = 0;
        nextPressureAt = 1;
        basePressureCost = 50f;
        
        BoardTile startTile = GetStartTile();
        
        foreach (Player p in players)
        {
            p.isBankrupt = false;
            p.cash = startingCash;
            p.ownedProperties.Clear();
            p.isInJail = false;
            p.jailTurnsRemaining = 0;
            
            if (startTile != null)
            {
                p.MoveToTile(startTile, false);
                Debug.Log($"重置 {p.playerName} 位置到起点");
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
            uiManager.UpdateCashDisplay(currentPlayer?.cash ?? startingCash);
            uiManager.UpdatePressureSystemUI();
        }
        
        UpdateUI();
        
        Debug.Log("游戏已重启，可以继续投骰子");
    }

    public void ResetGame()
    {
        Debug.Log("???????");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void AddPlayer(Player player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"???????: {player.playerName}");
        }
    }

    public void RemovePlayer(Player player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($"??????: {player.playerName}");

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

    public void OnEventPanelClosed()
    {
        // ???????????????????
        Debug.Log("??????????????????");
        SetRollDiceButtonInteractable(true);
        UpdateUI();
    }

    void CheckStartTiles()
    {
        if (boardManager != null)
        {
            Debug.Log("=== ???????? ===");
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    Debug.Log($"?????: {tile.tileName}, ID: {tile.tileID}");

                    // ?????????????????
                    if (tile.isBuildable)
                    {
                        Debug.LogError($"????: {tile.tileName} ????????????");
                    }

                    // ???????????????
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($"????: {tile.tileName} ????????");
                    }
                }
            }
        }
    }
}
