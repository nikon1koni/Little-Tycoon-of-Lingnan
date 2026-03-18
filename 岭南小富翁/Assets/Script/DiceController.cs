using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiceController : MonoBehaviour
{
    [Header("UI引用")]
    public Button rollDiceButton;
    public Text diceResultText;
    public Text diceAnimationText; // 显示动画文本

    [Header("骰子设置")]
    public int minDiceValue = 1;
    public int maxDiceValue = 6;
    public float animationDuration = 1f;
    public float animationInterval = 0.1f;

    [Header("音效")]
    public AudioClip rollSound;
    public AudioClip stopSound;

    [Header("玩家管理")]
    public GameManager gameManager; // 引用GameManager
    private AudioSource audioSource;

    private bool isRolling = false;
    private int currentDiceValue = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始化UI
        if (diceResultText != null)
            diceResultText.text = "准备";

        // 绑定按钮事件
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        }
    }

    // 按钮点击事件
    public void OnRollDiceClicked()
    {
        if (!isRolling && gameManager != null && gameManager.CanRollDice())
        {
            StartCoroutine(RollDice());
        }
    }

    public IEnumerator RollDice()
    {
        isRolling = true;

        // 禁用按钮
        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        // 播放音效
        if (rollSound != null)
            audioSource.PlayOneShot(rollSound);

        // 骰子动画
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            // 随机显示数字
            int randomValue = Random.Range(minDiceValue, maxDiceValue + 1);

            // 更新UI
            if (diceAnimationText != null)
                diceAnimationText.text = randomValue.ToString();

            yield return new WaitForSeconds(animationInterval);
        }

        // 最终结果
        currentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);

        // 显示最终结果
        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (diceAnimationText != null)
            diceAnimationText.text = currentDiceValue.ToString();

        // 播放停止音效
        if (stopSound != null)
            audioSource.PlayOneShot(stopSound);

        Debug.Log($"掷骰结果: {currentDiceValue}");

        // 通知GameManager
        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        // 重新启用按钮
        yield return new WaitForSeconds(0.5f);
        if (rollDiceButton != null)
            rollDiceButton.interactable = true;

        isRolling = false;
    }

    // 获取骰子值
    public int GetDiceValue()
    {
        return currentDiceValue;
    }

    // 重置骰子
    public void ResetDice()
    {
        currentDiceValue = 0;
        if (diceResultText != null)
            diceResultText.text = "准备";
    }

    // 是否可以掷骰子
    public bool CanRoll()
    {
        return !isRolling;
    }
}