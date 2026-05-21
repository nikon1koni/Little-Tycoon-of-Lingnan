using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIController : MonoBehaviour
{
    [Header("升级面板引用")]
    public GameObject upgradePanel;
    public TextMeshProUGUI titleText;
    
    [Header("对比区域引用")]
    public GameObject comparisonArea;
    
    [Header("点击提示引用")]
    public GameObject clickHintPanel;
    public TextMeshProUGUI clickHintText;
    
    [Header("当前建筑信息引用")]
    public TextMeshProUGUI currentNameText;
    public TextMeshProUGUI currentDescriptionText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI currentIncomeText;
    public TextMeshProUGUI currentBuffText;
    
    [Header("箭头/成本信息引用")]
    public GameObject arrowPanel;
    public TextMeshProUGUI arrowCostText;
    public TextMeshProUGUI arrowText;
    
    [Header("下一级建筑信息引用")]
    public TextMeshProUGUI nextNameText;
    public TextMeshProUGUI nextDescriptionText;
    public TextMeshProUGUI nextLevelText;
    public TextMeshProUGUI nextIncomeText;
    public TextMeshProUGUI nextBuffText;
    
    [Header("按钮引用")]
    public Button confirmButton;
    public Button cancelButton;
    public Button exitButton;
    
    [Header("颜色设置")]
    public Color costColorSufficient = Color.green;
    public Color costColorInsufficient = Color.red;
    
    [Header("轮廓颜色设置")]
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
        Debug.Log($"upgradePanel: {(upgradePanel != null ? $"已赋值 - {upgradePanel.name}" : "未赋值")}");
        Debug.Log($"clickHintPanel: {(clickHintPanel != null ? $"已赋值 - {clickHintPanel.name}" : "未赋值")}");
        Debug.Log($"comparisonArea: {(comparisonArea != null ? $"已赋值 - {comparisonArea.name}" : "未赋值")}");
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            Debug.Log("upgradePanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("upgradePanel 未赋值");
        }
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
            Debug.Log("clickHintPanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("clickHintPanel 未赋值");
        }
        
        if (comparisonArea != null)
        {
            comparisonArea.SetActive(false);
            Debug.Log("comparisonArea 已隐藏");
        }
        else
        {
            Debug.LogWarning("comparisonArea 未赋值，请在Inspector中赋值");
        }
        
        SetupButtons();
    }

    void Update()
    {
        if (isUpgradeMode && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
        }
    }

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
            clickHintText.text = $"点击要升级的建筑\n当前共有 {GetPlayerBuildingCount()} 个建筑\n按ESC退出";
        }
    }

    private void HideClickHint()
    {
        Debug.Log("HideClickHint: 隐藏提示，显示对比区域");
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
            Debug.Log("clickHintPanel 已隐藏");
        }
        else
        {
            Debug.LogWarning("clickHintPanel 未赋值");
        }
        
        if (comparisonArea != null)
        {
            comparisonArea.SetActive(true);
            Debug.Log("comparisonArea 已显示");
        }
        else
        {
            Debug.LogWarning("comparisonArea 未赋值，请在Inspector中赋值");
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
        
        Debug.Log("退出升级模式");
    }

    public void OnTileClicked(BoardTile tile)
    {
        if (!isUpgradeMode || currentPlayer == null) return;
        
        if (tile == null)
        {
            Debug.Log("无效的地块");
            return;
        }
        
        if (tile.currentBuildingData == null)
        {
            ShowStatus("该地块上没有建筑");
            return;
        }
        
        if (tile.ownerPlayer != currentPlayer)
        {
            ShowStatus("该建筑不属于你");
            return;
        }
        
        hasSelectedBuilding = true;
        selectedTile = tile;
        HideClickHint();
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
            int income = currentData.GetIncomeAmount(tile.buildingLevel);
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
                arrowText.text = "升级至 >";
            
            if (nextNameText != null)
                nextNameText.text = nextData.buildingName;
            
            if (nextDescriptionText != null)
                nextDescriptionText.text = nextData.description;
            
            if (nextLevelText != null)
                nextLevelText.text = $"等级 {tile.buildingLevel + 1}";
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Income ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                int income = nextData.GetIncomeAmount(tile.buildingLevel + 1);
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
                nextDescriptionText.text = "该建筑已达到最高等级";
            
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
            ShowStatus("升级失败，金币不足或已达最高等级");
            Debug.Log("升级失败");
        }
    }

    private void OnCancel()
    {
        ExitUpgradeMode();
        
        if (buildingSelectionPanelController != null)
        {
            buildingSelectionPanelController.OnExitUpgradeMode();
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
