using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanel : MonoBehaviour
{
    [Header("UI ÒýÓÃ")]
    public TextMeshProUGUI titleText;
    public Image eventImage;
    public TextMeshProUGUI descriptionText;
    public Transform optionsContainer;
    public Button optionButtonPrefab;
    public Button closeButton;

    private EventData currentEvent;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    public void ShowEvent(EventData eventData)
    {
        if (eventData == null)
        {
            Debug.LogWarning("EventData is null!");
            return;
        }

        currentEvent = eventData;

        if (titleText != null)
            titleText.text = eventData.eventTitle;

        if (eventImage != null)
        {
            eventImage.sprite = eventData.eventImage;
            eventImage.enabled = eventData.eventImage != null;
        }

        if (descriptionText != null)
            descriptionText.text = eventData.eventDescription;

        ClearOptions();
        CreateOptions(eventData.options);

        gameObject.SetActive(true);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);
    }

    void ClearOptions()
    {
        if (optionsContainer == null) return;

        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void CreateOptions(EventData.EventOption[] options)
    {
        if (optionsContainer == null || optionButtonPrefab == null) return;

        foreach (EventData.EventOption option in options)
        {
            Button button = Instantiate(optionButtonPrefab, optionsContainer);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = option.optionText;

            button.onClick.AddListener(() =>
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.UIClick);

                option.onOptionSelected?.Invoke();
                HidePanel();
            });
        }
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIClose);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEventPanelClosed();
        }
    }
}
