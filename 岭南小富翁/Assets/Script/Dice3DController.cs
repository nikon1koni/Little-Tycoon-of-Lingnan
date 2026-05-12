using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Dice3DController : MonoBehaviour
{
    [Header("3D骰子对象")]
    public GameObject dice3D; // pCube1对象

    [Header("UI引用")]
    public Button rollDiceButton;
    public Text diceResultText;

    [Header("旋转参数")]
    public float rotationSpeed = 720f; // 每秒旋转角度
    public float rollDuration = 2f; // 滚动持续时间

    [Header("音效")]
    public AudioClip rollSound;
    public AudioClip stopSound;

    [Header("引用")]
    public GameManager gameManager;

    [Header("各面旋转配置 (请用DiceFaceCalibrator校准)")]
    public Vector3 face1Rotation = Vector3.zero;
    public Vector3 face2Rotation = new Vector3(90, 0, 0);
    public Vector3 face3Rotation = new Vector3(0, 0, 90);
    public Vector3 face4Rotation = new Vector3(0, 0, -90);
    public Vector3 face5Rotation = new Vector3(-90, 0, 0);
    public Vector3 face6Rotation = new Vector3(0, 180, 0);

    [Header("调试预览")]
    [Range(1, 6)]
    public int previewFace = 1;

    private AudioSource audioSource;
    private bool isRolling = false;
    private int currentDiceValue = 0;
    private Quaternion[] faceRotations = new Quaternion[7];

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始化每个点数的旋转角度
        InitializeFaceRotations();

        // 骰子初始旋转到1点
        SetDiceFace(1);

        if (diceResultText != null)
            diceResultText.text = "";
    }

    void OnValidate()
    {
        if (dice3D != null && Application.isEditor && !Application.isPlaying)
        {
            InitializeFaceRotations();
            SetDiceFace(previewFace);
        }
    }

    // 初始化每个点数对应的旋转
    void InitializeFaceRotations()
    {
        faceRotations[1] = Quaternion.Euler(face1Rotation);
        faceRotations[2] = Quaternion.Euler(face2Rotation);
        faceRotations[3] = Quaternion.Euler(face3Rotation);
        faceRotations[4] = Quaternion.Euler(face4Rotation);
        faceRotations[5] = Quaternion.Euler(face5Rotation);
        faceRotations[6] = Quaternion.Euler(face6Rotation);
    }

    // 设置骰子显示特定点数
    public void SetDiceFace(int value)
    {
        if (dice3D != null && value >= 1 && value <= 6)
        {
            dice3D.transform.localRotation = faceRotations[value];
        }
    }

    // 开始掷骰子
    public void StartRollDice()
    {
        if (!isRolling)
        {
            StartCoroutine(RollDice3D());
        }
        else
        {
            Debug.Log("骰子正在滚动中，请稍候...");
        }
    }

    // 3D骰子滚动协程
    IEnumerator RollDice3D()
    {
        isRolling = true;

        // 1. 禁用按钮
        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        // 2. 清空UI
        if (diceResultText != null)
            diceResultText.text = "?";

        // 3. 播放滚动音效
        if (rollSound != null)
            audioSource.PlayOneShot(rollSound);

        // 4. 随机决定最终点数
        currentDiceValue = Random.Range(1, 7);
        Debug.Log($"骰子将显示: {currentDiceValue}点");

        // 5. 骰子疯狂旋转
        float elapsed = 0f;
        Vector3 randomRotationAxis = Random.onUnitSphere;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            // 随机旋转
            dice3D.transform.Rotate(
                Random.Range(-rotationSpeed, rotationSpeed) * Time.deltaTime,
                Random.Range(-rotationSpeed, rotationSpeed) * Time.deltaTime,
                Random.Range(-rotationSpeed, rotationSpeed) * Time.deltaTime
            );

            yield return null;
        }

        // 6. 平滑旋转到目标点数
        Quaternion startRotation = dice3D.transform.localRotation;
        Quaternion targetRotation = faceRotations[currentDiceValue];

        // 先做一个完整的额外旋转增加视觉效果
        float smoothTime = 0.5f;
        float smoothElapsed = 0f;

        while (smoothElapsed < smoothTime)
        {
            smoothElapsed += Time.deltaTime;
            float t = smoothElapsed / smoothTime;

            // 使用Slerp平滑过渡
            dice3D.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // 7. 确保最终位置准确
        dice3D.transform.localRotation = targetRotation;

        // 8. 播放停止音效
        if (stopSound != null)
            audioSource.PlayOneShot(stopSound);

        // 9. 更新UI显示结果
        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        // 10. 通知GameManager
        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        Debug.Log($"骰子最终显示: {currentDiceValue}点");

        isRolling = false;
    }

    // 获取当前骰子值
    public int GetDiceValue()
    {
        return currentDiceValue;
    }

    // 是否可以掷骰子
    public bool CanRoll()
    {
        return !isRolling;
    }

    // 重置骰子
    public void ResetDice()
    {
        currentDiceValue = 0;
        SetDiceFace(1);
        if (diceResultText != null)
            diceResultText.text = "";
    }
}
