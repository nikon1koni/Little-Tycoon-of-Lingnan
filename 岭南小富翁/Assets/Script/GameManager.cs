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

    [Header("游戏管理器引用")]
    public BoardManager boardManager;
    public UIManager uiManager;

    [Header("游戏规则")]
    public int startingCash = 1500;
    public int salaryAmount = 200;
    public int jailTurns = 3;

    [Header("压力系统")]
    public bool enablePressureSystem = true;

    private int diceRollCount = 0;          // 记录掷骰子次数
    private int pressureInterval = 1;        // 压力发生的间隔（每N次掷骰子）
    private int nextPressureAt = 1;          // 下次触发压力的次数
    public float basePressureCost = 50f;   // 基础压力成本
    public float pressureMultiplier = 1.2f;

    public int DiceRollCount => diceRollCount;
    public int CurrentRound => diceRollCount / 6;

    [Header("调试功能")]
    public bool enableDebugKeys = true;

    [Header("背景音乐")]
    public bool enableBackgroundMusic = true;
    public MusicManager musicManager;

    [Header("音效")]
    public SFXConfig sfxConfig;
    public bool enableSFX = true;

    [Header("骰子控制")]
    [Range(0f, 10f)]
    public float diceCooldownTime = 0f; // 骰子冷却时间（秒）
    private float lastDiceRollTime = -1000f; // 上次掷骰子时间（设置为很早之前）

    // 游戏状态枚举
    public enum GameState
    {
        Waiting,           // 等待开始
        PlayerTurn,        // 玩家回合
        RollingDice,       // 投掷骰子中
        Moving,            // 移动中
        ProcessingTile,    // 处理格子效果
        BuyingProperty,    // 购买地产
        BuildingSelection, // 建筑选择
        BuildingPlacement, // 建筑放置
        GameOver           // 游戏结束
    }

    void Awake()
    {
        // 设置单例模式
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
        Debug.Log("=== 岭南富翁游戏开始启动 ===");
        InitializeGame();
        // === 阶段1: 初始建筑放置阶段 ===
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

        // 设置初始游戏状态
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
            Debug.Log("背景音乐系统已启动");
        }
        else
        {
            Debug.LogWarning("MusicManager 未找到或没有音轨");
        }
    }

    void InitializeSFXSystem()
    {
        if (!enableSFX)
        {
            Debug.Log("音效已禁用");
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
                    Debug.LogWarning("SFXConfig 未找到，请在Inspector中引用或放在Resources文件夹中");
                }
            }

            Debug.Log("SFXManager 已创建");
        }
        else if (sfxConfig != null && SFXManager.Instance.config == null)
        {
            SFXManager.Instance.config = sfxConfig;
            SFXManager.Instance.ReloadClips();
        }
    }

    // === 初始建筑放置阶段 ===
    IEnumerator StartInitialBuildingPhase()
    {
        // 等待UI初始化完成
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
                // 创建临时的"起始商店"Tile来启动UI
                BoardTile startShopTile = CreateStartPurchaseTile();
                uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
            }
            else
            {
                Debug.LogWarning("UIManager 未找到，跳过建筑选择阶段");
                // 如果没有UI管理器，直接进入游戏
                OnBuildingPurchaseCompleted();
            }
        }
    }

    // === 创建临时购买Tile ===
    private BoardTile startPurchaseTileCache = null;

    BoardTile CreateStartPurchaseTile()
    {
        if (startPurchaseTileCache == null)
        {
            GameObject tempObj = new GameObject("StartPurchaseTile_Dummy");
            startPurchaseTileCache = tempObj.AddComponent<BoardTile>();
        }

        startPurchaseTileCache.tileName = "起始商店";
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

        Debug.Log($"查找组件结果: DiceController={diceController != null}, BoardManager={boardManager != null}, UIManager={uiManager != null}");
    }

    void FindAllPlayers()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        players.Clear();
        players.AddRange(allPlayers);
        players.Sort((a, b) => a.playerID.CompareTo(b.playerID));

        if (players.Count == 0)
        {
            Debug.LogWarning("场景中没有找到Player对象，请确保场景中有Player对象");
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
            Debug.LogWarning("没有找到任何格子");
        }
        else
        {
            Debug.Log($"棋盘初始化完成，共 {boardManager.allTiles.Count} 个格子");
        }
    }

    void InitializePlayerPositions()
    {
        if (players.Count == 0 || boardManager == null) return;

        BoardTile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("没有找到起始格子");
            return;
        }

        float offset = 0.3f;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            Vector3 startPos = startTile.transform.position;

            startPos.x += (i % 2 == 0 ? -offset : offset);
            startPos.z += (i / 2) * offset;

            // 检查PlayerMovement组件的高度偏移
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                startPos.y = startTile.transform.position.y + playerMovement.heightOffset;
            }
            else
            {
                startPos.y = 0.875f; // 假设的格子高度 + 玩家高度
            }

            player.transform.position = startPos;
            player.currentTile = startTile;
            player.currentTileIndex = 0;
            player.cash = startingCash;

            Debug.Log($"{player.playerName} 起始资金: {player.cash}");

            // === 初始化玩家UI显示 ===
            if (UIManager.Instance != null)
            {
                // 只更新第一个玩家的现金显示
                if (i == 0) // 第一个玩家
                {
                    UIManager.Instance.UpdateCashDisplay(player.cash);
                }
            }
            // === UI初始化完成 ===
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
                Debug.Log($"{tile.tileName} 是可建造地块: {tile.propertyPrice} 金币");

                if (uiManager != null)
                {
                    uiManager.ShowBuildingSelectionUI(tile, currentPlayer);
                }
            }
            else
            {
                currentState = GameState.BuyingProperty;
                Debug.Log($"{tile.tileName} 是可购买地产: {tile.propertyPrice} 金币");

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
                uiManager.ShowToast($"获得关联收入: {totalIncome} 金币", 2f);
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
            Debug.Log("掷骰子按钮事件已设置");
        }
        else
        {
            Debug.LogWarning("RollDiceButton 未设置，请在Inspector中引用或通过名称查找");

            GameObject buttonObj = GameObject.Find("RollDiceButton");
            if (buttonObj != null)
            {
                rollDiceButton = buttonObj.GetComponent<Button>();
                if (rollDiceButton != null)
                {
                    rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
                    Debug.Log("通过名称找到了掷骰子按钮");
                }
            }
        }
    }

    // ================= 骰子相关功能 =================

    public void OnRollDiceButtonClicked()
    {
        Debug.Log("掷骰子按钮被点击");

        // === 阶段2: 检查是否可以掷骰子 ===
        if (!CanRollDice())
        {
            Debug.Log($"当前状态无法掷骰子: {currentState}");

            // 如果是建筑选择阶段，给出提示
            if (currentState == GameState.BuildingSelection)
            {
                if (uiManager != null)
                {
                    uiManager.ShowToast("请先完成建筑选择或按ESC跳过", 2f);
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
        lastDiceRollTime = Time.time; // 记录掷骰子时间
        Debug.Log($"骰子点数: {value}");

        int previousRound = CurrentRound;
        diceRollCount++;
        Debug.Log($"骰子投掷次数: {diceRollCount}");

        // 通知回合变化给BuffSystem
        if (CurrentRound != previousRound && BuffSystem.Instance != null)
        {
            BuffSystem.Instance.OnRoundChanged();
        }

        UpdateUI();

        if (uiManager != null)
        {
            uiManager.UpdatePressureSystemUI();
        }

        // 检查骰子点数为偶数的建筑效果
        CheckDiceEvenBuildings(value);

        StartMovePlayer();
    }

    /// <summary>
    /// 检查骰子为偶数的建筑奖励
    /// </summary>
    private void CheckDiceEvenBuildings(int diceValue)
    {
        // 只在点数为2, 4, 6时触发
        if (diceValue % 2 != 0) return;

        if (currentPlayer == null) return;

        Debug.Log($"? 掷出点数 {diceValue}，检查偶数建筑奖励...");

        int totalReward = 0;
        int buildingCount = 0;

        // 遍历玩家所有地产，检查是否有偶数建筑
        foreach (BoardTile property in currentPlayer.ownedProperties)
        {
            if (property == null || property.currentBuildingData == null) continue;

            // 只有DiceEven类型的建筑才触发
            if (property.currentBuildingData.functionType == BuildingData.BuildingFunctionType.DiceEven)
            {
                int reward = property.currentBuildingData.diceEvenReward;
                currentPlayer.ReceiveCash(reward);
                totalReward += reward;
                buildingCount++;

                Debug.Log($"? {property.tileName} ({property.currentBuildingData.buildingName}) 触发奖励: {reward} 金币");

                // 播放建筑效果
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

        // 显示奖励消息
        if (totalReward > 0 && uiManager != null)
        {
            string message = $"? 偶数点数触发{buildingCount}个建筑，获得奖励 {totalReward} 金币";
            uiManager.ShowToast(message, 3f);
            Debug.Log(message);
        }
    }

    // 检查压力系统触发
    private void CheckPressureTrigger()
    {
        if (!enablePressureSystem)
            return;

        int currentRound = diceRollCount / 6;
        
        Debug.Log($"CheckPressureTrigger: diceRollCount={diceRollCount}, currentRound={currentRound}, nextPressureAt={nextPressureAt}");

        // 检查是否到达下一个压力触发回合
        if (currentRound >= nextPressureAt)
        {
            TriggerPressure(currentRound);
        }
    }

    // 触发压力系统
    private void TriggerPressure(int currentRound)
    {
        Debug.Log($"第 {currentRound} 轮压力系统触发");

        int cost = Mathf.RoundToInt(basePressureCost);

        // 准备下一次触发的压力成本
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
                $"第 {currentRound} 轮压力   支出 {cost} 金币"
            );
        }
    }

    // === 阶段3: 检查是否可以掷骰子 ===
    public bool CanRollDice()
    {
        // 检查冷却时间
        float timeSinceLastRoll = Time.time - lastDiceRollTime;
        bool cooldownFinished = timeSinceLastRoll >= diceCooldownTime;
        
        bool canRoll = isGameStarted &&
                       currentState == GameState.PlayerTurn && // 只有在玩家回合时
                       !isMoving &&
                       currentPlayer != null &&
                       !currentPlayer.isInJail &&
                       !currentPlayer.isBankrupt &&
                       cooldownFinished;

        string cooldownText = !cooldownFinished ? $"{diceCooldownTime - timeSinceLastRoll:F1}s" : "OK";
        Debug.Log($"CanRollDice: {canRoll} | State: {currentState} | isMoving: {isMoving} | Player: {currentPlayer?.playerName} | Bankrupt: {currentPlayer?.isBankrupt} | Cooldown: {cooldownText}");

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

        // 检查玩家当前是否在起点格子上(tileID == 0 或 tileType == Start)
        bool isOnStartTile = (currentPlayer.currentTile.tileID == 0 ||
                             currentPlayer.currentTile.tileType == BoardTile.TileType.Start);

        int previousIndex = (currentIndex - lastDiceValue) % boardManager.allTiles.Count;
        if (previousIndex < 0) previousIndex += boardManager.allTiles.Count;

        // 检查是否经过起点(通过索引绕回判断或刚好停在起点)
        if (!isOnStartTile && previousIndex > currentIndex)
        {
            Debug.Log($"{currentPlayer.playerName} 经过了起点");

            // 1. 发放过路费
            int salary = salaryAmount;
            currentPlayer.ReceiveCash(salary);
            Debug.Log($"{currentPlayer.playerName} 获得 {salary} 过路费");

            if (uiManager != null)
            {
                uiManager.ShowToast($"经过起点！获得{salary}金币！", 2f);
            }

            // 2. 设置状态为建筑选择
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            // 3. 触发建筑购买界面
            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
        else if (isOnStartTile)
        {
            Debug.Log($"{currentPlayer.playerName} 停在了起点格子上");

            // 即使停在起点也提供购买机会(BoardTile.OnLanded应该已处理)
            currentState = GameState.BuildingSelection;
            isPlayerTurn = false;
            SetRollDiceButtonInteractable(false);

            StartCoroutine(TriggerBuildingPurchaseAfterStart());
        }
    }

    // === 经过起点后触发建筑购买 ===
    IEnumerator TriggerBuildingPurchaseAfterStart()
    {
        // 给点时间让玩家看到提示再显示UI
        yield return new WaitForSeconds(1f);

        if (uiManager != null)
        {
            BoardTile startShopTile = CreateStartPurchaseTile();
            uiManager.ShowBuildingSelectionUI(startShopTile, currentPlayer);
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 跳过了建筑购买");
            // 如果没有UI管理器，直接进入下一轮
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
                Debug.Log($"{currentPlayer.playerName} 自动购买了 {tile.tileName}");
            }
        }
        else
        {
            Debug.Log($"{currentPlayer.playerName} 买不起 {tile.tileName}");
        }

        StartCoroutine(EndMoveAfterDelay(1f));
    }

    public void OnPropertyPurchaseComplete(bool purchased)
    {
        Debug.Log($"购买完成: {(purchased ? "已购买" : "未购买")}");
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

        // 在这里检查是否有建筑的 buildingStartRound == CurrentRound 之类的

        Debug.Log($"=== {currentPlayer.playerName} 的回合 ===");
        UpdateUI();

        // === 更新玩家UI ===
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
            Debug.Log($"{currentPlayer.playerName} 在监狱里，还剩 {currentPlayer.jailTurnsRemaining} 回合");

            if (uiManager != null)
            {
                uiManager.ShowToast($"{currentPlayer.playerName} 在监狱里，还剩{currentPlayer.jailTurnsRemaining}回合", 2f);
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
            Debug.Log($"释放地产: {property.tileName}");
        }
        player.ownedProperties.Clear();

        Debug.Log($"{player.playerName} 已破产");

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
            Debug.Log($"=== 游戏结束! {player.playerName}: {(isWinner ? "获胜": "失败")} ===");

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
                Debug.Log("=== 没有人获胜，所有人都破产了 ===");
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
            playerCashText.text = $"现金: {currentPlayer.cash}";

        if (diceResultText != null)
            diceResultText.text = $"骰子: {lastDiceValue}";

        if (currentTileText != null && currentPlayer.currentTile != null)
            currentTileText.text = $"当前位置: {currentPlayer.currentTile.tileName}";

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

    // === 阶段6: 建筑购买完成 ===
    public void OnBuildingPurchaseCompleted()
    {
        Debug.Log("建筑购买完成，进入正常回合流程");

        isMoving = false;//重置移动状态

        // 恢复正常游戏状态
        currentState = GameState.PlayerTurn;
        isPlayerTurn = true;

        // 更新UI和按钮
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
        
        // 重要：在这里检查玩家是否破产了(如果压力系统触发了破产)
        // 先恢复状态，再检查破产
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} 已破产，跳过此回合");
            return;
        }
        
        // 检查压力系统是否要触发
        CheckPressureTrigger();
        
        // 再次检查是否破产(压力系统可能导致破产)
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} 因压力系统触发破产");
            return;
        }
        
        // 正常结束回合
        EndTurn();
    }

    // ================= 调试按键 =================

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
            Debug.Log("测试：跳过回合");
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
        Debug.Log("测试：掷骰子");
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
        Debug.Log($"当前索引: {currentPlayerIndex}");
        Debug.Log($"游戏开始: {isGameStarted}");
        Debug.Log($"玩家回合: {isPlayerTurn}");
        Debug.Log($"正在移动: {isMoving}");
        Debug.Log($"骰子点数: {lastDiceValue}");

        if (currentPlayer != null)
        {
            Debug.Log($"现金: {currentPlayer.cash}");
            Debug.Log($"当前位置: {currentPlayer.currentTile?.tileName}");
            Debug.Log($"是否在监狱: {currentPlayer.isInJail}");
            Debug.Log($"监狱剩余回合: {currentPlayer.jailTurnsRemaining}");
        }
    }

    public void RestartFromGameOver()
    {
        Debug.Log("从游戏结束重新开始");
        
        currentState = GameState.PlayerTurn;
        isGameStarted = true;
        isPlayerTurn = true;
        isMoving = false;
        currentPlayerIndex = 0;
        diceRollCount = 0;
        nextPressureAt = 1;
        basePressureCost = 50f;
        
        // 重置骰子冷却
        ResetDiceCooldown();
        
        // 清除所有建筑
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
        
        // 等待后显示建筑购买面板
        StartCoroutine(DelayedShowBuildingPanelAfterRestart());
        
        Debug.Log("重新初始化完成");
    }
    
    // 重新开始后延迟显示建筑购买面板
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
            Debug.Log($"{currentPlayer.playerName} 在起点，显示建筑购买面板");
            
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

    // 清除所有建筑
    private void ClearAllBuildings()
    {
        Debug.Log("清除所有建筑数据...");
        
        if (boardManager == null || boardManager.allTiles == null)
        {
            Debug.LogWarning("BoardManager 或 allTiles 未找到");
            return;
        }
        
        foreach (BoardTile tile in boardManager.allTiles)
        {
            if (tile == null) continue;
            
            // 清除建筑数据
            tile.currentBuildingData = null;
            tile.currentBuildingType = BoardTile.BuildingType.None;
            tile.buildingLevel = 0;
            tile.ownerPlayer = null;
            
            // 清除建筑对象
            if (tile.currentBuilding != null)
            {
                Destroy(tile.currentBuilding);
                tile.currentBuilding = null;
            }
        }
        
        Debug.Log("清除所有建筑完成");
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
        // 事件面板关闭
        Debug.Log("事件面板已关闭");
        
        // 检查玩家是否破产
        if (currentPlayer != null && currentPlayer.isBankrupt)
        {
            Debug.Log($"{currentPlayer.playerName} 在事件后破产了");
            HandlePlayerBankrupt(currentPlayer);
            if (players.Count <= 1)
            {
                GameOver();
            }
            return;
        }
        
        // 恢复掷骰子按钮
        SetRollDiceButtonInteractable(true);
        
        // 如果是建筑选择状态，直接返回
        if (currentState == GameState.BuildingSelection)
        {
            Debug.Log("保持建筑选择状态");
            return;
        }
        
        // 正常结束移动
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

                    // 检查是否可建造
                    if (tile.isBuildable)
                    {
                        Debug.LogError($"警告: {tile.tileName} 不应该是可建造的");
                    }

                    // 检查是否有建筑
                    if (tile.currentBuilding != null)
                    {
                        Debug.LogError($"警告: {tile.tileName} 不应该有建筑");
                    }
                }
            }
        }
    }

    // 设置骰子滚动速度
    public void SetDiceRollSpeed(float multiplier)
    {
        if (dice3DController != null)
        {
            dice3DController.SetRollSpeedMultiplier(multiplier);
        }
        if (diceController != null)
        {
            // 旧版DiceController也需要类似的设置
        }
        Debug.Log($"GameManager: 设置骰子滚动速度为 {multiplier}x");
    }

    // 设置骰子冷却时间
    public void SetDiceCooldown(float cooldownSeconds)
    {
        diceCooldownTime = Mathf.Max(0f, cooldownSeconds);
        Debug.Log($"GameManager: 设置骰子冷却时间为 {diceCooldownTime}秒");
    }

    // 获取骰子冷却时间
    public float GetDiceCooldown()
    {
        return diceCooldownTime;
    }

    // 获取剩余冷却时间
    public float GetDiceCooldownRemaining()
    {
        float timeSinceLastRoll = Time.time - lastDiceRollTime;
        return Mathf.Max(0f, diceCooldownTime - timeSinceLastRoll);
    }

    // 重置骰子冷却
    public void ResetDiceCooldown()
    {
        lastDiceRollTime = -1000f;
        Debug.Log("GameManager: 骰子冷却已重置");
    }

    // 禁用骰子冷却
    public void DisableDiceCooldown()
    {
        diceCooldownTime = 0f;
        ResetDiceCooldown();
        Debug.Log("GameManager: 骰子冷却已禁用");
    }
}
