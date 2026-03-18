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

    [Header("玩家管理")]
    public List<Player> players = new List<Player>();
    public Player currentPlayer;

    [Header("骰子管理")]
    public DiceController diceController;
    public int lastDiceValue = 0;

    [Header("UI引用")]
    public Text currentPlayerText;
    public Text playerCashText;
    public Text diceResultText;
    public Text currentTileText;
    public Button rollDiceButton;

    [Header("其他管理器")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("游戏设置")]
    public int startingCash = 1500;
    public int salaryAmount = 200;  // 经过起点获得的薪水
    public int jailTurns = 3;       // 监狱回合数

    [Header("调试设置")]
    public bool enableDebugKeys = true;

    // 游戏状态枚举
    public enum GameState
    {
        Waiting,           // 等待开始
        PlayerTurn,        // 玩家回合
        RollingDice,       // 掷骰子中
        Moving,           // 移动中
        ProcessingTile,   // 处理格子事件
        BuyingProperty,   // 购买地产中
        GameOver          // 游戏结束
    }

    // 初始化
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
        Debug.Log("=== 大富翁游戏初始化 ===");
        InitializeGame();
    }

    // 游戏初始化
    void InitializeGame()
    {
        // 查找必要的组件
        FindRequiredComponents();

        // 收集所有玩家
        FindAllPlayers();

        // 检查棋盘
        CheckBoard();

        // 初始化玩家位置
        InitializePlayerPositions();

        // 设置初始玩家
        if (players.Count > 0)
        {
            currentPlayer = players[currentPlayerIndex];
        }

        // 更新状态
        currentState = GameState.PlayerTurn;
        isGameStarted = true;

        // 更新UI
        UpdateUI();

        // 绑定按钮事件
        SetupButtonEvents();

        Debug.Log($"游戏初始化完成！玩家数: {players.Count}");
        Debug.Log($"当前玩家: {currentPlayer?.playerName ?? "无"}");
    }

    // 查找必要组件
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

    // 查找所有玩家
    void FindAllPlayers()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        players.Clear();
        players.AddRange(allPlayers);

        // 按玩家ID排序
        players.Sort((a, b) => a.playerID.CompareTo(b.playerID));

        if (players.Count == 0)
        {
            Debug.LogWarning("没有找到玩家！请确保场景中有Player对象。");
        }
    }

    // 检查棋盘
    void CheckBoard()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager 未找到！");
            return;
        }

        if (boardManager.allTiles == null || boardManager.allTiles.Count == 0)
        {
            Debug.LogWarning("棋盘没有格子！");
        }
        else
        {
            Debug.Log($"棋盘初始化完成，共有 {boardManager.allTiles.Count} 个格子");
        }
    }

    // 初始化玩家位置
    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("找不到起点格子！");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            // 计算偏移位置，避免玩家重叠
            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;
            startPos.y = 0.5f;  // 在格子上方

            player.transform.position = startPos;

            // 设置玩家当前格子
            player.currentTile = startTile;
            player.currentTileIndex = 0;

            // 设置初始现金
            player.cash = startingCash;

            Debug.Log($"{player.playerName} 放置在起点，现金: {player.cash}");
        }
    }

    // 获取起点格子
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
            return boardManager.allTiles[0];  // 返回第一个格子作为起点
        }
        return null;
    }

    // 设置按钮事件
    void SetupButtonEvents()
    {
        if (rollDiceButton != null)
        {
            // 移除之前的监听器
            rollDiceButton.onClick.RemoveAllListeners();

            // 添加新的监听器
            rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);

            Debug.Log("骰子按钮事件已绑定");
        }
        else
        {
            Debug.LogWarning("RollDiceButton 未分配，将在UI中手动查找");

            // 尝试自动查找
            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("已自动找到并绑定骰子按钮");
                }
            }
        }
    }

    // ================= 核心游戏逻辑 =================

    // 骰子按钮点击事件 - 这是UIManager会调用的方法
    public void OnRollDiceButtonClicked()
    {
        Debug.Log("骰子按钮被点击");

        if (!CanRollDice())
        {
            Debug.Log($"现在不能掷骰子，状态: {currentState}");
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("当前玩家为空！");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} 开始掷骰子");

        // 改变状态
        currentState = GameState.RollingDice;
        isPlayerTurn = false;

        // 如果有DiceController，使用它掷骰子
        if (diceController != null)
        {
            diceController.RollDice();
        }
        else
        {
            // 如果没有DiceController，直接生成随机数
            RollDiceSimple();
        }
    }

    // 简单的掷骰子（没有动画）
    void RollDiceSimple()
    {
        lastDiceValue = Random.Range(1, 7);
        Debug.Log($"{currentPlayer.playerName} 掷出了 {lastDiceValue} 点");

        // 更新UI
        if (diceResultText != null)
            diceResultText.text = lastDiceValue.ToString();

        if (uiManager != null)
            uiManager.UpdateDiceResult(lastDiceValue);

        // 开始移动
        StartMovePlayer();
    }

    // DiceController掷骰完成后的回调
    public void OnDiceRolled(int value)
    {
        lastDiceValue = value;
        Debug.Log($"骰子结果: {value}");

        // 更新UI
        UpdateUI();

        // 开始移动
        StartMovePlayer();
    }

    // 检查是否可以掷骰子
    public bool CanRollDice()
    {
        return isGameStarted &&
               currentState == GameState.PlayerTurn &&
               !isMoving &&
               currentPlayer != null &&
               !currentPlayer.isInJail;
    }

    // 开始移动玩家
    void StartMovePlayer()
    {
        if (currentPlayer == null) return;

        Debug.Log($"{currentPlayer.playerName} 开始移动 {lastDiceValue} 步");

        currentState = GameState.Moving;
        isMoving = true;

        // 获取PlayerMovement组件
        PlayerMovement movement = currentPlayer.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogError($"{currentPlayer.playerName} 没有PlayerMovement组件！");
            EndMove();
            return;
        }

        // 开始移动
        movement.MoveSteps(lastDiceValue);

        // 监听移动完成
        StartCoroutine(WaitForMoveComplete(movement));
    }

    IEnumerator WaitForMoveComplete(PlayerMovement movement)
    {
        // 等待移动完成
        while (movement.isMoving)
        {
            yield return null;
        }

        Debug.Log($"{currentPlayer.playerName} 移动完成");

        // 检查是否经过起点
        CheckPassingStart();

        // 处理格子事件
        ProcessCurrentTile();
    }

    // 检查是否经过起点
    void CheckPassingStart()
    {
        if (boardManager == null || currentPlayer == null) return;

        int startTileID = 0;
        int currentIndex = currentPlayer.currentTileIndex;
        int startIndex = 0;

        // 找到起点索引
        for (int i = 0; i < boardManager.allTiles.Count; i++)
        {
            if (boardManager.allTiles[i].tileType == BoardTile.TileType.Start)
            {
                startIndex = i;
                break;
            }
        }

        // 检查是否经过了起点
        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        if (previousIndex > currentIndex) // 经过了起点
        {
            // 玩家经过了起点
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} 经过起点，获得 {salary} 元薪水");

            if (uiManager != null)
            {
                uiManager.ShowToast($"经过起点，获得{salary}元薪水！", 2f);
            }
        }
    }

    // 处理当前格子事件
    void ProcessCurrentTile()
    {
        if (currentPlayer == null || currentPlayer.currentTile == null)
        {
            EndMove();
            return;
        }

        currentState = GameState.ProcessingTile;

        Debug.Log($"{currentPlayer.playerName} 落在 {currentPlayer.currentTile.tileName}");

        // 格子事件会在BoardTile.OnLanded中自动处理
        // 这里我们可以添加额外的逻辑

        // 如果是地产格，检查是否需要购买
        if (currentPlayer.currentTile.tileType == BoardTile.TileType.Property ||
            currentPlayer.currentTile.tileType == BoardTile.TileType.Railroad ||
            currentPlayer.currentTile.tileType == BoardTile.TileType.Utility)
        {
            HandlePropertyTile();
        }
        else
        {
            // 其他格子，延迟后结束移动
            StartCoroutine(EndMoveAfterDelay(1f));
        }
    }

    // 处理地产格子
    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        if (tile.ownerPlayer == null)
        {
            // 无主地产，可以购买
            currentState = GameState.BuyingProperty;

            Debug.Log($"{tile.tileName} 可以购买，价格: {tile.propertyPrice} 元");

            // 显示购买UI
            if (uiManager != null)
            {
                uiManager.ShowPropertyPurchasePanel(tile, currentPlayer);
            }
            else
            {
                // 没有UI管理器，自动决定是否购买
                AutoDecidePurchase(tile);
            }
        }
        else
        {
            // 已经有主，结束移动
            StartCoroutine(EndMoveAfterDelay(1f));
        }
    }

    // 自动决定是否购买（没有UI时使用）
    void AutoDecidePurchase(BoardTile tile)
    {
        if (currentPlayer.cash >= tile.propertyPrice)
        {
            // 现金足够，自动购买
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

    // 地产购买完成
    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($"地产购买完成: {(purchased ? "已购买" : "未购买")}");
        StartCoroutine(EndMoveAfterDelay(0.5f));
    }

    IEnumerator EndMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndMove();
    }

    // 结束移动
    void EndMove()
    {
        Debug.Log($"{currentPlayer.playerName} 的移动阶段结束");

        isMoving = false;

        // 检查玩家是否破产
        if (currentPlayer.cash < 0)
        {
            Debug.Log($"{currentPlayer.playerName} 现金为负，破产了！");
            HandlePlayerBankrupt(currentPlayer);

            if (players.Count <= 1)
            {
                GameOver();
                return;
            }
        }

        // 结束回合
        EndTurn();
    }

    // 结束当前回合
    public void EndTurn()
    {
        Debug.Log($"{currentPlayer.playerName} 的回合结束");

        // 切换到下一个玩家
        SwitchToNextPlayer();

        // 开始下一个玩家的回合
        StartCoroutine(StartNextTurnAfterDelay(1f));
    }

    // 切换到下一个玩家
    void SwitchToNextPlayer()
    {
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            currentPlayer = players[currentPlayerIndex];
        }
        while (currentPlayer.isBankrupt);  // 跳过破产的玩家
    }

    IEnumerator StartNextTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 检查玩家是否在监狱
        if (currentPlayer.isInJail)
        {
            HandleJailTurn();
        }
        else
        {
            StartPlayerTurn();
        }
    }

    // 开始玩家回合
    void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        Debug.Log($"=== {currentPlayer.playerName} 的回合开始 ===");

        // 更新UI
        UpdateUI();

        // 启用骰子按钮
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = true;
        }

        if (uiManager != null)
        {
            uiManager.SetRollDiceButtonInteractable(true);
            uiManager.UpdateRollDiceButtonText("掷骰子");
        }
    }

    // 处理玩家在监狱的情况
    void HandleJailTurn()
    {
        currentPlayer.jailTurnsRemaining--;

        if (currentPlayer.jailTurnsRemaining <= 0)
        {
            currentPlayer.isInJail = false;
            Debug.Log($"{currentPlayer.playerName} 出狱了！");
            StartPlayerTurn();
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 在监狱中，还剩 {currentPlayer.jailTurnsRemaining} 回合");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} 在监狱中，还剩{currentPlayer.jailTurnsRemaining}回合", 2f);
            }

            EndTurn();  // 跳过回合
        }
    }

    // 处理玩家破产
    void HandlePlayerBankrupt(Player player)
    {
        Debug.Log($"=== 处理玩家破产: {player.playerName} ===");

        // 标记为破产
        player.isBankrupt = true;

        // 释放所有地产
        foreach (BoardTile property in player.ownedProperties)
        {
            property.ownerPlayer = null;
            property.ownerPlayer = null;
            Debug.Log($"释放地产: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} 已破产退出游戏");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} 破产退出游戏！", 3f);
        }
    }

    // 游戏结束
    void GameOver()
    {
        currentState = GameState.GameOver;
        isGameStarted = false;

        if (players.Count == 1)
        {
            Player winner = players[0];
            Debug.Log($"=== 游戏结束！{winner.playerName} 获胜！ ===");

            // 显示胜利UI
            if (uiManager != null)
            {
                uiManager.ShowGameOverPanel(winner.playerName);
            }
        }
        else
        {
            Debug.Log("=== 游戏结束！没有赢家 ===");
        }
    }

    // ================= UI 管理 =================

    // 更新UI显示
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

        // 通知UIManager
        if (uiManager != null)
        {
            uiManager.UpdateCurrentPlayerInfo(currentPlayer);
        }
    }

    // 设置当前玩家（用于调试）
    public void SetCurrentPlayer(Player player)
    {
        if (player == null || !players.Contains(player)) return;

        currentPlayerIndex = players.IndexOf(player);
        currentPlayer = player;
        UpdateUI();
    }

    // 设置骰子按钮状态
    public void SetRollDiceButtonInteractable(bool interactable)
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = interactable;
        }
    }

    // ================= 调试和测试功能 =================

    void Update()
    {
        if (!enableDebugKeys) return;

        // 调试快捷键
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
    }

    // 测试掷骰子
    void TestRollDice()
    {
        Debug.Log("测试掷骰子");
        OnRollDiceButtonClicked();
    }

    // 测试移动玩家
    public void TestMovePlayer(int steps)
    {
        if (currentPlayer == null || isMoving) return;

        Debug.Log($"测试移动: {currentPlayer.playerName} 移动 {steps} 步");

        lastDiceValue = steps;
        StartMovePlayer();
    }

    // 调试游戏状态
    void DebugGameState()
    {
        Debug.Log("=== 游戏状态调试 ===");
        Debug.Log($"当前状态: {currentState}");
        Debug.Log($"当前玩家: {currentPlayer?.playerName}");
        Debug.Log($"玩家总数: {players.Count}");
        Debug.Log($"当前回合: {currentPlayerIndex}");
        Debug.Log($"是否游戏中: {isGameStarted}");
        Debug.Log($"是否玩家回合: {isPlayerTurn}");
        Debug.Log($"是否移动中: {isMoving}");
        Debug.Log($"骰子值: {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($"玩家现金: {currentPlayer.cash}");
            Debug.Log($"当前格子: {currentPlayer.currentTile?.tileName}");
            Debug.Log($"是否在监狱: {currentPlayer.isInJail}");
            Debug.Log($"监狱剩余回合: {currentPlayer.jailTurnsRemaining}");
        }
    }

    // 重置游戏
    public void ResetGame()
    {
        Debug.Log("重置游戏");

        // 重新加载场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // 添加玩家
    public void AddPlayer(Player player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"添加玩家: {player.playerName}");
        }
    }

    // 移除玩家
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

    // 获取所有玩家
    public List<Player> GetAllPlayers()
    {
        return new List<Player>(players);
    }

    // 获取特定玩家
    public Player GetPlayerByID(int id)
    {
        foreach (Player player in players)
        {
            if (player.playerID == id)
                return player;
        }
        return null;
    }

    // 检查游戏是否结束
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

    // 获取赢家
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
}