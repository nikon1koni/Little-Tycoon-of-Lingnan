using UnityEngine;

public class SimpleDayNight : MonoBehaviour
{
    [Header("Day/Night Settings")]
    public float dayLength = 60f;
    public Light sunLight;
    
    [Header("Day Colors")]
    public Color daySkyTop = new Color(0.15f, 0.3f, 0.6f);
    public Color daySkyHorizon = new Color(0.5f, 0.65f, 0.85f);
    public Color daySkyGround = new Color(0.3f, 0.4f, 0.5f);
    public Color daySun = new Color(1f, 0.95f, 0.7f);
    public Color dayAmbient = new Color(0.6f, 0.7f, 0.8f);
    public Color dayShadow = new Color(0.15f, 0.2f, 0.3f);
    public Color dayFog = new Color(0.6f, 0.7f, 0.85f);
    
    [Header("Night Colors")]
    public Color nightSkyTop = new Color(0.05f, 0.05f, 0.15f);
    public Color nightSkyHorizon = new Color(0.1f, 0.12f, 0.2f);
    public Color nightSkyGround = new Color(0.03f, 0.03f, 0.08f);
    public Color nightSun = new Color(0.2f, 0.15f, 0.3f);
    public Color nightAmbient = new Color(0.05f, 0.05f, 0.1f);
    public Color nightShadow = new Color(0.05f, 0.05f, 0.1f);
    public Color nightFog = new Color(0.1f, 0.1f, 0.15f);
    
    [Header("Skybox Material")]
    public Material skyboxMaterial;
    
    [Header("Lighting Settings")]
    [Range(0, 1)] public float ambientIntensity = 1f;
    [Range(0, 1)] public float fogDensity = 0.01f;
    public bool useFog = true;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    private float time;
    private bool initialized = false;
    private FogMode fogModeBackup;
    private Color fogColorBackup;
    private float fogDensityBackup;
    
    void Start()
    {
        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
        
        if (skyboxMaterial == null)
        {
            Debug.LogError("Skybox 材质未设置！");
            enabled = false;
            return;
        }
        
        if (sunLight == null)
        {
            Debug.LogError("太阳光未设置！");
            enabled = false;
            return;
        }
        
        fogModeBackup = RenderSettings.fogMode;
        fogColorBackup = RenderSettings.fogColor;
        fogDensityBackup = RenderSettings.fogDensity;
        
        Debug.Log("SimpleDayNight 初始化完成！");
        initialized = true;
    }
    
    void Update()
    {
        if (!initialized) return;
        
        time += Time.deltaTime / dayLength;
        if (time > 1) time = 0;
        
        float t = time;
        float sunProgress = Mathf.Abs(Mathf.Sin(t * Mathf.PI));
        
        Color skyTop = Color.Lerp(nightSkyTop, daySkyTop, sunProgress);
        Color skyHorizon = Color.Lerp(nightSkyHorizon, daySkyHorizon, sunProgress);
        Color skyGround = Color.Lerp(nightSkyGround, daySkyGround, sunProgress);
        Color sunColor = Color.Lerp(nightSun, daySun, sunProgress);
        Color ambientColor = Color.Lerp(nightAmbient, dayAmbient, sunProgress) * ambientIntensity;
        Color shadowColor = Color.Lerp(nightShadow, dayShadow, sunProgress);
        Color fogColor = Color.Lerp(nightFog, dayFog, sunProgress);
        Color atmosphereColor = Color.Lerp(new Color(0.2f, 0.2f, 0.3f), new Color(0.8f, 0.6f, 0.4f), sunProgress);
        
        float sunIntensity = Mathf.Lerp(0.1f, 2.5f, sunProgress);
        float lightIntensity = Mathf.Lerp(0.1f, 1.5f, sunProgress);
        
        skyboxMaterial.SetColor("_SkyColor", skyTop);
        skyboxMaterial.SetColor("_HorizonColor", skyHorizon);
        skyboxMaterial.SetColor("_GroundColor", skyGround);
        skyboxMaterial.SetColor("_SunColor", sunColor);
        skyboxMaterial.SetFloat("_SunIntensity", sunIntensity);
        skyboxMaterial.SetColor("_AtmosphereColor", atmosphereColor);
        
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.subtractiveShadowColor = shadowColor;
        
        if (useFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity * Mathf.Lerp(0.5f, 1f, sunProgress);
        }
        
        float sunYaw = t * 360f;
        float sunPitch = Mathf.Lerp(-60f, 60f, (Mathf.Sin(t * Mathf.PI) + 1) / 2);
        Quaternion sunRot = Quaternion.Euler(sunPitch, sunYaw, 0);
        
        sunLight.transform.rotation = sunRot;
        sunLight.color = sunColor;
        sunLight.intensity = lightIntensity;
        
        Vector3 forward = sunRot * Vector3.forward;
        Vector4 sunDir = new Vector4(-forward.x, -forward.y, -forward.z, 0);
        skyboxMaterial.SetVector("_WorldSpaceLightPos0", sunDir);
        
        if (debugMode && Time.frameCount % 30 == 0)
        {
            string period = "夜晚";
            if (t < 0.25f || t >= 0.9f) period = "夜晚";
            else if (t < 0.45f) period = "黎明";
            else if (t < 0.7f) period = "白天";
            else period = "黄昏";
            
            Debug.Log($"时间: {period} {t:F2} | 太阳角度: {sunPitch:F0}度 | 环境亮度: {(int)(ambientColor.grayscale * 100)}%");
        }
    }
    
    void OnDestroy()
    {
        RenderSettings.fogMode = fogModeBackup;
        RenderSettings.fogColor = fogColorBackup;
        RenderSettings.fogDensity = fogDensityBackup;
    }
}