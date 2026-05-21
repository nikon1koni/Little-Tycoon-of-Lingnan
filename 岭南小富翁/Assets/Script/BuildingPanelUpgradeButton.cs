using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingPanelUpgradeButton : MonoBehaviour
{
    [Header("???????")]
    public Button upgradeButton;
    public TextMeshProUGUI buttonText;
    
    [Header("???????")]
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
            buttonText.text = "????????";
        }
    }
    
    private void OnUpgradeButtonClicked()
    {
        Debug.Log("????????????");
        
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Player currentPlayer = GameManager.Instance.currentPlayer;
            
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
            
            Debug.Log($"???? {upgradeableBuildings.Count} ????????????");
            
            BuildingDataConfig.Instance.EnterUpgradeMode(currentPlayer);
            
            if (buildingSelectionPanel != null)
            {
                buildingSelectionPanel.SetActive(false);
            }
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast($"??????????????????", 3f);
            }
        }
        else
        {
            Debug.LogWarning("??????????????????????????");
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
