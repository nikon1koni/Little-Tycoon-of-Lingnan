using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 单例实例
    public static GameManager Instance;

    [Header("游戏状态")]
    public GameState currentState = GameState.Waiting;
    public int currentPlayerIndex = 0;
    public bool isGameStarted = false;
    public bool isPlayerTurn = true;
    public bool isMoving = false;

    [Header("玩家列表")]
    public List<Player> players = new List<Player>();
    public Player currentPlayer;

    [Header("骰子系统")]
    public DiceController diceController;
    public Dice3DController dice3DController;
    public int lastDiceValue = 0;

    [Header("UI 引用")]
    public Text currentPlayerText;
    public Text playerCashText;
    public Text diceResultText;
    public Text currentTileText;
    public Button rollDiceButton;

    [Header("管理器引用")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("游戏参数")]
    public int startingCash = 1500;
    public int salaryAmount = 200;
    public int jailTurns = 3;

    [Header("压力系统")]
    public bool enablePressureSystem = true;

    private int diceRollCount = 0;          // 投骰总次数
    private int pressureInterval = 1;        // 压力触发间隔（回合数）
    private int nextPressureAt = 1;          // 下次触发压力的回合数
    public float basePressureCost = 50f;   // 压力征税基础金额
    public float pressureMultiplier = 1.2f; // 压力递增倍率

    [Header("调试")]
    public bool enableDebugKeys = true;

    [Header("音乐系统")]
    public bool enableBackgroundMusic = true;
    public MusicManager musicManager;

    [Header("音效配置")]
    public SFXConfig sfxConfig;
    public bool enableSFX = true;

    // 游戏状态枚举
    public enum GameState
    {
        Waiting,           // 等待中
        PlayerTurn,        // 玩家回合
        RollingDice,       // 投骰子中
        Moving,            // 移动中
        ProcessingTile,    // 处理地块事件
        BuyingProperty,    // 购买地产
        BuildingSelection, // 建筑选择阶段
        BuildingPlacement, // 建筑放置阶段
        GameOver           // 游戏结束
    }

    void Awake()
    {
        // 单例模式初始化
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
        Debug.Log("=== 岭南小富翁 游戏启动 ===");
        InitializeGame();
        // === 阶段1: 初始建筑购买阶段 ===
        StartCoroutine(StartInitialBuildingPhase());
    }

    // 初始化游戏
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

        // 设置初始状态为等待状态
        currentState = GameState.Waiting;
        isGameStarted = true;

        UpdateUI();
        SetupButtonEvents();
        InitializeMusicSystem();
        InitializeSFXSystem();

        Debug.Log($"玩家数量: {players.Count}");
        Debug.Log($"当前玩家: {currentPlayer?.playerName ?? ""}");
    }

    void InitializeMusicSystem()
    {
        if (!enableBackgroundMusic)
        {
            Debug.Log("背景音乐已禁用");
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
                Debug.Log("MusicManager 已创建");
            }
        }

        if (musicManager != null && musicManager.GetTotalTracks() > 0)
        {
            musicManager.Play();
            Debug.Log("背景音乐开始播放");
        }
        else
        {
            Debug.LogWarning("MusicManager 没有可用的音乐轨道");
        }
    }

    void InitializeSFXSystem()
    {
        if (!enableSFX)
        {
            Debug.Log("音效系统已禁用");
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
                    Debug.LogWarning("SFXConfig 未找到，请在Inspector中设置或放入Resources文件夹");
                }
            }

            Debug.Log("SFXManager 初始化完成");
        }
        else if (sfxConfig != null && SFXManager.Instance.config == null)
        {
            SFXManager.Instance.config = sfxConfig;
            SFXManager.Instance.ReloadClips();
        }
    }

    // === 初始建筑购买阶段 ===
    IEnumerator StartInitialBuildingPhase()
    {
        // 短暂延迟确保UI准备就绪
        yield return new WaitForSeconds(0.5f);

        if (currentPlayer != null)
        {
            Debug.Log($"=== 阶段: {currentPlayer.playerName} 可购买初始建筑 ===");

            // 1. 切换到建筑选择状态并锁定操作
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;

            // 2. 禁用投骰按钮
            SetRollDiceButtonInteractable(false);

            // 3. 显示建筑选择UI
            if (uiManager != null)
            {
                // 创建一个虚拟的"起点商店"Tile用于UI显示
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager 未找到，跳过初始建筑选择");
                // 直接完成建筑购买流程
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === 创建虚拟起点购买Tile ===
    private BoardTile startPurchaseTileCache = null;

    BoardTile CreateStartPurchaseTile()
    {
        if (startPurchaseTileCache == null)
        {
            GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
            startPurchaseTileCache = tempObj.AddComponent<BoardTile>();
        }

        startPurchaseTileCache.tileName = "起点商店";
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

        Debug.Log($"组件查找: DiceController={diceController != null}, BoardManager={boardManager != null}, UIManager={uiManager != null}");
    }

    void FindAllPlayers()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        players.Clear();
        players.AddRange(allPlayers);
        players.Sort((a, b) => a.playerID.CompareTo(b.playerID));

        if (players.Count == 0)
        {
            Debug.LogWarning("未找到任何Player对象，请检查场景中的Player预制体");
        }
    }

    void CheckBoard()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager 未找到");
            return;
        }

        if (boardManager.allTiles == null || boardManager.allTiles.Count == 0)
        {
            Debug.LogWarning("棋盘上没有地块");
        }
        else
        {
            Debug.Log($"棋盘共有 {boardManager.allTiles.Count} 个地块");
        }
    }

    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("未找到起点地块");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;

            // 获取PlayerMovement组件获取高度偏移
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                startPos.y = startTile.transform.position.y + playerMovement.heightOffset;
            }
            else
            {
                startPos.y = 0.875f; // 默认高度偏移 + 地面高度
            }

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} 初始资金: {player.cash}");

            // === 更新第一个玩家的资金显示UI ===
            if (UIManager.Instance != null)
            {
                // 只为第一个玩家更新资金显示
                if (i == 0) // 第一个玩家
                {
                    UIManager.Instance.UpdateCashDisplay(player.cash);
                }
            }
            // === 初始化结束 ===
        }
    }

    // 处理地产地块事件
    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        CheckLinkedBuildingIncome(tile, currentPlayer);

        if (tile.ownerPlayer == null)
        {
            if (tile.tileType == BoardTile.TileType.Buildable)
            {
                currentState = GameState.BuildingSelection;
                Debug.Log($"{tile.tileName} 建筑出售价格: {tile.propertyPrice} 金币");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} 购买价格: {tile.propertyPrice} 金币");

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

    // 检查关联建筑收入
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
                uiManager.ShowToast($"关联收入: {totalIncome} 金币", 2f);
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
            Debug.Log("投骰按钮事件已绑定");
        }
        else
        {
            Debug.LogWarning("RollDiceButton 未在Inspector中设置，尝试自动查找");

            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("投骰按钮已绑定");
                }
            }
        }
    }

    // ================= 玩家操作 =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("投骰按钮被点击");

        // === 步骤2: 状态检查 ===
        if (!CanRollDice())
        {
            Debug.Log($"当前不允许投骰，状态: {currentState}");

            // 提示原因
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("请先完成建筑选择或按ESC取消", 2f);
                }
            }
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("当前玩家为空");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} 开始投骰");

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
        Debug.Log($"{currentPlayer.playerName} 掷出 {lastDiceValue} 点");

        if (diceResultText != null)
            diceResultText.text = lastDiceValue.ToString();

        if (uiManager != null)
            uiManager.UpdateDiceResult(lastDiceValue);

        StartMovePlayer();
    }

    public void OnDiceRolled(int value)
    {
        lastDiceValue = value;
        Debug.Log($"骰子结果: {value}");

        diceRollCount++;//投骰次数+1
        Debug.Log($"累计投骰次数: {diceRollCount}");
        CheckPressureTrigger();

        UpdateUI();
        StartMovePlayer();
    }

    // 检查是否触发压力系统
    private void CheckPressureTrigger()
    {
        if (!enablePressureSystem)
            return;

        int currentRound = diceRollCount / 6;

        if (currentRound >= nextPressureAt)
        {
            TriggerPressure(currentRound);
        }
    }

    // 触发压力征税
    private void TriggerPressure(int currentRound)
    {
        Debug.Log($"第 {currentRound} 回合触发压力征税");

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
                    UIManager.Instance.ShowGameOverPanel(p.playerName);
                }

                GameOver();
                return;
            }
        }

        // 准备下一次压力征税
        nextPressureAt++;

        basePressureCost *= pressureMultiplier;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTurnAnnouncement(
                $"第 {currentRound} 回合   征税 {cost} 金币"
            );
        }
    }

    // === 步骤3: 是否可以投骰 ===
    public bool CanRollDice()
    {
        bool canRoll = isGameStarted &&
                       currentState == GameState.PlayerTurn && // 必须是玩家回合状态
                       !isMoving &&
                       currentPlayer != null &&
                       !currentPlayer.isInJail;

        Debug.Log($"CanRollDice: {canRoll} | State: {currentState} | isMoving: {isMoving} | Player: {currentPlayer?.playerName}");

        return canRoll;
    }

    void StartMovePlayer()
    {
        if (currentPlayer == null) return;

        Debug.Log($"{currentPlayer.playerName} 开始移动 {lastDiceValue} 步");

        currentState = GameState.Moving;
        isMoving = true;

        PlayerMovement movement = currentPlayer.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError($"{currentPlayer.playerName} 没有 PlayerMovement 组件");
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

        Debug.Log($"{currentPlayer.playerName} 移动完成");

        // === 步骤4: 检查是否经过起点 ===
        CheckPassingStart();

        ProcessCurrentTile();
    }

    // === 步骤5: 经过起点检查 ===
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

        // 检查当前是否正好在起点（tileID == 0 或 tileType == Start）
        bool isOnStartTile = (currentPlayer.currentTile.tileID == 0 ||
                             currentPlayer.currentTile.tileType == BoardTile.TileType.Start);

        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        // 如果之前的位置索引大于当前位置索引，说明经过了起点
        if (!isOnStartTile && previousIndex > currentIndex)
        {
            Debug.Log($"{currentPlayer.playerName} 经过起点，获得工资");

            // 1. 发放工资
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} 获得 {salary} 金币工资");

            if (uiManager != null)
            {
                uiManager.ShowToast($"经过起点获得{salary}金币!", 2f);
            }

            // 2. 进入建筑购买机会状态
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. 显示建筑选择面板
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
        else if (isOnStartTile)
        {
            Debug.Log($"{currentPlayer.playerName} 正好在起点BoardTile上");

            // 即使停在起点也提供购买机会，但通过BoardTile.OnLanded处理
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // === 触发起点后的建筑购买 ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // 稍微延迟让玩家看到位置变化
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 跳过建筑选择");
            // 如果没有UI管理器，直接继续游戏
            OnBuildingPurchaseCompleted();
        }
    }

    // 处理当前所在地块
    void ProcessCurrentTile()
    {
        if (currentPlayer == null || currentPlayer.currentTile == null)
        {
            EndMove();
            return;
        }

        currentState = GameState.ProcessingTile;

        Debug.Log($"{currentPlayer.playerName} 到达 {currentPlayer.currentTile.tileName}");

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
                Debug.Log($"{currentPlayer.playerName} 自动购买了 {tile.tileName}");
            }
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 无法负担 {tile.tileName}");
        }

        StartCoroutine(EndMoveAfterDelay(1f));
    }

    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($"地产购买: {(purchased ? "成功" : "取消")}");
        StartCoroutine(EndMoveAfterDelay(0.5f));
    }

    IEnumerator EndMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMove();
    }

    void EndMove()
    {
        Debug.Log($"{currentPlayer.playerName} 移动回合结束");

        isMoving = false;

        if (currentPlayer.cash < 0)
        {
            Debug.Log($"{currentPlayer.playerName} 资金不足");
            HandlePlayerBankrupt(currentPlayer);

            if (players.Count <= 1)
            {
                GameOver();
                return;
            }
        }

        EndTurn();
    }

    public void EndTurn()
    {
        Debug.Log($"{currentPlayer.playerName} 结束回合");

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

        Debug.Log($"=== {currentPlayer.playerName} 的回合 ===");
        UpdateUI();

        // === 更新资金显示UI ===
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
        }
        // === UI更新完成 ===

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
            Debug.Log($"{currentPlayer.playerName} 出狱");
            StartPlayerTurn();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 还需坐牢 {currentPlayer.jailTurnsRemaining} 回合");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} 坐牢中还需{currentPlayer.jailTurnsRemaining}回合", 2f);
            }

            EndTurn();
        }
    }

    void HandlePlayerBankrupt(Player player)
    {
        Debug.Log($"=== 破产检测: {player.playerName} ===");

        player.isBankrupt = true;

        foreach (BoardTile property in player.ownedProperties)
        {
            property.ownerPlayer = null;
            Debug.Log($"释放地产: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} 已破产");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} 已破产!", 3f);
        }
    }

    void GameOver()
    {
        currentState = GameState.GameOver;
        isGameStarted = false;

        if (players.Count == 1)
        {
            Player winner = players[0];
            Debug.Log($"=== 游戏结束! 胜者: {winner.playerName} ===");

            if (uiManager != null)
            {
                uiManager.ShowGameOverPanel(winner.playerName);
            }
        }
        else
        {
            Debug.Log("=== 游戏结束，无胜者 ===");
        }
    }

    // ================= UI 更新 =================

    public void UpdateUI()
    {
        if (currentPlayer == null) return;

        if (currentPlayerText != null)
            currentPlayerText.text = $"当前玩家: {currentPlayer.playerName}";

        if (playerCashText != null)
            playerCashText.text = $"金币: {currentPlayer.cash}";

        if (diceResultText != null)
            diceResultText.text = $"骰子: {lastDiceValue}";

        if (currentTileText != null && currentPlayer.currentTile != null)
            currentTileText.text = $"位置: {currentPlayer.currentTile.tileName}";

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

    // === 步骤6: 建筑购买完成后恢复 ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("建筑购买完成，恢复游戏流程");

        isMoving = false;//重置移动状态

        // 切换回玩家回合状态
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // 恢复UI和按钮交互
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

    // ================= 调试功能 =================

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
            Debug.Log("手动结束回合");
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
        Debug.Log("测试投骰");
        OnRollDiceButtonClicked();
    }

    public void TestMovePlayer(int steps)
    {
        if (currentPlayer == null || isMoving) return;

        Debug.Log($"测试移动: {currentPlayer.playerName} 移动 {steps} 步");

        lastDiceValue = steps;
        StartMovePlayer();
    }

    void DebugGameState()
    {
        Debug.Log("=== 游戏状态 ===");
        Debug.Log($"状态: {currentState}");
        Debug.Log($"玩家: {currentPlayer?.playerName}");
        Debug.Log($"人数: {players.Count}");
        Debug.Log($"玩家索引: {currentPlayerIndex}");
        Debug.Log($"是否开始: {isGameStarted}");
        Debug.Log($"玩家回合: {isPlayerTurn}");
        Debug.Log($"是否移动: {isMoving}");
        Debug.Log($"骰子值: {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($"资金: {currentPlayer.cash}");
            Debug.Log($"当前位置: {currentPlayer.currentTile?.tileName}");
            Debug.Log($"是否坐牢: {currentPlayer.isInJail}");
            Debug.Log($"牢狱剩余: {currentPlayer.jailTurnsRemaining}");
        }
    }

    public void ResetGame()
    {
        Debug.Log("重置游戏");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void AddPlayer(Player player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"添加玩家: {player.playerName}");
        }
    }

    public void RemovePlayer(Player player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($"移除玩家: {player.playerName}");

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
        // 事件面板关闭后，继续游戏流程
        Debug.Log("事件面板已关闭，继续游戏");
        SetRollDiceButtonInteractable(true);
        UpdateUI();
    }

    void CheckStartTiles()
    {
        if (boardManager != null)
        {
            Debug.Log("=== 检查起点地块 ===");
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    Debug.Log($"起点地块: {tile.tileName}, ID: {tile.tileID}");

                    // 检查起点是否标记为可建造
                    if (tile.isBuildable)
                    {
                        Debug.LogError($"错误: {tile.tileName} 不应标记为可建造");
                    }

                    // 检查起点是否有建筑
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($"警告: {tile.tileName} 上有建筑");
                    }
                }
            }
        }
    }
}
