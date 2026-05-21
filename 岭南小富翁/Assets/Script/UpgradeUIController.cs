using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIController : MonoBehaviour
{
    [Header("升级面板组件")]
    public GameObject upgradePanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI currentInfoText;
    public TextMeshProUGUI nextInfoText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statusText;
    
    public Button confirmButton;
    public Button cancelButton;

    private BoardTile selectedTile;
    private Player currentPlayer;
    private bool isUpgradeMode = false;
    
    [Header("引用 - 建筑选择面板控制器")]
    public BuildingSelectionPanelController buildingSelectionPanelController;

    void Start()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
        
        SetupButtons();
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
    }

    public void EnterUpgradeMode(Player player)
    {
        if (player == null) return;
        
        isUpgradeMode = true;
        currentPlayer = player;
        selectedTile = null;
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            ClearDisplay();
            
            if (statusText != null)
            {
                statusText.text = $"点击你的建筑查看升级信息\n共有 {GetPlayerBuildingCount()} 个建筑";
            }
        }
        
        Debug.Log($"进入升级模式: {player.playerName}");
    }

    public void ExitUpgradeMode()
    {
        isUpgradeMode = false;
        selectedTile = null;
        currentPlayer = null;
        
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
            Debug.Log("无效地块");
            return;
        }
        
        if (tile.currentBuildingData == null)
        {
            ShowStatus("这个地块没有建筑");
            Debug.Log("这个地块没有建筑");
            return;
        }
        
        if (tile.ownerPlayer != currentPlayer)
        {
            ShowStatus("这不是你的建筑");
            Debug.Log("这不是你的建筑");
            return;
        }
        
        selectedTile = tile;
        DisplayUpgradeInfo(tile);
    }

    private void DisplayUpgradeInfo(BoardTile tile)
    {
        if (tile == null || tile.currentBuildingData == null) return;
        
        BuildingData currentData = tile.currentBuildingData;
        BuildingData nextData = tile.GetNextUpgradeBuilding();
        
        string currentInfo = "";
        currentInfo += $"当前建筑\n";
        currentInfo += $"名称: {currentData.buildingName}\n";
        currentInfo += $"等级: {tile.buildingLevel}\n";
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Income ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            int income = currentData.GetIncomeAmount(tile.buildingLevel);
            currentInfo += $"收入: {income} 金币/回合\n";
        }
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Buff ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            float buffValue = currentData.GetBuffValue(tile.buildingLevel);
            string buffName = BuildingData.GetBuffEffectName(currentData.buffEffect);
            currentInfo += $"{buffName}: +{buffValue * 100}%\n";
        }
        
        if (currentInfoText != null)
        {
            currentInfoText.text = currentInfo;
        }
        
        if (nextData != null)
        {
            string nextInfo = "";
            nextInfo += $"升级后\n";
            nextInfo += $"名称: {nextData.buildingName}\n";
            nextInfo += $"等级: {tile.buildingLevel + 1}\n";
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Income ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                int income = nextData.GetIncomeAmount(tile.buildingLevel + 1);
                nextInfo += $"收入: {income} 金币/回合\n";
            }
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Buff ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                float buffValue = nextData.GetBuffValue(tile.buildingLevel + 1);
                string buffName = BuildingData.GetBuffEffectName(nextData.buffEffect);
                nextInfo += $"{buffName}: +{buffValue * 100}%\n";
            }
            
            if (nextInfoText != null)
            {
                nextInfoText.text = nextInfo;
            }
            
            int cost = tile.GetUpgradeCost();
            if (costText != null)
            {
                costText.text = $"升级费用: {cost} 金币";
                
                if (currentPlayer != null && currentPlayer.cash < cost)
                {
                    costText.color = Color.red;
                }
                else
                {
                    costText.color = new Color(0.2f, 0.6f, 0.2f);
                }
            }
            
            if (confirmButton != null)
            {
                confirmButton.interactable = tile.CanUpgradeBuilding(currentPlayer);
            }
            
            ShowStatus("");
        }
        else
        {
            if (nextInfoText != null)
            {
                nextInfoText.text = "已达最高等级\n无法继续升级";
            }
            
            if (costText != null)
            {
                costText.text = "无法升级";
                costText.color = Color.gray;
            }
            
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
        }
    }

    private void ClearDisplay()
    {
        if (currentInfoText != null) currentInfoText.text = "";
        if (nextInfoText != null) nextInfoText.text = "";
        if (costText != null) costText.text = "";
        if (statusText != null) statusText.text = "";
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
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
            ShowStatus("请先选择一个建筑");
            return;
        }
        
        if (selectedTile.UpgradeBuilding(currentPlayer))
        {
            ShowStatus("升级成功！");
            Debug.Log($"升级成功");
            
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventBuildingUpgraded);
            
            DisplayUpgradeInfo(selectedTile);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
            }
        }
        else
        {
            ShowStatus("升级失败，请检查金币是否足够");
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
