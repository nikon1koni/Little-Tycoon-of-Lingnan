using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 游戏管理器
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

    [Header("骰子控制")]
    public DiceController diceController;
    public Dice3DController dice3DController;
    public int lastDiceValue = 0;

    [Header("UI 引用")]
    public Text currentPlayerText;
    public Text playerCashText;
    public Text diceResultText;
    public Text currentTileText;
    public Button rollDiceButton;

    [Header("核心管理器")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("游戏配置")]
    public int startingCash = 1500;
    public int salaryAmount = 200;
    public int jailTurns = 3;

    [Header("压力系统")]
    public bool enablePressureSystem = true;

    private int diceRollCount = 0;          // 骰子投掷次数
    private int pressureInterval = 1;        // 压力触发间隔（回合数）
    private int nextPressureAt = 1;          // 下次压力触发的回合
    public float basePressureCost = 50f;   // 基础压力费用
    public float pressureMultiplier = 1.2f;

    public int DiceRollCount => diceRollCount;
    public int CurrentRound => diceRollCount / 6;

    [Header("调试")]
    public bool enableDebugKeys = true;

    [Header("背景音乐")]
    public bool enableBackgroundMusic = true;
    public MusicManager musicManager;

    [Header("音效系统")]
    public SFXConfig sfxConfig;
    public bool enableSFX = true;

    // 游戏状态枚举
    public enum GameState
    {
        Waiting,           // 等待中
        PlayerTurn,        // 玩家回合
        RollingDice,       // 掷骰子中
        Moving,            // 移动中
        ProcessingTile,    // 处理格子事件
        BuyingProperty,    // 购买地产
        BuildingSelection, // 选择建筑
        BuildingPlacement, // 放置建筑中
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
        Debug.Log("=== 岭南小富翁 游戏开始 ===");
        InitializeGame();
        // === 阶段1: 初始建筑选择阶段 ===
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

        // 设置为等待状态
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
                Debug.Log("MusicManager 创建成功");
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
                    Debug.LogWarning("SFXConfig 未设置，请在Inspector中设置或放在Resources文件夹中");
                }
            }

            Debug.Log("SFXManager 创建完成");
        }
        else if (sfxConfig != null && SFXManager.Instance.config == null)
        {
            SFXManager.Instance.config = sfxConfig;
            SFXManager.Instance.ReloadClips();
        }
    }

    // === 开始初始建筑选择阶段 ===
    IEnumerator StartInitialBuildingPhase()
    {
        // 等待UI加载完成
        yield return new WaitForSeconds(0.5f);

        if (currentPlayer != null)
        {
            Debug.Log($"=== 阶段: {currentPlayer.playerName} 选择初始建筑 ===");

            // 1. 设置游戏状态为建筑选择
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;

            // 2. 禁用掷骰子按钮
            SetRollDiceButtonInteractable(false);

            // 3. 显示建筑选择UI
            if (uiManager != null)
            {
                // 创建一个"初始购买"Tile用于UI显示
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager 未设置，跳过建筑选择");
                // 直接继续游戏流程
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === 创建初始购买Tile ===
    private BoardTile startPurchaseTileCache = null;

    BoardTile CreateStartPurchaseTile()
    {
        if (startPurchaseTileCache == null)
        {
            GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
            startPurchaseTileCache = tempObj.AddComponent<BoardTile>();
        }

        startPurchaseTileCache.tileName = "初始购买";
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

        Debug.Log($"组件查找结果: DiceController={diceController != null}, BoardManager={boardManager != null}, UIManager={uiManager != null}");
    }

    void FindAllPlayers()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        players.Clear();
        players.AddRange(allPlayers);
        players.Sort((a, b) => a.playerID.CompareTo(b.playerID));

        if (players.Count == 0)
        {
            Debug.LogWarning("场景中没有找到Player组件，请确保场景中有Player对象");
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
            Debug.LogWarning("棋盘上没有格子");
        }
        else
        {
            Debug.Log($"棋盘初始化完成，共有 {boardManager.allTiles.Count} 个格子");
        }
    }

    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("找不到起点格子");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;

            // 获取PlayerMovement组件来设置高度
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                startPos.y = startTile.transform.position.y + playerMovement.heightOffset;
            }
            else
            {
                startPos.y = 0.875f; // 默认高度 + 角色高度
            }

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} 初始资金: {player.cash}");

            // === 更新初始玩家的UI显示 ===
            if (UIManager.Instance != null)
            {
                // 更新第一个玩家的现金显示
                if (i == 0) // 第一个玩家
                {
                    UIManager.Instance.UpdateCashDisplay(player.cash);
                }
            }
            // === UI更新完成 ===
        }
    }

    // 处理地产格子
    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        if (tile.ownerPlayer == null)
        {
            if (tile.tileType == BoardTile.TileType.Buildable)
            {
                currentState = GameState.BuildingSelection;
                Debug.Log($"{tile.tileName} 可建造，价格: {tile.propertyPrice} 金币");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} 可购买，价格: {tile.propertyPrice} 金币");

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
            Debug.Log("掷骰子按钮事件已绑定");
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
                    Debug.Log("掷骰子按钮已找到");
                }
            }
        }
    }

    // ================= 掷骰子相关 =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("掷骰子按钮被点击");

        // === 阶段2: 检查是否可以掷骰子 ===
        if (!CanRollDice())
        {
            Debug.Log($"当前无法掷骰子，状态: {currentState}");

            // 如果在建筑选择状态
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("请先完成建筑选择（按ESC取消）", 2f);
                }
            }
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("没有当前玩家");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} 开始掷骰子");

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

        diceRollCount++;
        Debug.Log($"总掷骰子次数: {diceRollCount}");

        UpdateUI();

        if (uiManager != null)
        {
            uiManager.UpdatePressureSystemUI();
        }

        StartMovePlayer();
    }

    // 检查压力触发
    private void CheckPressureTrigger()
    {
        if (!enablePressureSystem)
            return;

        int currentRound = diceRollCount / 6;
        
        Debug.Log($"CheckPressureTrigger: diceRollCount={diceRollCount}, currentRound={currentRound}, nextPressureAt={nextPressureAt}");

        // 如果当前回合达到压力触发回合
        if (currentRound >= nextPressureAt)
        {
            TriggerPressure(currentRound);
        }
    }

    // 触发压力机制
    private void TriggerPressure(int currentRound)
    {
        Debug.Log($"第 {currentRound} 回合触发压力机制");

        int cost = Mathf.RoundToInt(basePressureCost);

        // 更新下次压力触发回合和压力费用
        nextPressureAt++;
        basePressureCost *= pressureMultiplier;

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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTurnAnnouncement(
                $"第 {currentRound} 回合   支付 {cost} 金币"
            );
        }
    }

    // === 阶段3: 检查是否可以掷骰子 ===
    public bool CanRollDice()
    {
        bool canRoll = isGameStarted &&
                       currentState == GameState.PlayerTurn && // 必须是玩家回合
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

        Debug.Log($"{currentPlayer.playerName} 开始移动 {lastDiceValue} 步");

        currentState = GameState.Moving;
        isMoving = true;

        PlayerMovement movement = currentPlayer.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError($"{currentPlayer.playerName} 缺少 PlayerMovement 组件");
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

        // === 阶段4: 检查是否经过起点 ===
        CheckPassingStart();

        ProcessCurrentTile();
    }

    // === 阶段5: 检查是否经过起点 ===
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

        // 判断玩家是否站在起点（tileID == 0 或 tileType == Start）
        bool isOnStartTile = (currentPlayer.currentTile.tileID == 0 ||
                             currentPlayer.currentTile.tileType == BoardTile.TileType.Start);

        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        // 如果玩家经过了起点（索引从高到低变化）但不在起点格子上
        if (!isOnStartTile && previousIndex > currentIndex)
        {
            Debug.Log($"{currentPlayer.playerName} 经过了起点");

            // 1. 发放工资
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} 获得 {salary} 元工资");

            if (uiManager != null)
            {
                uiManager.ShowToast($"经过起点!获得{salary}金币!", 2f);
            }

            // 2. 进入建筑购买阶段
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. 触发建筑购买
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
        else if (isOnStartTile)
        {
            Debug.Log($"{currentPlayer.playerName} 站在起点BoardTile上");

            // 起点格子的逻辑已经在BoardTile.OnLanded中处理
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // === 经过起点后触发建筑购买 ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // 等待动画结束后再显示建筑选择UI
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 跳过建筑选择");
            // 如果没有UI管理，直接完成购买流程
            OnBuildingPurchaseCompleted();
        }
    }

    // 处理当前格子
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
                Debug.Log($"{currentPlayer.playerName} 购买了地产 {tile.tileName}");
            }
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 到达了 {tile.tileName}");
        }

        StartCoroutine(EndMoveAfterDelay(1f));
    }

    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($"购买结果: {(purchased ? "成功" : "失败")}");
        StartCoroutine(EndMoveAfterDelay(0.5f));
    }

    IEnumerator EndMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMove();
    }

    void EndMove()
    {
        Debug.Log($"{currentPlayer.playerName} 移动结束");

        isMoving = false;

        bool shouldEndTurn = true;

        CheckPressureTrigger();

        if (currentPlayer.isBankrupt)
        {
            shouldEndTurn = false;
        }
        else if (currentPlayer.cash < 0)
        {
            Debug.Log($"{currentPlayer.playerName} 破产了");
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
        Debug.Log($"{currentPlayer.playerName} 回合结束");

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

        // === 更新当前玩家UI ===
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
            Debug.Log($"{currentPlayer.playerName} 出狱了");
            StartPlayerTurn();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 还在监狱中，剩余 {currentPlayer.jailTurnsRemaining} 回合");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} 在监狱中，剩余{currentPlayer.jailTurnsRemaining}回合", 2f);
            }

            EndTurn();
        }
    }

    void HandlePlayerBankrupt(Player player)
    {
        Debug.Log($"=== 破产处理: {player.playerName} ===");

        player.isBankrupt = true;

        foreach (BoardTile property in player.ownedProperties)
        {
            property.ownerPlayer = null;
            Debug.Log($"释放地产: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} 破产了");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} 破产了!", 3f);
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
            Debug.Log($"=== 游戏结束! {player.playerName}: {(isWinner ? "胜利": "失败")} ===");

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
                Debug.Log($"=== 游戏结束! 获胜者: {winner.playerName} ===");
                if (uiManager != null)
                {
                    uiManager.ShowGameOverPanel(winner.playerName, true);
                }
            }
            else
            {
                Debug.Log("=== 游戏结束，没有获胜者 ===");
            }
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
            diceResultText.text = $"点数: {lastDiceValue}";

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

    // === 阶段6: 建筑购买完成后处理 ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("建筑购买完成，继续游戏流程");

        isMoving = false;//重置移动状态

        // 设置回玩家回合状态
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // 更新UI显示
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
        
        // 在检查压力触发前检查玩家是否已破产
        // 因为购买建筑可能导致破产
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} 购买后已破产");
            return;
        }
        
        // 检查是否需要触发压力机制
        CheckPressureTrigger();
        
        // 再次检查玩家是否在压力触发后破产
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} 压力触发后已破产");
            return;
        }
        
        // 结束当前回合
        EndTurn();
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
        Debug.Log("测试掷骰子");
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
        Debug.Log($"当前玩家: {currentPlayer?.playerName}");
        Debug.Log($"玩家数量: {players.Count}");
        Debug.Log($"玩家索引: {currentPlayerIndex}");
        Debug.Log($"游戏开始: {isGameStarted}");
        Debug.Log($"玩家回合: {isPlayerTurn}");
        Debug.Log($"正在移动: {isMoving}");
        Debug.Log($"骰子点数: {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($"金币: {currentPlayer.cash}");
            Debug.Log($"当前格子: {currentPlayer.currentTile?.tileName}");
            Debug.Log($"是否入狱: {currentPlayer.isInJail}");
            Debug.Log($"剩余刑期: {currentPlayer.jailTurnsRemaining}");
        }
    }

    public void RestartFromGameOver()
    {
        Debug.Log("重新开始游戏");
        
        currentState = GameState.PlayerTurn;
        isGameStarted = true;
        isPlayerTurn = true;
        isMoving = false;
        currentPlayerIndex = 0;
        diceRollCount = 0;
        nextPressureAt = 1;
        basePressureCost = 50f;
        
        // 清除所有建筑
        ClearAllBuildings();
        
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
                Debug.Log($"重置 {p.playerName} 到起点");
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
        
        // 重新开始初始建筑选择阶段
        StartCoroutine(StartInitialBuildingPhase());
        
        Debug.Log("游戏重新开始完成");
    }

    // 清除所有建筑
    private void ClearAllBuildings()
    {
        Debug.Log("正在清除所有建筑...");
        
        if (boardManager == null || boardManager.allTiles == null)
        {
            Debug.LogWarning("BoardManager 或 allTiles 未初始化");
            return;
        }
        
        foreach (BoardTile tile in boardManager.allTiles)
        {
            if (tile == null) continue;
            
            // 重置建筑数据
            tile.currentBuildingData = null;
            tile.currentBuildingType = BoardTile.BuildingType.None;
            tile.buildingLevel = 0;
            tile.ownerPlayer = null;
            
            // 销毁建筑对象
            if (tile.currentBuilding != null)
            {
                Destroy(tile.currentBuilding);
                tile.currentBuilding = null;
            }
        }
        
        Debug.Log("所有建筑已清除");
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
        // 事件面板关闭后的处理
        Debug.Log("事件面板已关闭");
        
        // 检查玩家是否已破产
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} 在事件后已破产");
            HandlePlayerBankrupt(currentPlayer);
            if (players.Count <= 1)
            {
                GameOver();
            }
            return;
        }
        
        // 重新启用掷骰子按钮
        SetRollDiceButtonInteractable(true);
        
        // 如果是建筑选择状态，不执行结束移动
        if (currentState == GameState.BuildingSelection)
        {
            Debug.Log("当前是建筑选择状态");
            return;
        }
        
        // 否则继续游戏流程，结束当前移动
        StartCoroutine(EndMoveAfterDelay(0.1f));
        UpdateUI();
    }

    void CheckStartTiles()
    {
        if (boardManager != null)
        {
            Debug.Log("=== 检查起点格子 ===");
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    Debug.Log($"起点: {tile.tileName}, ID: {tile.tileID}");

                    // 检查起点是否可建造（不应该）
                    if (tile.isBuildable)
                    {
                        Debug.LogError($"错误: {tile.tileName} 起点设置为可建造");
                    }

                    // 检查起点是否有建筑（不应该）
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($"错误: {tile.tileName} 起点上有建筑");
                    }
                }
            }
        }
    }
}
