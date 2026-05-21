using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIController : MonoBehaviour
{
    [Header("??????????")]
    public GameObject upgradePanel;
    public TextMeshProUGUI titleText;
    
    [Header("??????")]
    public GameObject clickHintPanel;
    public TextMeshProUGUI clickHintText;
    
    [Header("????????????????")]
    public TextMeshProUGUI currentNameText;
    public TextMeshProUGUI currentDescriptionText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI currentIncomeText;
    public TextMeshProUGUI currentBuffText;
    
    [Header("??????????????§Þ?")]
    public GameObject arrowPanel;
    public TextMeshProUGUI arrowCostText;
    public TextMeshProUGUI arrowText;
    
    [Header("???????????????")]
    public TextMeshProUGUI nextNameText;
    public TextMeshProUGUI nextDescriptionText;
    public TextMeshProUGUI nextLevelText;
    public TextMeshProUGUI nextIncomeText;
    public TextMeshProUGUI nextBuffText;
    
    [Header("???")]
    public Button confirmButton;
    public Button cancelButton;
    public Button exitButton;

    private BoardTile selectedTile;
    private Player currentPlayer;
    private bool isUpgradeMode = false;
    private bool hasSelectedBuilding = false;
    
    [Header("???? - ???????????????")]
    public BuildingSelectionPanelController buildingSelectionPanelController;

    void Start()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
        }
        
        if (arrowPanel != null)
        {
            arrowPanel.SetActive(false);
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
            upgradePanel.SetActive(false);
            upgradePanel.SetActive(true);
            upgradePanel.gameObject.SetActive(true);
        }
        
        ShowClickHint();
        
        Debug.Log($"??????????: {player.playerName}");
    }

    private void ShowClickHint()
    {
        hasSelectedBuilding = false;
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(true);
        }
        
        if (clickHintText != null)
        {
            clickHintText.text = $"??????????????????\n???? {GetPlayerBuildingCount()} ??????\n??ESC???";
        }
        
        HideUpgradeComparison();
    }

    private void HideClickHint()
    {
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
        }
    }

    private void HideUpgradeComparison()
    {
        if (currentNameText != null) currentNameText.text = "";
        if (currentDescriptionText != null) currentDescriptionText.text = "";
        if (currentLevelText != null) currentLevelText.text = "";
        if (currentIncomeText != null) currentIncomeText.text = "";
        if (currentBuffText != null) currentBuffText.text = "";
        
        if (arrowPanel != null) arrowPanel.SetActive(false);
        
        if (nextNameText != null) nextNameText.text = "";
        if (nextDescriptionText != null) nextDescriptionText.text = "";
        if (nextLevelText != null) nextLevelText.text = "";
        if (nextIncomeText != null) nextIncomeText.text = "";
        if (nextBuffText != null) nextBuffText.text = "";
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
        
        Debug.Log("?????????");
    }

    public void OnTileClicked(BoardTile tile)
    {
        if (!isUpgradeMode || currentPlayer == null) return;
        
        if (tile == null)
        {
            Debug.Log("??§¹???");
            return;
        }
        
        if (tile.currentBuildingData == null)
        {
            ShowStatus("????????§ß???");
            Debug.Log("????????§ß???");
            return;
        }
        
        if (tile.ownerPlayer != currentPlayer)
        {
            ShowStatus("??????????");
            Debug.Log("??????????");
            return;
        }
        
        hasSelectedBuilding = true;
        selectedTile = tile;
        HideClickHint();
        DisplayUpgradeInfo(tile);
        
        Debug.Log($"??§ß???: {tile.currentBuildingData.buildingName}");
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
            currentLevelText.text = $"??? {tile.buildingLevel}";
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Income ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            int income = currentData.GetIncomeAmount(tile.buildingLevel);
            if (currentIncomeText != null)
                currentIncomeText.text = $"????: {income} ???/???";
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
                arrowCostText.text = $"{cost} ???";
                if (currentPlayer != null && currentPlayer.cash < cost)
                {
                    arrowCostText.color = Color.red;
                }
                else
                {
                    arrowCostText.color = new Color(0.2f, 0.6f, 0.2f);
                }
            }
            
            if (arrowText != null)
                arrowText.text = "??";
            
            if (nextNameText != null)
                nextNameText.text = nextData.buildingName;
            
            if (nextDescriptionText != null)
                nextDescriptionText.text = nextData.description;
            
            if (nextLevelText != null)
                nextLevelText.text = $"??? {tile.buildingLevel + 1}";
            
            if (nextData.functionType == BuildingData.BuildingFunctionType.Income ||
                nextData.functionType == BuildingData.BuildingFunctionType.Mixed)
            {
                int income = nextData.GetIncomeAmount(tile.buildingLevel + 1);
                if (nextIncomeText != null)
                    nextIncomeText.text = $"????: {income} ???/???";
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
                    arrowText.text = "??";
                if (arrowCostText != null)
                {
                    arrowCostText.text = "??????";
                    arrowCostText.color = Color.gray;
                }
            }
            
            if (nextNameText != null)
                nextNameText.text = "??????";
            
            if (nextDescriptionText != null)
                nextDescriptionText.text = "???????????";
            
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
                ShowStatus("??????????????");
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
            
            Debug.Log("???????");
        }
        else
        {
            ShowStatus("??????????????");
            Debug.Log("???????");
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
