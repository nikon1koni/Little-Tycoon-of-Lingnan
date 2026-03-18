// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    // 单例模式
    public static UIManager Instance;

    [Header("UI预制体")]
    public GameObject rollDiceButtonPrefab;
    public GameObject playerInfoPrefab;
    public GameObject propertyPanelPrefab;

    [Header("UI引用")]
    public Canvas mainCanvas;
    public Text diceResultText;
    public Text currentPlayerText;
    public Text playerCashText;
    public Text currentTileText;

    [Header("UI面板")]
    public GameObject gamePanel;          // 游戏主界面
    public GameObject menuPanel;          // 菜单界面
    public GameObject pausePanel;         // 暂停界面
    public GameObject gameOverPanel;      // 游戏结束界面
    public GameObject propertyPurchasePanel; // 地产购买面板

    [Header("骰子UI")]
    public Button rollDiceButton;
    public Text diceAnimationText;

    [Header("玩家信息UI列表")]
    public List<PlayerInfoUI> playerInfoUIs = new List<PlayerInfoUI>();

    [Header("设置")]
    public Vector2 diceButtonPosition = new Vector2(-200, -100);

    // 当前显示的UI类型
    private UIType currentUIType = UIType.Game;

    // UI类型枚举
    public enum UIType
    {
        Menu,       // 菜单
        Game,       // 游戏中
        Pause,      // 暂停
        GameOver    // 游戏结束
    }

    // 玩家信息UI类
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
        // 单例初始化
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
        InitializeUI();
    }

    void InitializeUI()
    {
        Debug.Log("=== 初始化UI ===");

        // 1. 确保Canvas存在
        EnsureCanvasExists();

        // 2. 创建骰子按钮
        CreateRollDiceButton();

        // 3. 设置初始界面
        SwitchToGameUI();

        // 4. 隐藏其他面板
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (propertyPurchasePanel != null) propertyPurchasePanel.SetActive(false);

        Debug.Log("UI初始化完成");
    }

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

        // 添加Canvas Scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 添加Graphic Raycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("已创建Canvas");
    }

    // 创建骰子按钮
    void CreateRollDiceButton()
    {
        if (rollDiceButtonPrefab == null)
        {
            Debug.LogWarning("骰子按钮预制体未设置");
            return;
        }

        GameObject buttonObj = Instantiate(rollDiceButtonPrefab, mainCanvas.transform);
        buttonObj.name = "Btn_RollDice";

        // 设置位置
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = diceButtonPosition;

        // 获取Button组件
        rollDiceButton = buttonObj.GetComponent<Button>();
        if (rollDiceButton != null)
        {
            // 绑定点击事件
            rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
            Debug.Log("骰子按钮创建并绑定完成");
        }
    }

    // 骰子按钮点击事件
    void OnRollDiceButtonClicked()
    {
        Debug.Log("骰子按钮被点击");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRollDiceButtonClicked();
        }
        else
        {
            Debug.LogError("GameManager.Instance 为空！");
        }
    }

    // 更新骰子结果显示
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

    // 更新当前玩家信息
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

    // 切换界面
    public void SwitchUI(UIType uiType)
    {
        currentUIType = uiType;

        // 隐藏所有面板
        if (gamePanel != null) gamePanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 显示目标面板
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

    // 快捷方法
    public void SwitchToMenuUI() => SwitchUI(UIType.Menu);
    public void SwitchToGameUI() => SwitchUI(UIType.Game);
    public void SwitchToPauseUI() => SwitchUI(UIType.Pause);
    public void SwitchToGameOverUI() => SwitchUI(UIType.GameOver);

    // 显示地产购买面板
    public void ShowPropertyPurchasePanel(BoardTile property, Player player)
    {
        if (propertyPurchasePanel == null)
        {
            Debug.LogWarning("地产购买面板预制体未设置");
            return;
        }

        // 实例化或显示面板
        if (!propertyPurchasePanel.activeSelf)
        {
            propertyPurchasePanel.SetActive(true);
        }

        // 更新面板信息
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

    // 隐藏地产购买面板
    public void HidePropertyPurchasePanel()
    {
        if (propertyPurchasePanel != null)
        {
            propertyPurchasePanel.SetActive(false);
        }
    }

    // 显示游戏结束面板
    public void ShowGameOverPanel(string winnerName)
    {
        SwitchToGameOverUI();

        Text winnerText = gameOverPanel.transform.Find("WinnerText")?.GetComponent<Text>();
        if (winnerText != null)
        {
            winnerText.text = $"{winnerName} 获胜！";
        }
    }

    // 设置骰子按钮状态
    public void SetRollDiceButtonInteractable(bool interactable)
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = interactable;
        }
    }

    // 更新按钮文本
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

    // 创建玩家信息UI
    public void CreatePlayerInfoUI(Player player)
    {
        if (playerInfoPrefab == null)
        {
            Debug.LogWarning("玩家信息UI预制体未设置");
            return;
        }

        GameObject playerInfoObj = Instantiate(playerInfoPrefab, mainCanvas.transform);
        playerInfoObj.name = $"PlayerInfo_{player.playerName}";

        // 获取组件
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

        // 更新初始信息
        UpdatePlayerInfoUI(infoUI);

        // 设置位置
        RectTransform rt = playerInfoObj.GetComponent<RectTransform>();
        int index = playerInfoUIs.Count - 1;
        rt.anchoredPosition = new Vector2(200, -50 - index * 80);
    }

    // 更新玩家信息UI
    public void UpdatePlayerInfoUI(PlayerInfoUI infoUI)
    {
        if (infoUI.playerNameText != null)
            infoUI.playerNameText.text = infoUI.player.playerName;

        if (infoUI.cashText != null)
            infoUI.cashText.text = $"{infoUI.player.cash} 元";

        if (infoUI.playerColorImage != null)
            infoUI.playerColorImage.color = infoUI.player.playerColor;
    }

    // 更新所有玩家信息
    public void UpdateAllPlayerInfo()
    {
        foreach (var infoUI in playerInfoUIs)
        {
            UpdatePlayerInfoUI(infoUI);
        }
    }

    // 显示Toast消息
    public void ShowToast(string message, float duration = 2f)
    {
        // 创建临时文本显示消息
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

        // 添加背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toastObj.transform);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // 移动到最前
        text.transform.SetAsLastSibling();

        // 自动销毁
        Destroy(toastObj, duration);
    }

    void OnDestroy()
    {
        // 清理事件绑定
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.RemoveAllListeners();
        }
    }
}