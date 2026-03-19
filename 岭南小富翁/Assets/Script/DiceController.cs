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

        // 初始化UI
        if (diceResultText != null)
            diceResultText.text = "准备";

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

        // 关键修改1：立即生成结果，而不是等动画结束
        currentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);
        Debug.Log($"立即生成骰子结果: {currentDiceValue}");

        // 关键修改2：立即通知GameManager开始移动
        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        // 禁用按钮
        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        // 播放骰子动画（后台运行，不阻塞移动）
        if (rollSound != null)
            audioSource.PlayOneShot(rollSound);

        // 骰子动画（快速完成，0.3秒）
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            int randomValue = Random.Range(minDiceValue, maxDiceValue + 1);

            if (diceAnimationText != null)
                diceAnimationText.text = randomValue.ToString();

            yield return new WaitForSeconds(animationInterval);
        }

        // 显示最终结果
        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (diceAnimationText != null)
            diceAnimationText.text = currentDiceValue.ToString();

        // 播放停止音效
        if (stopSound != null)
            audioSource.PlayOneShot(stopSound);

        isRolling = false;

        // 注意：按钮在玩家移动结束后由GameManager重新启用
        // 这里不再启用按钮
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