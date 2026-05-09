using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Diagnostics;
using static BoardTile;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 单例模式
    public static UIManager Instance;

    [Header("UI 预制体 (用于动态生成)")]
    public GameObject rollDiceButtonPrefab;
    public GameObject playerInfoPrefab;
    public GameObject propertyPanelPrefab;

    [Header("UI 文本引用")]
    public Canvas mainCanvas;
    public Text diceResultText;
    public Text currentPlayerText;
    public Text playerCashText;
    public Text currentTileText;

    [Header("UI 面板")]
    public GameObject gamePanel;
    public GameObject menuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject propertyPurchasePanel;

    [Header("UI 组件")]
    public Button rollDiceButton;
    public Text diceAnimationText;

    [Header("玩家信息UI列表")]
    public List<PlayerInfoUI> playerInfoUIs = new List<PlayerInfoUI>();

    [Header("位置")]
    public Vector2 diceButtonPosition = new Vector2(-20, -10); // 根据您的截图调整

    [Header("建筑系统UI")]
    public GameObject buildingSelectionPanel;
    public Transform buildingButtonContainer;
    public Button closeBuildingPanelButton;
    public Text selectedBuildingText;
    public Image selectedBuildingImage;
    public Text buildingPriceText;

    [Header("建筑数据")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("升级系统UI")]
    public GameObject buildingUpgradePanel;
    public Button upgradeButton;
    public Text upgradeCostText;
    public Text currentLevelText;
    public Text nextLevelText;
    public Image upgradeBuildingImage;
    public Button closeUpgradePanelButton;

    [Header("左下角Toast")]
    public GameObject persistentToastPanel; // 持久显示的Toast面板
    public Text persistentToastText;        // Toast文本组件
    public Vector2 toastPosition = new Vector2(20, 20); // 左下角位置

    [Header("玩家资金显示")]
    [SerializeField] private GameObject cashDisplayPanel; // 整个面板，方便开关
    [SerializeField] private TextMeshProUGUI cashText;    // 显示具体金额的文本

    [Header("回合公告UI")]
    public GameObject turnAnnouncePanel;   // 面板

    public TextMeshProUGUI turnAnnounceText;           // 文本
    public float announceDuration = 2.5f;  // 显示时长

    public TextMeshProUGUI CashText => cashText;

    // 新增状态变量
    private bool isBuildingSelected = false; // 是否已选择建筑
    private GameObject activePersistentToast; // 当前活动的持久Toast

    // 当前选中的用于升级的地块
    private BoardTile upgradeSelectedTile = null;
    private Player upgradeSelectedPlayer = null;

    // 当前选中的建筑和地块
    private BuildingData selectedBuildingData = null;
    private BoardTile selectedBoardTile = null;
    private Player currentBuildingPlayer = null;

    // 地块高亮相关
    private Dictionary<BoardTile, Color> originalTileColors = new Dictionary<BoardTile, Color>();
    private List<BoardTile> highlightableTiles = new List<BoardTile>();

    // 当前显示的UI类型
    private UIType currentUIType = UIType.Game;

    // UI 类型枚举
    public enum UIType
    {
        Menu,
        Game,
        Pause,
        GameOver
    }

    // 玩家信息UI结构
    [System.Serializable]
    public class PlayerInfoUI
    {
        public GameObject uiObject;
        public Text playerNameText;
        public Text cashText;
        public Image playerColorImage;
        public Player player;
    }

    void Update()
    {
        // 检测ESC键取消建筑选择
        if (isBuildingSelected && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelBuildingSelection();
        }
    }

    void Awake()
    {
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
        UnityEngine.Debug.Log("=== 初始化UI ===");
        InitializeUI();

        // 【修改点2】确保游戏开始时，建筑选择面板是隐藏的
        // 即使您在编辑器中已取消激活，这里再加一层保障
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            UnityEngine.Debug.Log("UIManager: 已确保 BuildingSelectionPanel 初始为隐藏状态。");
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: buildingSelectionPanel 未在Inspector中赋值，请拖拽场景中的面板对象。");
        }
    }

    void InitializeUI()
    {
        EnsureCanvasExists();
        CreateRollDiceButton();
        SwitchToGameUI();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (propertyPurchasePanel != null) propertyPurchasePanel.SetActive(false);

        // === 新增：初始化资金显示 ===
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            UpdateCashDisplay(GameManager.Instance.currentPlayer.cash);
        }
        // === 新增结束 ===

        UnityEngine.Debug.Log("UI初始化完成");
    }

    // === 新增：更新独立的资金显示面板 ===
    public void UpdateCashDisplay(int cashAmount)
    {
        // 检查 cashText 引用是否有效
        if (cashText != null)
        {
            // 更新文本内容
            cashText.text = $"{cashAmount}";
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: cashText 引用为空，资金显示将不会更新。请检查Inspector中是否为'Cash Text'字段赋值。");
        }
    }
    // === 新增结束 ===

    // === 核心修改：显示建筑选择UI（整合版，包含建筑规模和点击放置功能）===
    public void ShowBuildingSelectionUI(BoardTile buildableTile, Player player)
    {
        // 修改点1：保存当前上下文，但不立即高亮
        selectedBoardTile = buildableTile;
        currentBuildingPlayer = player;

        UnityEngine.Debug.Log($"UIManager: 显示建筑选择面板，地块规模: {buildableTile.tileScale}");

        // 1. 关闭其他可能冲突的UI
        HidePropertyPurchasePanel();

        // 2. 激活面板
        if (buildingSelectionPanel == null)
        {
            UnityEngine.Debug.LogError("UIManager: BuildingSelectionPanel 引用为空！");
            return;
        }

        buildingSelectionPanel.SetActive(true);
        buildingSelectionPanel.transform.SetAsLastSibling();

        // 3. 更新UI信息
        Text tileNameText = buildingSelectionPanel.transform.Find("TileName")?.GetComponent<Text>();
        Text priceText = buildingSelectionPanel.transform.Find("Price")?.GetComponent<Text>();

        if (tileNameText != null)
            tileNameText.text = buildableTile.tileName;
        if (priceText != null)
            priceText.text = $"地块价格: {buildableTile.propertyPrice} 元";

        // 4. 清除之前的建筑按钮
        ClearBuildingButtons();

        // 5. 根据地块规模过滤可建造的建筑
        List<BuildingData> compatibleBuildings = FilterBuildingsByScale(buildableTile.tileScale);

        // 6. 创建建筑选择按钮
        CreateBuildingButtons(compatibleBuildings);

        // 7. 设置关闭按钮事件
        Button closeButton = buildingSelectionPanel.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                UnityEngine.Debug.Log("UIManager: 关闭按钮被点击");
                HideBuildingSelectionUI();
            });
        }

        // 不在这里高亮，而是在玩家选择建筑后高亮

        // 8. 禁用骰子按钮
        SetRollDiceButtonInteractable(false);
    }

    // === 核心修改：隐藏建筑选择UI（不销毁，只是隐藏）===
    public void HideBuildingSelectionUI(bool keepButtons = false)
    {
        UnityEngine.Debug.Log($"隐藏建筑选择UI，keepButtons={keepButtons}");

        // 清除相关UI元素
        ClearTileHighlights();
        HidePersistentToast();

        // 隐藏面板
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
        }

        // 根据参数决定是否隐藏按钮容器
        if (buildingButtonContainer != null)
        {
            if (keepButtons)
            {
                // 只隐藏容器，不销毁按钮
                buildingButtonContainer.gameObject.SetActive(false);
            }
            else
            {
                // 完全关闭时清除所有按钮
                ClearBuildingButtons();
            }
        }

        // 重置状态
        if (!keepButtons)
        {
            selectedBuildingData = null;
            currentBuildingPlayer = null;
            isBuildingSelected = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBuildingPurchaseCompleted();
            }
        }

        // 恢复骰子按钮交互
        SetRollDiceButtonInteractable(true);
    }

    // 根据规模过滤建筑
    private List<BuildingData> FilterBuildingsByScale(int tileScale)
    {
        List<BuildingData> result = new List<BuildingData>();

        foreach (BuildingData building in availableBuildings)
        {
            if (tileScale >= building.minTileScale && tileScale <= building.maxTileScale)
            {
                result.Add(building);
            }
        }

        return result;
    }

    // 清除建筑按钮
    private void ClearBuildingButtons()
    {
        if (buildingButtonContainer == null) return;

        foreach (Transform child in buildingButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // 取消建筑选择
    private void OnCancelBuildingSelection()
    {
        UnityEngine.Debug.Log("按ESC取消建筑选择，返回建筑面板");

        // 1. 清除Toast和高亮
        HidePersistentToast();
        ClearTileHighlights();

        // 2. 重新显示建筑选择面板
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(true);
        }

        // 3. 重新显示建筑按钮容器
        if (buildingButtonContainer != null)
        {
            buildingButtonContainer.gameObject.SetActive(true);
        }

        // 4. 重置选择状态但保留按钮
        selectedBuildingData = null;
        isBuildingSelected = false;
        UnityEngine.Debug.Log("已返回建筑选择面板，建筑按钮已重新显示");
    }

    // 创建建筑按钮
    private void CreateBuildingButtons(List<BuildingData> buildings)
    {
        if (buildingButtonContainer == null)
        {
            UnityEngine.Debug.LogWarning("buildingButtonContainer 为空，无法创建建筑按钮");
            return;
        }

        foreach (BuildingData building in buildings)
        {
            // 创建按钮对象
            GameObject buttonObj = new GameObject($"Btn_{building.buildingName}");
            buttonObj.transform.SetParent(buildingButtonContainer);

            // 添加UI组件
            Image image = buttonObj.AddComponent<Image>();
            Button button = buttonObj.AddComponent<Button>();

            // 设置按钮图片
            if (building.buildingIcon != null)
            {
                image.sprite = building.buildingIcon;
            }
            else
            {
                // 如果没有图标，使用默认颜色
                image.color = Color.gray;
            }

            // 设置按钮大小
            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);

            // 添加建筑名称文本
            GameObject textObj = new GameObject("BuildingName");
            textObj.transform.SetParent(buttonObj.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = $"{building.buildingName}\n{building.purchasePrice}金币";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // 绑定点击事件
            BuildingData currentBuilding = building; // 避免闭包问题
            button.onClick.AddListener(() => OnBuildingSelected(currentBuilding));
        }
    }

    // 建筑被选中时的处理
    private void OnBuildingSelected(BuildingData building)
    {
        UnityEngine.Debug.Log($"选中建筑: {building.buildingName}, 价格: {building.purchasePrice}");

        selectedBuildingData = building;
        isBuildingSelected = true;

        // 【修改点1】隐藏建筑选择面板
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
        }

        // 【修改点2】同时隐藏存放所有建筑按钮的容器！
        if (buildingButtonContainer != null && buildingButtonContainer.gameObject.activeSelf)
        {
            buildingButtonContainer.gameObject.SetActive(false);
            UnityEngine.Debug.Log("已隐藏建筑按钮容器。");
        }

        // 3. 在左下角显示持久Toast
        ShowPersistentToast($"已选择: {building.buildingName}\n点击地图上的高亮地块放置，或按ESC取消。");

        // 4. 高亮可放置的地块
        HighlightPlaceableTiles(currentBuildingPlayer, (int)building.requiredScale);

        UnityEngine.Debug.Log("已进入建筑放置模式。");
    }

    private void ShowPersistentToast(string message)
    {
        // 清除现有的Toast
        HidePersistentToast();

        // 创建或获取Toast面板
        if (persistentToastPanel != null)
        {
            activePersistentToast = Instantiate(persistentToastPanel, mainCanvas.transform);
            activePersistentToast.name = "PersistentToast";

            // 获取文本组件
            Text toastText = activePersistentToast.GetComponentInChildren<Text>();
            if (toastText != null)
            {
                toastText.text = message;
            }

            // 设置位置（左下角）
            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            activePersistentToast.SetActive(true);
        }
        else
        {
            // 如果没有预制体，动态创建
            activePersistentToast = new GameObject("PersistentToast");
            activePersistentToast.transform.SetParent(mainCanvas.transform);

            Image bg = activePersistentToast.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            GameObject textObj = new GameObject("ToastText");
            textObj.transform.SetParent(activePersistentToast.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            // 设置尺寸和位置
            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            // 文本填充父对象
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 0);
            textRt.offsetMax = Vector2.zero;
        }
    }

    private void HidePersistentToast()
    {
        if (activePersistentToast != null)
        {
            Destroy(activePersistentToast);
            activePersistentToast = null;
        }
    }

    private void HighlightPlaceableTiles(Player player, int requiredScale)
    {
        ClearTileHighlights();

        if (BoardManager.Instance == null)
        {
            UnityEngine.Debug.LogWarning("BoardManager.Instance 为空，无法高亮地块");
            return;
        }

        UnityEngine.Debug.Log($"开始高亮可放置地块，需求规模: {requiredScale}");
        UnityEngine.Debug.Log($"玩家: {player.playerName}");

        int highlightCount = 0;

        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            // 记录地块信息
            UnityEngine.Debug.Log($"检查地块: {tile.tileName}, 类型: {tile.tileType}, 规模: {tile.tileScale}, 所有者: {tile.ownerPlayer?.playerName ?? "无主"}");

            // 检查地块是否可放置建筑
            if (IsTilePlaceable(tile, player, requiredScale))
            {
                // 额外检查：确保不是起始格子
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    UnityEngine.Debug.LogError($"严重错误：{tile.tileName} 是起始格子，不应该被高亮！");
                    continue;
                }

                // 保存原始颜色
                MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    originalTileColors[tile] = renderer.material.color;
                    renderer.material.color = Color.green;
                    highlightableTiles.Add(tile);
                    highlightCount++;

                    // 添加点击检测组件
                    AddTileClickHandler(tile);

                    UnityEngine.Debug.Log($" 高亮地块: {tile.tileName}, 规模: {tile.tileScale}, 类型: {tile.tileType}");
                }
            }
        }

        UnityEngine.Debug.Log($" 高亮了 {highlightCount} 个可放置地块");

        if (highlightCount == 0)
        {
            ShowToast("没有可放置的地块，请检查地块规模和所有权", 2f);
        }
    }

    public bool IsTileHighlighted(BoardTile tile)
    {
        return highlightableTiles.Contains(tile);
    }

    // 检查地块是否可放置建筑
    private bool IsTilePlaceable(BoardTile tile, Player player, int requiredScale)
    {
        bool isPlaceable = false;

        UnityEngine.Debug.Log($"=== 详细检查地块可放置性 ===");
        UnityEngine.Debug.Log($"检查地块: {tile.tileName}");
        UnityEngine.Debug.Log($"地块类型: {tile.tileType}");
        UnityEngine.Debug.Log($"是否为可建造地块: {tile.tileType == TileType.Buildable || tile.tileType == TileType.BuildingSite}");
        UnityEngine.Debug.Log($"地块可建造标记: {tile.isBuildable}");
        UnityEngine.Debug.Log($"地块规模: {tile.tileScale}, 所需规模: {requiredScale}, 是否足够: {tile.tileScale >= requiredScale}");
        UnityEngine.Debug.Log($"地块当前是否有建筑: {tile.currentBuilding != null}");
        UnityEngine.Debug.Log($"地块所有者: {tile.ownerPlayer?.playerName ?? "null"}");
        UnityEngine.Debug.Log($"当前玩家: {player.playerName}");
        UnityEngine.Debug.Log($"地块所有者是否为空或为当前玩家: {tile.ownerPlayer == null || tile.ownerPlayer == player}");

        // 原有检查逻辑...
        isPlaceable = (tile.tileType == BoardTile.TileType.Buildable ||
                      tile.tileType == BoardTile.TileType.BuildingSite) &&
                     tile.isBuildable &&
                     tile.currentBuilding == null &&
                     tile.tileScale >= requiredScale &&
                     (tile.ownerPlayer == null || tile.ownerPlayer == player);

        UnityEngine.Debug.Log($"最终可放置结果: {isPlaceable}");
        return isPlaceable;
    }

    // 为地块添加点击处理器
    private void AddTileClickHandler(BoardTile tile)
    {
        UnityEngine.Debug.Log($"UIManager: 为地块 {tile.tileName} 添加点击处理器");

        // 添加新的事件触发器
        EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();
        UnityEngine.Debug.Log($"地块 {tile.tileName} 已添加 EventTrigger: {trigger != null}");

        // 创建点击事件
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => {
            UnityEngine.Debug.Log($"点击事件触发: 地块 {tile.tileName} 被点击");
            OnTileClickedForPlacement(tile);
        });

        trigger.triggers.Add(entry);
    }

    // 地块被点击（用于放置建筑）
    public void OnTileClickedForPlacement(BoardTile tile)
    {
        UnityEngine.Debug.Log("=== 开始处理地块点击 ===");

        // 1. 基本信息调试
        UnityEngine.Debug.Log($"选中的地块: {tile.tileName}, ID: {tile.tileID}");
        UnityEngine.Debug.Log($"当前选中的建筑: {selectedBuildingData?.buildingName ?? "无"}");
        UnityEngine.Debug.Log($"当前玩家: {currentBuildingPlayer?.playerName ?? "无"}");
        UnityEngine.Debug.Log($"选中的建筑价格: {selectedBuildingData?.purchasePrice ?? 0} 玩家资金: {currentBuildingPlayer?.cash ?? 0}");

        if (selectedBuildingData == null || currentBuildingPlayer == null)
        {
            UnityEngine.Debug.LogWarning("未选择建筑或玩家不存在");
            UnityEngine.Debug.Log($"selectedBuildingData: {selectedBuildingData}, currentBuildingPlayer: {currentBuildingPlayer}");
            return;
        }

        UnityEngine.Debug.Log($"尝试在 {tile.tileName} 上放置 {selectedBuildingData.buildingName}");

        // 2. 资金检查的详细调试
        if (currentBuildingPlayer.cash < selectedBuildingData.purchasePrice)
        {
            UnityEngine.Debug.LogError($"资金不足！需要: {selectedBuildingData.purchasePrice}, 拥有: {currentBuildingPlayer.cash}");
            ShowToast("资金不足，无法购买建筑！", 2f);
            return;
        }
        else
        {
            UnityEngine.Debug.Log($"✓ 资金检查通过: 玩家有 {currentBuildingPlayer.cash}，建筑价格 {selectedBuildingData.purchasePrice}");
        }

        // 3. 地块可放置性详细调试
        UnityEngine.Debug.Log($"检查地块可放置性: {tile.tileName}");
        UnityEngine.Debug.Log($"建筑所需规模: {selectedBuildingData.requiredScale} (int: {(int)selectedBuildingData.requiredScale})");
        UnityEngine.Debug.Log($"地块规模: {tile.tileScale}");

        if (!IsTilePlaceable(tile, currentBuildingPlayer, (int)selectedBuildingData.requiredScale))
        {
            UnityEngine.Debug.LogError($"地块不可放置！详细检查结果:");

            // 在 IsTilePlaceable 方法内部也需要添加调试，临时可以这样：
            UnityEngine.Debug.Log($"- 地块类型: {tile.tileType}");
            UnityEngine.Debug.Log($"- 地块是否可建造: {tile.isBuildable}");
            UnityEngine.Debug.Log($"- 地块当前建筑: {tile.currentBuilding != null}");
            UnityEngine.Debug.Log($"- 地块所有者: {tile.ownerPlayer?.playerName ?? "无"}");
            UnityEngine.Debug.Log($"- 规模是否足够: {tile.tileScale >= (int)selectedBuildingData.requiredScale}");

            ShowToast("此地块无法放置该建筑！", 2f);
            return;
        }
        else
        {
            UnityEngine.Debug.Log("✓ 地块可放置性检查通过");
        }

        // 4. 执行购买和放置
        UnityEngine.Debug.Log("开始执行 PurchaseAndPlaceBuilding 方法...");
        if (PurchaseAndPlaceBuilding(tile, selectedBuildingData, currentBuildingPlayer))
        {
            UnityEngine.Debug.Log($"建筑 {selectedBuildingData.buildingName} 放置成功！");

            // 清除高亮
            ClearTileHighlights();

            // 隐藏建筑选择面板
            HideBuildingSelectionUI();

            // 验证放置结果
            UnityEngine.Debug.Log($"放置后验证: 地块 {tile.tileName} 的建筑 = {tile.currentBuilding?.name ?? "null"}");
            UnityEngine.Debug.Log($"放置后验证: 地块建筑等级 = {tile.buildingLevel}");
        }
        else
        {
            UnityEngine.Debug.LogError("PurchaseAndPlaceBuilding 方法返回 false，放置失败！");
        }
    }

    // 购买并放置建筑
    private bool PurchaseAndPlaceBuilding(BoardTile tile, BuildingData buildingData, Player player)
    {
        UnityEngine.Debug.Log("=== 执行 PurchaseAndPlaceBuilding ===");

        // 1. 扣款
        int purchasePrice = buildingData.purchasePrice;
        UnityEngine.Debug.Log($"扣款前玩家资金: {player.cash}, 扣款金额: {purchasePrice}");

        if (player.PayCash(purchasePrice))
        {
            UnityEngine.Debug.Log($"扣款成功，扣款后玩家资金: {player.cash}");
        }
        else
        {
            UnityEngine.Debug.LogError("扣款失败！");
            return false;
        }

        // 2. 设置地块所有者
        tile.ownerPlayer = player;
        UnityEngine.Debug.Log($"设置地块所有者为: {player.playerName}");

        // 3. 设置建筑数据
        UnityEngine.Debug.Log($"为地块设置建筑数据: {buildingData.buildingName}");
        tile.SetBuildingData(buildingData, 1);

        // 4. 验证设置结果
        UnityEngine.Debug.Log($"验证: 地块当前建筑数据 = {tile.currentBuildingData?.buildingName ?? "null"}");
        UnityEngine.Debug.Log($"验证: 地块建筑类型 = {tile.currentBuildingType}");
        UnityEngine.Debug.Log($"验证: 地块建筑等级 = {tile.buildingLevel}");

        // 5. 更新地块类型
        tile.tileType = BoardTile.TileType.BuildingSite;
        UnityEngine.Debug.Log($"更新地块类型为: BuildingSite");

        // 6. 添加建筑模型
        if (buildingData.buildingPrefab != null)
        {
            UnityEngine.Debug.Log($"实例化建筑预制体: {buildingData.buildingPrefab.name}");
            GameObject buildingObj = Instantiate(buildingData.buildingPrefab, tile.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            buildingObj.transform.SetParent(tile.transform);
            tile.currentBuilding = buildingObj;
            UnityEngine.Debug.Log($"建筑实例化成功: {buildingObj.name}");
        }
        else
        {
            UnityEngine.Debug.LogWarning("建筑预制体为空，将不生成3D模型");
        }

        return true;
    }

    // 清除地块高亮
    private void ClearTileHighlights()
    {
        foreach (BoardTile tile in highlightableTiles)
        {
            MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                // 特殊处理起始格子
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    // 起始格子恢复为起始颜色
                    // renderer.material.color = new Color(0f, 0.8f, 0f);
                }
                else
                {
                    // 其他格子调用 UpdateTileVisual 恢复正确颜色
                    tile.UpdateTileVisual();
                }

                // 移除事件触发器
                EventTrigger trigger = tile.GetComponent<EventTrigger>();
                if (trigger != null)
                {
                    // 可以在这里移除触发器，但为了安全，我们不清除
                }
            }
        }

        originalTileColors.Clear();
        highlightableTiles.Clear();

        UnityEngine.Debug.Log("已清除所有地块高亮，起始格子恢复为起始颜色");
    }

    // 辅助方法：从BuildingData获取BuildingType
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        // 这里需要根据建筑名称或类型进行映射
        // 这是一个简化版本，实际应根据游戏设计调整
        if (data.buildingName.Contains("小"))
            return BoardTile.BuildingType.SmallHouse;
        else if (data.buildingName.Contains("中"))
            return BoardTile.BuildingType.MediumHouse;
        else if (data.buildingName.Contains("大"))
            return BoardTile.BuildingType.LargeHouse;
        else
            return BoardTile.BuildingType.None;
    }

    // ================= 以下为原有代码，保持不变 =================

    void EnsureCanvasExists()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                UnityEngine.Debug.LogWarning("没有找到Canvas，正在创建...");
                CreateCanvas();
            }
        }
    }

    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        UnityEngine.Debug.Log("已创建Canvas");
    }

    void CreateRollDiceButton()
    {
        if (rollDiceButtonPrefab == null)
        {
            UnityEngine.Debug.LogWarning("掷骰子按钮预制体未设置");
            return;
        }

        GameObject buttonObj = Instantiate(rollDiceButtonPrefab, mainCanvas.transform);
        buttonObj.name = "Btn_RollDice";

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = diceButtonPosition;

        rollDiceButton = buttonObj.GetComponent<Button>();
        if (rollDiceButton != null)
        {
            UnityEngine.Debug.Log("掷骰子按钮创建成功");
        }
    }

    // 注意：这个方法是给按钮的OnClick事件使用的
    public void OnRollDiceButtonClicked()
    {
        UnityEngine.Debug.Log("UIManager: 点击掷骰子按钮");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRollDiceButtonClicked();
        }
        else
        {
            UnityEngine.Debug.LogError("GameManager.Instance 为空，无法处理掷骰子");
        }
    }

    public void UpdateDiceResult(int value)
    {
        if (diceResultText != null)
        {
            diceResultText.text = value.ToString();
        }

        if (diceAnimationText != null)
        {
            diceAnimationText.text = value.ToString();
        }
    }

    public void UpdateCurrentPlayerInfo(Player player)
    {
        if (player == null) return;

        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"当前玩家: {player.playerName}";
        }

        if (playerCashText != null)
        {
            playerCashText.text = $"现金: {player.cash} 元";
        }

        if (currentTileText != null && player.currentTile != null)
        {
            currentTileText.text = $"位置: {player.currentTile.tileName}";
        }

        // === 新增：同时更新独立资金显示面板 ===
        UpdateCashDisplay(player.cash);
        // === 新增结束 ===
    }

    public void SwitchUI(UIType uiType)
    {
        currentUIType = uiType;

        if (gamePanel != null) gamePanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        switch (uiType)
        {
            case UIType.Menu:
                if (menuPanel != null) menuPanel.SetActive(true);
                break;
            case UIType.Game:
                if (gamePanel != null) gamePanel.SetActive(true);
                break;
            case UIType.Pause:
                if (pausePanel != null) pausePanel.SetActive(true);
                break;
            case UIType.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                break;
        }

        UnityEngine.Debug.Log($"切换到 {uiType} 界面");
    }

    public void SwitchToMenuUI() => SwitchUI(UIType.Menu);
    public void SwitchToGameUI() => SwitchUI(UIType.Game);
    public void SwitchToPauseUI() => SwitchUI(UIType.Pause);
    public void SwitchToGameOverUI() => SwitchUI(UIType.GameOver);

    public void ShowPropertyPurchasePanel(BoardTile property, Player player)
    {
        if (propertyPurchasePanel == null)
        {
            UnityEngine.Debug.LogWarning("地产购买面板预制体未设置");
            return;
        }

        if (!propertyPurchasePanel.activeSelf)
        {
            propertyPurchasePanel.SetActive(true);
        }

        Text propertyNameText = propertyPurchasePanel.transform.Find("PropertyName")?.GetComponent<Text>();
        Text priceText = propertyPurchasePanel.transform.Find("Price")?.GetComponent<Text>();
        Button buyButton = propertyPurchasePanel.transform.Find("BuyButton")?.GetComponent<Button>();
        Button cancelButton = propertyPurchasePanel.transform.Find("CancelButton")?.GetComponent<Button>();

        if (propertyNameText != null)
            propertyNameText.text = property.tileName;

        if (priceText != null)
            priceText.text = $"价格: {property.propertyPrice} 元";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                if (player.BuyProperty(property))
                {
                    HidePropertyPurchasePanel();
                }
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HidePropertyPurchasePanel);
        }
    }

    public void HidePropertyPurchasePanel()
    {
        if (propertyPurchasePanel != null)
        {
            propertyPurchasePanel.SetActive(false);
        }
    }

    public void ShowGameOverPanel(string Name)
    {
        SwitchToGameOverUI();

        Text winnerText = gameOverPanel.transform.Find("WinnerText")?.GetComponent<Text>();
        if (winnerText != null)
        {
            winnerText.text = $"{Name} 失败";
        }
    }

    public void SetRollDiceButtonInteractable(bool interactable)
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = interactable;
        }
    }

    public void UpdateRollDiceButtonText(string text)
    {
        if (rollDiceButton != null)
        {
            Text buttonText = rollDiceButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
    }

    public void CreatePlayerInfoUI(Player player)
    {
        if (playerInfoPrefab == null)
        {
            UnityEngine.Debug.LogWarning("玩家信息UI预制体未设置");
            return;
        }

        GameObject playerInfoObj = Instantiate(playerInfoPrefab, mainCanvas.transform);
        playerInfoObj.name = $"PlayerInfo_{player.playerName}";

        Text nameText = playerInfoObj.transform.Find("PlayerName")?.GetComponent<Text>();
        Text cashText = playerInfoObj.transform.Find("Cash")?.GetComponent<Text>();
        Image colorImage = playerInfoObj.transform.Find("PlayerColor")?.GetComponent<Image>();

        PlayerInfoUI infoUI = new PlayerInfoUI
        {
            uiObject = playerInfoObj,
            playerNameText = nameText,
            cashText = cashText,
            playerColorImage = colorImage,
            player = player
        };

        playerInfoUIs.Add(infoUI);
        UpdatePlayerInfoUI(infoUI);

        RectTransform rt = playerInfoObj.GetComponent<RectTransform>();
        int index = playerInfoUIs.Count - 1;
        rt.anchoredPosition = new Vector2(200, -50 - index * 80);
    }

    public void UpdatePlayerInfoUI(PlayerInfoUI infoUI)
    {
        if (infoUI.playerNameText != null)
            infoUI.playerNameText.text = infoUI.player.playerName;

        if (infoUI.cashText != null)
            infoUI.cashText.text = $"{infoUI.player.cash} 元";

        if (infoUI.playerColorImage != null)
            infoUI.playerColorImage.color = infoUI.player.playerColor;
    }

    public void UpdateAllPlayerInfo()
    {
        foreach (var infoUI in playerInfoUIs)
        {
            UpdatePlayerInfoUI(infoUI);
        }
    }

    public void ShowToast(string message, float duration = 2f)
    {
        GameObject toastObj = new GameObject("ToastMessage");
        toastObj.transform.SetParent(mainCanvas.transform);

        Text text = toastObj.AddComponent<Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform rt = toastObj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(500, 50);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toastObj.transform);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        text.transform.SetAsLastSibling();
        Destroy(toastObj, duration);
    }

    public void ShowTurnAnnouncement(string msg)
    {
        if (turnAnnouncePanel == null || turnAnnounceText == null)
            return;

        turnAnnouncePanel.SetActive(true);
        turnAnnounceText.text = msg;

        CancelInvoke(nameof(HideTurnAnnouncement));
        Invoke(nameof(HideTurnAnnouncement), announceDuration);
    }
    private void HideTurnAnnouncement()
    {
        if (turnAnnouncePanel != null)
            turnAnnouncePanel.SetActive(false);
    }

    // 显示建筑升级面板
    public void ShowBuildingUpgradeUI(BoardTile tile, Player player)
    {
        if (buildingUpgradePanel == null)
        {
            UnityEngine.Debug.LogWarning("升级面板未设置");
            return;
        }

        upgradeSelectedTile = tile;
        upgradeSelectedPlayer = player;

        // 显示面板
        buildingUpgradePanel.SetActive(true);
        buildingUpgradePanel.transform.SetAsLastSibling();

        // 更新UI信息
        UpdateUpgradePanelInfo();

        // 绑定按钮事件
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }

        if (closeUpgradePanelButton != null)
        {
            closeUpgradePanelButton.onClick.RemoveAllListeners();
            closeUpgradePanelButton.onClick.AddListener(HideBuildingUpgradeUI);
        }

        // 禁用骰子按钮
        SetRollDiceButtonInteractable(false);
    }

    // 更新升级面板信息
    private void UpdateUpgradePanelInfo()
    {
        if (upgradeSelectedTile == null || upgradeSelectedPlayer == null) return;

        if (upgradeCostText != null)
        {
            int upgradeCost = upgradeSelectedTile.GetUpgradeCost();
            upgradeCostText.text = $"升级费用: {upgradeCost}金币";

            if (upgradeSelectedPlayer.cash < upgradeCost)
            {
                upgradeCostText.color = Color.red;
            }
            else
            {
                upgradeCostText.color = Color.black;
            }
        }

        if (currentLevelText != null)
        {
            currentLevelText.text = $"当前等级: {upgradeSelectedTile.buildingLevel}";
        }

        if (nextLevelText != null)
        {
            int nextLevel = upgradeSelectedTile.buildingLevel + 1;
            nextLevelText.text = $"升级后: {nextLevel}级";
        }

        // 显示当前建筑功能
        if (upgradeSelectedTile.currentBuildingData != null)
        {
            // 获取建筑数据
            BuildingData buildingData = upgradeSelectedTile.currentBuildingData;

            // 显示功能描述
            string functionDesc = GetBuildingFunctionDescription(buildingData, upgradeSelectedTile.buildingLevel);
            ShowToast($"当前功能: {functionDesc}", 3f);

            // 显示下一级功能
            if (buildingData.nextLevelBuilding != null)
            {
                string nextFunctionDesc = GetBuildingFunctionDescription(
                    buildingData.nextLevelBuilding,
                    upgradeSelectedTile.buildingLevel + 1);
                UnityEngine.Debug.Log($"升级后功能: {nextFunctionDesc}");
            }
        }

        // 检查规模限制
        BuildingData nextBuilding = upgradeSelectedTile.GetNextUpgradeBuilding();
        if (nextBuilding != null)
        {
            if (!upgradeSelectedTile.CheckScaleForUpgrade(nextBuilding.requiredScale))
            {
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = false;
                    ShowToast($"地块规模不足，需要规模{(int)nextBuilding.requiredScale}以上", 2f);
                }
            }
            else
            {
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = upgradeSelectedTile.CanUpgradeBuilding(upgradeSelectedPlayer);
                }
            }
        }
    }

    // 获取建筑功能描述
    private string GetBuildingFunctionDescription(BuildingData buildingData, int level)
    {
        if (buildingData == null) return "未知功能";

        switch (buildingData.functionType)
        {
            case BuildingData.BuildingFunctionType.Income:
                int income = buildingData.GetIncomeAmount(level);
                return $"每回合收入: {income}金币";

            case BuildingData.BuildingFunctionType.Buff:
                float buffValue = buildingData.GetBuffValue(level);
                string buffName = GetBuffEffectName(buildingData.buffEffect);
                if (buildingData.buffDuration > 0)
                {
                    return $"{buffName}: +{buffValue * 100}% (持续{buildingData.buffDuration}秒)";
                }
                else
                {
                    return $"{buffName}: +{buffValue * 100}% (永久)";
                }

            case BuildingData.BuildingFunctionType.Mixed:
                income = buildingData.GetIncomeAmount(level);
                buffValue = buildingData.GetBuffValue(level);
                buffName = GetBuffEffectName(buildingData.buffEffect);
                return $"收入: {income}金币 + {buffName}: +{buffValue * 100}%";

            default:
                return "无功能";
        }
    }

    // 获取buff效果名称
    private string GetBuffEffectName(BuildingData.BuffEffect effect)
    {
        switch (effect)
        {
            case BuildingData.BuffEffect.MoveSpeedBoost: return "移动速度";
            case BuildingData.BuffEffect.DiceBoost: return "骰子加成";
            case BuildingData.BuffEffect.IncomeMultiplier: return "收入倍率";
            case BuildingData.BuffEffect.DefenseBoost: return "防御加成";
            case BuildingData.BuffEffect.LuckBoost: return "幸运加成";
            case BuildingData.BuffEffect.AllIncomeBoost: return "全收入加成";
            default: return "未知效果";
        }
    }

    // 升级按钮点击
    private void OnUpgradeButtonClicked()
    {
        if (upgradeSelectedTile == null || upgradeSelectedPlayer == null) return;

        if (upgradeSelectedTile.UpgradeBuilding(upgradeSelectedPlayer))
        {
            ShowToast("升级成功！", 2f);

            // 更新UI
            UpdateUpgradePanelInfo();
            UpdateCurrentPlayerInfo(upgradeSelectedPlayer);

            // 如果是当前玩家，更新主UI
            if (GameManager.Instance != null && GameManager.Instance.currentPlayer == upgradeSelectedPlayer)
            {
                GameManager.Instance.UpdateUI();
            }

            // 如果不能再升级，自动关闭面板
            if (!upgradeSelectedTile.CanUpgradeBuilding(upgradeSelectedPlayer))
            {
                HideBuildingUpgradeUI();
            }
        }
        else
        {
            ShowToast("升级失败，请检查条件", 2f);
        }
    }

    // 隐藏升级面板
    public void HideBuildingUpgradeUI()
    {
        if (buildingUpgradePanel != null)
        {
            buildingUpgradePanel.SetActive(false);
        }

        upgradeSelectedTile = null;
        upgradeSelectedPlayer = null;

        // 恢复骰子按钮
        SetRollDiceButtonInteractable(true);
    }

    void OnDestroy()
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.RemoveAllListeners();
        }
    }
}