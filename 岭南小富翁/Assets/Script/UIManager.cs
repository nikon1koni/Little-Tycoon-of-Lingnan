﻿﻿﻿﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using static BoardTile;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 单例实例
    public static UIManager Instance;

    [Header("UI 预制体 (如果需要动态创建)")]
    public GameObject rollDiceButtonPrefab;
    public GameObject playerInfoPrefab;
    public GameObject propertyPanelPrefab;

    [Header("UI 管理器引用")]
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

    [Header("UI 按钮")]
    public Button rollDiceButton;
    public Text diceAnimationText;

    [Header("玩家信息UI列表")]
    public List<PlayerInfoUI> playerInfoUIs = new List<PlayerInfoUI>();

    [Header("位置设置")]
    public Vector2 diceButtonPosition = new Vector2(-20, -10); // 掷骰子按钮位置

    [Header("建筑选择UI")]
    public GameObject buildingSelectionPanel;
    public Button[] buildingButtons = new Button[4];
    public TextMeshProUGUI tileInfoText;
    public Button closeBuildingPanelButton;
    public Text selectedBuildingText;
    public Image selectedBuildingImage;
    public Text buildingPriceText;

    [Header("可用建筑列表")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("建筑升级UI")]
    public GameObject buildingUpgradePanel;
    public Button upgradeButton;
    public Text upgradeCostText;
    public Text currentLevelText;
    public Text nextLevelText;
    public Image upgradeBuildingImage;
    public Button closeUpgradePanelButton;

    [Header("提示Toast")]
    public GameObject persistentToastPanel;
    public Text persistentToastText;
    public Vector2 toastPosition = new Vector2(20, 20);

    [Header("现金显示")]
    [SerializeField] private GameObject cashDisplayPanel;
    [SerializeField] private TextMeshProUGUI cashText;

    [Header("压力系统UI")]
    public GameObject pressureSystemPanel;
    public TextMeshProUGUI diceRollCountText;
    public TextMeshProUGUI currentRoundText;

    [Header("回合公告UI")]
    public GameObject turnAnnouncePanel;
    public TextMeshProUGUI turnAnnounceText;
    public float announceDuration = 2.5f;

    [Header("事件UI")]
    public EventPanel eventPanel;

    [Header("信息提示UI")]
    public GameObject infoToastPanel;
    public TextMeshProUGUI infoToastText;
    private Coroutine hideInfoToastCoroutine;

    public TextMeshProUGUI CashText => cashText;

    // 建筑选择相关
    private bool isBuildingSelected = false;
    private GameObject activePersistentToast;
    private List<int> activeBuildingButtonIndices = new List<int>();

    // 升级模式状态
    private BoardTile upgradeSelectedTile = null;
    private Player upgradeSelectedPlayer = null;

    // 建筑选择状态
    private BuildingData selectedBuildingData = null;
    private BoardTile selectedBoardTile = null;
    private Player currentBuildingPlayer = null;

    // 格子高亮
    private Dictionary<BoardTile, Color> originalTileColors = new Dictionary<BoardTile, Color>();
    private List<BoardTile> highlightableTiles = new List<BoardTile>();

    // 当前UI状态
    private UIType currentUIType = UIType.Game;

    // UI 类型枚举
    public enum UIType
    {
        Menu,
        Game,
        Pause,
        GameOver
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

    void Update()
    {
        // 检查ESC键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 检查是否在升级模式
            if (BuildingDataConfig.Instance != null && BuildingDataConfig.Instance.IsUpgradeModeActive())
            {
                // 退出升级模式
                BuildingDataConfig.Instance.ExitUpgradeMode();
                return; // 返回，不要执行其他ESC逻辑
            }
            else if (isBuildingSelected)
            {
                // 取消建筑选择
                OnCancelBuildingSelection();
            }
            else if (buildingSelectionPanel != null && buildingSelectionPanel.activeSelf)
            {
                // 关闭建筑选择面板
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
        UnityEngine.Debug.Log("=== 初始化UI ===");
        InitializeUI();

        // 确保建筑选择面板默认隐藏
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            UnityEngine.Debug.Log("UIManager: 建筑选择面板已隐藏");
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: 建筑选择面板未在Inspector中设置，请确保已设置引用");
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

        // === 更新初始玩家现金显示 ===
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            UpdateCashDisplay(GameManager.Instance.currentPlayer.cash);
        }
        // === 更新完成 ===

        UnityEngine.Debug.Log("UI初始化完成");
    }

    // === 更新现金显示 ===
    public void UpdateCashDisplay(int cashAmount)
    {
        if (cashText != null)
        {
            cashText.text = $"{cashAmount}";
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: 现金文本未设置，请在Inspector中设置'Cash Text'引用");
        }
    }

    // === 更新压力系统UI ===
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
            diceRollCountText.text = $"骰子: {diceInCurrentRound}/6";
        }

        if (currentRoundText != null)
        {
            currentRoundText.text = $"回合: {currentRound}";
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
                UnityEngine.Debug.Log("UIManager: 找到压力系统面板");
            }
        }

        if (diceRollCountText == null && pressureSystemPanel != null)
        {
            Transform diceTrans = pressureSystemPanel.transform.Find("DiceRollCountText");
            if (diceTrans != null)
            {
                diceRollCountText = diceTrans.GetComponent<TextMeshProUGUI>();
                UnityEngine.Debug.Log("UIManager: 找到骰子计数文本");
            }
        }

        if (currentRoundText == null && pressureSystemPanel != null)
        {
            Transform roundTrans = pressureSystemPanel.transform.Find("CurrentRoundText");
            if (roundTrans != null)
            {
                currentRoundText = roundTrans.GetComponent<TextMeshProUGUI>();
                UnityEngine.Debug.Log("UIManager: 找到回合文本");
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
                UnityEngine.Debug.Log("UIManager: 找到游戏结束面板");
            }
            else
            {
                UnityEngine.Debug.LogWarning("UIManager: 未找到GameOverPanel，请在Inspector中设置或创建一个名为GameOverPanel的游戏对象");
            }
        }
    }

    // === 显示建筑选择UI面板，传入可建造的格子和当前玩家 ===
    public void ShowBuildingSelectionUI(BoardTile buildableTile, Player player)
    {
        // 步骤1: 设置当前选中的格子和玩家
        selectedBoardTile = buildableTile;
        currentBuildingPlayer = player;

        UnityEngine.Debug.Log($"UIManager: 显示建筑选择UI，格子规模: {buildableTile.tileScale}");

        // 1. 隐藏其他可能打开的UI
        HidePropertyPurchasePanel();

        // 2. 确保建筑选择面板存在并显示
        if (buildingSelectionPanel == null)
        {
            UnityEngine.Debug.LogError("UIManager: 建筑选择面板未设置");
            return;
        }

        buildingSelectionPanel.SetActive(true);
        buildingSelectionPanel.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);

        SetNonButtonRaycastTargets(false);

        if (tileInfoText != null)
        {
            tileInfoText.text = $"{buildableTile.tileName} - 价格: {buildableTile.propertyPrice} 金币";
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
                UnityEngine.Debug.Log("UIManager: 关闭按钮被点击");
                HideBuildingSelectionUI();
            });
            
            UnityEngine.Debug.Log("UIManager: 关闭按钮已配置");
        }
        else
        {
            UnityEngine.Debug.LogError("UIManager: 未找到关闭按钮");
        }

        // 8. 禁用掷骰子按钮
        SetRollDiceButtonInteractable(false);
    }

    // === 隐藏建筑选择UI面板，同时可以选择是否保持按钮状态 ===
    public void HideBuildingSelectionUI(bool keepButtons = false)
    {
        UnityEngine.Debug.Log($"隐藏建筑选择UI，保持按钮: {keepButtons}");

        // 清理UI状态
        ClearTileHighlights();
        HidePersistentToast();

        // 隐藏建筑选择面板
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

        // 清理选择状态
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

        // 恢复掷骰子按钮
        SetRollDiceButtonInteractable(true);
    }

    // 查找关闭按钮
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
            UnityEngine.Debug.Log($"UIManager: 找到关闭按钮，名称: '{closeBtnTrans.name}'");
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

    // 清理建筑按钮
    private void ClearBuildingButtons()
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] != null)
            {
                buildingButtons[i].onClick.RemoveAllListeners();
                buildingButtons[i].gameObject.SetActive(false);

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

    // 取消建筑选择
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
        UnityEngine.Debug.Log("返回建筑选择面板");
    }

    // 配置建筑按钮
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
                        nameTmp.text = $"{building.buildingName}\n{building.purchasePrice}金币";
                    }
                }

                buildingButtons[i].onClick.RemoveAllListeners();
                BuildingData currentBuilding = building;
                buildingButtons[i].onClick.AddListener(() => OnBuildingSelected(currentBuilding));

                activeBuildingButtonIndices.Add(i);
            }
            else
            {
                buildingButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 建筑被选中
    private void OnBuildingSelected(BuildingData building)
    {
        UnityEngine.Debug.Log($"选中建筑: {building.buildingName}, 价格: {building.purchasePrice}");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);

        selectedBuildingData = building;
        isBuildingSelected = true;

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

        // 3. 显示提示信息
        ShowPersistentToast($"选中: {building.buildingName}\n点击可建造的格子放置，按ESC取消");

        // 4. 高亮可建造的格子
        HighlightPlaceableTiles(currentBuildingPlayer, (int)building.requiredScale);

        UnityEngine.Debug.Log("等待放置建筑");
    }

    private void ShowPersistentToast(string message)
    {
        // 隐藏已存在的Toast
        HidePersistentToast();

        // 创建新的持续Toast
        if (persistentToastPanel != null)
        {
            activePersistentToast = Instantiate(persistentToastPanel, mainCanvas.transform);
            activePersistentToast.name = "PersistentToast";

            // 设置文本
            Text toastText = activePersistentToast.GetComponentInChildren<Text>();
            if (toastText != null)
            {
                toastText.text = message;
            }

            // 设置位置 - 左下角
            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            activePersistentToast.SetActive(true);
        }
        else
        {
            // 如果没有预制体，创建一个简单的Toast
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

            // 设置UI布局
            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            // 设置文本位置
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
            ShowToast("没有可放置建筑的格子，请检查是否有足够的规模", 2f);
        }
    }

    public bool IsTileHighlighted(BoardTile tile)
    {
        return highlightableTiles.Contains(tile);
    }

    // 检查格子是否可放置
    private bool IsTilePlaceable(BoardTile tile, Player player, int requiredScale)
    {
        return (tile.tileType == BoardTile.TileType.Buildable ||
                tile.tileType == BoardTile.TileType.BuildingSite) &&
               tile.isBuildable &&
               tile.currentBuilding == null &&
               tile.tileScale >= requiredScale &&
               (tile.ownerPlayer == null || tile.ownerPlayer == player);
    }

    // 添加格子点击事件
    private void AddTileClickHandler(BoardTile tile)
    {
        EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => OnTileClickedForPlacement(tile));

        trigger.triggers.Add(entry);
    }

    // 格子被点击用于放置建筑
    public void OnTileClickedForPlacement(BoardTile tile)
    {
        if (selectedBuildingData == null || currentBuildingPlayer == null)
            return;

        if (currentBuildingPlayer.cash < selectedBuildingData.purchasePrice)
        {
            ShowToast("金币不足，无法购买建筑", 2f);
            return;
        }

        if (!IsTilePlaceable(tile, currentBuildingPlayer, (int)selectedBuildingData.requiredScale))
        {
            ShowToast("该格子无法放置此建筑", 2f);
            return;
        }

        if (PurchaseAndPlaceBuilding(tile, selectedBuildingData, currentBuildingPlayer))
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingPlaced);

            ClearTileHighlights();
            HidePersistentToast();
            
            // 继续选择建筑
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
            
            ShowToast("建筑放置成功！可继续选择其他建筑", 2f);
        }
    }

    // 购买并放置建筑
    private bool PurchaseAndPlaceBuilding(BoardTile tile, BuildingData buildingData, Player player)
    {
        int purchasePrice = buildingData.purchasePrice;

        if (!player.PayCash(purchasePrice))
            return false;

        tile.ownerPlayer = player;
        tile.SetBuildingData(buildingData, buildingData.buildingLevel);
        tile.tileType = BoardTile.TileType.BuildingSite;

        if (buildingData.buildingPrefab != null)
        {
            GameObject buildingObj = Instantiate(buildingData.buildingPrefab, tile.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            buildingObj.transform.SetParent(tile.transform);
            tile.currentBuilding = buildingObj;
        }

        return true;
    }

    // 清理格子高亮
    private void ClearTileHighlights()
    {
        foreach (BoardTile tile in highlightableTiles)
        {
            MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    // 起点格子不要修改颜色
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

    // 从BuildingData获取BuildingType
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        // 这是一个简单的判断逻辑
        // 你可以根据实际需求修改
        if (data.buildingName.Contains("小") || data.buildingName.Contains("Small"))
            return BoardTile.BuildingType.SmallHouse;
        else if (data.buildingName.Contains("中") || data.buildingName.Contains("Medium"))
            return BoardTile.BuildingType.MediumHouse;
        else if (data.buildingName.Contains("大") || data.buildingName.Contains("Large"))
            return BoardTile.BuildingType.LargeHouse;
        else
            return BoardTile.BuildingType.None;
    }

    // ================= 辅助方法 =================

    void EnsureCanvasExists()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                UnityEngine.Debug.LogWarning("场景中没有Canvas，正在创建...");
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

        UnityEngine.Debug.Log("创建Canvas完成");
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
            UnityEngine.Debug.Log("掷骰子按钮创建完成");
        }
    }

    // 掷骰子按钮点击事件
    public void OnRollDiceButtonClicked()
    {
        UnityEngine.Debug.Log("UIManager: 掷骰子按钮被点击");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);

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
            currentPlayerText.text = $"当前玩家: {player.playerName}";
        }

        if (playerCashText != null)
        {
            playerCashText.text = $"金币: {player.cash}";
        }

        if (currentTileText != null && player.currentTile != null)
        {
            currentTileText.text = $"位置: {player.currentTile.tileName}";
        }

        // === 更新现金显示 ===
        UpdateCashDisplay(player.cash);
        // === 更新完成 ===
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
            UnityEngine.Debug.LogWarning("地产购买面板未设置");
            return;
        }

        if (!propertyPurchasePanel.activeSelf)
        {
            propertyPurchasePanel.SetActive(true);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.UIOpen);
        }

        Text propertyNameText = propertyPurchasePanel.transform.Find("PropertyName")?.GetComponent<Text>();
        Text priceText = propertyPurchasePanel.transform.Find("Price")?.GetComponent<Text>();
        Button buyButton = propertyPurchasePanel.transform.Find("BuyButton")?.GetComponent<Button>();
        Button cancelButton = propertyPurchasePanel.transform.Find("CancelButton")?.GetComponent<Button>();

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
            UnityEngine.Debug.LogError("UIManager: 游戏结束面板未在Inspector中设置");
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

        UnityEngine.Debug.Log($"显示游戏结束面板: 玩家={playerName}, 胜利={isWinner}, 回合={roundCount}, 骰子次数={diceCount}, 得分={score}");

        SetText("ResultText", isWinner ? $"{playerName} 获胜!" : $"{playerName} 失败");
        SetText("RoundText", $"游戏回合: {roundCount}");
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

        UnityEngine.Debug.LogError($"{objectName} 没有 Text 或 TextMeshProUGUI 组件");
    }

    private Button FindRestartButton()
    {
        if (gameOverPanel == null)
        {
            UnityEngine.Debug.LogError("gameOverPanel 为空，无法查找按钮");
            return null;
        }

        Button button = gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"找到按钮: RestartButton");
            return button;
        }

        button = gameOverPanel.transform.Find("Button")?.GetComponent<Button>();
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

        UnityEngine.Debug.LogError("在 gameOverPanel 中没有找到任何 Button 组件");
        return null;
    }

    private void OnRestartButtonClicked()
    {
        UnityEngine.Debug.Log("=== 重新开始游戏 ===");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            UnityEngine.Debug.Log("游戏结束面板已隐藏");
        }
        else
        {
            UnityEngine.Debug.LogError("gameOverPanel 为空");
        }
        
        SwitchToGameUI();
        UnityEngine.Debug.Log("切换到游戏UI");
        
        if (GameManager.Instance != null)
        {
            UnityEngine.Debug.Log("调用GameManager重新开始");
            GameManager.Instance.RestartFromGameOver();
        }
        else
        {
            UnityEngine.Debug.LogError("GameManager.Instance 为空");
        }
        
        UnityEngine.Debug.Log("=== 重新开始完成 ===");
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
            UnityEngine.Debug.LogWarning("玩家信息预制体未设置");
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
            infoUI.cashText.text = $"{infoUI.player.cash} 金币";

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
        // 优先使用信息提示UI
        if (infoToastPanel != null && infoToastText != null)
        {
            ShowInfoToast(message, duration);
            return;
        }

        // 否则创建简单的Toast
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

    // 使用信息提示UI显示
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

    // 隐藏信息提示面板
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
    }

    private System.Collections.IEnumerator HideInfoToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (infoToastPanel != null)
        {
            infoToastPanel.SetActive(false);
        }
        hideInfoToastCoroutine = null;
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

    // 显示建筑升级UI
    public void ShowBuildingUpgradeUI(BoardTile tile, Player player)
    {
        if (buildingUpgradePanel == null)
        {
            UnityEngine.Debug.LogWarning("建筑升级面板未设置");
            return;
        }

        upgradeSelectedTile = tile;
        upgradeSelectedPlayer = player;

        // 显示面板
        buildingUpgradePanel.SetActive(true);
        buildingUpgradePanel.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);

        // 更新UI信息
        UpdateUpgradePanelInfo();

        // 配置按钮
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

        // 禁用掷骰子按钮
        SetRollDiceButtonInteractable(false);
    }

    // 更新升级面板信息
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
            nextLevelText.text = $"下一等级: {nextLevel} 级";
        }

        // 显示建筑效果描述
        if (upgradeSelectedTile.currentBuildingData != null)
        {
            // 获取当前建筑效果
            BuildingData buildingData = upgradeSelectedTile.currentBuildingData;

            // 显示效果描述
            string functionDesc = GetBuildingFunctionDescription(buildingData, upgradeSelectedTile.buildingLevel, upgradeSelectedTile);
            ShowToast($"当前建筑效果: {functionDesc}", 3f);

            // 显示下一级效果
            if (buildingData.nextLevelBuilding != null)
            {
                string nextFunctionDesc = GetBuildingFunctionDescription(buildingData.nextLevelBuilding, upgradeSelectedTile.buildingLevel + 1, upgradeSelectedTile);
                UnityEngine.Debug.Log($"下一级效果: {nextFunctionDesc}");
            }
        }

        // 检查是否可以升级
        BuildingData nextBuilding = upgradeSelectedTile.GetNextUpgradeBuilding();
        if (nextBuilding != null)
        {
            if (!upgradeSelectedTile.CheckScaleForUpgrade(nextBuilding.requiredScale))
            {
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = false;
                    ShowToast($"升级需要规模 {(int)nextBuilding.requiredScale}，当前规模不足", 2f);
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
    private string GetBuildingFunctionDescription(BuildingData buildingData, int level, BoardTile tile = null)
    {
        if (buildingData == null) return "无描述";

        switch (buildingData.functionType)
        {
            case BuildingData.BuildingFunctionType.Income:
                int income;
                if (tile != null)
                {
                    income = buildingData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
                }
                else
                {
                    income = buildingData.GetIncomeAmount(level);
                }
                return $"每回合收入: {income} 金币";

            case BuildingData.BuildingFunctionType.Buff:
                float buffValue = buildingData.GetBuffValue(level);
                string buffName = GetBuffEffectName(buildingData.buffEffect);
                if (buildingData.buffDuration > 0)
                {
                    return $"{buffName}: +{buffValue * 100}% (持续{buildingData.buffDuration}回合)";
                }
                else
                {
                    return $"{buffName}: +{buffValue * 100}% (永久)";
                }

            case BuildingData.BuildingFunctionType.Mixed:
                if (tile != null)
                {
                    income = buildingData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
                }
                else
                {
                    income = buildingData.GetIncomeAmount(level);
                }
                buffValue = buildingData.GetBuffValue(level);
                buffName = GetBuffEffectName(buildingData.buffEffect);
                return $"收入: {income} 金币 + {buffName}: +{buffValue * 100}%";

            default:
                return "未知";
        }
    }

    // 获取Buff效果名称
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

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingUpgraded);

            // 更新UI
            UpdateUpgradePanelInfo();
            UpdateCurrentPlayerInfo(upgradeSelectedPlayer);

            // 更新压力系统UI
            if (GameManager.Instance != null && GameManager.Instance.currentPlayer == upgradeSelectedPlayer)
            {
                GameManager.Instance.UpdateUI();
            }

            // 检查是否还能继续升级
            if (!upgradeSelectedTile.CanUpgradeBuilding(upgradeSelectedPlayer))
            {
                HideBuildingUpgradeUI();
            }
        }
        else
        {
            ShowToast("升级失败，请检查金币或建筑等级", 2f);
        }
    }

    // 隐藏建筑升级UI
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

        // 恢复掷骰子按钮
        SetRollDiceButtonInteractable(true);
    }

    public void ShowEventPanel(EventData eventData)
    {
        if (eventPanel != null)
        {
            SetRollDiceButtonInteractable(false);
            eventPanel.ShowEvent(eventData);
        }
        else
        {
            Debug.LogWarning("EventPanel is not assigned in UIManager!");
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
