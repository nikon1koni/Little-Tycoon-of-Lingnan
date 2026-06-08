using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SellBuildingUIController : MonoBehaviour
{
    [Header("UI面板引用")]
    public GameObject sellPanel;
    public TextMeshProUGUI titleText;
    
    [Header("提示面板")]
    public GameObject clickHintPanel;
    public TextMeshProUGUI clickHintText;
    
    [Header("建筑信息显示区域")]
    public GameObject buildingInfoArea;
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI buildingDescriptionText;
    public TextMeshProUGUI buildingLevelText;
    public TextMeshProUGUI buildingIncomeText;
    public TextMeshProUGUI sellPriceText;
    
    [Header("操作按钮")]
    public Button confirmButton;
    public Button cancelButton;
    public Button exitButton;
    
    [Header("卖出价格颜色")]
    public Color sellPriceColor = Color.green;
    
    [Header("引用 - 建筑选择面板控制器")]
    public BuildingSelectionPanelController buildingSelectionPanelController;

    private BoardTile selectedTile;
    private Player currentPlayer;
    private bool isSellMode = false;
    private bool hasSelectedBuilding = false;

    void Start()
    {
        Debug.Log("=== SellBuildingUIController Start ===");
        
        if (sellPanel != null)
        {
            sellPanel.SetActive(false);
        }
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
        }
        
        if (buildingInfoArea != null)
        {
            buildingInfoArea.SetActive(false);
        }
        
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmSell);
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

    public void EnterSellMode(Player player)
    {
        if (player == null) return;
        
        isSellMode = true;
        currentPlayer = player;
        selectedTile = null;
        hasSelectedBuilding = false;
        
        // 隐藏infoToastPanel，避免干扰卖出模式的UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInfoToast();
        }
        
        if (sellPanel != null)
        {
            sellPanel.SetActive(true);
        }
        
        if (titleText != null)
        {
            titleText.text = "卖出建筑";
        }
        
        ShowClickHint();
        
        Debug.Log("进入卖出模式");
    }

    private void ShowClickHint()
    {
        hasSelectedBuilding = false;
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(true);
        }
        
        if (buildingInfoArea != null)
        {
            buildingInfoArea.SetActive(false);
        }
        
        if (clickHintText != null)
        {
            clickHintText.text = $"请点击要卖出的建筑\n你拥有 {GetPlayerBuildingCount()} 个建筑\n按ESC取消";
        }
    }

    private void HideClickHint()
    {
        Debug.Log("HideClickHint: 隐藏提示并显示建筑信息");
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
        }
        
        if (buildingInfoArea != null)
        {
            buildingInfoArea.SetActive(true);
        }
    }

    public void ExitSellMode()
    {
        isSellMode = false;
        selectedTile = null;
        currentPlayer = null;
        hasSelectedBuilding = false;
        
        if (sellPanel != null)
        {
            sellPanel.SetActive(false);
        }
        
        if (clickHintPanel != null)
        {
            clickHintPanel.SetActive(false);
        }
        
        if (buildingInfoArea != null)
        {
            buildingInfoArea.SetActive(false);
        }
        
        Debug.Log("退出卖出模式");
    }

    public void OnTileClicked(BoardTile tile)
    {
        if (!isSellMode || currentPlayer == null) return;
        
        if (tile == null)
        {
            ShowStatus("点击的格子为空");
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
        
        DisplaySellInfo(tile);
        
        Debug.Log($"选中建筑: {tile.currentBuildingData.buildingName}");
    }

    private void DisplaySellInfo(BoardTile tile)
    {
        if (tile == null || tile.currentBuildingData == null) return;
        
        BuildingData currentData = tile.currentBuildingData;
        
        if (buildingNameText != null)
            buildingNameText.text = currentData.buildingName;
        
        if (buildingDescriptionText != null)
            buildingDescriptionText.text = currentData.description;
        
        if (buildingLevelText != null)
            buildingLevelText.text = $"等级 {tile.buildingLevel}";
        
        if (currentData.functionType == BuildingData.BuildingFunctionType.Income ||
            currentData.functionType == BuildingData.BuildingFunctionType.Mixed)
        {
            int income = currentData.GetIncomeAmountByTurns(tile.GetBuildingTurnsOwned());
            if (buildingIncomeText != null)
                buildingIncomeText.text = $"收入: {income} 金币/回合";
        }
        else
        {
            if (buildingIncomeText != null)
                buildingIncomeText.text = "";
        }
        
        int sellPrice = tile.GetSellPrice();
        if (sellPriceText != null)
        {
            sellPriceText.text = $"卖出价格: {sellPrice} 金币";
            sellPriceText.color = sellPriceColor;
        }
        
        if (confirmButton != null)
        {
            confirmButton.interactable = tile.CanSellBuilding(currentPlayer);
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

    private void OnConfirmSell()
    {
        if (selectedTile == null || currentPlayer == null)
        {
            if (!hasSelectedBuilding)
            {
                ShowStatus("请先点击要卖出的建筑");
            }
            return;
        }
        
        int sellPrice = selectedTile.GetSellPrice();
        
        if (selectedTile.SellBuilding(currentPlayer))
        {
            // 播放出售建筑音效（交易成功）
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayBuildingSoldSound(true);
            
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventGainMoney);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCashDisplay(currentPlayer.cash);
            }
            
            ShowClickHint();
            selectedTile = null;
            hasSelectedBuilding = false;
            
            Debug.Log("卖出成功");
        }
        else
        {
            ShowStatus("卖出失败");
            Debug.Log("卖出失败");
        }
    }

    private void OnCancel()
    {
        if (BuildingDataConfig.Instance != null)
        {
            BuildingDataConfig.Instance.ExitSellMode();
        }
        else
        {
            ExitSellMode();
            if (buildingSelectionPanelController != null)
            {
                buildingSelectionPanelController.OnExitSellMode();
            }
        }
    }

    public bool IsSellModeActive()
    {
        return isSellMode;
    }

    public BoardTile GetSelectedTile()
    {
        return selectedTile;
    }
}
