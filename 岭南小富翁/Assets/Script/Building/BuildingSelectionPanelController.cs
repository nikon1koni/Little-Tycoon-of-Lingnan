using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSelectionPanelController : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject buildingSelectionPanel;
    public GameObject upgradePanel;
    
    [Header("升级按钮引用")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    
    [Header("卖出按钮引用")]
    public Button sellButton;
    public TextMeshProUGUI sellButtonText;
    
    [Header("设置")]
    public bool showUpgradeButton = true;
    public bool showSellButton = true;
    
    private bool isUpgradeMode = false;
    private bool isSellMode = false;
    
    [Header("控制器引用")]
    public UpgradeUIController upgradeUIController;
    public SellBuildingUIController sellBuildingUIController;

    void Start()
    {
        SetupUpgradeButton();
        SetupSellButton();
    }

    private void SetupUpgradeButton()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = "升级建筑";
            }
            
            UpdateUpgradeButtonVisibility();
        }
    }

    private void SetupSellButton()
    {
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnSellButtonClicked);
            
            if (sellButtonText != null)
            {
                sellButtonText.text = "卖出建筑";
            }
            
            UpdateSellButtonVisibility();
        }
    }

    private void UpdateUpgradeButtonVisibility()
    {
        if (upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(showUpgradeButton);
        }
    }
    
    private void UpdateSellButtonVisibility()
    {
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(showSellButton);
        }
    }

    public void OnUpgradeButtonClicked()
    {
        Debug.Log("升级按钮被点击");
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Player currentPlayer = GameManager.Instance.currentPlayer;
            
            if (BuildingDataConfig.Instance != null)
            {
                var upgradeableBuildings = BuildingDataConfig.Instance.GetPlayerUpgradeableBuildings(currentPlayer);
                
                if (upgradeableBuildings.Count == 0)
                {
                    Debug.Log("没有可升级的建筑");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast("没有可升级的建筑", 2f);
                    }
                    return;
                }
                
                Debug.Log($"找到 {upgradeableBuildings.Count} 个可升级的建筑");
                
                isUpgradeMode = true;
                
                BuildingDataConfig.Instance.EnterUpgradeMode(currentPlayer);
                
                if (buildingSelectionPanel != null)
                {
                    buildingSelectionPanel.SetActive(false);
                }
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("进入升级模式，点击要升级的建筑", 3f);
                }
            }
        }
        else
        {
            Debug.LogWarning("GameManager.Instance 或 currentPlayer 为空");
        }
    }

    public void OnExitUpgradeMode()
    {
        isUpgradeMode = false;
        
        if (BuildingDataConfig.Instance != null)
        {
            BuildingDataConfig.Instance.ExitUpgradeMode();
        }
        
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(true);
        }
    }
    
    public void OnSellButtonClicked()
    {
        Debug.Log("卖出按钮被点击");
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Player currentPlayer = GameManager.Instance.currentPlayer;
            
            if (BuildingDataConfig.Instance != null)
            {
                var sellableBuildings = BuildingDataConfig.Instance.GetPlayerSellableBuildings(currentPlayer);
                
                if (sellableBuildings.Count == 0)
                {
                    Debug.Log("没有可卖出的建筑");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast("没有可卖出的建筑", 2f);
                    }
                    return;
                }
                
                Debug.Log($"找到 {sellableBuildings.Count} 个可卖出的建筑");
                
                isSellMode = true;
                
                BuildingDataConfig.Instance.EnterSellMode(currentPlayer);
                
                if (buildingSelectionPanel != null)
                {
                    buildingSelectionPanel.SetActive(false);
                }
                
                // 不显示Toast提示，避免infoToastPanel被调用
                // if (UIManager.Instance != null)
                // {
                //     UIManager.Instance.ShowToast("进入卖出模式，点击要卖出的建筑", 3f);
                // }
            }
        }
        else
        {
            Debug.LogWarning("GameManager.Instance 或 currentPlayer 为空");
        }
    }
    
    public void OnExitSellMode()
    {
        isSellMode = false;
        
        // 延迟一帧显示建筑面板，避免UIManager.Update()在同一帧中再次隐藏它
        Invoke(nameof(ShowBuildingPanelDelayed), 0.01f);
    }
    
    private void ShowBuildingPanelDelayed()
    {
        Debug.Log($"ShowBuildingPanelDelayed: buildingSelectionPanel = {buildingSelectionPanel}");
        
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(true);
            Debug.Log("建筑面板已显示");
        }
        else
        {
            Debug.LogError("buildingSelectionPanel 引用未配置！");
        }
    }

    public bool IsUpgradeModeActive()
    {
        return isUpgradeMode;
    }
    
    public bool IsSellModeActive()
    {
        return isSellMode;
    }
}
