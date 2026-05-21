using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
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

    [Header("UI ???????")]
    public Canvas mainCanvas;
    public Text diceResultText;
    public Text currentPlayerText;
    public Text playerCashText;
    public Text currentTileText;

    [Header("UI ???")]
    public GameObject gamePanel;
    public GameObject menuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject propertyPurchasePanel;

    [Header("UI ???")]
    public Button rollDiceButton;
    public Text diceAnimationText;

    [Header("??????UI?б?")]
    public List<PlayerInfoUI> playerInfoUIs = new List<PlayerInfoUI>();

    [Header("λ??")]
    public Vector2 diceButtonPosition = new Vector2(-20, -10); // ??????????????

    [Header("??????UI")]
    public GameObject buildingSelectionPanel;
    public Button[] buildingButtons = new Button[4];
    public TextMeshProUGUI tileInfoText;
    public Button closeBuildingPanelButton;
    public Text selectedBuildingText;
    public Image selectedBuildingImage;
    public Text buildingPriceText;

    [Header("????????")]
    public List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("??????UI")]
    public GameObject buildingUpgradePanel;
    public Button upgradeButton;
    public Text upgradeCostText;
    public Text currentLevelText;
    public Text nextLevelText;
    public Image upgradeBuildingImage;
    public Button closeUpgradePanelButton;

    [Header("?????Toast")]
    public GameObject persistentToastPanel;
    public Text persistentToastText;
    public Vector2 toastPosition = new Vector2(20, 20);

    [Header("?????????")]
    [SerializeField] private GameObject cashDisplayPanel;
    [SerializeField] private TextMeshProUGUI cashText;

    [Header("?????UI - ?????")]
    public GameObject pressureSystemPanel;
    public TextMeshProUGUI diceRollCountText;
    public TextMeshProUGUI currentRoundText;

    [Header("??????UI")]
    public GameObject turnAnnouncePanel;
    public TextMeshProUGUI turnAnnounceText;
    public float announceDuration = 2.5f;

    [Header("事件系统UI")]
    public EventPanel eventPanel;

    [Header("提示信息UI")]
    public GameObject infoToastPanel;
    public TextMeshProUGUI infoToastText;
    private Coroutine hideInfoToastCoroutine;

    public TextMeshProUGUI CashText => cashText;

    // 新增状态变量
    private bool isBuildingSelected = false;
    private GameObject activePersistentToast;
    private List<int> activeBuildingButtonIndices = new List<int>();

    // ?????е?????????????
    private BoardTile upgradeSelectedTile = null;
    private Player upgradeSelectedPlayer = null;

    // ?????е????????
    private BuildingData selectedBuildingData = null;
    private BoardTile selectedBoardTile = null;
    private Player currentBuildingPlayer = null;

    // ?????????
    private Dictionary<BoardTile, Color> originalTileColors = new Dictionary<BoardTile, Color>();
    private List<BoardTile> highlightableTiles = new List<BoardTile>();

    // ????????UI????
    private UIType currentUIType = UIType.Game;

    // UI ???????
    public enum UIType
    {
        Menu,
        Game,
        Pause,
        GameOver
    }

    // ??????UI??
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
        // ???ESC??
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isBuildingSelected)
            {
                // ??????????????????????????
                OnCancelBuildingSelection();
            }
            else if (buildingSelectionPanel != null && buildingSelectionPanel.activeSelf)
            {
                // ??????????????У???????
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
        UnityEngine.Debug.Log("=== ?????UI ===");
        InitializeUI();

        // ??????2???????????????????????????????
        // ?????????????????????????????????
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(false);
            UnityEngine.Debug.Log("UIManager: ????? BuildingSelectionPanel ????????????");
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: buildingSelectionPanel δ??Inspector?и??????????????е???????");
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

        // === ????????????????? ===
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            UpdateCashDisplay(GameManager.Instance.currentPlayer.cash);
        }
        // === ???????? ===

        UnityEngine.Debug.Log("UI????????");
    }

    // === ???????????????????????? ===
    public void UpdateCashDisplay(int cashAmount)
    {
        if (cashText != null)
        {
            cashText.text = $"{cashAmount}";
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: cashText ????????????????????????????Inspector??????'Cash Text'??θ????");
        }
    }

    // === ???????????????UI???????? ===
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
                UnityEngine.Debug.Log("UIManager: ??????? pressureSystemPanel");
            }
        }

        if (diceRollCountText == null && pressureSystemPanel != null)
        {
            Transform diceTrans = pressureSystemPanel.transform.Find("DiceRollCountText");
            if (diceTrans != null)
            {
                diceRollCountText = diceTrans.GetComponent<TextMeshProUGUI>();
                UnityEngine.Debug.Log("UIManager: ??????? diceRollCountText");
            }
        }

        if (currentRoundText == null && pressureSystemPanel != null)
        {
            Transform roundTrans = pressureSystemPanel.transform.Find("CurrentRoundText");
            if (roundTrans != null)
            {
                currentRoundText = roundTrans.GetComponent<TextMeshProUGUI>();
                UnityEngine.Debug.Log("UIManager: ??????? currentRoundText");
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
                UnityEngine.Debug.Log("UIManager: ??????? gameOverPanel");
            }
            else
            {
                UnityEngine.Debug.LogWarning("UIManager: δ??? GameOverPanel??????Inspector???????????GameOverPanel????");
            }
        }
    }

    // === ??????????????????UI??????棬??????????????????ù????===
    public void ShowBuildingSelectionUI(BoardTile buildableTile, Player player)
    {
        // ????1?????浱????????????????????
        selectedBoardTile = buildableTile;
        currentBuildingPlayer = player;

        UnityEngine.Debug.Log($"UIManager: ????????????壬?????: {buildableTile.tileScale}");

        // 1. ???????????????UI
        HidePropertyPurchasePanel();

        // 2. ???????
        if (buildingSelectionPanel == null)
        {
            UnityEngine.Debug.LogError("UIManager: BuildingSelectionPanel ????????");
            return;
        }

        buildingSelectionPanel.SetActive(true);
        buildingSelectionPanel.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);

        SetNonButtonRaycastTargets(false);

        if (tileInfoText != null)
        {
            tileInfoText.text = $"{buildableTile.tileName} - 地块价格: {buildableTile.propertyPrice} 金币";
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
                UnityEngine.Debug.Log("UIManager: ??????????");
                HideBuildingSelectionUI();
            });
            
            UnityEngine.Debug.Log("UIManager: ????????????");
        }
        else
        {
            UnityEngine.Debug.LogError("UIManager: ?????CloseButton??");
        }

        // ????????????????????????????????

        // 8. ??????????
        SetRollDiceButtonInteractable(false);
    }

    // === ??????????????????UI?????????????????===
    public void HideBuildingSelectionUI(bool keepButtons = false)
    {
        UnityEngine.Debug.Log($"??????????UI??keepButtons={keepButtons}");

        // ??????UI???
        ClearTileHighlights();
        HidePersistentToast();

        // ???????
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

        // ??????
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

        // ?????????????
        SetRollDiceButtonInteractable(true);
    }

    // ?????????????
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
            UnityEngine.Debug.Log($"UIManager: ???CloseButton, ????='{closeBtnTrans.name}'");
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

    // ??????????
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

    // ??????????
    private void OnCancelBuildingSelection()
    {
        UnityEngine.Debug.Log("??ESC???????????????????");

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
        UnityEngine.Debug.Log("?????????????壬????????????????");
    }

    // ???????????
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

    // ???????????????
    private void OnBuildingSelected(BuildingData building)
    {
        UnityEngine.Debug.Log($"??н???: {building.buildingName}, ???: {building.purchasePrice}");

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

        // 3. ?????????????Toast
        ShowPersistentToast($"?????: {building.buildingName}\n?????????????????????ESC?????");

        // 4. ????????????
        HighlightPlaceableTiles(currentBuildingPlayer, (int)building.requiredScale);

        UnityEngine.Debug.Log("???????????????");
    }

    private void ShowPersistentToast(string message)
    {
        // ??????е?Toast
        HidePersistentToast();

        // ????????Toast???
        if (persistentToastPanel != null)
        {
            activePersistentToast = Instantiate(persistentToastPanel, mainCanvas.transform);
            activePersistentToast.name = "PersistentToast";

            // ?????????
            Text toastText = activePersistentToast.GetComponentInChildren<Text>();
            if (toastText != null)
            {
                toastText.text = message;
            }

            // ????λ?????????
            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            activePersistentToast.SetActive(true);
        }
        else
        {
            // ??????????壬???????
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

            // ???ó???λ??
            RectTransform rt = activePersistentToast.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = toastPosition;

            // ?????丸????
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
            ShowToast("??п??????飬???????????????", 2f);
        }
    }

    public bool IsTileHighlighted(BoardTile tile)
    {
        return highlightableTiles.Contains(tile);
    }

    // ???????????????
    private bool IsTilePlaceable(BoardTile tile, Player player, int requiredScale)
    {
        return (tile.tileType == BoardTile.TileType.Buildable ||
                tile.tileType == BoardTile.TileType.BuildingSite) &&
               tile.isBuildable &&
               tile.currentBuilding == null &&
               tile.tileScale >= requiredScale &&
               (tile.ownerPlayer == null || tile.ownerPlayer == player);
    }

    // ????????????????
    private void AddTileClickHandler(BoardTile tile)
    {
        EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => OnTileClickedForPlacement(tile));

        trigger.triggers.Add(entry);
    }

    // ??鱻?????????????????
    public void OnTileClickedForPlacement(BoardTile tile)
    {
        if (selectedBuildingData == null || currentBuildingPlayer == null)
            return;

        if (currentBuildingPlayer.cash < selectedBuildingData.purchasePrice)
        {
            ShowToast("????????????????", 2f);
            return;
        }

        if (!IsTilePlaceable(tile, currentBuildingPlayer, (int)selectedBuildingData.requiredScale))
        {
            ShowToast("?????????????????", 2f);
            return;
        }

        if (PurchaseAndPlaceBuilding(tile, selectedBuildingData, currentBuildingPlayer))
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingPlaced);

            ClearTileHighlights();
            HidePersistentToast();
            
            // ???????????????壬????????
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
            
            ShowToast("???????ó??????????????????????????", 2f);
        }
    }

    // ??????????
    private bool PurchaseAndPlaceBuilding(BoardTile tile, BuildingData buildingData, Player player)
    {
        int purchasePrice = buildingData.purchasePrice;

        if (!player.PayCash(purchasePrice))
            return false;

        tile.ownerPlayer = player;
        tile.SetBuildingData(buildingData, 1);
        tile.tileType = BoardTile.TileType.BuildingSite;

        if (buildingData.buildingPrefab != null)
        {
            GameObject buildingObj = Instantiate(buildingData.buildingPrefab, tile.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            buildingObj.transform.SetParent(tile.transform);
            tile.currentBuilding = buildingObj;
        }

        return true;
    }

    // ?????????
    private void ClearTileHighlights()
    {
        foreach (BoardTile tile in highlightableTiles)
        {
            MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                if (tile.tileType == BoardTile.TileType.Start)
                {
                    // ????????????????
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

    // ????????????BuildingData???BuildingType
    private BoardTile.BuildingType GetBuildingTypeFromData(BuildingData data)
    {
        // ?????????????????????????????
        // ?????????汾???????????????????
        if (data.buildingName.Contains("С"))
            return BoardTile.BuildingType.SmallHouse;
        else if (data.buildingName.Contains("??"))
            return BoardTile.BuildingType.MediumHouse;
        else if (data.buildingName.Contains("??"))
            return BoardTile.BuildingType.LargeHouse;
        else
            return BoardTile.BuildingType.None;
    }

    // ================= ???????д?????????? =================

    void EnsureCanvasExists()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                UnityEngine.Debug.LogWarning("??????Canvas?????????...");
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

        UnityEngine.Debug.Log("?????Canvas");
    }

    void CreateRollDiceButton()
    {
        if (rollDiceButtonPrefab == null)
        {
            UnityEngine.Debug.LogWarning("?????????????δ????");
            return;
        }

        GameObject buttonObj = Instantiate(rollDiceButtonPrefab, mainCanvas.transform);
        buttonObj.name = "Btn_RollDice";

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchoredPosition = diceButtonPosition;

        rollDiceButton = buttonObj.GetComponent<Button>();
        if (rollDiceButton != null)
        {
            UnityEngine.Debug.Log("???????????????");
        }
    }

    // ??????????????????OnClick???????
    public void OnRollDiceButtonClicked()
    {
        UnityEngine.Debug.Log("UIManager: ???????????");

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRollDiceButtonClicked();
        }
        else
        {
            UnityEngine.Debug.LogError("GameManager.Instance ?????????????????");
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
            currentPlayerText.text = $"??????: {player.playerName}";
        }

        if (playerCashText != null)
        {
            playerCashText.text = $"???: {player.cash} ?";
        }

        if (currentTileText != null && player.currentTile != null)
        {
            currentTileText.text = $"λ??: {player.currentTile.tileName}";
        }

        // === ???????????????????????? ===
        UpdateCashDisplay(player.cash);
        // === ???????? ===
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

        UnityEngine.Debug.Log($"?л??? {uiType} ????");
    }

    public void SwitchToMenuUI() => SwitchUI(UIType.Menu);
    public void SwitchToGameUI() => SwitchUI(UIType.Game);
    public void SwitchToPauseUI() => SwitchUI(UIType.Pause);
    public void SwitchToGameOverUI() => SwitchUI(UIType.GameOver);

    public void ShowPropertyPurchasePanel(BoardTile property, Player player)
    {
        if (propertyPurchasePanel == null)
        {
            UnityEngine.Debug.LogWarning("???????????????δ????");
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
            priceText.text = $"???: {property.propertyPrice} ?";

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
            UnityEngine.Debug.LogError("UIManager: gameOverPanel ????????????????壡");
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

        UnityEngine.Debug.Log($"??????????: ???={playerName}, ???={isWinner}, ???={roundCount}, ????={diceCount}, ?÷?={score}");

        SetText("ResultText", isWinner ? $"{playerName} ?????" : $"{playerName} ???");
        SetText("RoundText", $"???????: {roundCount}");
        SetText("DiceText", $"???????????: {diceCount}");
        SetText("ScoreText", $"?÷?: {score}");

        Button restartButton = FindRestartButton();
        if (restartButton != null)
        {
            UnityEngine.Debug.Log("???????????????????");
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        else
        {
            UnityEngine.Debug.LogError("????????????????");
        }
    }

    private void SetText(string objectName, string text)
    {
        Transform trans = gameOverPanel.transform.Find(objectName);
        if (trans == null)
        {
            UnityEngine.Debug.LogError($"?????????: {objectName}");
            return;
        }

        Text textComp = trans.GetComponent<Text>();
        if (textComp != null)
        {
            textComp.text = text;
            UnityEngine.Debug.Log($"???? {objectName} = {text} (Text)");
            return;
        }

        TextMeshProUGUI tmpComp = trans.GetComponent<TextMeshProUGUI>();
        if (tmpComp != null)
        {
            tmpComp.text = text;
            UnityEngine.Debug.Log($"???? {objectName} = {text} (TextMeshProUGUI)");
            return;
        }

        UnityEngine.Debug.LogError($"{objectName} ?????? Text ?? TextMeshProUGUI ???");
    }

    private Button FindRestartButton()
    {
        if (gameOverPanel == null)
        {
            UnityEngine.Debug.LogError("gameOverPanel ????????????????????");
            return null;
        }

        Button button = gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"??????: RestartButton");
            return button;
        }

        button = gameOverPanel.transform.Find("Button")?.GetComponent<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"??????: Button");
            return button;
        }

        button = gameOverPanel.GetComponentInChildren<Button>();
        if (button != null)
        {
            UnityEngine.Debug.Log($"??????????? GetComponentInChildren??: {button.name}");
            return button;
        }

        UnityEngine.Debug.LogError("?? gameOverPanel ????????κ? Button ???");
        return null;
    }

    private void OnRestartButtonClicked()
    {
        UnityEngine.Debug.Log("=== ???????????? ===");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            UnityEngine.Debug.Log("??????????");
        }
        else
        {
            UnityEngine.Debug.LogError("gameOverPanel ???");
        }
        
        SwitchToGameUI();
        UnityEngine.Debug.Log("?л??????UI");
        
        if (GameManager.Instance != null)
        {
            UnityEngine.Debug.Log("???? RestartFromGameOver");
            GameManager.Instance.RestartFromGameOver();
        }
        else
        {
            UnityEngine.Debug.LogError("GameManager.Instance ????");
        }
        
        UnityEngine.Debug.Log("=== ????????????? ===");
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
            UnityEngine.Debug.LogWarning("??????UI?????δ????");
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
            infoUI.cashText.text = $"{infoUI.player.cash} ?";

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
        // 优先使用手动拖入的UI组件
        if (infoToastPanel != null && infoToastText != null)
        {
            ShowInfoToast(message, duration);
            return;
        }

        // 回退到代码生成方式
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

    // 使用手动拖入的UI显示提示信息
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

    // ??????????????
    public void ShowBuildingUpgradeUI(BoardTile tile, Player player)
    {
        if (buildingUpgradePanel == null)
        {
            UnityEngine.Debug.LogWarning("???????δ????");
            return;
        }

        upgradeSelectedTile = tile;
        upgradeSelectedPlayer = player;

        // ??????
        buildingUpgradePanel.SetActive(true);
        buildingUpgradePanel.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);

        // ????UI???
        UpdateUpgradePanelInfo();

        // ???????
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

        // ??????????
        SetRollDiceButtonInteractable(false);
    }

    // ??????????????
    private void UpdateUpgradePanelInfo()
    {
        if (upgradeSelectedTile == null || upgradeSelectedPlayer == null) return;

        if (upgradeCostText != null)
        {
            int upgradeCost = upgradeSelectedTile.GetUpgradeCost();
            upgradeCostText.text = $"????????: {upgradeCost}???";

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
            currentLevelText.text = $"??????: {upgradeSelectedTile.buildingLevel}";
        }

        if (nextLevelText != null)
        {
            int nextLevel = upgradeSelectedTile.buildingLevel + 1;
            nextLevelText.text = $"??????: {nextLevel}??";
        }

        // ??????????????
        if (upgradeSelectedTile.currentBuildingData != null)
        {
            // ???????????
            BuildingData buildingData = upgradeSelectedTile.currentBuildingData;

            // ???????????
            string functionDesc = GetBuildingFunctionDescription(buildingData, upgradeSelectedTile.buildingLevel);
            ShowToast($"???????: {functionDesc}", 3f);

            // ????????????
            if (buildingData.nextLevelBuilding != null)
            {
                string nextFunctionDesc = GetBuildingFunctionDescription(
                    buildingData.nextLevelBuilding,
                    upgradeSelectedTile.buildingLevel + 1);
                UnityEngine.Debug.Log($"????????: {nextFunctionDesc}");
            }
        }

        // ?????????
        BuildingData nextBuilding = upgradeSelectedTile.GetNextUpgradeBuilding();
        if (nextBuilding != null)
        {
            if (!upgradeSelectedTile.CheckScaleForUpgrade(nextBuilding.requiredScale))
            {
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = false;
                    ShowToast($"???????????????{(int)nextBuilding.requiredScale}????", 2f);
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

    // ???????????????
    private string GetBuildingFunctionDescription(BuildingData buildingData, int level)
    {
        if (buildingData == null) return "δ?????";

        switch (buildingData.functionType)
        {
            case BuildingData.BuildingFunctionType.Income:
                int income = buildingData.GetIncomeAmount(level);
                return $"????????: {income}???";

            case BuildingData.BuildingFunctionType.Buff:
                float buffValue = buildingData.GetBuffValue(level);
                string buffName = GetBuffEffectName(buildingData.buffEffect);
                if (buildingData.buffDuration > 0)
                {
                    return $"{buffName}: +{buffValue * 100}% (????{buildingData.buffDuration}??)";
                }
                else
                {
                    return $"{buffName}: +{buffValue * 100}% (????)";
                }

            case BuildingData.BuildingFunctionType.Mixed:
                income = buildingData.GetIncomeAmount(level);
                buffValue = buildingData.GetBuffValue(level);
                buffName = GetBuffEffectName(buildingData.buffEffect);
                return $"????: {income}??? + {buffName}: +{buffValue * 100}%";

            default:
                return "?????";
        }
    }

    // ???buffЧ??????
    private string GetBuffEffectName(BuildingData.BuffEffect effect)
    {
        switch (effect)
        {
            case BuildingData.BuffEffect.MoveSpeedBoost: return "??????";
            case BuildingData.BuffEffect.DiceBoost: return "??????";
            case BuildingData.BuffEffect.IncomeMultiplier: return "??????";
            case BuildingData.BuffEffect.DefenseBoost: return "???????";
            case BuildingData.BuffEffect.LuckBoost: return "??????";
            case BuildingData.BuffEffect.AllIncomeBoost: return "???????";
            default: return "δ?Ч??";
        }
    }

    // ??????????
    private void OnUpgradeButtonClicked()
    {
        if (upgradeSelectedTile == null || upgradeSelectedPlayer == null) return;

        if (upgradeSelectedTile.UpgradeBuilding(upgradeSelectedPlayer))
        {
            ShowToast("?????????", 2f);

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingUpgraded);

            // ????UI
            UpdateUpgradePanelInfo();
            UpdateCurrentPlayerInfo(upgradeSelectedPlayer);

            // ?????????????????UI
            if (GameManager.Instance != null && GameManager.Instance.currentPlayer == upgradeSelectedPlayer)
            {
                GameManager.Instance.UpdateUI();
            }

            // ????????????????????????
            if (!upgradeSelectedTile.CanUpgradeBuilding(upgradeSelectedPlayer))
            {
                HideBuildingUpgradeUI();
            }
        }
        else
        {
            ShowToast("????????????????", 2f);
        }
    }

    // ???????????
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

        // ?????????
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