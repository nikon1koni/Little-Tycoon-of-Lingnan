using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    [Header("Day/Night Cycle")]
    public float dayLength = 120f;
    public bool autoCycle = true;
    
    [Header("Sun Settings")]
    public Light sunLight;
    public Vector3 daySunRotation = new Vector3(60, 0, 0);
    public Vector3 nightSunRotation = new Vector3(-60, 0, 0);
    
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
    
    void Awake()
    {
        skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
        RenderSettings.skybox = skyboxMaterial;
        
        if (skyGradient == null)
        {
            skyGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.6f, 0.8f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f);
            skyGradient.colorKeys = colorKeys;
        }
    }
    
    void Update()
    {
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
    }
    
    public void ToggleCycle()
    {
        autoCycle = !autoCycle;
    }
    
    void UpdateSkybox()
    {
        float t = currentTime;
        
        Color skyColor = skyGradient.Evaluate(t);
        Color sunColor = sunColorGradient.Evaluate(t);
        Color ambientColor = ambientGradient.Evaluate(t);
        
        skyboxMaterial.SetColor("_SkyColor", skyColor);
        skyboxMaterial.SetColor("_HorizonColor", skyColor * 0.8f);
        skyboxMaterial.SetColor("_GroundColor", skyColor * 0.3f);
        
        if (sunLight != null)
        {
            sunLight.color = sunColor;
            sunLight.intensity = Mathf.Lerp(0.2f, 1.5f, Mathf.Abs(Mathf.Sin(t * Mathf.PI)));
            
            Quaternion dayRotation = Quaternion.Euler(daySunRotation);
            Quaternion nightRotation = Quaternion.Euler(nightSunRotation);
            sunLight.transform.rotation = Quaternion.Lerp(nightRotation, dayRotation, Mathf.Sin(t * Mathf.PI));
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
        else if (t < 0.45f) return "ÈÕ³ö";
        else if (t < 0.70f) return "°×Ìì";
        else if (t < 0.80f) return "ÈÕÂä";
        else if (t < 0.90f) return "»Æ»è";
        else return "Ò¹Íí";
    }
}