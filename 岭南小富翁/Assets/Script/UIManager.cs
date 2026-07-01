﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using static BoardTile;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 
    public static UIManager Instance;

    [Header("UI (Prefabs)")]
    public GameObject rollDiceButtonPrefab;
    public GameObject playerInfoPrefab;
    public GameObject propertyPanelPrefab;

    [Header("UI")]
    public Canvas mainCanvas;
    public Text diceResultText;
    public Text currentPlayerText;
    public Text playerCashText;
    public Text currentTileText;

    [Header("UI")]
    public GameObject gamePanel;
    public GameObject menuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject propertyPurchasePanel;

    [Header("UI")]
    public Button rollDiceButton;
    public Text diceAnimationText;

    [Header("UI")]
    public List<PlayerInfoUI> playerInfoUIs = new List<PlayerInfoUI>();

    [Header("")]
    public Vector2 diceButtonPosition = new Vector2(-20, -10); // 

    [Header("UI")]
    public GameObject buildingSelectionPanel;
    public Button[] buildingButtons = new Button[4];
    public TextMeshProUGUI tileInfoText;
    public Button closeBuildingPanelButton;
    public Text selectedBuildingText;
    public Image selectedBuildingImage;
    public Text buildingPriceText;

    // 建筑悬停提示（运行时自动生成，跟随鼠标）
    private GameObject buildingTooltipObj;
    private RectTransform buildingTooltipRect;
    private TextMeshProUGUI buildingTooltipText;
    private bool buildingTooltipVisible = false;

    [Header("")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("UI")]
    public GameObject buildingUpgradePanel;
    public Button upgradeButton;
    public Text upgradeCostText;
    public Text currentLevelText;
    public Text nextLevelText;
    public Image upgradeBuildingImage;
    public Button closeUpgradePanelButton;

    [Header("Toast")]
    public GameObject persistentToastPanel;
    public Text persistentToastText;
    public Vector2 toastPosition = new Vector2(20, 20);
    
    [Header("UI")]
    public GameObject buildingPlacementHintPanel;
    public TextMeshProUGUI buildingPlacementHintText;

    [Header("")]
    [SerializeField] private GameObject cashDisplayPanel;
    [SerializeField] private TextMeshProUGUI cashText;

    [Header("UI")]
    public GameObject pressureSystemPanel;
    public TextMeshProUGUI diceRollCountText;
    public TextMeshProUGUI currentRoundText;

    [Header("UI")]
    public GameObject turnAnnouncePanel;
    public TextMeshProUGUI turnAnnounceText;
    public float announceDuration = 2.5f;

    [Header("UI")]
    public EventPanel eventPanel;

    [Header("ToastUI")]
    public GameObject infoToastPanel;
    public TextMeshProUGUI infoToastText;
    private Coroutine hideInfoToastCoroutine;
    
    [Header("Toast")]
    public float toastInterval = 0.5f;
    private Queue<ToastMessage> toastQueue = new Queue<ToastMessage>();
    private bool isToastPlaying = false;
    private string currentToastMessage = null;
    
    public struct ToastMessage
    {
        public string message;
        public float duration;
        public bool isAnnouncement;   // true=压力系统横幅(turnAnnouncePanel)，false=普通toast(infoToastPanel)
        
        public ToastMessage(string msg, float dur, bool announcement = false)
        {
            message = msg;
            duration = dur;
            isAnnouncement = announcement;
        }
    }

    public TextMeshProUGUI CashText => cashText;

    // 
    private bool isBuildingSelected = false;
    private GameObject activePersistentToast;
    private List<int> activeBuildingButtonIndices = new List<int>();

    // 
    private BoardTile upgradeSelectedTile = null;
    private Player upgradeSelectedPlayer = null;

    // 
    private BuildingData selectedBuildingData = null;
    private BoardTile selectedBoardTile = null;
    private Player currentBuildingPlayer = null;

    // 
    private Dictionary<BoardTile, Color> originalTileColors = new Dictionary<BoardTile, Color>();
    private List<BoardTile> highlightableTiles = new List<BoardTile>();

    // UI
    private UIType currentUIType = UIType.Game;

    // UI
    public enum UIType
    {
        Menu,
        Game,
        Pause,
        GameOver
    }

    // UI
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
        if (buildingTooltipVisible)
        {
            UpdateBuildingTooltipPosition();
        }

        // ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 
            if (BuildingDataConfig.Instance != null && BuildingDataConfig.Instance.IsUpgradeModeActive())
            {
                // 
                BuildingDataConfig.Instance.ExitUpgradeMode();
                return; // ESC
            }
            // 
            else if (BuildingDataConfig.Instance != null && BuildingDataConfig.Instance.IsSellModeActive())
            {
                // 
                BuildingDataConfig.Instance.ExitSellMode();
                return; // ESC
            }
            else if (isBuildingSelected)
            {
                // 
                OnCancelBuildingSelection();
            }
            else if (buildingSelectionPanel != null && buildingSelectionPanel.activeSelf)
            {
                // 
                HideBuildingSelectionUI();
            }
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
        UnityEngine.Debug.Log("=== UI初始化 ===");
        InitializeUI();

        // 
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            UnityEngine.Debug.Log("UIManager: 建筑选择面板已初始化隐藏");
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: 建筑选择面板未在Inspector中赋值");
        }
    }

    void InitializeUI()
    {
        EnsureCanvasExists();
        CreateRollDiceButton();
        AutoSetupPressureSystemUI();
        AutoSetupGameOverPanel();
        SwitchToGameUI();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (propertyPurchasePanel != null) propertyPurchasePanel.SetActive(false);

        // ===  ===
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            UpdateCashDisplay(GameManager.Instance.currentPlayer.cash);
        }
        // ===  ===

        UnityEngine.Debug.Log("UI初始化完成");
    }

    // ===  ===
    public void UpdateCashDisplay(int cashAmount)
    {
        if (cashText != null)
        {
            cashText.text = $"{cashAmount}";
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: 请在Inspector中设置'Cash Text'");
        }
    }

    // === UI ===
    public void UpdatePressureSystemUI()
    {
        if (GameManager.Instance == null) return;

        int totalDiceRollCount = 0;
        int currentRound = 0;

#if UNITY_EDITOR
        totalDiceRollCount = GameManager.Instance.DiceRollCount;
        currentRound = GameManager.Instance.CurrentRound;
#else
        var field = typeof(GameManager).GetField("diceRollCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            totalDiceRollCount = (int)field.GetValue(GameManager.Instance);
            currentRound = totalDiceRollCount / 6;
        }
#endif

        int diceInCurrentRound = totalDiceRollCount % 6;

        if (diceRollCountText != null)
        {
            string label = "骰子次数";
            if (!string.IsNullOrEmpty(diceRollCountText.text) && diceRollCountText.text.Contains(":"))
            {
                label = diceRollCountText.text.Substring(0, diceRollCountText.text.IndexOf(":") + 1);
            }
            diceRollCountText.text = $"{label} {diceInCurrentRound}/6";
        }

        if (currentRoundText != null)
        {
            int maxRounds = GameManager.Instance.maxRounds;
            currentRoundText.text = $"轮数：{currentRound}/{maxRounds}";
        }
    }

    private void AutoSetupPressureSystemUI()
    {
        if (pressureSystemPanel == null)
        {
            GameObject panel = GameObject.Find("PressureSystemPanel");
            if (panel != null)
            {
                pressureSystemPanel = panel;
                UnityEngine.Debug.Log("UIManager: 自动找到压力系统面板");
            }
        }

        if (diceRollCountText == null && pressureSystemPanel != null)
        {
            Transform diceTrans = pressureSystemPanel.transform.Find("DiceRollCountText");
            if (diceTrans != null)
            {
                diceRollCountText = diceTrans.GetComponent<TextMeshProUGUI>();
                UnityEngine.Debug.Log("UIManager: 自动找到骰子计数文本");
            }
        }

        if (currentRoundText == null && pressureSystemPanel != null)
        {
            Transform roundTrans = pressureSystemPanel.transform.Find("CurrentRoundText");
            if (roundTrans != null)
            {
                currentRoundText = roundTrans.GetComponent<TextMeshProUGUI>();
                UnityEngine.Debug.Log("UIManager: 自动找到回合文本");
            }
        }

        if (pressureSystemPanel != null)
        {
            UpdatePressureSystemUI();
        }
    }

    private void AutoSetupGameOverPanel()
    {
        if (gameOverPanel == null)
        {
            GameObject panel = GameObject.Find("GameOverPanel");
            if (panel != null)
            {
                gameOverPanel = panel;
                UnityEngine.Debug.Log("UIManager: 自动找到游戏结束面板");
            }
            else
            {
                UnityEngine.Debug.LogWarning("UIManager: 未找到GameOverPanel，请在Inspector中设置");
            }
        }
    }

    // === UI ===
    public void ShowBuildingSelectionUI(BoardTile buildableTile, Player player)
    {
        // 
        selectedBoardTile = buildableTile;
        currentBuildingPlayer = player;

        UnityEngine.Debug.Log($"UIManager: 显示建筑选择UI，地块规模: {buildableTile.tileScale}");

        // 
        HidePropertyPurchasePanel();

        // 
        if (buildingSelectionPanel == null)
        {
            UnityEngine.Debug.LogError("UIManager: 建筑选择面板为空");
            return;
        }

        buildingSelectionPanel.SetActive(true);
        buildingSelectionPanel.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);

        SetNonButtonRaycastTargets(false);

        if (tileInfoText != null)
        {
            tileInfoText.text = $"{buildableTile.tileName} - : {buildableTile.propertyPrice}";
            tileInfoText.raycastTarget = false;
        }

        ClearBuildingButtons();

        List<BuildingData> compatibleBuildings = FilterBuildingsByScale(buildableTile.tileScale);

        ConfigureBuildingButtons(compatibleBuildings);

        Button closeBtnToUse = closeBuildingPanelButton;
        
        if (closeBtnToUse == null)
        {
            closeBtnToUse = FindCloseButton();
        }
        
        if (closeBtnToUse != null)
        {
            closeBtnToUse.gameObject.SetActive(true);
            closeBtnToUse.interactable = true;
            
            Image btnImg = closeBtnToUse.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.raycastTarget = true;
                btnImg.color = new Color(0.9f, 0.9f, 0.9f, 1.0f);
            }
            
            foreach (Transform child in closeBtnToUse.transform)
            {
                Image childImg = child.GetComponent<Image>();
                if (childImg != null) childImg.raycastTarget = false;
                TextMeshProUGUI childTmp = child.GetComponent<TextMeshProUGUI>();
                if (childTmp != null) childTmp.raycastTarget = false;
            }
            
            closeBtnToUse.onClick.RemoveAllListeners();
            closeBtnToUse.onClick.AddListener(() => {
                UnityEngine.Debug.Log("UIManager: 点击关闭按钮，关闭建筑选择面板");
                HideBuildingSelectionUI();
            });

            UnityEngine.Debug.Log("UIManager: 关闭按钮配置完成");
        }
        else
        {
            UnityEngine.Debug.LogError("UIManager: 未找到关闭按钮");
        }

        // 
        SetRollDiceButtonInteractable(false);
    }

    // === UI ===
    public void HideBuildingSelectionUI(bool keepButtons = false)
    {
        UnityEngine.Debug.Log($"隐藏建筑选择UI: {keepButtons}");

        HideBuildingTooltip();

        // UI
        ClearTileHighlights();
        HidePersistentToast();

        // 
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.UIClose);
        }

        if (!keepButtons)
        {
            ClearBuildingButtons();
        }

        // 
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

        // 
        SetRollDiceButtonInteractable(true);
    }

    // 
    private Button FindCloseButton()
    {
        if (buildingSelectionPanel == null) return null;

        Transform closeBtnTrans = buildingSelectionPanel.transform.Find("CloseButton");
        if (closeBtnTrans == null)
        {
            foreach (Transform child in buildingSelectionPanel.transform)
            {
                if (child.name.Trim() == "CloseButton" || child.name.Contains("Close"))
                {
                    closeBtnTrans = child;
                    break;
                }
            }
        }

        if (closeBtnTrans != null)
        {
            UnityEngine.Debug.Log($"UIManager: 找到关闭按钮对象: '{closeBtnTrans.name}'");
            return closeBtnTrans.GetComponent<Button>();
        }

        return null;
    }

    private void SetNonButtonRaycastTargets(bool enable)
    {
        if (buildingSelectionPanel == null) return;

        foreach (Transform child in buildingSelectionPanel.transform)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) continue;

            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.raycastTarget = enable;
                continue;
            }

            Image img = child.GetComponent<Image>();
            if (img != null && btn == null)
            {
                img.raycastTarget = enable;
            }
        }
    }

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

    // 
    private void ClearBuildingButtons()
    {
        HideBuildingTooltip();

        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] != null)
            {
                buildingButtons[i].onClick.RemoveAllListeners();
                buildingButtons[i].gameObject.SetActive(false);

                EventTrigger hoverTrigger = buildingButtons[i].GetComponent<EventTrigger>();
                if (hoverTrigger != null)
                {
                    hoverTrigger.triggers.Clear();
                }

                Transform iconTransform = buildingButtons[i].transform.Find("Icon");
                if (iconTransform != null)
                {
                    Image iconImg = iconTransform.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = null;
                        iconImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                }

                Transform nameTransform = buildingButtons[i].transform.Find("BuildingName");
                if (nameTransform != null)
                {
                    TextMeshProUGUI nameTmp = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameTmp != null)
                    {
                        nameTmp.text = "";
                    }
                }
            }
        }
    }

    // 
    private void OnCancelBuildingSelection()
    {
        UnityEngine.Debug.Log("按ESC取消建筑选择");

        HidePersistentToast();
        ClearTileHighlights();

        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(true);
        }

        for (int i = 0; i < activeBuildingButtonIndices.Count; i++)
        {
            int idx = activeBuildingButtonIndices[i];
            if (buildingButtons[idx] != null)
            {
                buildingButtons[idx].gameObject.SetActive(true);
            }
        }

        selectedBuildingData = null;
        isBuildingSelected = false;
        UnityEngine.Debug.Log("已回到建筑选择界面");
    }

    // 
    private void ConfigureBuildingButtons(List<BuildingData> buildings)
    {
        activeBuildingButtonIndices.Clear();

        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] == null) continue;

            if (i < buildings.Count)
            {
                BuildingData building = buildings[i];
                buildingButtons[i].gameObject.SetActive(true);

                Transform iconTransform = buildingButtons[i].transform.Find("Icon");
                if (iconTransform != null)
                {
                    Image iconImg = iconTransform.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        if (building.buildingIcon != null)
                        {
                            iconImg.sprite = building.buildingIcon;
                            iconImg.color = Color.white;
                        }
                        else
                        {
                            iconImg.sprite = null;
                            iconImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                        }
                    }
                }

                Transform nameTransform = buildingButtons[i].transform.Find("BuildingName");
                if (nameTransform != null)
                {
                    TextMeshProUGUI nameTmp = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameTmp != null)
                    {
                        nameTmp.text = $"{building.buildingName}\n{building.purchasePrice} ";
                    }
                }

                buildingButtons[i].onClick.RemoveAllListeners();
                BuildingData currentBuilding = building;
                buildingButtons[i].onClick.AddListener(() => OnBuildingSelected(currentBuilding));

                AddBuildingHoverEvents(buildingButtons[i], currentBuilding);

                activeBuildingButtonIndices.Add(i);
            }
            else
            {
                buildingButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 给建筑按钮挂上鼠标进入/离开事件，用于显示悬停提示
    private void AddBuildingHoverEvents(Button button, BuildingData building)
    {
        if (button == null) return;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((data) => ShowBuildingTooltip(building));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((data) => HideBuildingTooltip());
        trigger.triggers.Add(exit);
    }

    // 从建筑名称文本上取一个支持中文的字体，避免运行时生成的提示框出现方块
    private TMP_FontAsset GetBuildingTooltipFont()
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] == null) continue;
            Transform nameTransform = buildingButtons[i].transform.Find("BuildingName");
            if (nameTransform != null)
            {
                TextMeshProUGUI tmp = nameTransform.GetComponent<TextMeshProUGUI>();
                if (tmp != null && tmp.font != null) return tmp.font;
            }
        }
        return null;
    }

    // 懒加载创建提示框（背景 + 文本）
    private void EnsureBuildingTooltip()
    {
        if (buildingTooltipObj != null) return;
        if (mainCanvas == null) return;

        buildingTooltipObj = new GameObject("BuildingTooltip");
        buildingTooltipObj.transform.SetParent(mainCanvas.transform, false);

        buildingTooltipRect = buildingTooltipObj.AddComponent<RectTransform>();
        buildingTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        buildingTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        buildingTooltipRect.pivot = new Vector2(0f, 1f);
        buildingTooltipRect.sizeDelta = new Vector2(260f, 80f);

        Image bg = buildingTooltipObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = false;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buildingTooltipObj.transform, false);

        buildingTooltipText = textObj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetBuildingTooltipFont();
        if (font != null) buildingTooltipText.font = font;
        buildingTooltipText.fontSize = 22;
        buildingTooltipText.color = Color.white;
        buildingTooltipText.alignment = TextAlignmentOptions.TopLeft;
        buildingTooltipText.enableWordWrapping = true;
        buildingTooltipText.raycastTarget = false;

        RectTransform textRect = buildingTooltipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 8f);
        textRect.offsetMax = new Vector2(-10f, -8f);

        buildingTooltipObj.SetActive(false);
    }

    // 显示某个建筑的悬停提示
    private void ShowBuildingTooltip(BuildingData building)
    {
        if (building == null) return;

        EnsureBuildingTooltip();
        if (buildingTooltipText == null) return;

        buildingTooltipText.text = !string.IsNullOrEmpty(building.description)
            ? building.description
            : building.GetTooltipDescription();
        buildingTooltipObj.SetActive(true);
        buildingTooltipObj.transform.SetAsLastSibling();

        buildingTooltipText.ForceMeshUpdate();
        float height = buildingTooltipText.preferredHeight + 16f;
        buildingTooltipRect.sizeDelta = new Vector2(260f, Mathf.Max(50f, height));

        buildingTooltipVisible = true;
        UpdateBuildingTooltipPosition();
    }

    private void HideBuildingTooltip()
    {
        buildingTooltipVisible = false;
        if (buildingTooltipObj != null)
        {
            buildingTooltipObj.SetActive(false);
        }
    }

    // 让提示框跟随鼠标，并限制在画布范围内
    private void UpdateBuildingTooltipPosition()
    {
        if (buildingTooltipRect == null || mainCanvas == null) return;

        RectTransform canvasRect = mainCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera cam = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, cam, out localPoint))
            return;

        Vector2 size = buildingTooltipRect.sizeDelta;
        Vector2 pos = localPoint + new Vector2(16f, -8f);

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;

        if (pos.x + size.x > halfW) pos.x = localPoint.x - 16f - size.x;
        if (pos.x < -halfW) pos.x = -halfW;
        if (pos.y - size.y < -halfH) pos.y = -halfH + size.y;
        if (pos.y > halfH) pos.y = halfH;

        buildingTooltipRect.anchoredPosition = pos;
    }

    // 
    private void OnBuildingSelected(BuildingData building)
    {
        UnityEngine.Debug.Log($"选择建筑: {building.buildingName}, 价格: {building.purchasePrice}");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);

        selectedBuildingData = building;
        isBuildingSelected = true;

        HideBuildingTooltip();

        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
        }

        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] != null)
            {
                buildingButtons[i].gameObject.SetActive(false);
            }
        }

        // 3. 
        ShowPersistentToast($"当前建筑：{building.buildingName}(按ESC退出)");

        // 4. 
        HighlightPlaceableTiles(currentBuildingPlayer, (int)building.requiredScale);

        UnityEngine.Debug.Log("建筑已选中");
    }

    private void ShowPersistentToast(string message)
    {
        HidePersistentToast();

        if (buildingPlacementHintPanel != null && buildingPlacementHintText != null)
        {
            buildingPlacementHintPanel.SetActive(true);
            buildingPlacementHintText.text = message;
            activePersistentToast = buildingPlacementHintPanel;
        }
        else if (persistentToastPanel != null)
        {
            activePersistentToast = Instantiate(persistentToastPanel, mainCanvas.transform);
            activePersistentToast.name = "PersistentToast";

            Text toastText = activePersistentToast.GetComponentInChildren<Text>();
            if (toastText != null)
            {
                toastText.text = message;
            }

            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            activePersistentToast.SetActive(true);
        }
        else
        {
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

            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

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
            if (activePersistentToast == buildingPlacementHintPanel)
            {
                activePersistentToast.SetActive(false);
            }
            else
            {
                Destroy(activePersistentToast);
            }
            activePersistentToast = null;
        }
    }

    private void HighlightPlaceableTiles(Player player, int requiredScale)
    {
        ClearTileHighlights();

        if (BoardManager.Instance == null) return;

        int highlightCount = 0;

        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            if (tile.tileType == BoardTile.TileType.Start) continue;

            if (IsTilePlaceable(tile, player, requiredScale))
            {
                MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    originalTileColors[tile] = renderer.material.color;
                    renderer.material.color = Color.green;
                    highlightableTiles.Add(tile);
                    highlightCount++;
                    AddTileClickHandler(tile);
                }
            }
        }

        if (highlightCount == 0)
        {
            ShowToast("没有可放置的地块", 2f);
        }
    }

    public bool IsTileHighlighted(BoardTile tile)
    {
        return highlightableTiles.Contains(tile);
    }

    // 
    private bool IsTilePlaceable(BoardTile tile, Player player, int requiredScale)
    {
        return (tile.tileType == BoardTile.TileType.Buildable ||
                tile.tileType == BoardTile.TileType.BuildingSite) &&
               tile.isBuildable &&
               tile.currentBuilding == null &&
               tile.tileScale >= requiredScale &&
               (tile.ownerPlayer == null || tile.ownerPlayer == player);
    }

    // 
    private void AddTileClickHandler(BoardTile tile)
    {
        EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => OnTileClickedForPlacement(tile));

        trigger.triggers.Add(entry);
    }

    // 
    public void OnTileClickedForPlacement(BoardTile tile)
    {
        if (selectedBuildingData == null || currentBuildingPlayer == null)
            return;

        if (currentBuildingPlayer.cash < selectedBuildingData.purchasePrice)
        {
            ShowToast("金币不足", 2f);
            return;
        }

        if (!IsTilePlaceable(tile, currentBuildingPlayer, (int)selectedBuildingData.requiredScale))
        {
            ShowToast("该位置无法放置", 2f);
            return;
        }

        // 
        bool hasBuildingSelected = selectedBuildingData != null;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayTileSelectSound(hasBuildingSelected);

        if (PurchaseAndPlaceBuilding(tile, selectedBuildingData, currentBuildingPlayer))
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingPlaced);

            ClearTileHighlights();
            HidePersistentToast();
            
            // 
            isBuildingSelected = false;
            selectedBuildingData = null;
            
            if (buildingSelectionPanel != null)
            {
                buildingSelectionPanel.SetActive(true);
            }
            
            for (int i = 0; i < activeBuildingButtonIndices.Count; i++)
            {
                int idx = activeBuildingButtonIndices[i];
                if (buildingButtons[idx] != null)
                {
                    buildingButtons[idx].gameObject.SetActive(true);
                }
            }
            
            ShowToast("建造成功", 2f);
        }
    }

    // 
    private bool PurchaseAndPlaceBuilding(BoardTile tile, BuildingData buildingData, Player player)
    {
        int purchasePrice = buildingData.purchasePrice;

        if (!player.PayCash(purchasePrice))
            return false;

        tile.ownerPlayer = player;
        tile.SetBuildingData(buildingData, buildingData.buildingLevel);
        tile.tileType = BoardTile.TileType.BuildingSite;
        
        tile.ApplyBuffToPlayer(player);
        
        // 
        if (!player.ownedProperties.Contains(tile))
        {
            player.ownedProperties.Add(tile);
            Debug.Log($"地块 {tile.tileName} 已归属 {player.playerName}");
        }

        if (buildingData.buildingPrefab != null)
        {
            Vector3 pos = tile.transform.position + buildingData.positionOffset;
            Quaternion rot = Quaternion.Euler(buildingData.rotationEuler);
            GameObject buildingObj = Instantiate(buildingData.buildingPrefab, pos, rot);
            buildingObj.transform.SetParent(tile.transform);
            tile.currentBuilding = buildingObj;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterBuildingPlaced();
        }

        return true;
    }

    // 
    private void ClearTileHighlights()
    {
        foreach (BoardTile tile in highlightableTiles)
        {
            MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    // 
                }
                else if (originalTileColors.ContainsKey(tile))
                {
                    // 
                    renderer.material.color = originalTileColors[tile];
                    UnityEngine.Debug.Log($"恢复 {tile.tileName} 的原始颜色");
                }
                else
                {
                    tile.UpdateTileVisual();
                }
            }

            EventTrigger trigger = tile.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                Destroy(trigger);
            }
        }

        originalTileColors.Clear();
        highlightableTiles.Clear();
    }

    // BuildingData
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        if (data.buildingName.Contains("") || data.buildingName.Contains("Small"))
            return BoardTile.BuildingType.SmallHouse;
        else if (data.buildingName.Contains("") || data.buildingName.Contains("Medium"))
            return BoardTile.BuildingType.MediumHouse;
        else if (data.buildingName.Contains("") || data.buildingName.Contains("Large"))
            return BoardTile.BuildingType.LargeHouse;
        else
            return BoardTile.BuildingType.None;
    }

    // ================= UI =================

    void EnsureCanvasExists()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                UnityEngine.Debug.LogWarning("场景中没有找到Canvas，自动创建一个...");
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
            UnityEngine.Debug.LogWarning("骰子按钮预制件为空");
            return;
        }

        GameObject buttonObj = Instantiate(rollDiceButtonPrefab, mainCanvas.transform);
        buttonObj.name = "Btn_RollDice";

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = diceButtonPosition;

        rollDiceButton = buttonObj.GetComponent<Button>();
        if (rollDiceButton != null)
        {
            UnityEngine.Debug.Log("掷骰子按钮已创建");
        }
    }

    // 
    public void OnRollDiceButtonClicked()
    {
        UnityEngine.Debug.Log("UIManager: 掷骰子按钮被点击");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRollDiceButtonClicked();
        }
        else
        {
            UnityEngine.Debug.LogError("GameManager.Instance 为空");
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
            currentPlayerText.text = $"玩家: {player.playerName}";
        }

        if (playerCashText != null)
        {
            playerCashText.text = $"金币: {player.cash}";
        }

        if (currentTileText != null && player.currentTile != null)
        {
            currentTileText.text = $"当前位置: {player.currentTile.tileName}";
        }

        // 
        UpdateCashDisplay(player.cash);

        // ===  ===
        if (ItemPanelUI.Instance != null)
        {
            ItemPanelUI.Instance.UpdateItemDisplay();
        }
        // ===  ===
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

        UnityEngine.Debug.Log($"切换UI: {uiType}");
    }

    public void SwitchToMenuUI() => SwitchUI(UIType.Menu);
    public void SwitchToGameUI() => SwitchUI(UIType.Game);
    public void SwitchToPauseUI() => SwitchUI(UIType.Pause);
    public void SwitchToGameOverUI() => SwitchUI(UIType.GameOver);

    public void ShowPropertyPurchasePanel(BoardTile property, Player player)
    {
        if (propertyPurchasePanel == null)
        {
            UnityEngine.Debug.LogWarning("地产购买面板为空");
            return;
        }

        if (!propertyPurchasePanel.activeSelf)
        {
            propertyPurchasePanel.SetActive(true);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.UIOpen);
        }

        Text propertyNameText = propertyPurchasePanel.transform.Find("PropertyName").GetComponent<Text>();
        Text priceText = propertyPurchasePanel.transform.Find("Price").GetComponent<Text>();
        Button buyButton = propertyPurchasePanel.transform.Find("BuyButton").GetComponent<Button>();
        Button cancelButton = propertyPurchasePanel.transform.Find("CancelButton").GetComponent<Button>();

        if (propertyNameText != null)
            propertyNameText.text = property.tileName;

        if (priceText != null)
            priceText.text = $"价格: {property.propertyPrice} 金币";

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
            if (propertyPurchasePanel.activeSelf && SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.UIClose);

            propertyPurchasePanel.SetActive(false);
        }
    }

    public void ShowGameOverPanel(string playerName, bool isWinner = true)
    {
        if (gameOverPanel == null)
        {
            UnityEngine.Debug.LogError("UIManager: 请在Inspector中设置GameOverPanel");
            return;
        }

        gameOverPanel.SetActive(true);

        int diceCount = 0;
        int roundCount = 0;
        
        if (GameManager.Instance != null)
        {
            diceCount = GameManager.Instance.DiceRollCount;
            roundCount = GameManager.Instance.CurrentRound;
        }
        
        int score = roundCount * 100 + diceCount + 10;

        UnityEngine.Debug.Log($"玩家={playerName}, 是否获胜={isWinner}, 回合数={roundCount}, 骰子次数={diceCount}, 分数={score}");

        int maxRounds = GameManager.Instance != null ? GameManager.Instance.maxRounds : roundCount;

        SetText("ResultText", isWinner ? $"{playerName} 获胜!" : $"{playerName} 失败");
        SetText("RoundText", $"轮数：{roundCount}/{maxRounds}");
        SetText("DiceText", $"骰子投掷次数: {diceCount}");
        SetText("ScoreText", $"最终得分: {score}");

        Button restartButton = FindRestartButton();
        if (restartButton != null)
        {
            UnityEngine.Debug.Log("找到重新开始按钮");
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        else
        {
            UnityEngine.Debug.LogError("未找到重新开始按钮");
        }
    }

    private void SetText(string objectName, string text)
    {
        Transform trans = gameOverPanel.transform.Find(objectName);
        if (trans == null)
        {
            UnityEngine.Debug.LogError($"未找到对象: {objectName}");
            return;
        }

        Text textComp = trans.GetComponent<Text>();
        if (textComp != null)
        {
            textComp.text = text;
            UnityEngine.Debug.Log($"设置 {objectName} = {text} (Text)");
            return;
        }

        TextMeshProUGUI tmpComp = trans.GetComponent<TextMeshProUGUI>();
        if (tmpComp != null)
        {
            tmpComp.text = text;
            UnityEngine.Debug.Log($"设置 {objectName} = {text} (TextMeshProUGUI)");
            return;
        }

        UnityEngine.Debug.LogError($"{objectName} 没有附加 Text 或 TextMeshProUGUI 组件");
    }

    private Button FindRestartButton()
    {
        if (gameOverPanel == null)
        {
            UnityEngine.Debug.LogError("gameOverPanel 为空");
            return null;
        }

        Button button = gameOverPanel.transform.Find("RestartButton").GetComponent<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"找到按钮: RestartButton");
            return button;
        }

        button = gameOverPanel.transform.Find("Button").GetComponent<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"找到按钮: Button");
            return button;
        }

        button = gameOverPanel.GetComponentInChildren<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"通过GetComponentInChildren找到按钮: {button.name}");
            return button;
        }

        UnityEngine.Debug.LogError("在 gameOverPanel 中未找到按钮");
        return null;
    }

    private void OnRestartButtonClicked()
    {
        UnityEngine.Debug.Log("=== 重新开始游戏 ===");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            UnityEngine.Debug.Log("隐藏游戏结束面板");
        }
        else
        {
            UnityEngine.Debug.LogError("gameOverPanel 为空");
        }
        
        SwitchToGameUI();
        UnityEngine.Debug.Log("切换到游戏UI");
        
        if (GameManager.Instance != null)
        {
            UnityEngine.Debug.Log("调用 GameManager");
            GameManager.Instance.RestartFromGameOver();
        }
        else
        {
            UnityEngine.Debug.LogError("GameManager.Instance 为空");
        }
        
        UnityEngine.Debug.Log("=== 完成 ===");
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
            UnityEngine.Debug.LogWarning("玩家信息预制件为空");
            return;
        }

        GameObject playerInfoObj = Instantiate(playerInfoPrefab, mainCanvas.transform);
        playerInfoObj.name = $"PlayerInfo_{player.playerName}";

        Text nameText = playerInfoObj.transform.Find("PlayerName").GetComponent<Text>();
        Text cashText = playerInfoObj.transform.Find("Cash").GetComponent<Text>();
        Image colorImage = playerInfoObj.transform.Find("PlayerColor").GetComponent<Image>();

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
            infoUI.playerNameText.text = $"{infoUI.player.playerName}";

        if (infoUI.cashText != null)
            infoUI.cashText.text = $"{infoUI.player.cash} ";

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
        if (infoToastPanel != null && infoToastText != null)
        {
            EnqueueToast(message, duration);
            return;
        }

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

    private void EnqueueToast(string message, float duration, bool isAnnouncement = false)
    {
        // 避免同一条提示重复入队堆叠，导致面板迟迟不按时消失（如反复点击买不起的地块）
        if (message == currentToastMessage)
        {
            return;
        }
        foreach (ToastMessage queued in toastQueue)
        {
            if (queued.message == message)
            {
                return;
            }
        }

        toastQueue.Enqueue(new ToastMessage(message, duration, isAnnouncement));
        
        if (!isToastPlaying)
        {
            StartCoroutine(ProcessToastQueue());
        }
    }

    private System.Collections.IEnumerator ProcessToastQueue()
    {
        isToastPlaying = true;
        
        while (toastQueue.Count > 0)
        {
            ToastMessage msg = toastQueue.Dequeue();
            if (msg.isAnnouncement)
            {
                yield return StartCoroutine(ShowAnnouncementWithDelay(msg.message, msg.duration));
            }
            else
            {
                yield return StartCoroutine(ShowInfoToastWithDelay(msg.message, msg.duration));
            }
            yield return new WaitForSeconds(toastInterval);
        }
        
        isToastPlaying = false;
    }

    private System.Collections.IEnumerator ShowAnnouncementWithDelay(string message, float duration)
    {
        if (turnAnnouncePanel == null || turnAnnounceText == null)
        {
            yield break;
        }

        CancelInvoke(nameof(HideTurnAnnouncement));
        turnAnnounceText.text = message;
        turnAnnouncePanel.SetActive(true);
        turnAnnouncePanel.transform.SetAsLastSibling();

        yield return new WaitForSeconds(duration);

        if (turnAnnouncePanel != null)
        {
            turnAnnouncePanel.SetActive(false);
        }
    }

    private System.Collections.IEnumerator ShowInfoToastWithDelay(string message, float duration)
    {
        if (hideInfoToastCoroutine != null)
        {
            StopCoroutine(hideInfoToastCoroutine);
        }

        infoToastText.text = message;
        infoToastPanel.SetActive(true);
        infoToastPanel.transform.SetAsLastSibling();
        currentToastMessage = message;

        yield return new WaitForSeconds(duration);
        
        if (infoToastPanel != null && toastQueue.Count == 0)
        {
            infoToastPanel.SetActive(false);
            currentToastMessage = null;
        }
        
        hideInfoToastCoroutine = null;
    }

    private void ShowInfoToast(string message, float duration = 2f)
    {
        if (hideInfoToastCoroutine != null)
        {
            StopCoroutine(hideInfoToastCoroutine);
        }

        infoToastText.text = message;
        infoToastPanel.SetActive(true);
        infoToastPanel.transform.SetAsLastSibling();

        hideInfoToastCoroutine = StartCoroutine(HideInfoToastAfterDelay(duration));
    }

    // 
    public void HideInfoToast()
    {
        if (hideInfoToastCoroutine != null)
        {
            StopCoroutine(hideInfoToastCoroutine);
            hideInfoToastCoroutine = null;
        }
        
        if (infoToastPanel != null)
        {
            infoToastPanel.SetActive(false);
        }
        currentToastMessage = null;
    }

    private System.Collections.IEnumerator HideInfoToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (infoToastPanel != null)
        {
            infoToastPanel.SetActive(false);
        }
        currentToastMessage = null;
        hideInfoToastCoroutine = null;
    }

    public void ShowTurnAnnouncement(string msg)
    {
        if (turnAnnouncePanel == null || turnAnnounceText == null)
            return;

        // 纳入统一的提示队列，和普通 toast 按顺序显示、互不重叠
        EnqueueToast(msg, announceDuration, true);
    }
    private void HideTurnAnnouncement()
    {
        if (turnAnnouncePanel != null)
            turnAnnouncePanel.SetActive(false);
    }

    // UI
    public void ShowBuildingUpgradeUI(BoardTile tile, Player player)
    {
        if (buildingUpgradePanel == null)
        {
            UnityEngine.Debug.LogWarning("建筑升级面板为空");
            return;
        }

        upgradeSelectedTile = tile;
        upgradeSelectedPlayer = player;

        // 
        buildingUpgradePanel.SetActive(true);
        buildingUpgradePanel.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);

        // UI
        UpdateUpgradePanelInfo();

        // 
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

        // 
        SetRollDiceButtonInteractable(false);
    }

    // 
    private void UpdateUpgradePanelInfo()
    {
        if (upgradeSelectedTile == null || upgradeSelectedPlayer == null) return;

        if (upgradeCostText != null)
        {
            int upgradeCost = upgradeSelectedTile.GetUpgradeCost();
            upgradeCostText.text = $"升级费用: {upgradeCost} 金币";

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
            nextLevelText.text = $"下一等级: {nextLevel}";
        }

        if (upgradeSelectedTile.currentBuildingData != null)
        {
            BuildingData buildingData = upgradeSelectedTile.currentBuildingData;

            string functionDesc = GetBuildingFunctionDescription(buildingData, upgradeSelectedTile.buildingLevel, upgradeSelectedTile);
            ShowToast($"功能: {functionDesc}", 3f);

            if (buildingData.nextLevelBuilding != null)
            {
                string nextFunctionDesc = GetBuildingFunctionDescription(buildingData.nextLevelBuilding, upgradeSelectedTile.buildingLevel + 1, upgradeSelectedTile);
                UnityEngine.Debug.Log($"升级后功能: {nextFunctionDesc}");
            }
        }

        BuildingData nextBuilding = upgradeSelectedTile.GetNextUpgradeBuilding();
        if (nextBuilding != null)
        {
            if (!upgradeSelectedTile.CheckScaleForUpgrade(nextBuilding.requiredScale))
            {
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = false;
                    ShowToast($"需要地块等级: {(int)nextBuilding.requiredScale}", 2f);
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

    // 
    private string GetBuildingFunctionDescription(BuildingData buildingData, int level, BoardTile tile = null)
    {
        if (buildingData == null) return "";

        switch (buildingData.functionType)
        {
            case BuildingData.BuildingFunctionType.Income:
                int income;
                if (tile != null)
                {
                    int baseIncome = buildingData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
                    Player currentPlayer = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
                    income = currentPlayer != null ? currentPlayer.GetIncomeWithMultiplier(baseIncome) : baseIncome;
                }
                else
                {
                    int baseIncome = buildingData.GetIncomeAmount(level);
                    Player currentPlayer = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
                    income = currentPlayer != null ? currentPlayer.GetIncomeWithMultiplier(baseIncome) : baseIncome;
                }
                
                string multiplierInfo = "";
                if (GameManager.Instance.currentPlayer != null && BuffSystem.Instance != null)
                {
                    float multiplier = BuffSystem.Instance.GetIncomeMultiplier(GameManager.Instance.currentPlayer);
                    if (multiplier > 1.0f)
                    {
                        multiplierInfo = $" ({multiplier:F1})";
                    }
                }
                return $": {income}  {multiplierInfo}";

            case BuildingData.BuildingFunctionType.Buff:
                float buffValue = buildingData.GetBuffValue(level);
                string buffName = GetBuffEffectName(buildingData.buffEffect);
                if (buildingData.buffDuration > 0)
                {
                    return $"{buffName}: +{buffValue * 100}% ({buildingData.buffDuration})";
                }
                else
                {
                    return $"{buffName}: +{buffValue * 100}% ()";
                }

            case BuildingData.BuildingFunctionType.Mixed:
                int mixedIncome;
                if (tile != null)
                {
                    int baseMixedIncome = buildingData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
                    Player currentMixedPlayer = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
                    mixedIncome = currentMixedPlayer != null ? currentMixedPlayer.GetIncomeWithMultiplier(baseMixedIncome) : baseMixedIncome;
                }
                else
                {
                    int baseMixedIncome = buildingData.GetIncomeAmount(level);
                    Player currentMixedPlayer = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
                    mixedIncome = currentMixedPlayer != null ? currentMixedPlayer.GetIncomeWithMultiplier(baseMixedIncome) : baseMixedIncome;
                }
                buffValue = buildingData.GetBuffValue(level);
                buffName = GetBuffEffectName(buildingData.buffEffect);
                
                string mixedMultiplierInfo = "";
                if (GameManager.Instance.currentPlayer != null && BuffSystem.Instance != null)
                {
                    float mixedMultiplier = BuffSystem.Instance.GetIncomeMultiplier(GameManager.Instance.currentPlayer);
                    if (mixedMultiplier > 1.0f)
                    {
                        mixedMultiplierInfo = $" ({mixedMultiplier:F1})";
                    }
                }
                return $": {mixedIncome}  {mixedMultiplierInfo} + {buffName}: +{buffValue * 100}%";

            case BuildingData.BuildingFunctionType.DiceEven:
                return $": {buildingData.GetDiceRuleDescription()}";

            case BuildingData.BuildingFunctionType.Appreciation:
                return $": +{buildingData.appreciationPerRound}";

            default:
                return "";
        }
    }

    private string GetBuffEffectName(BuildingData.BuffEffect effect)
    {
        switch (effect)
        {
            case BuildingData.BuffEffect.MoveSpeedBoost: return "";
            case BuildingData.BuffEffect.DiceBoost: return "";
            case BuildingData.BuffEffect.IncomeMultiplier: return "";
            case BuildingData.BuffEffect.DefenseBoost: return "";
            case BuildingData.BuffEffect.LuckBoost: return "";
            case BuildingData.BuffEffect.AllIncomeBoost: return "";
            default: return "";
        }
    }

    // 
    private void OnUpgradeButtonClicked()
    {
        if (upgradeSelectedTile == null || upgradeSelectedPlayer == null) return;

        if (upgradeSelectedTile.UpgradeBuilding(upgradeSelectedPlayer))
        {
            ShowToast("升级成功！", 2f);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingUpgraded);

            UpdateUpgradePanelInfo();
            UpdateCurrentPlayerInfo(upgradeSelectedPlayer);

            if (GameManager.Instance != null && GameManager.Instance.currentPlayer == upgradeSelectedPlayer)
            {
                GameManager.Instance.UpdateUI();
            }

            if (!upgradeSelectedTile.CanUpgradeBuilding(upgradeSelectedPlayer))
            {
                HideBuildingUpgradeUI();
            }
        }
        else
        {
            ShowToast("升级失败!", 2f);
        }
    }

    // UI
    public void HideBuildingUpgradeUI()
    {
        if (buildingUpgradePanel != null)
        {
            if (buildingUpgradePanel.activeSelf && SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.UIClose);

            buildingUpgradePanel.SetActive(false);
        }

        upgradeSelectedTile = null;
        upgradeSelectedPlayer = null;

        // 
        SetRollDiceButtonInteractable(true);
    }

    public void ShowEventPanel(EventData eventData, Player player = null)
    {
        if (eventPanel != null)
        {
            SetRollDiceButtonInteractable(false);
            eventPanel.ShowEvent(eventData, player);
        }
        else
        {
            Debug.LogWarning("EventPanel is not assigned in UIManager!");
        }
    }

    /// <summary>
    /// Toast
    /// </summary>
    public static void ShowToastStatic(string message, float duration = 2f)
    {
        if (Instance != null)
        {
            Instance.ShowToast(message, duration);
        }
    }

    void OnDestroy()
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.RemoveAllListeners();
        }
    }
}
