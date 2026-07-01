﻿﻿﻿﻿﻿using UnityEngine;
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
    private RectTransform activeTooltipRect;
    private RectTransform tooltipCanvasRect;
    private Camera tooltipCamera;
    
    [Header("")]
    public int roundThreshold = 3;              // 
    public Color buffBelowThresholdColor = Color.red;      // Buff
    public Color buffAboveThresholdColor = Color.green;     // Buff
    public Color debuffBelowThresholdColor = Color.green;   // Debuff
    public Color debuffAboveThresholdColor = Color.red;     // Debuff
    
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

        activeTooltipRect = activeTooltip.GetComponent<RectTransform>();
        tooltipCanvasRect = parentTransform as RectTransform;
        Canvas parentCanvas = parentTransform.GetComponent<Canvas>();
        tooltipCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera : null;

        // 统一锚点/轴心，改为跟随鼠标定位（与建筑描述提示一致），避免窗口缩放时错位
        if (activeTooltipRect != null)
        {
            activeTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            activeTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            activeTooltipRect.pivot = new Vector2(0f, 1f);
        }

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

        activeTooltip.transform.SetAsLastSibling();
        UpdateTooltipPosition();
    }

    private void Update()
    {
        if (activeTooltip != null)
        {
            UpdateTooltipPosition();
        }
    }

    // 让提示框跟随鼠标，并限制在画布范围内（窗口缩放安全）
    private void UpdateTooltipPosition()
    {
        if (activeTooltipRect == null || tooltipCanvasRect == null) return;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tooltipCanvasRect, Input.mousePosition, tooltipCamera, out localPoint))
            return;

        Vector2 size = activeTooltipRect.rect.size;
        Vector2 pos = localPoint + new Vector2(tooltipOffset.x, -tooltipOffset.y);

        float halfW = tooltipCanvasRect.rect.width * 0.5f;
        float halfH = tooltipCanvasRect.rect.height * 0.5f;

        if (pos.x + size.x > halfW) pos.x = localPoint.x - tooltipOffset.x - size.x;
        if (pos.x < -halfW) pos.x = -halfW;
        if (pos.y - size.y < -halfH) pos.y = -halfH + size.y;
        if (pos.y > halfH) pos.y = halfH;

        activeTooltipRect.anchoredPosition = pos;
    }
    
    private void HideTooltip()
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
            activeTooltipRect = null;
        }
    }
    
    private string GetBuffDescription()
    {
        string resultDescription = "";
        
        if (!string.IsNullOrEmpty(currentBuff.customDescription))
        {
            resultDescription = currentBuff.customDescription;
            resultDescription += "\n" + GetRemainingTimeText();
            return resultDescription;
        }
        
        resultDescription += $"<b>{BuildingData.GetBuffEffectName(currentBuff.effectType)}</b>\n";
        resultDescription += $"+{currentBuff.value * 100:F1}%\n";
        resultDescription += $"来源：{currentBuff.sourceName}\n";
        
        resultDescription += GetRemainingTimeText();
        
        return resultDescription;
    }
    
    private string GetRemainingTimeText()
    {
        string timeText = "";
        
        if (currentBuff.effectType == BuildingData.BuffEffect.NextRollMultiplier)
        {
            timeText = "生效：<color=green>下次掷骰</color>";
        }
        else if (currentBuff.isPermanent)
        {
            timeText = "剩余：<color=green>永久</color>";
        }
        else if (currentBuff.useRoundTimer)
        {
            bool isDebuff = IsDebuff();
            Color textColor = GetTimeColor(isDebuff, currentBuff.remainingRounds);
            string colorHex = ColorToHex(textColor);
            timeText = $"剩余回合：<color={colorHex}>{currentBuff.remainingRounds}</color>";
        }
        else
        {
            timeText = $"剩余时间：{currentBuff.remainingTime:F1}秒";
        }
        
        return timeText;
    }
    
    private bool IsDebuff()
    {
        if (currentBuff.effectType == BuildingData.BuffEffect.Bankrupt ||
            currentBuff.effectType == BuildingData.BuffEffect.IncomeReduction)
        {
            return true;
        }

        if (currentBuff.effectType == BuildingData.BuffEffect.NextRollMultiplier && currentBuff.value < 1f)
        {
            return true;
        }
        
        return false;
    }
    
    private Color GetTimeColor(bool isDebuff, int remainingRounds)
    {
        if (isDebuff)
        {
            // Debuff
            return remainingRounds <= roundThreshold ? debuffBelowThresholdColor : debuffAboveThresholdColor;
        }
        else
        {
            // Buff
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
