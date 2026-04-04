using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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
        Debug.Log("=== 初始化UI ===");
        InitializeUI();

        // 【修改点2】确保游戏开始时，建筑选择面板是隐藏的
        // 即使您在编辑器中已取消激活，这里再加一层保障
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            Debug.Log("UIManager: 已确保 BuildingSelectionPanel 初始为隐藏状态。");
        }
        else
        {
            Debug.LogWarning("UIManager: buildingSelectionPanel 未在Inspector中赋值，请拖拽场景中的面板对象。");
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

        Debug.Log("UI初始化完成");
    }

    // === 核心修改：显示建筑选择UI（整合版，包含建筑规模和点击放置功能）===
    public void ShowBuildingSelectionUI(BoardTile buildableTile, Player player)
    {
        // 修改点1：保存当前上下文，但不立即高亮
        selectedBoardTile = buildableTile;
        currentBuildingPlayer = player;

        Debug.Log($"UIManager: 显示建筑选择面板，地块规模: {buildableTile.tileScale}");

        // 1. 关闭其他可能冲突的UI
        HidePropertyPurchasePanel();

        // 2. 激活面板
        if (buildingSelectionPanel == null)
        {
            Debug.LogError("UIManager: BuildingSelectionPanel 引用为空！");
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
                Debug.Log("UIManager: 关闭按钮被点击");
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
        Debug.Log($"UIManager: 隐藏建筑选择面板，保持按钮: {keepButtons}");

        // 1. 清除高亮
        ClearTileHighlights();

        // 2. 清除持久Toast（如果有）
        HidePersistentToast();

        // 【新增】3. 如果不是保持按钮状态，清除所有建筑按钮
        if (!keepButtons)
        {
            ClearBuildingButtons();
        }

        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            Debug.Log("UIManager: 建筑选择面板已隐藏");
        }

        // 4. 根据参数决定是否重置选择状态
        if (!keepButtons)
        {
            // 完全关闭时的重置逻辑
            selectedBuildingData = null;
            currentBuildingPlayer = null;
            isBuildingSelected = false;

            // 通知GameManager购买阶段结束
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBuildingPurchaseCompleted();
            }
        }
        else
        {
            // 只隐藏面板，不清除按钮和选择状态
            Debug.Log("保持建筑按钮和选择状态");
        }

        // 5. 重新启用骰子按钮
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
            // 检查是否是返回按钮
            if (child.name == "Btn_Return" || child.name.StartsWith("Btn_"))
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("已清除所有建筑按钮和返回按钮");
    }

    // 取消建筑选择
    private void OnCancelBuildingSelection()
    {
        Debug.Log("取消建筑选择");

        // 1. 清除持久Toast
        HidePersistentToast();

        // 2. 清除地块高亮
        ClearTileHighlights();

        // 3. 重新打开建筑选择面板
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(true);
            Debug.Log("建筑选择面板已重新打开");
        }

        // 4. 重置选择状态
        selectedBuildingData = null;
        isBuildingSelected = false;

        // 注意：不清除buildingButtonContainer中的按钮
        // 保持按钮存在，用户可以重新选择
    }


    // 创建建筑按钮
    private void CreateBuildingButtons(List<BuildingData> buildings)
    {
        if (buildingButtonContainer == null)
        {
            Debug.LogWarning("buildingButtonContainer 为空，无法创建建筑按钮");
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
        Debug.Log($"选中建筑: {building.buildingName}, 价格: {building.purchasePrice}");

        selectedBuildingData = building;
        isBuildingSelected = true;

        // 1. 暂时关闭建筑选择面板
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
        }

        // 【新增】2. 清除建筑按钮
        ClearBuildingButtons();

        // 3. 在左下角显示持久Toast
        ShowPersistentToast($"已选择: {building.buildingName}");

        // 4. 高亮可放置的地块
        HighlightPlaceableTiles(currentBuildingPlayer, (int)building.requiredScale);

        Debug.Log("建筑面板已暂时关闭，等待放置或按ESC取消");
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
        // 1. 清除之前的高亮
        ClearTileHighlights();

        if (BoardManager.Instance == null)
        {
            Debug.LogWarning("BoardManager.Instance 为空，无法高亮地块");
            return;
        }

        Debug.Log($"=== 开始高亮可放置地块 ===");
        Debug.Log($"玩家: {player.playerName}");
        Debug.Log($"需求规模: {requiredScale}");
        Debug.Log($"总地块数: {BoardManager.Instance.allTiles.Count}");

        int highlightCount = 0;
        int checkedCount = 0;

        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            checkedCount++;

            // 跳过起始格子
            if (tile.tileType == BoardTile.TileType.Start)
            {
                Debug.Log($"跳过起始格子: {tile.tileName}");
                continue;
            }

            // 检查地块是否可放置
            bool isPlaceable = IsTilePlaceable(tile, player, requiredScale);

            if (isPlaceable)
            {
                // 保存原始颜色
                MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    // 1. 保存原始颜色
                    originalTileColors[tile] = renderer.material.color;

                    // 2. 设置高亮颜色
                    renderer.material.color = Color.green;

                    // 3. 添加到高亮列表
                    highlightableTiles.Add(tile);
                    highlightCount++;

                    // 【核心修改点】4. 绑定点击事件
                    AddTileClickHandler(tile);
                    Debug.Log($"✅ 高亮并绑定点击: {tile.tileName}");
                }
                else
                {
                    Debug.LogWarning($"地块 {tile.tileName} 没有 MeshRenderer，无法高亮");
                }
            }
            else
            {
                // 记录不可放置的原因（可选，用于调试）
                //if (enableDebugLogs)
                //{
                //    Debug.Log($"❌ 跳过地块: {tile.tileName} (不可放置)");
                //}
            }
        }

        Debug.Log($"=== 高亮完成 ===");
        Debug.Log($"检查了 {checkedCount} 个地块");
        Debug.Log($"高亮了 {highlightCount} 个可放置地块");

        if (highlightCount == 0)
        {
            ShowToast("没有可放置的地块，请检查地块规模和所有权", 2f);

            // 【可选】如果没有可放置地块，自动取消选择
            if (isBuildingSelected)
            {
                Debug.Log("没有可放置地块，自动取消选择");
                OnCancelBuildingSelection();
            }
        }
        else
        {
            ShowToast($"找到 {highlightCount} 个可放置地块，请点击选择", 2f);
        }
    }
    public bool IsTileHighlighted(BoardTile tile)
    {
        return highlightableTiles.Contains(tile);
    }

    // 检查地块是否可放置建筑
    private bool IsTilePlaceable(BoardTile tile, Player player, int requiredScale)
    {
        // 条件0: 绝对不能是起始格子
        if (tile.tileType == BoardTile.TileType.Start)
        {
            Debug.Log($"[高亮检查] 地块 {tile.tileName} 是起始格子，绝对不可放置建筑");
            return false;
        }

        // 条件1: 地块必须是可建造类型
        bool isBuildableType = (tile.tileType == BoardTile.TileType.Buildable ||
                               tile.tileType == BoardTile.TileType.BuildingSite);
        if (!isBuildableType)
        {
            Debug.Log($"[高亮检查] 地块 {tile.tileName} 不是可建造类型: {tile.tileType}");
            return false;
        }

        // 条件2: 地块规模符合要求
        if (tile.tileScale < requiredScale)
        {
            Debug.Log($"[高亮检查] 地块 {tile.tileName} 规模不足: {tile.tileScale} < {requiredScale}");
            return false;
        }

        // 条件3: 地块没有建筑
        if (tile.currentBuilding != null)
        {
            Debug.Log($"[高亮检查] 地块 {tile.tileName} 已有建筑");
            return false;
        }

        // 条件4: 玩家拥有该地块或地块无主
        bool isOwned = tile.ownerPlayer == null || tile.ownerPlayer == player;
        if (!isOwned)
        {
            Debug.Log($"[高亮检查] 地块 {tile.tileName} 不属于玩家");
        }

        // 条件5: 检查地块是否允许建造
        if (!tile.isBuildable)
        {
            Debug.Log($"[高亮检查] 地块 {tile.tileName} 的 isBuildable 为 false");
            return false;
        }

        return isOwned;
    }
    private void HideReturnButton()
    {
        if (buildingButtonContainer == null) return;

        Transform returnButton = buildingButtonContainer.Find("Btn_Return");
        if (returnButton != null)
        {
            Destroy(returnButton.gameObject);
        }
    }

    // 为地块添加点击处理器
    private void AddTileClickHandler(BoardTile tile)
    {
        if (tile == null)
        {
            Debug.LogWarning("AddTileClickHandler: tile 为 null");
            return;
        }

        try
        {
            // 移除现有的事件触发器（避免重复）
            EventTrigger existingTrigger = tile.GetComponent<EventTrigger>();
            if (existingTrigger != null)
            {
                // 只销毁我们添加的事件触发器
                if (existingTrigger.triggers.Count == 1 &&
                    existingTrigger.triggers[0].eventID == EventTriggerType.PointerClick)
                {
                    Destroy(existingTrigger);
                }
            }

            // 添加新的事件触发器
            EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();

            // 创建点击事件
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;

            // 使用闭包捕获当前 tile
            BoardTile currentTile = tile;
            clickEntry.callback.AddListener((eventData) =>
            {
                Debug.Log($"地块 {currentTile.tileName} 被点击");
                OnTileClickedForPlacement(currentTile);
            });

            trigger.triggers.Add(clickEntry);

            Debug.Log($"✅ 成功为 {tile.tileName} 绑定点击事件");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"为地块 {tile.tileName} 添加点击处理器失败: {e.Message}");
        }
    }


    // 地块被点击（用于放置建筑）
    private void OnTileClickedForPlacement(BoardTile tile)
    {
        if (selectedBuildingData == null || currentBuildingPlayer == null)
        {
            Debug.LogWarning("未选择建筑或玩家不存在");
            return;
        }

        Debug.Log($"尝试在 {tile.tileName} 上放置 {selectedBuildingData.buildingName}");

        // 详细的检查步骤
        Debug.Log($"=== 放置建筑详细检查 ===");
        Debug.Log($"1. 玩家现金: {currentBuildingPlayer.cash}, 建筑价格: {selectedBuildingData.purchasePrice}");

        // 检查玩家资金
        if (currentBuildingPlayer.cash < selectedBuildingData.purchasePrice)
        {
            Debug.LogError("资金不足！");
            ShowToast("资金不足，无法购买建筑！", 2f);
            return;
        }
        Debug.Log($"2. 资金检查通过");

        // 详细检查地块是否可放置
        bool isPlaceable = IsTilePlaceable(tile, currentBuildingPlayer, (int)selectedBuildingData.requiredScale);
        Debug.Log($"3. 地块可放置性检查: {isPlaceable}");

        if (!isPlaceable)
        {
            // 添加详细的检查信息
            Debug.Log($"地块 {tile.tileName} 检查详情：");
            Debug.Log($"  - 地块类型: {tile.tileType}");
            Debug.Log($"  - 地块规模: {tile.tileScale}, 需求规模: {selectedBuildingData.requiredScale}");
            Debug.Log($"  - 现有建筑: {tile.currentBuilding != null}");
            Debug.Log($"  - 地块所有者: {tile.ownerPlayer?.playerName ?? "无主"}");
            Debug.Log($"  - 当前玩家: {currentBuildingPlayer.playerName}");

            ShowToast("此地块无法放置该建筑！", 2f);
            return;
        }

        Debug.Log($"4. 所有检查通过，开始购买放置");

        // 购买并放置建筑
        if (PurchaseAndPlaceBuilding(tile, selectedBuildingData, currentBuildingPlayer))
        {
            Debug.Log($"建筑 {selectedBuildingData.buildingName} 放置成功！");

            // 清除高亮
            ClearTileHighlights();

            // 【新增】隐藏返回按钮
            HideReturnButton();

            // 隐藏建筑选择面板
            HideBuildingSelectionUI();
        }
    }
    // 创建返回按钮
    private void CreateReturnButton()
    {
        if (buildingButtonContainer == null) return;

        // 创建返回按钮对象
        GameObject returnButtonObj = new GameObject("Btn_Return");
        returnButtonObj.transform.SetParent(buildingButtonContainer);

        // 添加UI组件
        Image image = returnButtonObj.AddComponent<Image>();
        Button button = returnButtonObj.AddComponent<Button>();

        // 设置返回按钮样式
        image.color = Color.yellow;  // 黄色背景，与建筑按钮区分

        // 设置按钮大小
        RectTransform rt = returnButtonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);

        // 添加返回文本
        GameObject textObj = new GameObject("ReturnText");
        textObj.transform.SetParent(returnButtonObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = "返回\n(Esc)";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        // 绑定点击事件 - 调用ESC取消功能
        button.onClick.AddListener(() => {
            Debug.Log("返回按钮被点击");
            OnCancelBuildingSelection();
        });
    }

    // 购买并放置建筑
    private bool PurchaseAndPlaceBuilding(BoardTile tile, BuildingData buildingData, Player player)
    {
        // 扣除资金
        if (!player.PayCash(buildingData.purchasePrice))
        {
            ShowToast("购买失败：资金不足！", 2f);
            return false;
        }

        // 实例化建筑模型
        if (buildingData.buildingPrefab != null)
        {
            GameObject buildingInstance = Instantiate(buildingData.buildingPrefab,
                tile.transform.position,
                Quaternion.identity,
                tile.transform);

            buildingInstance.transform.localPosition = Vector3.up * 0.5f; // 稍微抬高

            // 更新地块信息
            tile.currentBuilding = buildingInstance;
            tile.currentBuildingType = GetBuildingTypeFromData(buildingData);
            tile.buildingLevel = 1;
            tile.isBuildable = false;

            // 如果地块无主，则分配给玩家
            if (tile.ownerPlayer == null)
            {
                tile.ownerPlayer = player;
            }

            // 更新地块类型
            tile.tileType = BoardTile.TileType.BuildingSite;
        }

        // 显示成功消息
        ShowToast($"成功建造 {buildingData.buildingName}！", 2f);

        // 更新玩家UI
        UpdateCurrentPlayerInfo(player);

        return true;
    }

    // 清除地块高亮
    private void ClearTileHighlights()
    {
        Debug.Log($"开始清除 {highlightableTiles.Count} 个地块的高亮");

        foreach (BoardTile tile in highlightableTiles)
        {
            MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                // 恢复原始颜色
                if (originalTileColors.ContainsKey(tile))
                {
                    renderer.material.color = originalTileColors[tile];
                }
                else
                {
                    // 调用地块自身的视觉更新方法
                    tile.UpdateTileVisual();
                }
            }

            // 【关键】移除事件触发器
            EventTrigger trigger = tile.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                // 检查是否是我们添加的简单触发器
                bool isSimpleTrigger = trigger.triggers.Count == 1 &&
                                      trigger.triggers[0].eventID == EventTriggerType.PointerClick;

                if (isSimpleTrigger)
                {
                    Destroy(trigger);
                    Debug.Log($"已移除 {tile.tileName} 的点击事件");
                }
            }
        }

        originalTileColors.Clear();
        highlightableTiles.Clear();

        Debug.Log("已清除所有地块高亮和点击事件");
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
                Debug.LogWarning("没有找到Canvas，正在创建...");
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

        Debug.Log("已创建Canvas");
    }

    void CreateRollDiceButton()
    {
        if (rollDiceButtonPrefab == null)
        {
            Debug.LogWarning("掷骰子按钮预制体未设置");
            return;
        }

        GameObject buttonObj = Instantiate(rollDiceButtonPrefab, mainCanvas.transform);
        buttonObj.name = "Btn_RollDice";

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = diceButtonPosition;

        rollDiceButton = buttonObj.GetComponent<Button>();
        if (rollDiceButton != null)
        {
            Debug.Log("掷骰子按钮创建成功");
        }
    }

    // 注意：这个方法是给按钮的OnClick事件使用的
    public void OnRollDiceButtonClicked()
    {
        Debug.Log("UIManager: 点击掷骰子按钮");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRollDiceButtonClicked();
        }
        else
        {
            Debug.LogError("GameManager.Instance 为空，无法处理掷骰子");
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

        Debug.Log($"切换到 {uiType} 界面");
    }

    public void SwitchToMenuUI() => SwitchUI(UIType.Menu);
    public void SwitchToGameUI() => SwitchUI(UIType.Game);
    public void SwitchToPauseUI() => SwitchUI(UIType.Pause);
    public void SwitchToGameOverUI() => SwitchUI(UIType.GameOver);

    public void ShowPropertyPurchasePanel(BoardTile property, Player player)
    {
        if (propertyPurchasePanel == null)
        {
            Debug.LogWarning("地产购买面板预制体未设置");
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

    public void ShowGameOverPanel(string winnerName)
    {
        SwitchToGameOverUI();

        Text winnerText = gameOverPanel.transform.Find("WinnerText")?.GetComponent<Text>();
        if (winnerText != null)
        {
            winnerText.text = $"{winnerName} 获胜";
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
            Debug.LogWarning("玩家信息UI预制体未设置");
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
    // 显示建筑升级面板
    public void ShowBuildingUpgradeUI(BoardTile tile, Player player)
    {
        if (buildingUpgradePanel == null)
        {
            Debug.LogWarning("升级面板未设置");
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
                Debug.Log($"升级后功能: {nextFunctionDesc}");
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