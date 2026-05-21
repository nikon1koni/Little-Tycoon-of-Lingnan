using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIController : MonoBehaviour
{
    [Header("??????????")]
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
                statusText.text = $"??????????????????\n????? {GetPlayerBuildingCount()} ??????";
            }
        }
        
        Debug.Log($"??????????????????: {player.playerName}");
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
        
        Debug.Log("?????????");
    }

    public void OnTileClicked(BoardTile tile)
    {
        if (!isUpgradeMode || currentPlayer == null) return;
        
        if (tile == null)
        {
            Debug.Log("??????");
            return;
        }
        
        if (tile.currentBuildingData == null)
        {
            ShowStatus("???????????");
            Debug.Log("???????????");
            return;
        }
        
        if (tile.ownerPlayer != currentPlayer)
        {
            ShowStatus("??????????");
            Debug.Log("??????????");
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
        currentInfo += $"???????????\n";
        currentInfo += $"????: {currentData.buildingName}\n";
        currentInfo += $"???: {tile.buildingLevel}\n";
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Income ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            int income = currentData.GetIncomeAmount(tile.buildingLevel);
            currentInfo += $"????: {income} ?/???\n";
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
            nextInfo += $"????????\n";
            nextInfo += $"????: {nextData.buildingName}\n";
            nextInfo += $"???: {tile.buildingLevel + 1}\n";
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Income ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                int income = nextData.GetIncomeAmount(tile.buildingLevel + 1);
                nextInfo += $"????: {income} ?/???\n";
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
                costText.text = $"????????: {cost} ?";
                
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
                nextInfoText.text = "????????\n???????????";
            }
            
            if (costText != null)
            {
                costText.text = "????????";
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
            ShowStatus("??????????????");
            return;
        }
        
        if (selectedTile.UpgradeBuilding(currentPlayer))
        {
            ShowStatus("?????????");
            Debug.Log($"?????????");
            
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
            ShowStatus("??????????????????????");
            Debug.Log("???????");
        }
    }

    private void OnCancel()
    {
        ExitUpgradeMode();
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
