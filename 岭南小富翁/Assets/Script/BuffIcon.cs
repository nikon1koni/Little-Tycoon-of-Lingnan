using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI ")]
    public Image iconImage;
    public TextMeshProUGUI stackText;
    
    [Header("Buff ")]
    public GameObject tooltipPrefab;
    public Vector2 tooltipOffset = new Vector2(50, 50);
    private GameObject activeTooltip;
    
    [Header("?????????????")]
    public int roundThreshold = 3;              // ???????
    public Color buffBelowThresholdColor = Color.red;      // Buff???????????
    public Color buffAboveThresholdColor = Color.green;     // Buff???????????
    public Color debuffBelowThresholdColor = Color.green;   // Debuff???????????
    public Color debuffAboveThresholdColor = Color.red;     // Debuff???????????
    
    private BuffSystem.Buff currentBuff;
    
    public void Initialize(BuffSystem.Buff buff, Sprite icon)
    {
        Debug.Log($"BuffIcon: Initialize called, buff={buff != null}, icon={icon != null}");
        currentBuff = buff;
        
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        
        if (stackText != null)
        {
            stackText.text = "";
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("BuffIcon: OnPointerEnter called");
        ShowTooltip();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("BuffIcon: OnPointerExit called");
        HideTooltip();
    }
    
    private void ShowTooltip()
    {
        Debug.Log($"BuffIcon: ShowTooltip called, currentBuff={currentBuff != null}, tooltipPrefab={tooltipPrefab != null}");
        
        if (currentBuff == null || tooltipPrefab == null) 
        {
            Debug.Log($"BuffIcon: ShowTooltip failed - currentBuff={currentBuff != null}, tooltipPrefab={tooltipPrefab != null}");
            return;
        }
        
        Transform parentTransform = null;
        if (UIManager.Instance != null && UIManager.Instance.mainCanvas != null)
        {
            parentTransform = UIManager.Instance.mainCanvas.transform;
        }
        else
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                parentTransform = canvas.transform;
            }
        }
        
        if (parentTransform == null) return;
        
        activeTooltip = Instantiate(tooltipPrefab, parentTransform);
        Debug.Log($"BuffIcon: Tooltip instantiated successfully");
        
        RectTransform tooltipRect = activeTooltip.GetComponent<RectTransform>();
        RectTransform iconRect = GetComponent<RectTransform>();
        
        Vector2 anchoredPosition = iconRect.anchoredPosition;
        tooltipRect.anchoredPosition = anchoredPosition + tooltipOffset;
        Debug.Log($"BuffIcon: Using tooltipOffset={tooltipOffset}, finalPosition={tooltipRect.anchoredPosition}");
        
        TextMeshProUGUI tooltipText = activeTooltip.GetComponentInChildren<TextMeshProUGUI>();
        if (tooltipText != null)
        {
            tooltipText.text = GetBuffDescription();
        }
        
        Image tooltipBg = activeTooltip.GetComponentInChildren<Image>();
        if (tooltipBg != null)
        {
            tooltipBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }
    }
    
    private void HideTooltip()
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
        }
    }
    
    private string GetBuffDescription()
    {
        string resultDescription = "";
        
        if (!string.IsNullOrEmpty(currentBuff.customDescription))
        {
            // ???????????????????????????
            resultDescription = currentBuff.customDescription;
            resultDescription += "\n" + GetRemainingTimeText();
            return resultDescription;
        }
        
        resultDescription += $"<b>{BuildingData.GetBuffEffectName(currentBuff.effectType)}</b>\n";
        resultDescription += $"+{currentBuff.value * 100:F1}%\n";
        resultDescription += $"???: {currentBuff.sourceName}\n";
        
        // ????????????
        resultDescription += GetRemainingTimeText();
        
        return resultDescription;
    }
    
    private string GetRemainingTimeText()
    {
        string timeText = "";
        
        if (currentBuff.isPermanent)
        {
            timeText = "??????<color=green>???</color>";
        }
        else if (currentBuff.useRoundTimer)
        {
            // ?§Ø???Buff????Debuff
            bool isDebuff = IsDebuff();
            Color textColor = GetTimeColor(isDebuff, currentBuff.remainingRounds);
            string colorHex = ColorToHex(textColor);
            timeText = $"??????<color={colorHex}>{currentBuff.remainingRounds}</color>";
        }
        else
        {
            // ???????????????
            timeText = $"??????{currentBuff.remainingTime:F1}??";
        }
        
        return timeText;
    }
    
    private bool IsDebuff()
    {
        // ?§Ø?????Debuff
        // ???????????????Debuff
        if (currentBuff.effectType == BuildingData.BuffEffect.Bankrupt)
        {
            return true;
        }
        
        // ????BuffEffect???????????Buff
        return false;
    }
    
    private Color GetTimeColor(bool isDebuff, int remainingRounds)
    {
        if (isDebuff)
        {
            // Debuff??????????????????????????
            return remainingRounds <= roundThreshold ? debuffBelowThresholdColor : debuffAboveThresholdColor;
        }
        else
        {
            // Buff??????????????????????????
            return remainingRounds <= roundThreshold ? buffBelowThresholdColor : buffAboveThresholdColor;
        }
    }
    
    private string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255);
        int g = Mathf.RoundToInt(color.g * 255);
        int b = Mathf.RoundToInt(color.b * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
    
    private void OnDestroy()
    {
        HideTooltip();
    }
}
