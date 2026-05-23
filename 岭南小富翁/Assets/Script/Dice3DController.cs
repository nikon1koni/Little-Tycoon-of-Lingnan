using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Dice3DController : MonoBehaviour
{
    [Header("3D???????")]
    public GameObject dice3D; // pCube1????

    [Header("UI????")]
    public Button rollDiceButton;
    public Text diceResultText;

    [Header("???????")]
    public float rotationSpeed = 1200f; // ????????? (??/??)
    public float rollDuration = 2f;     // ???????? (??)

    [Header("??????")]
    [Range(0.5f, 3f)]
    public float rollSpeedMultiplier = 1f; // ?????????????

    [Header("??§¹")]
    public AudioClip rollSound;
    public AudioClip stopSound;

    [Header("????")]
    public GameManager gameManager;

    [Header("??????????? (????DiceFaceCalibrator§µ?)")]
    public Vector3 face1Rotation = Vector3.zero;
    public Vector3 face2Rotation = new Vector3(90, 0, 0);
    public Vector3 face3Rotation = new Vector3(0, 0, 90);
    public Vector3 face4Rotation = new Vector3(0, 0, -90);
    public Vector3 face5Rotation = new Vector3(-90, 0, 0);
    public Vector3 face6Rotation = new Vector3(0, 180, 0);

    [Header("???????")]
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
            Debug.Log("????????????§µ??????...");
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
        Debug.Log($"????????: {currentDiceValue}??");

        Quaternion targetRotation = faceRotations[currentDiceValue];
        
        // ????????????????????????????????§Ò???????
        Vector3 rotationAxis = Random.onUnitSphere.normalized;
        
        float elapsed = 0f;
        Quaternion startRotation = dice3D.transform.localRotation;
        float adjustedDuration = rollDuration / rollSpeedMultiplier; // ?????????

        while (elapsed < adjustedDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / adjustedDuration);
            
            // ?????????????????????????????????
            // ?????speed = initialSpeed * e^(-k * t^2) ???? k ?????????
            float speedCurve = Mathf.Exp(-2.5f * t * t);
            float currentSpeed = rotationSpeed * speedCurve * rollSpeedMultiplier;
            
            // ?????????????
            float rotationThisFrame = currentSpeed * Time.deltaTime;
            
            // ?????????????????????
            dice3D.transform.localRotation *= 
                Quaternion.AngleAxis(rotationThisFrame, rotationAxis);
            
            yield return null;
        }
        
        // ??????????????¦Ë????????????????????
        float smoothTime = 0.4f / rollSpeedMultiplier; // ?????????
        float smoothElapsed = 0f;
        Quaternion rotationBeforeSmooth = dice3D.transform.localRotation;
        
        while (smoothElapsed < smoothTime)
        {
            smoothElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(smoothElapsed / smoothTime);
            
            // ??? SmoothStep ?¨´?????????
            float smoothT = SmoothStep(t);
            
            // ????¦Ë?? Slerp ?????¦Ë??
            dice3D.transform.localRotation = 
                Quaternion.Slerp(rotationBeforeSmooth, targetRotation, smoothT);
            
            yield return null;
        }

        // ????????¦Ë
        dice3D.transform.localRotation = targetRotation;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(SFXClip.DiceStop);

        if (diceResultText != null)
            diceResultText.text = currentDiceValue.ToString();

        if (gameManager != null)
        {
            gameManager.OnDiceRolled(currentDiceValue);
        }

        Debug.Log($"???????????: {currentDiceValue}??");

        isRolling = false;
    }

    // SmoothStep ??????????????????§Þ????????
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

    // ?????????????????
    public void SetRollSpeedMultiplier(float multiplier)
    {
        rollSpeedMultiplier = Mathf.Clamp(multiplier, 0.5f, 3f);
        Debug.Log($"Dice3DController: ??????????????? {rollSpeedMultiplier}x");
    }

    // ????????????????
    public float GetRollSpeedMultiplier()
    {
        return rollSpeedMultiplier;
    }
}
