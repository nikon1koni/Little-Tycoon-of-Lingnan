using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDropZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public string zoneName = "Drop Zone";
    public Color normalColor = new Color(0.2f, 0.2f, 0.8f, 0.3f);
    public Color hoverColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color activeColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

    [Header("UI Elements")]
    public TextMeshProUGUI zoneNameText;
    public TextMeshProUGUI instructionText;
    public Image zoneImage;

    [Header("Text Visibility")]
    public bool showZoneName = true;
    public bool showInstruction = true;

    private bool isActive = true;
    private int hoveringCards = 0;

    void Start()
    {
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        if (zoneImage == null)
        {
            zoneImage = GetComponent<Image>();
            if (zoneImage == null)
            {
                zoneImage = gameObject.AddComponent<Image>();
                zoneImage.raycastTarget = true;
            }
        }

        if (zoneImage != null)
        {
            zoneImage.color = normalColor;
            zoneImage.raycastTarget = false;
        }

        if (showZoneName && zoneNameText == null)
        {
            GameObject textObj = new GameObject("ZoneName");
            textObj.transform.SetParent(transform);
            zoneNameText = textObj.AddComponent<TextMeshProUGUI>();
            zoneNameText.text = zoneName;
            zoneNameText.fontSize = 16;
            zoneNameText.alignment = TextAlignmentOptions.Center;
            zoneNameText.color = Color.white;

            RectTransform textRect = zoneNameText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.one;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(200, 30);
        }

        if (zoneNameText != null)
        {
            zoneNameText.gameObject.SetActive(showZoneName);
        }

        if (showInstruction && instructionText == null)
        {
            GameObject instructObj = new GameObject("Instruction");
            instructObj.transform.SetParent(transform);
            instructionText = instructObj.AddComponent<TextMeshProUGUI>();
            instructionText.text = "Drag items here to use";
            instructionText.fontSize = 12;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.color = new Color(1, 1, 1, 0.7f);

            RectTransform instructRect = instructionText.GetComponent<RectTransform>();
            instructRect.anchorMin = Vector2.zero;
            instructRect.anchorMax = Vector2.one;
            instructRect.offsetMin = new Vector2(10, 10);
            instructRect.offsetMax = new Vector2(-10, -30);
        }

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(showInstruction);
        }

        gameObject.tag = "ItemDropZone";
    }

    public void ToggleZoneName()
    {
        showZoneName = !showZoneName;
        if (zoneNameText != null)
        {
            zoneNameText.gameObject.SetActive(showZoneName);
        }
    }

    public void ToggleInstruction()
    {
        showInstruction = !showInstruction;
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(showInstruction);
        }
    }

    public void OnCardEnter()
    {
        hoveringCards++;
        if (isActive && zoneImage != null)
        {
            zoneImage.color = hoverColor;
        }
    }

    public void OnCardExit()
    {
        hoveringCards--;
        if (hoveringCards <= 0 && zoneImage != null)
        {
            hoveringCards = 0;
            zoneImage.color = normalColor;
        }
    }

    public void Activate()
    {
        isActive = true;
        if (zoneImage != null)
        {
            zoneImage.color = normalColor;
        }
    }

    public void Deactivate()
    {
        isActive = false;
        if (zoneImage != null)
        {
            zoneImage.color = activeColor;
        }
    }

    public void EnableDropDetection()
    {
        if (zoneImage != null)
        {
            zoneImage.raycastTarget = true;
        }
    }

    public void DisableDropDetection()
    {
        if (zoneImage != null)
        {
            zoneImage.raycastTarget = false;
        }
    }

    public bool IsActive()
    {
        return isActive;
    }

    void OnValidate()
    {
        if (zoneImage != null && Application.isPlaying == false)
        {
            zoneImage.color = normalColor;
        }
    }
}
