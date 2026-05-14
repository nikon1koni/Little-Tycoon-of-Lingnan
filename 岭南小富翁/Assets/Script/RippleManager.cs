
using UnityEngine;

public class RippleManager : MonoBehaviour
{
    public static RippleManager Instance;

    public float rippleDuration = 2f;
    public float maxRippleRadius = 1.5f;
    public float radiusMultiplier = 1f;
    public Material groundRainMaterial;
    public Transform groundTransform;
    
    private Vector3 currentPosition;
    private float startTime;
    private bool hasRipple;
    
    private const string RIPPLE_POSITION_PROPERTY = "_RipplePosition";
    private const string RIPPLE_RADIUS_PROPERTY = "_RippleRadius";
    private const string RIPPLE_ALPHA_PROPERTY = "_RippleAlpha";
    private const string RIPPLE_PROGRESS_PROPERTY = "_RippleProgress";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
            groundRainMaterial.SetFloat(RIPPLE_PROGRESS_PROPERTY, 0f);
        }
    }

    void Update()
    {
        UpdateShaderData();
    }

    public void AddRipple(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition;
        if (groundTransform != null)
        {
            localPosition = groundTransform.InverseTransformPoint(worldPosition);
        }
        
        currentPosition = localPosition;
        startTime = Time.time;
        hasRipple = true;
    }

    void UpdateShaderData()
    {
        if (groundRainMaterial == null)
        {
            return;
        }
        
        if (hasRipple)
        {
            float progress = (Time.time - startTime) / rippleDuration;
            
            if (progress >= 1f)
            {
                hasRipple = false;
                groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, Vector3.zero);
                groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, 0f);
                groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, 0f);
                groundRainMaterial.SetFloat(RIPPLE_PROGRESS_PROPERTY, 0f);
            }
            else
            {
                float currentRadius = maxRippleRadius * radiusMultiplier * progress;
                float currentAlpha = 1f - progress;
                
                groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, currentPosition);
                groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, currentRadius);
                groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, currentAlpha);
                groundRainMaterial.SetFloat(RIPPLE_PROGRESS_PROPERTY, progress);
            }
        }
        else
        {
            groundRainMaterial.SetVector(RIPPLE_POSITION_PROPERTY, Vector3.zero);
            groundRainMaterial.SetFloat(RIPPLE_RADIUS_PROPERTY, 0f);
            groundRainMaterial.SetFloat(RIPPLE_ALPHA_PROPERTY, 0f);
            groundRainMaterial.SetFloat(RIPPLE_PROGRESS_PROPERTY, 0f);
        }
    }
}
