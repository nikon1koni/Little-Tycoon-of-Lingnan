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
        
        for (int i = 0; i < options.Length; i++)
        {
            EventData.EventOption option = options[i];
            int optionIndex = i;  // 闭包捕获索引
            
            Debug.Log($"创建按钮 [{optionIndex}]: {option.optionText}");
            
            Button button = Instantiate(optionButtonPrefab, optionsContainer);
            button.gameObject.SetActive(true);
            
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = option.optionText;
                Debug.Log($"按钮文本已设置: {option.optionText}");
            }
            else
            {
                Debug.LogWarning("按钮没有找到 TextMeshPro 组件！");
            }

            // 检查是否可以支付（使用选项单独配置或全局配置）
            int costToPay = option.optionCostAmount > 0 ? option.optionCostAmount : currentEvent.costAmount;
            bool canAfford = true;
            
            if (costToPay > 0 && currentPlayer != null)
            {
                canAfford = currentPlayer.cash >= costToPay;
                
                if (!canAfford)
                {
                    Debug.Log($"选项 [{optionIndex}] 需要支付 {costToPay} 金币，但玩家只有 {currentPlayer.cash} 金币，禁用按钮");
                    
                    // 设置按钮为半透明/禁用状态
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
                Debug.Log($"=== 选项按钮被点击 [{finalIndex}] ===");
                Debug.Log($"EventEffectHandler.Instance: {EventEffectHandler.Instance != null}");
                Debug.Log($"currentPlayer: {currentPlayer?.playerName ?? "NULL"}");
                Debug.Log($"currentEvent: {currentEvent?.eventTitle ?? "NULL"}");
                
                if (!finalCanAfford)
                {
                    Debug.LogWarning("金币不足！");
                    if (UIManager.Instance != null)
                    {
                        UIManager.ShowToastStatic("金币不足，无法选择此选项！", 2f);
                    }
                    return;
                }

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(SFXClip.UIClick);

                // 调用原有的UnityEvent回调
                option.onOptionSelected?.Invoke();
                
                // ? 新增：调用事件效果处理器
                if (EventEffectHandler.Instance != null && currentPlayer != null)
                {
                    Debug.Log($"? 调用 ProcessOption: player={currentPlayer.playerName}, event={currentEvent.eventTitle}, option={finalIndex}");
                    EventEffectHandler.Instance.ProcessOption(currentPlayer, currentEvent, finalIndex);
                }
                else
                {
                    Debug.LogError($"? 无法调用 ProcessOption! Instance={EventEffectHandler.Instance != null}, Player={currentPlayer != null}");
                }

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
