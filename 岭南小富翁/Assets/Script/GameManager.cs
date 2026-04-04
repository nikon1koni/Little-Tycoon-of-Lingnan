using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 单例
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

    [Header("骰子相关")]
    public DiceController diceController;
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

    [Header("调试")]
    public bool enableDebugKeys = true;

    // 游戏状态枚举
    public enum GameState
    {
        Waiting,           // 等待开始
        PlayerTurn,        // 玩家回合
        RollingDice,       // 掷骰子中
        Moving,            // 移动中
        ProcessingTile,    // 处理地块事件
        BuyingProperty,    // 购买地产
        BuildingSelection, // 建筑选购状态
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
        Debug.Log("=== 游戏开始 ===");
        InitializeGame();
        // === 关键修改1: 游戏开始时直接进入开局购买阶段 ===
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

        // 状态初始化为等待，将在开局购买后转为玩家回合
        currentState = GameState.Waiting;
        isGameStarted = true;

        UpdateUI();
        SetupButtonEvents();

        Debug.Log($"游戏初始化完成，玩家数: {players.Count}");
        Debug.Log($"当前玩家: {currentPlayer?.playerName ?? ""}");
    }

    // === 新增：开局购买阶段 ===
    IEnumerator StartInitialBuildingPhase()
    {
        // 等待一帧确保所有UI加载完成
        yield return new WaitForSeconds(0.5f);

        if (currentPlayer != null)
        {
            Debug.Log($"=== 开局阶段：{currentPlayer.playerName} 获得初始资金，请购买建筑 ===");

            // 1. 切换状态到建筑选购，阻止移动
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;

            // 2. 禁用掷骰子按钮
            SetRollDiceButtonInteractable(false);

            // 3. 显示购买界面
            if (uiManager != null)
            {
                // 创建一个虚拟的"起点商店"Tile来触发购买UI
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager 未找到，无法显示开局购买界面");
                // 如果没有UI，则自动跳过购买阶段
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === 新增：创建用于开局购买的虚拟Tile ===
    BoardTile CreateStartPurchaseTile()
    {
        GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
        BoardTile tile = tempObj.AddComponent<BoardTile>();
        tile.tileName = "建筑地块"; // 不要使用"起点商店"
        tile.tileType = BoardTile.TileType.Buildable; // 必须是可建造类型，不是起始类型
        tile.propertyPrice = 100;
        tile.isBuildable = true;
        tile.tileScale = Random.Range(1, 4);

        // 设置为当前玩家拥有，防止被误认为可购买
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
            Debug.LogWarning("未找到任何玩家，请确认 Player 标签设置");
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
            startPos.y = 0.5f;

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} 放置在起点，现金: {player.cash}");
        }
    }

    void HandlePropertyTile()
    {
        BoardTile tile = currentPlayer.currentTile;

        if (tile.ownerPlayer == null)
        {
            if (tile.tileType == BoardTile.TileType.Buildable)
            {
                currentState = GameState.BuildingSelection;
                Debug.Log($"{tile.tileName} 是可建筑地块，价格: {tile.propertyPrice} 元");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} 可供购买，价格: {tile.propertyPrice} 元");

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
            Debug.Log("掷骰子按钮事件监听已设置");
        }
        else
        {
            Debug.LogWarning("RollDiceButton 未赋值，正在UI层级中查找");

            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("自动找到并绑定掷骰子按钮");
                }
            }
        }
    }

    // ================= 游戏逻辑 =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("点击掷骰子按钮");

        // === 关键修改2: 在掷骰子前检查当前状态 ===
        if (!CanRollDice())
        {
            Debug.Log($"当前不能掷骰子，状态: {currentState}");

            // 给玩家提示
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("请先完成建筑购买！", 2f);
                }
            }
            return;
        }

        if (currentPlayer == null)
        {
            Debug.LogError("当前玩家为空");
            return;
        }

        Debug.Log($"{currentPlayer.playerName} 开始掷骰子");

        currentState = GameState.RollingDice;
        isPlayerTurn = false;

        if (diceController != null)
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
        Debug.Log($"{currentPlayer.playerName} 掷出了 {lastDiceValue} 点");

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

        UpdateUI();
        StartMovePlayer();
    }

    // === 关键修改3: 强化掷骰子条件检查 ===
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

        // === 关键修改4: 检查是否经过起点，并触发购买 ===
        CheckPassingStart();

        ProcessCurrentTile();
    }

    // === 关键修改5: 强化经过起点的处理 ===
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

        if (previousIndex > currentIndex) // 已经绕了一圈
        {
            // 1. 发放薪水
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} 经过起点，获得 {salary} 元薪水");

            if (uiManager != null)
            {
                uiManager.ShowToast($"经过起点，获得{salary}元薪水", 2f);
            }

            // 2. 进入购买状态，阻止移动
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. 显示购买界面
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // === 新增：经过起点后触发购买 ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // 短暂延迟，让玩家看到薪水信息
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 可以购买建筑了");
            // 如果没有UI，直接结束购买状态
            OnBuildingPurchaseCompleted();
        }
    }

    void ProcessCurrentTile()
    {
        if (currentPlayer == null || currentPlayer.currentTile == null)
        {
            EndMove();
            return;
        }

        currentState = GameState.ProcessingTile;

        Debug.Log($"{currentPlayer.playerName} 落在 {currentPlayer.currentTile.tileName}");

        // 特殊处理起点：落在起点也要触发购买
        if (currentPlayer.currentTile.tileType == BoardTile.TileType.Start)
        {
            // 调用BoardTile中的处理，它已修改为也会触发购买
            currentPlayer.currentTile.OnLanded(currentPlayer);
        }
        else if (currentPlayer.currentTile.tileType == BoardTile.TileType.Property ||
                 currentPlayer.currentTile.tileType == BoardTile.TileType.Railroad ||
                 currentPlayer.currentTile.tileType == BoardTile.TileType.Utility)
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
            Debug.Log($"{currentPlayer.playerName} 现金为负宣布破产");
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
            Debug.Log($"{currentPlayer.playerName} 仍在狱中，剩余 {currentPlayer.jailTurnsRemaining} 回合");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} 仍在狱中，剩余{currentPlayer.jailTurnsRemaining}回合", 2f);
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
            Debug.Log($"释放拥有的地产: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} 已破产退出游戏");

        if (uiManager != null)
        {
            uiManager.ShowToast($"{player.playerName} 已破产退出游戏", 3f);
        }
    }

    void GameOver()
    {
        currentState = GameState.GameOver;
        isGameStarted = false;

        if (players.Count == 1)
        {
            Player winner = players[0];
            Debug.Log($"=== 游戏结束！{winner.playerName} 获胜 ===");

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

    // === 关键修改6: 购买完成后的回调 ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("建筑购买完成，可以开始移动");

        // 重置状态到玩家回合
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // 更新UI，启用掷骰子按钮
        UpdateUI();
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

    // ================= 调试和控制 =================

    void Update()
    {
        if (!enableDebugKeys) return;

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
            Debug.Log($"现金: {currentPlayer.cash}");
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
            Debug.Log("=== 检查起始格子 ===");
            foreach (BoardTile tile in boardManager.allTiles)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    Debug.Log($"找到起始格子: {tile.tileName}, ID: {tile.tileID}");

                    // 检查是否被错误地标记为可建造
                    if (tile.isBuildable)
                    {
                        Debug.LogError($"错误：起始格子 {tile.tileName} 被标记为可建造！");
                    }

                    // 检查是否有建筑
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($"错误：起始格子 {tile.tileName} 上有建筑！");
                    }
                }
            }
        }
    }
}