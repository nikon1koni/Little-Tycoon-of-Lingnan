using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("Day/Night Cycle")]
    public float dayLength = 120f;
    public bool autoCycle = true;
    
    [Header("Sun Settings")]
    public Light sunLight;
    public Vector3 daySunRotation = new Vector3(60, 0, 0);
    public Vector3 nightSunRotation = new Vector3(-60, 180, 0);
    
    [Header("Color Settings")]
    public Gradient skyGradient;
    public Gradient sunColorGradient;
    public Gradient ambientGradient;
    
    [Header("Fog Settings")]
    public bool useFog = true;
    public Gradient fogColorGradient;
    public AnimationCurve fogDensityCurve;
    
    [Header("Speed Multiplier")]
    public float timeScale = 1f;
    
    private float currentTime;
    private Material skyboxMaterial;
    private bool initialized = false;
    
    void Awake()
    {
        InitializeDefaults();
        initialized = true;
    }
    
    void Start()
    {
        skyboxMaterial = RenderSettings.skybox;
        
        if (skyboxMaterial == null)
        {
            skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
            RenderSettings.skybox = skyboxMaterial;
        }
        
        Debug.Log("SkyboxController initialized! Day Length: " + dayLength);
    }
    
    void InitializeDefaults()
    {
        if (skyGradient == null || skyGradient.colorKeys.Length == 0)
        {
            skyGradient = new Gradient();
            GradientColorKey[] skyKeys = new GradientColorKey[3];
            skyKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);
            skyKeys[1] = new GradientColorKey(new Color(0.25f, 0.45f, 0.8f), 0.5f);
            skyKeys[2] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);
            skyGradient.colorKeys = skyKeys;
        }
        
        if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
        {
            sunColorGradient = new Gradient();
            GradientColorKey[] sunKeys = new GradientColorKey[3];
            sunKeys[0] = new GradientColorKey(new Color(0.4f, 0.15f, 0.1f), 0f);
            sunKeys[1] = new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0.5f);
            sunKeys[2] = new GradientColorKey(new Color(0.4f, 0.15f, 0.1f), 1f);
            sunColorGradient.colorKeys = sunKeys;
        }
        
        if (ambientGradient == null || ambientGradient.colorKeys.Length == 0)
        {
            ambientGradient = new Gradient();
            GradientColorKey[] ambientKeys = new GradientColorKey[3];
            ambientKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.15f), 0f);
            ambientKeys[1] = new GradientColorKey(new Color(0.85f, 0.9f, 1f), 0.5f);
            ambientKeys[2] = new GradientColorKey(new Color(0.1f, 0.1f, 0.15f), 1f);
            ambientGradient.colorKeys = ambientKeys;
        }
        
        if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
        {
            fogColorGradient = new Gradient();
            GradientColorKey[] fogKeys = new GradientColorKey[3];
            fogKeys[0] = new GradientColorKey(new Color(0.15f, 0.15f, 0.2f), 0f);
            fogKeys[1] = new GradientColorKey(new Color(0.6f, 0.65f, 0.7f), 0.5f);
            fogKeys[2] = new GradientColorKey(new Color(0.15f, 0.15f, 0.2f), 1f);
            fogColorGradient.colorKeys = fogKeys;
        }
        
        if (fogDensityCurve == null)
        {
            fogDensityCurve = new AnimationCurve();
            fogDensityCurve.AddKey(0f, 0.02f);
            fogDensityCurve.AddKey(0.5f, 0.01f);
            fogDensityCurve.AddKey(1f, 0.02f);
        }
    }
    
    void Update()
    {
        if (!initialized) 
        {
            InitializeDefaults();
            initialized = true;
        }
        
        if (autoCycle)
        {
            currentTime += Time.deltaTime * timeScale / dayLength;
            if (currentTime >= 1f)
                currentTime = 0f;
            
            UpdateSkybox();
        }
    }
    
    public void SetTime(float time)
    {
        currentTime = Mathf.Clamp01(time);
        UpdateSkybox();
        Debug.Log("Time set to: " + currentTime + " (" + GetTimeOfDay() + ")");
    }
    
    public void ToggleCycle()
    {
        autoCycle = !autoCycle;
        Debug.Log("Auto cycle: " + autoCycle);
    }
    
    void UpdateSkybox()
    {
        float t = currentTime;
        
        Color skyColor = skyGradient.Evaluate(t);
        Color sunColor = sunColorGradient.Evaluate(t);
        Color ambientColor = ambientGradient.Evaluate(t);
        
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetColor("_SkyColor", skyColor);
            skyboxMaterial.SetColor("_HorizonColor", skyColor * 0.8f);
            skyboxMaterial.SetColor("_GroundColor", skyColor * 0.3f);
        }
        
        if (sunLight != null)
        {
            sunLight.color = sunColor;
            sunLight.intensity = Mathf.Lerp(0.2f, 1.5f, Mathf.Abs(Mathf.Sin(t * Mathf.PI)));
            
            float sunAngle = t * 360f;
            sunLight.transform.rotation = Quaternion.Euler(
                Mathf.Lerp(nightSunRotation.x, daySunRotation.x, (Mathf.Sin(t * Mathf.PI) + 1) / 2),
                sunAngle * 15f,
                0
            );
        }
        
        RenderSettings.ambientLight = ambientColor;
        
        if (useFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColorGradient.Evaluate(t);
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(t);
        }
        else
        {
            RenderSettings.fog = false;
        }
    }
    
    public float GetCurrentTime()
    {
        return currentTime;
    }
    
    public string GetTimeOfDay()
    {
        float t = currentTime;
        
        if (t < 0.25f) return "Ò¹Íí";
        else if (t < 0.35f) return "ÀèÃ÷";
        else if (t < 0.45f) return "Çå³¿";
        else if (t < 0.70f) return "°×Ìì";
        else if (t < 0.80f) return "°øÍí";
        else if (t < 0.90f) return "»Æ»è";
        else return "Ò¹Íí";
    }
    
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InitializeDefaults();
        }
    }
}
