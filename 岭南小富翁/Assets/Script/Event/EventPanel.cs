using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanel : MonoBehaviour
{
    [Header("UI 组件")]
    public TextMeshProUGUI titleText;
    public Image eventImage;
    public TextMeshProUGUI descriptionText;
    public Transform optionsContainer;
    public Button optionButtonPrefab;
    public Button closeButton;

    private EventData currentEvent;
    private Player currentPlayer;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    public void ShowEvent(EventData eventData, Player player = null)
    {
        if (eventData == null)
        {
            Debug.LogWarning("EventData is null!");
            return;
        }

        currentEvent = eventData;
        currentPlayer = player ?? (GameManager.Instance != null ? GameManager.Instance.currentPlayer : null);

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
        Debug.Log("===  ===");
        Debug.Log("optionsContainer : " + (optionsContainer == null));
        Debug.Log("optionButtonPrefab : " + (optionButtonPrefab == null));
        Debug.Log("options.Length: " + (options.Length > 0));

        if (optionsContainer == null || optionButtonPrefab == null) 
        {
            Debug.LogError("");
            return;
        }

        Debug.Log("...");
        
        for (int i = 0; i < options.Length; i++)
        {
            EventData.EventOption option = options[i];
            int optionIndex = i;  // 
            
            Debug.Log($" [{optionIndex}]: {option.optionText}");
            
            Button button = Instantiate(optionButtonPrefab, optionsContainer);
            button.gameObject.SetActive(true);
            
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = option.optionText;
                Debug.Log($": {option.optionText}");
            }
            else
            {
                Debug.LogWarning(" TextMeshPro ");
            }

            // 
            int costToPay = option.optionCostAmount > 0 ? option.optionCostAmount : currentEvent.costAmount;
            bool canAfford = true;
            
            if (costToPay > 0 && currentPlayer != null)
            {
                canAfford = currentPlayer.cash >= costToPay;
                
                if (!canAfford)
                {
                    Debug.Log($" [{optionIndex}]  {costToPay}  {currentPlayer.cash} ");
                    
                    // /
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        Color disabledColor = buttonImage.color;
                        disabledColor.a = 0.5f;
                        buttonImage.color = disabledColor;
                    }
                    
                    if (buttonText != null)
                    {
                        Color textColor = buttonText.color;
                        textColor.a = 0.5f;
                        buttonText.color = textColor;
                    }
                }
            }

            bool finalCanAfford = canAfford;
            int finalIndex = optionIndex;
            
            button.onClick.AddListener(() =>
            {
                Debug.Log($"=== 选项 [{finalIndex}] 被点击 ===");
                Debug.Log($"EventEffectHandler.Instance: {EventEffectHandler.Instance != null}");
                Debug.Log($"currentPlayer: {currentPlayer?.playerName ?? "NULL"}");
                Debug.Log($"currentEvent: {currentEvent?.eventTitle ?? "NULL"}");
                
                if (!finalCanAfford)
                {
                    Debug.LogWarning("无法支付选项费用");
                    if (UIManager.Instance != null)
                    {
                        UIManager.ShowToastStatic("", 2f);
                    }
                    return;
                }

                // 播放事件选择音效
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayEventSelectSound();
                
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.UIClick);

                // 触发UnityEvent
                option.onOptionSelected.Invoke();
                
                // 处理选项效果
                if (EventEffectHandler.Instance != null && currentPlayer != null)
                {
                    Debug.Log($"  ProcessOption: player={currentPlayer.playerName}, event={currentEvent.eventTitle}, option={finalIndex}");
                    EventEffectHandler.Instance.ProcessOption(currentPlayer, currentEvent, finalIndex);
                }
                else
                {
                    Debug.LogError($"  ProcessOption! Instance={EventEffectHandler.Instance != null}, Player={currentPlayer != null}");
                }

                HidePanel();
            });
        }
        
        Debug.Log("===  ===");
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
