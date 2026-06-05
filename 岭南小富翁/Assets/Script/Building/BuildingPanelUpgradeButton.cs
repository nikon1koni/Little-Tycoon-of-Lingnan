using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingPanelUpgradeButton : MonoBehaviour
{
    [Header("按钮引用")]
    public Button upgradeButton;
    public TextMeshProUGUI buttonText;
    
    [Header("面板引用")]
    public GameObject buildingSelectionPanel;
    
    private void Start()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
        
        if (buttonText != null)
        {
            buttonText.text = "升级建筑";
        }
    }
    
    private void OnUpgradeButtonClicked()
    {
        Debug.Log("升级按钮被点击");
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Player currentPlayer = GameManager.Instance.currentPlayer;
            
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
        else
        {
            Debug.LogWarning("GameManager.Instance 或 currentPlayer 为空");
        }
    }
    
    public void OnExitUpgradeMode()
    {
        BuildingDataConfig.Instance.ExitUpgradeMode();
        
        if (buildingSelectionPanel != null)
        {
            buildingSelectionPanel.SetActive(true);
        }
    }
}
