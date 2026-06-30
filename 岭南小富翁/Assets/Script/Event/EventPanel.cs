using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanel : MonoBehaviour
{
    [Header("UI ")]
    public TextMeshProUGUI titleText;
    public Image eventImage;
    public TextMeshProUGUI descriptionText;
    public Transform optionsContainer;
    public Button optionButtonPrefab;
    public Button closeButton;

    [Header("????")]
    public Sprite optionShallowSprite;
    public Sprite optionDeepSprite;

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
            if (eventData.eventImage != null)
            {
                eventImage.sprite = eventData.eventImage;
                eventImage.enabled = true;
                eventImage.gameObject.SetActive(true);
                eventImage.transform.SetAsFirstSibling();
            }
            else
            {
                eventImage.enabled = eventImage.sprite != null;
            }
        }
        else
        {
            Debug.LogWarning("EventPanel: eventImage组件为空！");
        }

        if (descriptionText != null)
            descriptionText.text = eventData.eventDescription;

        ClearOptions();
        CreateOptions(eventData.options);

        if (titleText != null)
            titleText.transform.SetAsLastSibling();
        if (descriptionText != null)
            descriptionText.transform.SetAsLastSibling();
        if (optionsContainer != null)
            optionsContainer.SetAsLastSibling();
        if (closeButton != null)
            closeButton.transform.SetAsLastSibling();

        Transform optionShallow = transform.Find("????");
        if (optionShallow != null)
        {
            optionShallow.gameObject.SetActive(true);
            optionShallow.SetAsLastSibling();
            Debug.Log($"选项浅背景: active={optionShallow.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("δ???????????????");
        }
        Transform optionDeep = transform.Find("?????");
        if (optionDeep != null)
        {
            optionDeep.gameObject.SetActive(true);
            optionDeep.SetAsLastSibling();
            Debug.Log($"选项深背景: active={optionDeep.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("δ??????????????");
        }

        gameObject.SetActive(true);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);
        
        StartCoroutine(EnsureImageEnabled());
    }

    private System.Collections.IEnumerator EnsureImageEnabled()
    {
        yield return null;
        
        if (eventImage != null && currentEvent != null && currentEvent.eventImage != null)
        {
            eventImage.enabled = true;
            eventImage.gameObject.SetActive(true);
            eventImage.transform.SetAsFirstSibling();
            Debug.Log($"EventPanel: 延迟确认图片启用 - enabled={eventImage.enabled}, active={eventImage.gameObject.activeSelf}");
        }
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
        Debug.Log($"optionsContainer: {(optionsContainer == null ? "空" : "已设置")}");
        Debug.Log($"optionButtonPrefab: {(optionButtonPrefab == null ? "空" : "已设置")}");
        Debug.Log($"options数量: {(options == null ? 0 : options.Length)}");

        if (optionsContainer == null || optionButtonPrefab == null) 
        {
            Debug.LogError("optionsContainer ?? optionButtonPrefab δ????");
            return;
        }

        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("没有选项数据");
            return;
        }

        Debug.Log("开始创建选项...");
        
        for (int i = 0; i < options.Length; i++)
        {
            EventData.EventOption option = options[i];
            int optionIndex = i;  
            
            Debug.Log($"选项数据 [{optionIndex}]: {option.optionText}");
            
            Button button = Instantiate(optionButtonPrefab, optionsContainer);
            button.gameObject.SetActive(true);
            button.name = $"OptionButton_{i}";
            
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = option.optionText;
                buttonText.enabled = true;
                
                buttonText.enableWordWrapping = true;
                buttonText.overflowMode = TextOverflowModes.Truncate;
                buttonText.fontSize = 20;
                buttonText.enableAutoSizing = true;
                buttonText.fontSizeMin = 12;
                buttonText.fontSizeMax = 24;
                
                Debug.Log($"设置按钮文本: {option.optionText}");
            }
            else
            {
                Debug.LogWarning("δ????????? TextMeshPro ???");
            }

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.enabled = true;
                
                if (i % 2 == 0 && optionShallowSprite != null)
                {
                    buttonImage.sprite = optionShallowSprite;
                    Debug.Log($"使用浅色背景");
                }
                else if (i % 2 == 1 && optionDeepSprite != null)
                {
                    buttonImage.sprite = optionDeepSprite;
                    Debug.Log($"使用深色背景");
                }
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.sizeDelta = new Vector2(500f, buttonRect.sizeDelta.y);
                Debug.Log($"按钮宽度设置为: {buttonRect.sizeDelta.x}");
            }

            int costToPay = option.optionCostAmount > 0 ? option.optionCostAmount : currentEvent.costAmount;
            bool canAfford = true;
            
            if (costToPay > 0 && currentPlayer != null)
            {
                canAfford = currentPlayer.cash >= costToPay;
                
                if (!canAfford)
                {
                    Debug.Log($"选项 [{optionIndex}] 花费 {costToPay} 铜钱，玩家现金 {currentPlayer.cash}");
                    
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
                    Debug.LogWarning("余额不足");
                    if (UIManager.Instance != null)
                    {
                        UIManager.ShowToastStatic("余额不足", 2f);
                    }
                    return;
                }

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayEventSelectSound();
                
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.UIClick);

                option.onOptionSelected.Invoke();
                
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