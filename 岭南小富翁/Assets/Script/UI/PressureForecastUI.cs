using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 压力系统预告 UI：把本组件挂到任意带文本的 UI 物体上即可显示
// “还有 x 次掷骰将会迎来一次恶性事件”。文本引用留空时会自动查找。
public class PressureForecastUI : MonoBehaviour
{
    [Header("文本引用（留空则自动获取本物体或子物体上的文本组件）")]
    [Tooltip("优先使用的 TextMeshProUGUI 组件")]
    public TextMeshProUGUI forecastText;
    [Tooltip("没有 TMP 时可改用的传统 UI Text 组件")]
    public Text legacyText;

    [Header("文案设置")]
    [Tooltip("正常预告文案，{0} 会替换为剩余掷骰次数")]
    public string forecastFormat = "还有 {0} 次掷骰将会迎来一次恶性事件";
    [Tooltip("剩余次数为 0（即将触发）时显示的文案")]
    public string imminentText = "本回合将迎来一次恶性事件";
    [Tooltip("压力系统关闭时显示的文案（留空则清空文本）")]
    public string disabledText = "";

    [Header("刷新设置")]
    [Tooltip("轮询刷新间隔（秒），使用非缩放时间")]
    public float refreshInterval = 0.2f;

    private float timer = 0f;
    private int lastShownValue = int.MinValue;

    void Awake()
    {
        AutoBindText();
    }

    void OnEnable()
    {
        lastShownValue = int.MinValue;
        timer = 0f;
        Refresh();
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            Refresh();
        }
    }

    void AutoBindText()
    {
        if (forecastText == null)
        {
            forecastText = GetComponent<TextMeshProUGUI>();
            if (forecastText == null)
                forecastText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (forecastText == null && legacyText == null)
        {
            legacyText = GetComponent<Text>();
            if (legacyText == null)
                legacyText = GetComponentInChildren<Text>();
        }
    }

    void Refresh()
    {
        if (GameManager.Instance == null)
            return;

        int remaining = GameManager.Instance.RollsUntilNextPressure;
        if (remaining == lastShownValue)
            return;

        lastShownValue = remaining;

        string content;
        if (remaining < 0)
            content = disabledText;
        else if (remaining == 0)
            content = imminentText;
        else
            content = string.Format(forecastFormat, remaining);

        SetText(content);
    }

    void SetText(string content)
    {
        if (forecastText != null)
            forecastText.text = content;
        if (legacyText != null)
            legacyText.text = content;
    }
}