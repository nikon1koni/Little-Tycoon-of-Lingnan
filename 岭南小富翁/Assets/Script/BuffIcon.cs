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
            // 对于自定义描述，添加剩余时间行
            resultDescription = currentBuff.customDescription;
            resultDescription += "\n" + GetRemainingTimeText();
            return resultDescription;
        }
        
        resultDescription += $"<b>{BuildingData.GetBuffEffectName(currentBuff.effectType)}</b>\n";
        resultDescription += $"+{currentBuff.value * 100:F1}%\n";
        resultDescription += $"来源: {currentBuff.sourceName}\n";
        
        // 添加剩余时间行
        resultDescription += GetRemainingTimeText();
        
        return resultDescription;
    }
    
    private string GetRemainingTimeText()
    {
        string timeText = "";
        
        if (currentBuff.isPermanent)
        {
            timeText = "剩余回合：永远";
        }
        else if (currentBuff.useRoundTimer)
        {
            timeText = $"剩余回合：{currentBuff.remainingRounds}";
        }
        else
        {
            timeText = $"剩余时间：{currentBuff.remainingTime:F1}秒";
        }
        
        return timeText;
    }
    
    private void OnDestroy()
    {
        HideTooltip();
    }
}
