﻿﻿﻿﻿﻿using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// End settlement scene controller: reads GameResult and shows the stats.
public class EndSceneController : MonoBehaviour
{
    [Header("Settlement Texts")]
    [Tooltip("Final score text")]
    public TextMeshProUGUI scoreText;
    [Tooltip("Buildings placed text")]
    public TextMeshProUGUI buildingCountText;
    [Tooltip("Dice roll count text")]
    public TextMeshProUGUI diceCountText;
    [Tooltip("Gold earned text")]
    public TextMeshProUGUI goldText;
    [Tooltip("Rounds text (optional)")]
    public TextMeshProUGUI roundText;

    [Header("Back")]
    [Tooltip("Back to menu button (optional)")]
    public Button backButton;
    [Tooltip("Scene name to return to; leave empty to disable")]
    public string menuSceneName = "";

    void Start()
    {
        // Chinese labels via \u escapes so the source stays pure ASCII (avoids encoding corruption).
        if (scoreText != null) scoreText.text = $"\u6700\u7EC8\u5F97\u5206\uFF1A{GameResult.Score}";
        if (buildingCountText != null) buildingCountText.text = $"\u653E\u7F6E\u5EFA\u7B51\u6B21\u6570\uFF1A{GameResult.BuildingsPlaced}";
        if (diceCountText != null) diceCountText.text = $"\u6295\u63B7\u6B21\u6570\uFF1A{GameResult.DiceRolls}";
        if (goldText != null) goldText.text = $"\u83B7\u5F97\u91D1\u5E01\uFF1A{GameResult.GoldEarned}";
        if (roundText != null) roundText.text = $"\u8F6E\u6570\uFF1A{GameResult.Rounds}/{GameResult.MaxRounds}";

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnBackButtonClicked()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
