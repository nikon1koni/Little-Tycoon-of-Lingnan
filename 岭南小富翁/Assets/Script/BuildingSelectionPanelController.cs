using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSelectionPanelController : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject buildingSelectionPanel;
    public GameObject upgradePanel;
    
    [Header("升级按钮")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    
    [Header("设置")]
    public bool showUpgradeButton = true;
    
    private bool isUpgradeMode = false;
    
    [Header("引用")]
    public UpgradeUIController upgradeUIController;

    void Start()
    {
        SetupUpgradeButton();
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

    private void UpdateUpgradeButtonVisibility()
    {
        if (upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(showUpgradeButton);
        }
    }

    public void OnUpgradeButtonClicked()
    {
        Debug.Log("点击升级按钮");
        
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
                
                Debug.Log("发现可升级建筑");
                
                isUpgradeMode = true;
                
                BuildingDataConfig.Instance.EnterUpgradeMode(currentPlayer);
                
                if (buildingSelectionPanel != null)
                {
                    buildingSelectionPanel.SetActive(false);
                }
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("进入升级模式", 3f);
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

    public bool IsUpgradeModeActive()
    {
        return isUpgradeMode;
    }
}