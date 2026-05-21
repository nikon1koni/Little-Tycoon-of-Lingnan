using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSelectionPanelController : MonoBehaviour
{
    [Header("???????")]
    public GameObject buildingSelectionPanel;
    public GameObject upgradePanel;
    
    [Header("???????")]
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;
    
    [Header("????")]
    public bool showUpgradeButton = true;
    
    private bool isUpgradeMode = false;
    
    [Header("????")]
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
        Debug.Log("??????????");
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Player currentPlayer = GameManager.Instance.currentPlayer;
            
            if (BuildingDataConfig.Instance != null)
            {
                var upgradeableBuildings = BuildingDataConfig.Instance.GetPlayerUpgradeableBuildings(currentPlayer);
                
                if (upgradeableBuildings.Count == 0)
                {
                    Debug.Log("??????????????");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast("??????????????", 2f);
                    }
                    return;
                }
                
                Debug.Log("?????????????");
                
                isUpgradeMode = true;
                
                // ?????BuildingDataConfig??EnterUpgradeMode
                BuildingDataConfig.Instance.EnterUpgradeMode(currentPlayer);
                
                if (buildingSelectionPanel != null)
                {
                    buildingSelectionPanel.SetActive(false);
                }
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("??????????", 3f);
                }
            }
        }
        else
        {
            Debug.LogWarning("GameManager.Instance ?? currentPlayer ???");
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