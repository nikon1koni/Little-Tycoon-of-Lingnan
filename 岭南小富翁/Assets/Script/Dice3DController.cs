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
    public float rotationSpeed = 1200f; // 初始旋转速度 (度/秒)
    public float rollDuration = 2f;     // 总滚动时间 (秒)

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

        InitializeFaceRotations();
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

    void InitializeFaceRotations()
    {
        faceRotations[1] = Quaternion.Euler(face1Rotation);
        faceRotations[2] = Quaternion.Euler(face2Rotation);
        faceRotations[3] = Quaternion.Euler(face3Rotation);
        faceRotations[4] = Quaternion.Euler(face4Rotation);
        faceRotations[5] = Quaternion.Euler(face5Rotation);
        faceRotations[6] = Quaternion.Euler(face6Rotation);
    }

    public void SetDiceFace(int value)
    {
        if (dice3D != null && value >= 1 && value <= 6)
        {
            dice3D.transform.localRotation = faceRotations[value];
        }
    }

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

    IEnumerator RollDice3D()
    {
        isRolling = true;

        if (rollDiceButton != null)
            rollDiceButton.interactable = false;

        if (diceResultText != null)
            diceResultText.text = "?";

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.DiceRoll);

        currentDiceValue = Random.Range(1, 7);
        Debug.Log($"骰子将显示: {currentDiceValue}点");

        Quaternion targetRotation = faceRotations[currentDiceValue];
        
        // 确定一个稳定的随机旋转轴（整个动画过程中保持一致）
        Vector3 rotationAxis = Random.onUnitSphere.normalized;
        
        float elapsed = 0f;
        Quaternion startRotation = dice3D.transform.localRotation;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rollDuration);
            
            // 计算当前速度：使用指数衰减曲线，开始快，逐渐变慢
            // 公式：speed = initialSpeed * e^(-k * t^2) 其中 k 控制减速程度
            float speedCurve = Mathf.Exp(-2.5f * t * t);
            float currentSpeed = rotationSpeed * speedCurve;
            
            // 计算当前帧的旋转量
            float rotationThisFrame = currentSpeed * Time.deltaTime;
            
            // 应用旋转：绕着固定的轴旋转
            dice3D.transform.localRotation *= 
                Quaternion.AngleAxis(rotationThisFrame, rotationAxis);
            
            yield return null;
        }
        
        // 最后平滑过渡到目标位置（确保精确停在目标点数）
        float smoothTime = 0.4f;
        float smoothElapsed = 0f;
        Quaternion rotationBeforeSmooth = dice3D.transform.localRotation;
        
        while (smoothElapsed < smoothTime)
        {
            smoothElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(smoothElapsed / smoothTime);
            
            // 使用 SmoothStep 让过渡更加自然
            float smoothT = SmoothStep(t);
            
            // 从当前位置 Slerp 到目标位置
            dice3D.transform.localRotation = 
                Quaternion.Slerp(rotationBeforeSmooth, targetRotation, smoothT);
            
            yield return null;
        }

        // 最终精确定位
        dice3D.transform.localRotation = targetRotation;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.DiceStop);

        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        Debug.Log($"骰子最终显示: {currentDiceValue}点");

        isRolling = false;
    }

    // SmoothStep 缓动函数：开始慢，中间快，结束慢
    float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public int GetDiceValue()
    {
        return currentDiceValue;
    }

    public bool CanRoll()
    {
        return !isRolling;
    }

    public void ResetDice()
    {
        currentDiceValue = 0;
        SetDiceFace(1);
        if (diceResultText != null)
            diceResultText.text = "";
    }
}
