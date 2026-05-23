using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI ???")]
    public Image iconImage;
    public TextMeshProUGUI stackText;
    
    [Header("Buff ??????????")]
    public GameObject tooltipPrefab;
    private GameObject activeTooltip;
    
    private BuffSystem.Buff currentBuff;
    
    public void Initialize(BuffSystem.Buff buff, Sprite icon)
    {
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
        ShowTooltip();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }
    
    private void ShowTooltip()
    {
        if (currentBuff == null || tooltipPrefab == null) return;
        
        if (UIManager.Instance != null && UIManager.Instance.mainCanvas != null)
        {
            activeTooltip = Instantiate(tooltipPrefab, UIManager.Instance.mainCanvas.transform);
            
            // ??????????????
            RectTransform tooltipRect = activeTooltip.GetComponent<RectTransform>();
            RectTransform iconRect = GetComponent<RectTransform>();
            
            Vector3 worldPos = iconRect.position;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                UIManager.Instance.mainCanvas.transform as RectTransform,
                screenPos,
                UIManager.Instance.mainCanvas.worldCamera,
                out localPoint
            );
            
            tooltipRect.localPosition = localPoint + new Vector2(0, 60);
            
            // ??????????
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
        string description = "";
        description += $"<b>{BuildingData.GetBuffEffectName(currentBuff.effectType)}</b>\n";
        description += $"+{currentBuff.value * 100:F1}%\n";
        description += $"???: {currentBuff.sourceName}\n";
        
        if (currentBuff.isPermanent)
        {
            description += "<color=green>????</color>";
        }
        else if (currentBuff.useRoundTimer)
        {
            description += $"??? {currentBuff.remainingRounds} ???";
        }
        else
        {
            description += $"??? {currentBuff.remainingTime:F1} ??";
        }
        
        return description;
    }
    
    private void OnDestroy()
    {
        HideTooltip();
    }
}
