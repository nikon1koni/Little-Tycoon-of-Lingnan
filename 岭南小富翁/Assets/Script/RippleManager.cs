using UnityEngine;

public class RippleManager : MonoBehaviour
{
    public static RippleManager Instance;

    [Header("????????")]
    public float rippleDuration = 2f;
    public float maxRippleRadius = 1.5f;
    
    [Header("????????")]
    public Material groundRainMaterial;
    
    private Vector3 currentRipplePosition;
    private float currentRippleStartTime;
    private float currentRippleDuration;
    private float currentRippleMaxRadius;
    private bool hasActiveRipple;
    
    private const string RIPPLE_POSITION_PROPERTY = "_RipplePosition";
    private const string RIPPLE_RADIUS_PROPERTY = "_RippleRadius";
    private const string RIPPLE_ALPHA_PROPERTY = "_RippleAlpha";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (groundRainMaterial != null)
        {
            groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, Vector3.zero);
            groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, 0f);
            groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, 0f);
        }
    }

    void Update()
    {
        UpdateShaderData();
    }

    public void AddRipple(Vector3 worldPosition)
    {
        hasActiveRipple = true;
        currentRipplePosition = worldPosition;
        currentRippleStartTime = Time.time;
        currentRippleDuration = rippleDuration;
        currentRippleMaxRadius = maxRippleRadius * Random.Range(0.8f, 1.2f);
    }

    void UpdateShaderData()
    {
        if (groundRainMaterial == null) return;
        
        if (hasActiveRipple)
        {
            float progress = (Time.time - currentRippleStartTime) / currentRippleDuration;
            
            if (progress >= 1f)
            {
                hasActiveRipple = false;
                groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, Vector3.zero);
                groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, 0f);
                groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, 0f);
            }
            else
            {
                float currentRadius = currentRippleMaxRadius * progress;
                float currentAlpha = 1f - progress;
                
                groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, currentRipplePosition);
                groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, currentRadius);
                groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, currentAlpha);
            }
        }
        else
        {
            groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, Vector3.zero);
            groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, 0f);
            groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, 0f);
        }
    }
}
