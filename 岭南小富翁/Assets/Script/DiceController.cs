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
    public float animationDuration = 0.3f;
    public float animationInterval = 0.05f;

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

        if (diceResultText != null)
            diceResultText.text = "";  // 改为空字符串

        if (diceAnimationText != null)
            diceAnimationText.text = "";

        //// 绑定按钮事件
        //if (rollDiceButton != null)
        //{
        //    rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        //}
    }

    public void StartRollDice()
    {
        if (!isRolling)
        {
            StartCoroutine(RollDice());
        }
        else
        {
            Debug.Log("骰子正在滚动中，忽略重复点击");
        }

    }
    // 按钮点击事件
    //public void OnRollDiceClicked()
    //{
    //    if (!isRolling && gameManager != null && gameManager.CanRollDice())
    //    {
    //        StartCoroutine(RollDice());
    //    }
    //}

    public IEnumerator RollDice()
    {
        isRolling = true;

        // 1. 立即清空"准备"文本
        if (diceResultText != null)
            diceResultText.text = "";

        if (diceAnimationText != null)
            diceAnimationText.text = "";

        // 2. 禁用按钮
        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        // 3. 播放骰子音效
        if (rollSound != null)
            audioSource.PlayOneShot(rollSound);

        // 4. 快速动画（0.2秒）
        float elapsed = 0f;
        int animationSteps = 5; // 显示5个随机数字

        for (int i = 0; i < animationSteps; i++)
        {
            int randomValue = Random.Range(minDiceValue, maxDiceValue + 1);

            // 同时更新两个文本
            if (diceAnimationText != null)
                diceAnimationText.text = randomValue.ToString();

            if (diceResultText != null)
                diceResultText.text = randomValue.ToString();

            yield return new WaitForSeconds(0.04f); // 每个数字显示0.04秒
        }

        // 5. 生成最终结果
        currentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);
        Debug.Log($"骰子结果: {currentDiceValue}");

        // 6. 立即显示最终结果
        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (diceAnimationText != null)
            diceAnimationText.text = currentDiceValue.ToString();

        // 7. 播放停止音效
        if (stopSound != null)
            audioSource.PlayOneShot(stopSound);

        // 8. 立即通知GameManager开始移动
        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

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