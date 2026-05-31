using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDropZone : MonoBehaviour
{
    [Header("????????")]
    public string zoneName = "?????????";
    public Color normalColor = new Color(0.2f, 0.2f, 0.8f, 0.3f);
    public Color hoverColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color activeColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

    [Header("UI???")]
    public TextMeshProUGUI zoneNameText;
    public TextMeshProUGUI instructionText;
    public Image zoneImage;

    private bool isActive = true;
    private int hoveringCards = 0;

    void Start()
    {
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        // ??????UI???????????
        if (zoneImage == null)
        {
            zoneImage = GetComponent<Image>();
            if (zoneImage == null)
            {
                zoneImage = gameObject.AddComponent<Image>();
            }
        }

        if (zoneImage != null)
        {
            zoneImage.color = normalColor;
        }

        if (zoneNameText == null)
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

        if (instructionText == null)
        {
            GameObject instructObj = new GameObject("Instruction");
            instructObj.transform.SetParent(transform);
            instructionText = instructObj.AddComponent<TextMeshProUGUI>();
            instructionText.text = "???????????????";
            instructionText.fontSize = 12;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.color = new Color(1, 1, 1, 0.7f);

            RectTransform instructRect = instructionText.GetComponent<RectTransform>();
            instructRect.anchorMin = Vector2.zero;
            instructRect.anchorMax = Vector2.one;
            instructRect.offsetMin = new Vector2(10, 10);
            instructRect.offsetMax = new Vector2(-10, -30);
        }

        // ????????????????
        gameObject.tag = "ItemDropZone";
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
