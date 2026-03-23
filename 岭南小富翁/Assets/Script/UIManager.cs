// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("建筑UI - 关键修改")]
    // 【修改点1】这个字段现在应该直接引用场景中已有的BuildingSelectionPanel游戏对象
    // 请在Unity编辑器中，将Hierarchy里的 BuildingSelectionPanel 拖拽到这里
    public GameObject buildingSelectionPanel;
    public GameObject buildingInfoPanelPrefab;
    // 注意：我们已经移除了 `currentBuildingSelectionPanel` 变量，因为直接使用上面的引用

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

    // === 核心修改：显示建筑选择UI（不再实例化，直接激活已有对象）===
    public void ShowBuildingSelectionUI(BoardTile buildableTile, Player player)
    {
        // 【修改点3】检查场景中的面板引用是否有效
        if (buildingSelectionPanel == null)
        {
            Debug.LogError("UIManager: BuildingSelectionPanel 引用为空！无法显示面板。");
            Debug.LogError("请在Inspector中将Hierarchy中的 BuildingSelectionPanel 拖拽到 UIManager 脚本的对应字段。");
            return;
        }

        Debug.Log("UIManager: 显示建筑选择面板");

        // 1. 关闭其他可能冲突的UI
        HidePropertyPurchasePanel();

        // 2. 【关键修改】激活场景中已有的面板（不再使用 Instantiate）
        if (!buildingSelectionPanel.activeSelf)
        {
            buildingSelectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("UIManager: 建筑选择面板已经是激活状态，可能已被重复调用。");
        }

        // 3. 将面板置顶显示
        buildingSelectionPanel.transform.SetAsLastSibling();

        // 4. 获取UI组件并更新信息
        Text tileNameText = buildingSelectionPanel.transform.Find("TileName")?.GetComponent<Text>();
        Text priceText = buildingSelectionPanel.transform.Find("Price")?.GetComponent<Text>();
        Transform buildingGrid = buildingSelectionPanel.transform.Find("BuildingGrid");

        // 【修改点4】获取按钮引用
        Button closeButton = buildingSelectionPanel.transform.Find("CloseButton")?.GetComponent<Button>();
        Button buyButton = buildingSelectionPanel.transform.Find("BuyButton")?.GetComponent<Button>();

        if (tileNameText != null)
            tileNameText.text = buildableTile.tileName;
        else
            Debug.LogWarning("未找到 TileName 文本组件");

        if (priceText != null)
            priceText.text = $"地块价格: {buildableTile.propertyPrice} 元";
        else
            Debug.LogWarning("未找到 Price 文本组件");

        // 【修改点5】确保关闭按钮事件绑定
        if (closeButton != null)
        {
            // 清除旧的监听器，防止重复绑定
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                Debug.Log("UIManager: 关闭按钮被点击");
                HideBuildingSelectionUI();
            });
            Debug.Log("UIManager: 已绑定关闭按钮事件");
        }
        else
        {
            Debug.LogError("UIManager: 在BuildingSelectionPanel上未找到CloseButton！请检查预制体结构。");
        }

        // 【修改点6】确保购买按钮事件绑定
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => {
                Debug.Log("UIManager: 购买按钮被点击");
                OnPurchaseBuildingTile(buildableTile, player);
            });
            Debug.Log("UIManager: 已绑定购买按钮事件");
        }
        else
        {
            Debug.LogError("UIManager: 在BuildingSelectionPanel上未找到BuyButton！请检查预制体结构。");
        }

        // 5. 动态创建建筑选择按钮（可选功能，根据您的需要）
        if (buildingGrid != null && buildableTile.availableBuildings.Count > 0)
        {
            // 清空现有选项
            foreach (Transform child in buildingGrid)
            {
                Destroy(child.gameObject);
            }

            // 为每个可建筑创建按钮
            for (int i = 0; i < buildableTile.availableBuildings.Count; i++)
            {
                GameObject buildingPrefab = buildableTile.availableBuildings[i];
                GameObject buildingButton = new GameObject($"BuildingButton_{i}");
                buildingButton.transform.SetParent(buildingGrid);

                // 添加按钮组件
                Button btn = buildingButton.AddComponent<Button>();
                Image img = buildingButton.AddComponent<Image>();

                // 这里可以添加建筑预览图片等

                // 添加点击事件
                int index = i; // 避免闭包问题
                btn.onClick.AddListener(() => OnBuildingSelected(buildableTile, player, index));
            }
        }

        // 6. 禁用掷骰子按钮，防止在购买期间移动
        SetRollDiceButtonInteractable(false);
    }

    // === 核心修改：隐藏建筑选择UI（不销毁，只是隐藏）===
    public void HideBuildingSelectionUI()
    {
        Debug.Log("UIManager: 隐藏建筑选择面板");

        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            Debug.Log("UIManager: 建筑选择面板已隐藏");

            // 通知GameManager购买阶段结束
            if (GameManager.Instance != null)
            {
                Debug.Log("UIManager: 通知GameManager购买完成");
                GameManager.Instance.OnBuildingPurchaseCompleted();
            }
            else
            {
                Debug.LogError("UIManager: GameManager.Instance 为空，无法通知购买完成");
                // 尝试直接查找
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    Debug.Log("UIManager: 找到GameManager，调用方法");
                    gm.OnBuildingPurchaseCompleted();
                }
            }
        }
        else
        {
            Debug.LogError("UIManager: buildingSelectionPanel 为null，无法隐藏");
        }
    }

    // 建筑选择回调
    void OnBuildingSelected(BoardTile tile, Player player, int buildingIndex)
    {
        Debug.Log($"UIManager: 玩家选择了建筑 {buildingIndex}");
        // 这里可以显示建筑详细信息或直接确认选择
    }

    // 购买地块
    void OnPurchaseBuildingTile(BoardTile tile, Player player)
    {
        if (player.BuyProperty(tile))
        {
            // 购买成功，更改地块类型为建筑工地
            tile.tileType = BoardTile.TileType.BuildingSite;

            // 隐藏面板
            HideBuildingSelectionUI();

            // 显示建筑放置UI（可选）
            ShowBuildingPlacementUI(tile, player);

            tile.isBuildable = false;
            tile.availableBuildings.Clear(); // 清空可建筑列表，防止UI再次显示

            Debug.Log($"UIManager: {player.playerName} 成功购买了 {tile.tileName}");
        }
        else
        {
            Debug.Log($"{player.playerName} 购买失败，资金不足");
            // 购买失败时也可以选择保持面板打开
            // 这里可以给玩家一个提示
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateUI();
            }
        }
    }

    // 显示建筑放置UI
    public void ShowBuildingPlacementUI(BoardTile tile, Player player)
    {
        Debug.Log($"UIManager: 显示建筑放置UI: {tile.tileName}");
        // 这里可以添加建筑放置的具体逻辑
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

    void OnDestroy()
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.RemoveAllListeners();
        }
    }
}