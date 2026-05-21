using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSelectionPanelController : MonoBehaviour
{
    [Header("???????")]
    public GameObject buildingSelectionPanel;
    public GameObject upgradePanel;
    
    [Header("??????? - ??????????????")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    
    [Header("?????")]
    public bool showUpgradeButton = true;
    
    private bool isUpgradeMode = false;

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
                upgradeButtonText.text = "????????";
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
        Debug.Log("????????????");
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Player currentPlayer = GameManager.Instance.currentPlayer;
            
            if (BuildingDataConfig.Instance != null)
            {
                var upgradeableBuildings = BuildingDataConfig.Instance.GetPlayerUpgradeableBuildings(currentPlayer);
                
                if (upgradeableBuildings.Count == 0)
                {
                    Debug.Log("??§á??????????");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast("??§á??????????", 2f);
                    }
                    return;
                }
                
                Debug.Log($"???? {upgradeableBuildings.Count} ????????????");
                
                isUpgradeMode = true;
                BuildingDataConfig.Instance.EnterUpgradeMode(currentPlayer);
                
                if (buildingSelectionPanel != null)
                {
                    buildingSelectionPanel.SetActive(false);
                }
                
                if (upgradePanel != null)
                {
                    upgradePanel.SetActive(true);
                }
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast($"??????????????????\n??????????: {upgradeableBuildings.Count} ??", 3f);
                }
            }
        }
        else
        {
            Debug.LogWarning("GameManager.Instance ?? currentPlayer ??????");
        }
    }

    public void OnExitUpgradeMode()
    {
        isUpgradeMode = false;
        
        if (BuildingDataConfig.Instance != null)
        {
            BuildingDataConfig.Instance.ExitUpgradeMode();
        }
        
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
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
