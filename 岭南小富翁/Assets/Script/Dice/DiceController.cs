using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiceController : MonoBehaviour
{
    [Header("UI引用")]
    public Button rollDiceButton;
    public Text diceResultText;
    public Text diceAnimationText;

    [Header("骰子参数")]
    public int minDiceValue = 1;
    public int maxDiceValue = 6;
    public float animationDuration = 0.3f;

    [Header("音效")]
    public AudioClip rollSound;
    public AudioClip stopSound;

    [Header("引用")]
    public GameManager gameManager;

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
            diceResultText.text = "";

        if (diceAnimationText != null)
            diceAnimationText.text = "";
    }

    public void StartRollDice()
    {
        if (!isRolling)
        {
            // 播放骰子点击音效
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayDiceClickSound();
            
            StartCoroutine(RollDice());
        }
        else
        {
            Debug.Log("骰子正在滚动中，请稍候...");
        }
    }

    public IEnumerator RollDice()
    {
        isRolling = true;

        if (diceResultText != null)
            diceResultText.text = "";

        if (diceAnimationText != null)
            diceAnimationText.text = "";

        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.DiceRoll);

        float elapsed = 0f;
        int animationSteps = 5;

        for (int i = 0; i < animationSteps; i++)
        {
            int randomValue = Random.Range(minDiceValue, maxDiceValue + 1);

            if (diceAnimationText != null)
                diceAnimationText.text = randomValue.ToString();

            if (diceResultText != null)
                diceResultText.text = randomValue.ToString();

            yield return new WaitForSeconds(0.04f);
        }

        currentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);

        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (diceAnimationText != null)
            diceAnimationText.text = currentDiceValue.ToString();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.DiceStop);

        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        isRolling = false;
    }

    public int GetDiceValue()
    {
        return currentDiceValue;
    }

    public void ResetDice()
    {
        currentDiceValue = 0;
        if (diceResultText != null)
            diceResultText.text = "准备";
    }

    public bool CanRoll()
    {
        return !isRolling;
    }
}
