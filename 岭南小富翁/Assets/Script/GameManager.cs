using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 单例模式
    public static GameManager Instance;

    [Header("游戏状态")]
    public GameState currentState = GameState.Waiting;
    public int currentPlayerIndex = 0;
    public bool isGameStarted = false;
    public bool isPlayerTurn = true;
    public bool isMoving = false;

    [Header("玩家管理")]
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

    [Header("系统引用")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("游戏参数")]
    public int startingCash = 1500;
    public int salaryAmount = 200;
    public int jailTurns = 3;

    [Header("压力系统")]
    public bool enablePressureSystem = true;

    private int diceRollCount = 0;          // 骰子总次数
    private int pressureInterval = 1;        // 当前间隔（回合数）
    private int nextPressureAt = 1;          // 下一次触发在第几个回合
    public float basePressureCost = 50f;   // 初始压力金额
    public float pressureMultiplier = 1.2f; // 每次增长倍率

    [Header("调试")]
    public bool enableDebugKeys = true;

    // 游戏状态枚举
    public enum GameState
    {
        Waiting,           // 等待开始
        PlayerTurn,        // 玩家回合
        RollingDice,       // 投骰子
        Moving,            // 移动中
        ProcessingTile,    // 处理地块事件
        BuyingProperty,    // 购买地产
        BuildingSelection, // 建筑选择状态
        BuildingPlacement, // 建筑放置状态
        GameOver           // 游戏结束
    }

    void Awake()
    {
        // 单例模式
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
        Debug.Log("=== 游戏开始初始化 ===");
        InitializeGame();
        // === 重要修改1: 游戏开始时直接进入购买建筑阶段 ===
        StartCoroutine(StartInitialBuildingPhase());
    }

    // 游戏初始化
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

        // 状态初始化为等待然后切换为玩家回合
        currentState = GameState.Waiting;
        isGameStarted = true;

        UpdateUI();
        SetupButtonEvents();

        Debug.Log($"游戏初始化完成，玩家数: {players.Count}");
        Debug.Log($"当前玩家: {currentPlayer?.playerName ?? ""}");
    }

    // === 购买建筑阶段 ===
    IEnumerator StartInitialBuildingPhase()
    {
        // 等待一帧确保UI就绪
        yield return new WaitForSeconds(0.5f);

        if (currentPlayer != null)
        {
            Debug.Log($"=== 进入购买阶段，{currentPlayer.playerName} 可以进行初始购买 ===");

            // 1. 切换状态为建筑选择或允许移动
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;

            // 2. 禁用骰子按钮
            SetRollDiceButtonInteractable(false);

            // 3. 显示建筑购买UI
            if (uiManager != null)
            {
                // 创建一个"起始商店"Tile的UI
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager 未找到，无法显示建筑购买界面");
                // 如果没有UI则自动跳过购买阶段
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === 创建起始购买Tile ===
    BoardTile CreateStartPurchaseTile()
    {
        GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
        BoardTile tile = tempObj.AddComponent<BoardTile>();
        tile.tileName = "起始地块"; // 需要使用"起始地块"
        tile.tileType = BoardTile.TileType.Buildable; // 确保是可建造类型
        tile.propertyPrice = 100;
        tile.isBuildable = true;
        tile.tileScale = Random.Range(1, 4);

        // 设置为当前玩家拥有，建筑为可购买
        tile.ownerPlayer = currentPlayer;

        return tile;
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
            Debug.LogWarning("未找到任何玩家，请确保场景中有 Player 标签");
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
            Debug.Log($"棋盘初始化完成，共 {boardManager.allTiles.Count} 个地块");
        }
    }

    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("未找到起始地块");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;

            // 获取PlayerMovement组件的高度偏移，避免穿模
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                startPos.y = startTile.transform.position.y + playerMovement.heightOffset;
            }
            else
            {
                startPos.y = 0.875f; // 默认高度：地块中心 + 偏移
            }

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} 初始化，初始资金: {player.cash}");

            // === 新增：初始化独立资金显示UI ===
            if (UIManager.Instance != null)
            {
                // 这里只需要为当前玩家更新一次
                if (i == 0) // 第一个玩家
                {
                    UIManager.Instance.UpdateCashDisplay(player.cash);
                }
            }
            // === 新增结束 ===
        }
    }
    //旧版
    //void HandlePropertyTile()
    //{
    //    BoardTile tile = currentPlayer.currentTile;

    //    if (tile.ownerPlayer == null)
    //    {
    //        if (tile.tileType == BoardTile.TileType.Buildable)
    //        {
    //            currentState = GameState.BuildingSelection;
    //            Debug.Log($"{tile.tileName} 是可建造地块，价格: {tile.propertyPrice} 元");

    //            if (uiManager != null)
    //            {
    //                uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
    //            }
    //        }
    //        else
    //        {
    //            currentState = GameState.BuyingProperty;
    //            Debug.Log($"{tile.tileName} 可购买，价格: {tile.propertyPrice} 元");

    //            if (uiManager != null)
    //            {
    //                uiManager.ShowPropertyPurchasePanel(tile, currentPlayer);
    //            }
    //            else
    //            {
    //                AutoDecidePurchase(tile);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        StartCoroutine(EndMoveAfterDelay(1f));
    //    }
    //}

    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        // ========== 新增：检查关联建筑收入 ==========
        // 检查是否有关联建筑需要产生收入
        if (tile.EnableLinkedIncome &&
            tile.LinkedBuildingTiles != null &&
            tile.LinkedBuildingTiles.Count > 0)
        {
            Debug.Log($"=== 检查 {tile.tileName} 的关联建筑收入 ===");

            float currentTime = Time.time;
            int totalIncome = 0;

            foreach (BoardTile buildingTile in tile.LinkedBuildingTiles)
            {
                if (buildingTile == null) continue;

                // 检查所有者
                if (buildingTile.ownerPlayer == null || buildingTile.ownerPlayer != currentPlayer)
                    continue;

                // 检查建筑数据
                if (buildingTile.currentBuildingData == null)
                    continue;

                // 检查功能类型
                if (buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Income &&
                    buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Mixed)
                    continue;

                // 检查冷却时间
                float lastTime = tile.GetLastIncomeTime(buildingTile);
                if (lastTime > 0 && (currentTime - lastTime) < buildingTile.IncomeInterval)
                {
                    Debug.Log($"建筑 {buildingTile.tileName} 冷却中，跳过");
                    continue;
                }

                // 计算收入
                int income = buildingTile.currentBuildingData.GetIncomeAmount(buildingTile.buildingLevel);
                if (income > 0)
                {
                    currentPlayer.ReceiveCash(income);
                    totalIncome += income;

                    // 更新最后收入时间
                    tile.SetLastIncomeTime(buildingTile, currentTime);

                    Debug.Log($"从关联建筑 {buildingTile.tileName} 获得收入: {income} 元");
                }
            }

            if (totalIncome > 0)
            {
                Debug.Log($"=== 总计关联收入: {totalIncome} 元 ===");
                if (uiManager != null)
                {
                    uiManager.ShowToast($"关联收入: {totalIncome} 元", 2f);
                }
            }
        }
        // ========== 新增结束 ==========

        // 原有逻辑保持不变
        if (tile.ownerPlayer == null)
        {
            if (tile.tileType == BoardTile.TileType.Buildable)
            {
                currentState = GameState.BuildingSelection;
                Debug.Log($"{tile.tileName} 是可建造地块，价格: {tile.propertyPrice} 元");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} 可购买，价格: {tile.propertyPrice} 元");

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
    private void CheckLinkedBuildingIncome(BoardTile tile, Player player)
    {
        if (tile == null || player == null) return;

        // 检查是否有关联建筑
        if (tile.EnableLinkedIncome &&
            tile.LinkedBuildingTiles != null &&
            tile.LinkedBuildingTiles.Count > 0)
        {
            Debug.Log($"=== 检查 {tile.tileName} 的关联建筑收入 ===");
            Debug.Log($"地块类型: {tile.tileType}, 关联建筑数: {tile.LinkedBuildingTiles.Count}");

            float currentTime = Time.time;
            int totalIncome = 0;

            foreach (BoardTile buildingTile in tile.LinkedBuildingTiles)
            {
                if (buildingTile == null)
                {
                    Debug.Log("关联建筑为空，跳过");
                    continue;
                }

                // 检查所有者
                if (buildingTile.ownerPlayer == null)
                {
                    Debug.Log($"建筑 {buildingTile.tileName} 无所有者，跳过");
                    continue;
                }

                if (buildingTile.ownerPlayer != player)
                {
                    Debug.Log($"建筑 {buildingTile.tileName} 不属于当前玩家，跳过");
                    continue;
                }

                // 检查建筑数据
                if (buildingTile.currentBuildingData == null)
                {
                    Debug.Log($"建筑 {buildingTile.tileName} 无建筑数据，跳过");
                    continue;
                }

                // 检查功能类型
                if (buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Income &&
                    buildingTile.currentBuildingData.functionType != BuildingData.BuildingFunctionType.Mixed)
                {
                    Debug.Log($"建筑 {buildingTile.tileName} 不是收入型建筑，跳过");
                    continue;
                }

                // 检查冷却时间
                float lastTime = tile.GetLastIncomeTime(buildingTile);
                if (lastTime > 0 && (currentTime - lastTime) < buildingTile.IncomeInterval)
                {
                    Debug.Log($"建筑 {buildingTile.tileName} 冷却中，跳过");
                    continue;
                }

                // 计算收入
                int income = buildingTile.currentBuildingData.GetIncomeAmount(buildingTile.buildingLevel);
                if (income > 0)
                {
                    player.ReceiveCash(income);
                    totalIncome += income;

                    // 更新最后收入时间
                    tile.SetLastIncomeTime(buildingTile, currentTime);

                    Debug.Log($"从关联建筑 {buildingTile.tileName} 获得收入: {income} 元");
                }
            }

            if (totalIncome > 0)
            {
                Debug.Log($"=== 总计关联收入: {totalIncome} 元 ===");
                if (uiManager != null)
                {
                    uiManager.ShowToast($"关联收入: {totalIncome} 元", 2f);
                }
            }
            else
            {
                Debug.Log("未产生关联收入，可能条件不满足");
            }
        }
        else
        {
            Debug.Log($"地块 {tile.tileName} 无关联建筑或关联收入未开启");
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
            Debug.Log("骰子按钮事件已注册");
        }
        else
        {
            Debug.LogWarning("RollDiceButton 未赋值，将在UI层级中查找");

            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("自动找到骰子按钮");
                }
            }
        }
    }

    // ================= 游戏逻辑 =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("骰子按钮被点击");

        // === 重要修改2: 检查当前状态 ===
        if (!CanRollDice())
        {
            Debug.Log($"当前不可投骰子，状态: {currentState}");

            // 显示提示
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("请先完成建筑购买", 2f);
                }
            }
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("当前玩家为空");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} 开始投骰子");

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
        Debug.Log($"{currentPlayer.playerName} 投出 {lastDiceValue} 点");

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

        diceRollCount++;//骰子数+1
        Debug.Log($"当前骰子总次数: {diceRollCount}");
        CheckPressureTrigger();

        UpdateUI();
        StartMovePlayer();

    }

    private void CheckPressureTrigger()
    {
        if (!enablePressureSystem)
            return;

        int currentRound = diceRollCount / 6;

        if (currentRound >= nextPressureAt)
        {
            TriggerPressure();
        }
    }

    private void TriggerPressure()
    {
        Debug.Log($"压力触发！第 {nextPressureAt} 个压力回合");

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

        //更新下一轮
        nextPressureAt += pressureInterval;
        pressureInterval++;

        basePressureCost *= pressureMultiplier;

        if (UIManager.Instance != null)
        {
            //UIManager.Instance.ShowToast(
            //    $"征税！需要支付 {cost} 金币",
            //    3f
            //);
            UIManager.Instance.ShowTurnAnnouncement(
                $"第 {nextPressureAt} 回合   征税 {cost} 金币"
            );
        }
    }

    // === 重要修改3: 投骰子条件检查 ===
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

        // === 重要修改4: 检查是否经过起点，触发工资和购买 ===
        CheckPassingStart();

        ProcessCurrentTile();
    }

    // === 重要修改5: 经过起点的处理 ===
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

        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        if (previousIndex > currentIndex) // 经过了一圈
        {
            // 1. 发工资
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} 经过起点，获得 {salary} 元工资");

            if (uiManager != null)
            {
                uiManager.ShowToast($"经过起点，获得{salary}元工资", 2f);
            }

            // 2. 触发购买建筑阶段或允许移动
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. 显示建筑购买界面
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // === 触发购买建筑 ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // 延迟一下，让玩家看到工资信息
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 获得工资");
            // 没有UI则直接跳过购买阶段
            OnBuildingPurchaseCompleted();
        }
    }
    //旧版
    //void ProcessCurrentTile()
    //{
    //    if (currentPlayer == null || currentPlayer.currentTile == null)
    //    {
    //        EndMove();
    //        return;
    //    }

    //    currentState = GameState.ProcessingTile;

    //    Debug.Log($"{currentPlayer.playerName} 到达 {currentPlayer.currentTile.tileName}");

    //    // 处理起点：也需要触发购买
    //    if (currentPlayer.currentTile.tileType == BoardTile.TileType.Start)
    //    {
    //        // BoardTile中的处理可能会被触发
    //        currentPlayer.currentTile.OnLanded(currentPlayer);
    //    }
    //    else if (currentPlayer.currentTile.tileType == BoardTile.TileType.Property ||
    //             currentPlayer.currentTile.tileType == BoardTile.TileType.Railroad ||
    //             currentPlayer.currentTile.tileType == BoardTile.TileType.Utility)
    //    {
    //        HandlePropertyTile();
    //    }
    //    else
    //    {
    //        StartCoroutine(EndMoveAfterDelay(0.2f));
    //    }
    //}
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

        // ========== 关键修复：为所有需要关联收入的地块类型检查关联收入 ==========
        // 包括 Normal 类型的地块
        bool shouldCheckLinkedIncome =
            currentTile.tileType == BoardTile.TileType.Normal ||
            currentTile.tileType == BoardTile.TileType.Property ||
            currentTile.tileType == BoardTile.TileType.Railroad ||
            currentTile.tileType == BoardTile.TileType.Utility ||
            currentTile.tileType == BoardTile.TileType.Buildable ||
            currentTile.tileType == BoardTile.TileType.BuildingSite;

        if (shouldCheckLinkedIncome)
        {
            CheckLinkedBuildingIncome(currentTile, currentPlayer);
        }
        // ========== 修复结束 ==========

        // 原有处理逻辑保持不变
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
            Debug.Log($"{currentPlayer.playerName} 现金不足，无法购买 {tile.tileName}");
        }

        StartCoroutine(EndMoveAfterDelay(1f));
    }

    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($"地产购买: {(purchased ? "已购买" : "未购买")}");
        StartCoroutine(EndMoveAfterDelay(0.5f));
    }

    IEnumerator EndMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMove();
    }

    void EndMove()
    {
        Debug.Log($"{currentPlayer.playerName} 移动阶段结束");

        isMoving = false;

        if (currentPlayer.cash < 0)
        {
            Debug.Log($"{currentPlayer.playerName} 现金为负破产");
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
        Debug.Log($"{currentPlayer.playerName} 的回合结束");

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

        Debug.Log($"=== {currentPlayer.playerName} 的回合开始 ===");
        UpdateUI();

        // === 新增：确保独立资金显示面板更新为当前玩家的资金 ===
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
        }
        // === 新增结束 ===

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
            Debug.Log($"{currentPlayer.playerName} 在狱中，剩余 {currentPlayer.jailTurnsRemaining} 回合");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} 在狱中，剩余{currentPlayer.jailTurnsRemaining}回合", 2f);
            }

            EndTurn();
        }
    }

    void HandlePlayerBankrupt(Player player)
    {
        Debug.Log($"=== 玩家破产: {player.playerName} ===");

        player.isBankrupt = true;

        foreach (BoardTile property in player.ownedProperties)
        {
            property.ownerPlayer = null;
            Debug.Log($"释放玩家拥有的地产: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} 破产退出游戏");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} 破产退出游戏", 3f);
        }
    }

    void GameOver()
    {
        currentState = GameState.GameOver;
        isGameStarted = false;

        if (players.Count == 1)
        {
            Player winner = players[0];
            Debug.Log($"=== 游戏结束，{winner.playerName} 获胜 ===");

            if (uiManager != null)
            {
                uiManager.ShowGameOverPanel(winner.playerName);
            }
        }
        else
        {
            Debug.Log("=== 游戏结束，没有赢家 ===");
        }
    }

    // ================= UI 更新 =================

    public void UpdateUI()
    {
        if (currentPlayer == null) return;

        if (currentPlayerText != null)
            currentPlayerText.text = $"当前玩家: {currentPlayer.playerName}";

        if (playerCashText != null)
            playerCashText.text = $"现金: {currentPlayer.cash} 元";

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

    // === 重要修改6: 建筑购买完成的回调 ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("建筑购买完成，可以开始移动");

        isMoving = false;//重置移动标志

        // 切换状态为玩家回合
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // 更新UI和骰子按钮
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

    // ================= 调试和控制 =================

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
            Debug.Log("强制结束当前回合");
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
            TriggerPressure();
        }
    }

    void TestRollDice()
    {
        Debug.Log("测试投骰子");
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
        Debug.Log($"当前状态: {currentState}");
        Debug.Log($"当前玩家: {currentPlayer?.playerName}");
        Debug.Log($"玩家数: {players.Count}");
        Debug.Log($"当前回合: {currentPlayerIndex}");
        Debug.Log($"游戏是否开始: {isGameStarted}");
        Debug.Log($"是否玩家回合: {isPlayerTurn}");
        Debug.Log($"是否移动中: {isMoving}");
        Debug.Log($"骰子值: {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($"玩家现金: {currentPlayer.cash}");
            Debug.Log($"当前位置: {currentPlayer.currentTile?.tileName}");
            Debug.Log($"是否在监狱: {currentPlayer.isInJail}");
            Debug.Log($"剩余回合: {currentPlayer.jailTurnsRemaining}");
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

    void CheckStartTiles()
    {
        if (boardManager != null)
        {
            Debug.Log("=== 检查起点 ===");
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    Debug.Log($"找到起点: {tile.tileName}, ID: {tile.tileID}");

                    // 检查是否被错误标记为可建造
                    if (tile.isBuildable)
                    {
                        Debug.LogError($"起点 {tile.tileName} 被标记为可建造，错误！");
                    }

                    // 检查是否有建筑
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($"起点 {tile.tileName} 有建筑");
                    }
                }
            }
        }
    }
}