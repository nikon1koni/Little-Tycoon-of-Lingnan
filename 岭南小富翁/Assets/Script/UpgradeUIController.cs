using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIController : MonoBehaviour
{
    [Header("UI面板引用")]
    public GameObject upgradePanel;
    public TextMeshProUGUI titleText;
    
    [Header("对比区域")]
    public GameObject comparisonArea;
    
    [Header("提示面板")]
    public GameObject clickHintPanel;
    public TextMeshProUGUI clickHintText;
    
    [Header("当前建筑信息")]
    public TextMeshProUGUI currentNameText;
    public TextMeshProUGUI currentDescriptionText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI currentIncomeText;
    public TextMeshProUGUI currentBuffText;
    
    [Header("箭头/费用区域")]
    public GameObject arrowPanel;
    public TextMeshProUGUI arrowCostText;
    public TextMeshProUGUI arrowText;
    
    [Header("下一级建筑信息")]
    public TextMeshProUGUI nextNameText;
    public TextMeshProUGUI nextDescriptionText;
    public TextMeshProUGUI nextLevelText;
    public TextMeshProUGUI nextIncomeText;
    public TextMeshProUGUI nextBuffText;
    
    [Header("操作按钮")]
    public Button confirmButton;
    public Button cancelButton;
    public Button exitButton;
    
    [Header("费用颜色")]
    public Color costColorSufficient = Color.green;
    public Color costColorInsufficient = Color.red;
    
    [Header("费用描边颜色")]
    public Color costOutlineColorSufficient = Color.white;
    public Color costOutlineColorInsufficient = new Color(1f, 0.84f, 0f);
    public float costOutlineThickness = 2f;
    
    [Header("引用 - 建筑选择面板控制器")]
    public BuildingSelectionPanelController buildingSelectionPanelController;

    private BoardTile selectedTile;
    private Player currentPlayer;
    private bool isUpgradeMode = false;
    private bool hasSelectedBuilding = false;

    void Start()
    {
        Debug.Log("=== UpgradeUIController Start ===");
        Debug.Log($"游戏对象名称: {gameObject.name}");
        Debug.Log($"游戏对象激活状态: {gameObject.activeSelf}");
        Debug.Log($"upgradePanel: {(upgradePanel != null ? $"存在 - {upgradePanel.name}" : "不存在")}");
        Debug.Log($"clickHintPanel: {(clickHintPanel != null ? $"存在 - {clickHintPanel.name}" : "不存在")}");
        Debug.Log($"comparisonArea: {(comparisonArea != null ? $"存在 - {comparisonArea.name}" : "不存在")}");
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            Debug.Log("upgradePanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("upgradePanel 未设置");
        }
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
            Debug.Log("clickHintPanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("clickHintPanel 未设置");
        }
        
        if (comparisonArea != null)
        {
            comparisonArea.SetActive(false);
            Debug.Log("comparisonArea 已隐藏");
        }
        else
        {
            Debug.LogWarning("comparisonArea 未设置，请在Inspector中设置引用");
        }
        
        SetupButtons();
    }

    // Update方法已暂时注释 - ESC键功能由UIManager处理，以避免重复处理
    // void Update()
    // {
    //     if (isUpgradeMode && Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         OnCancel();
    //     }
    // }

    private void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmUpgrade);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancel);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnCancel);
        }
    }

    public void EnterUpgradeMode(Player player)
    {
        if (player == null) return;
        
        isUpgradeMode = true;
        currentPlayer = player;
        selectedTile = null;
        hasSelectedBuilding = false;
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }
        
        if (titleText != null)
        {
            titleText.text = "建筑升级";
        }
        
        ShowClickHint();
        
        Debug.Log("进入升级模式");
    }

    private void ShowClickHint()
    {
        hasSelectedBuilding = false;
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(true);
        }
        
        if (comparisonArea != null)
        {
            comparisonArea.SetActive(false);
        }
        
        if (clickHintText != null)
        {
            clickHintText.text = $"请点击要升级的建筑\n你拥有 {GetPlayerBuildingCount()} 个建筑\n按ESC取消";
        }
    }

    private void HideClickHint()
    {
        Debug.Log("HideClickHint: 隐藏提示并显示对比区域");
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
            Debug.Log("clickHintPanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("clickHintPanel 未设置");
        }
        
        if (comparisonArea != null)
        {
            comparisonArea.SetActive(true);
            Debug.Log("comparisonArea 已显示");
        }
        else
        {
            Debug.LogWarning("comparisonArea 未设置，请在Inspector中设置引用");
        }
    }

    public void ExitUpgradeMode()
    {
        isUpgradeMode = false;
        selectedTile = null;
        currentPlayer = null;
        hasSelectedBuilding = false;
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
        
        // 确保所有子面板也被隐藏
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
        }
        
        if (comparisonArea != null)
        {
            comparisonArea.SetActive(false);
        }
        
        Debug.Log("退出升级模式");
    }

    public void OnTileClicked(BoardTile tile)
    {
        if (!isUpgradeMode || currentPlayer == null) return;
        
        if (tile == null)
        {
            Debug.Log("点击的格子为空");
            return;
        }
        
        if (tile.currentBuildingData == null)
        {
            ShowStatus("该格子上没有建筑");
            return;
        }
        
        if (tile.ownerPlayer != currentPlayer)
        {
            ShowStatus("这不是你的建筑");
            return;
        }
        
        hasSelectedBuilding = true;
        selectedTile = tile;
        HideClickHint();
        
        // 隐藏UIManager的infoToastPanel
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInfoToast();
        }
        
        DisplayUpgradeInfo(tile);
        
        Debug.Log($"选中建筑: {tile.currentBuildingData.buildingName}");
    }

    private void DisplayUpgradeInfo(BoardTile tile)
    {
        if (tile == null || tile.currentBuildingData == null) return;
        
        BuildingData currentData = tile.currentBuildingData;
        BuildingData nextData = tile.GetNextUpgradeBuilding();
        
        if (currentNameText != null)
            currentNameText.text = currentData.buildingName;
        
        if (currentDescriptionText != null)
            currentDescriptionText.text = currentData.description;
        
        if (currentLevelText != null)
            currentLevelText.text = $"等级 {tile.buildingLevel}";
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Income ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            int income = currentData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
            if (currentIncomeText != null)
                currentIncomeText.text = $"收入: {income} 金币/回合";
        }
        else
        {
            if (currentIncomeText != null)
                currentIncomeText.text = "";
        }
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Buff ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            float buffValue = currentData.GetBuffValue(tile.buildingLevel);
            string buffName = BuildingData.GetBuffEffectName(currentData.buffEffect);
            if (currentBuffText != null)
                currentBuffText.text = $"{buffName}: +{buffValue * 100}%";
        }
        else
        {
            if (currentBuffText != null)
                currentBuffText.text = "";
        }
        
        if (nextData != null)
        {
            if (arrowPanel != null)
                arrowPanel.SetActive(true);
            
            int cost = tile.GetUpgradeCost();
            if (arrowCostText != null)
            {
                arrowCostText.text = $"{cost} 金币";
                if (currentPlayer != null && currentPlayer.cash < cost)
                {
                    arrowCostText.color = costColorInsufficient;
                    arrowCostText.outlineColor = costOutlineColorInsufficient;
                }
                else
                {
                    arrowCostText.color = costColorSufficient;
                    arrowCostText.outlineColor = costOutlineColorSufficient;
                }
                
                arrowCostText.outlineWidth = costOutlineThickness;
            }
            
            if (arrowText != null)
                arrowText.text = "升级到 >";
            
            if (nextNameText != null)
                nextNameText.text = nextData.buildingName;
            
            if (nextDescriptionText != null)
                nextDescriptionText.text = nextData.description;
            
            if (nextLevelText != null)
                nextLevelText.text = $"等级 {tile.buildingLevel + 1}";
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Income ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                int income = nextData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
                if (nextIncomeText != null)
                    nextIncomeText.text = $"收入: {income} 金币/回合";
            }
            else
            {
                if (nextIncomeText != null)
                    nextIncomeText.text = "";
            }
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Buff ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                float buffValue = nextData.GetBuffValue(tile.buildingLevel + 1);
                string buffName = BuildingData.GetBuffEffectName(nextData.buffEffect);
                if (nextBuffText != null)
                    nextBuffText.text = $"{buffName}: +{buffValue * 100}%";
            }
            else
            {
                if (nextBuffText != null)
                    nextBuffText.text = "";
            }
            
            if (confirmButton != null)
            {
                confirmButton.interactable = tile.CanUpgradeBuilding(currentPlayer);
            }
        }
        else
        {
            if (arrowPanel != null)
            {
                arrowPanel.SetActive(true);
                if (arrowText != null)
                    arrowText.text = "已满级";
                if (arrowCostText != null)
                {
                    arrowCostText.text = "无法升级";
                    arrowCostText.color = Color.gray;
                }
            }
            
            if (nextNameText != null)
                nextNameText.text = "已满级";
            
            if (nextDescriptionText != null)
                nextDescriptionText.text = "该建筑已达最高等级";
            
            if (nextLevelText != null)
                nextLevelText.text = "";
            
            if (nextIncomeText != null)
                nextIncomeText.text = "";
            
            if (nextBuffText != null)
                nextBuffText.text = "";
            
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
        }
    }

    private void ShowStatus(string message)
    {
        if (clickHintText != null)
        {
            clickHintText.text = message;
        }
    }

    private int GetPlayerBuildingCount()
    {
        if (currentPlayer == null || BoardManager.Instance == null) return 0;
        
        int count = 0;
        foreach (BoardTile tile in BoardManager.Instance.allTiles)
        {
            if (tile != null && tile.ownerPlayer == currentPlayer && tile.currentBuildingData != null)
            {
                count++;
            }
        }
        return count;
    }

    private void OnConfirmUpgrade()
    {
        if (selectedTile == null || currentPlayer == null)
        {
            if (!hasSelectedBuilding)
            {
                ShowStatus("请先点击要升级的建筑");
            }
            return;
        }
        
        if (selectedTile.UpgradeBuilding(currentPlayer))
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingUpgraded);
            
            DisplayUpgradeInfo(selectedTile);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
            }
            
            Debug.Log("升级成功");
        }
        else
        {
            ShowStatus("升级失败，请检查金币或建筑等级");
            Debug.Log("升级失败");
        }
    }

    private void OnCancel()
    {
        // 优先使用BuildingDataConfig来处理升级模式退出
        if (BuildingDataConfig.Instance != null)
        {
            BuildingDataConfig.Instance.ExitUpgradeMode();
        }
        else
        {
            // 备用方案
            ExitUpgradeMode();
            if (buildingSelectionPanelController != null)
            {
                buildingSelectionPanelController.OnExitUpgradeMode();
            }
        }
    }

    public bool IsUpgradeModeActive()
    {
        return isUpgradeMode;
    }

    public BoardTile GetSelectedTile()
    {
        return selectedTile;
    }
}
