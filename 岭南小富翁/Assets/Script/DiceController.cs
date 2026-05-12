using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiceController : MonoBehaviour
{
    [Header("UI����")]
    public Button rollDiceButton;
    public Text diceResultText;
    public Text diceAnimationText; // ��ʾ�����ı�

    [Header("��������")]
    public int minDiceValue = 1;
    public int maxDiceValue = 6;
    public float animationDuration = 0.3f;
    public float animationInterval = 0.05f;

    [Header("��Ч")]
    public AudioClip rollSound;
    public AudioClip stopSound;

    [Header("��ҹ���")]
    public GameManager gameManager; // ����GameManager
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
            diceResultText.text = "";  // ��Ϊ���ַ���

        if (diceAnimationText != null)
            diceAnimationText.text = "";

        //// �󶨰�ť�¼�
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
            Debug.Log("�������ڹ����У������ظ����");
        }

    }
    // ��ť����¼�
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

        // 1. �������"׼��"�ı�
        if (diceResultText != null)
            diceResultText.text = "";

        if (diceAnimationText != null)
            diceAnimationText.text = "";

        // 2. ���ð�ť
        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        // 3. ����������Ч
        if (rollSound != null)
            audioSource.PlayOneShot(rollSound);

        // 4. ���ٶ�����0.2�룩
        float elapsed = 0f;
        int animationSteps = 5; // ��ʾ5���������

        for (int i = 0; i < animationSteps; i++)
        {
            int randomValue = Random.Range(minDiceValue, maxDiceValue + 1);

            // ͬʱ���������ı�
            if (diceAnimationText != null)
                diceAnimationText.text = randomValue.ToString();

            if (diceResultText != null)
                diceResultText.text = randomValue.ToString();

            yield return new WaitForSeconds(0.04f); // ÿ��������ʾ0.04��
        }

        // 5. �������ս��
        currentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);
        Debug.Log($"���ӽ��: {currentDiceValue}");

        // 6. ������ʾ���ս��
        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (diceAnimationText != null)
            diceAnimationText.text = currentDiceValue.ToString();

        // 7. ����ֹͣ��Ч
        if (stopSound != null)
            audioSource.PlayOneShot(stopSound);

        // 8. ����֪ͨGameManager��ʼ�ƶ�
        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        isRolling = false;
    }

    // ��ȡ����ֵ
    public int GetDiceValue()
    {
        return currentDiceValue;
    }

    // ��������
    public void ResetDice()
    {
        currentDiceValue = 0;
        if (diceResultText != null)
            diceResultText.text = "׼��";
    }

    // �Ƿ����������
    public bool CanRoll()
    {
        return !isRolling;
    }
}