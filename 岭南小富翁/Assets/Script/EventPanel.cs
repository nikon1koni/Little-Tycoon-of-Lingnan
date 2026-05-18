using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanel : MonoBehaviour
{
    [Header("UI 引用")]
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
        Debug.Log("=== 创建选项按钮 ===");
        Debug.Log("optionsContainer 是否为空: " + (optionsContainer == null));
        Debug.Log("optionButtonPrefab 是否为空: " + (optionButtonPrefab == null));
        Debug.Log("选项数量: " + (options?.Length ?? 0));

        if (optionsContainer == null || optionButtonPrefab == null) 
        {
            Debug.LogError("选项容器或按钮预制体为空！");
            return;
        }

        Debug.Log("开始创建按钮...");
        foreach (EventData.EventOption option in options)
        {
            Debug.Log("创建按钮: " + option.optionText);
            Button button = Instantiate(optionButtonPrefab, optionsContainer);
            button.gameObject.SetActive(true);
            
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = option.optionText;
                Debug.Log("按钮文本已设置: " + option.optionText);
            }
            else
            {
                Debug.LogWarning("按钮没有找到 TextMeshPro 组件！");
            }

            button.onClick.AddListener(() =>
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.UIClick);

                option.onOptionSelected?.Invoke();
                HidePanel();
            });
        }
        Debug.Log("=== 选项按钮创建完成 ===");
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
