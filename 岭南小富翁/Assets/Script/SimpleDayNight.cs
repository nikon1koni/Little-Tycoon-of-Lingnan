using UnityEngine;

public class SimpleDayNight : MonoBehaviour
{
    [Header("Day/Night Settings")]
    public float dayLength = 60f;
    public Light sunLight;
    
    [Header("Day Colors")]
    public Color daySkyTop = new Color(0.15f, 0.3f, 0.6f);
    public Color daySkyHorizon = new Color(0.4f, 0.6f, 0.85f);
    public Color daySkyGround = new Color(0.2f, 0.3f, 0.4f);
    public Color daySun = new Color(1f, 0.95f, 0.7f);
    public Color dayAmbient = new Color(0.9f, 0.95f, 1f);
    public Color dayAtmosphere = new Color(0.8f, 0.6f, 0.4f);
    
    [Header("Night Colors")]
    public Color nightSkyTop = new Color(0.05f, 0.05f, 0.15f);
    public Color nightSkyHorizon = new Color(0.1f, 0.1f, 0.25f);
    public Color nightSkyGround = new Color(0.05f, 0.05f, 0.1f);
    public Color nightSun = new Color(0.3f, 0.2f, 0.4f);
    public Color nightAmbient = new Color(0.05f, 0.05f, 0.1f);
    public Color nightAtmosphere = new Color(0.2f, 0.2f, 0.4f);
    
    [Header("Skybox Material")]
    public Material skyboxMaterial;
    
    private float time;
    private bool initialized = false;
    
    void Start()
    {
        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
        
        if (skyboxMaterial == null)
        {
            Debug.LogError("? 没有设置天空盒材质！");
            enabled = false;
            return;
        }
        
        if (sunLight == null)
        {
            Debug.LogError("? 没有设置太阳！");
            enabled = false;
            return;
        }
        
        Debug.Log("? SimpleDayNight 启动!");
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
        Color ambientColor = Color.Lerp(nightAmbient, dayAmbient, sunProgress);
        Color atmosphereColor = Color.Lerp(nightAtmosphere, dayAtmosphere, sunProgress);
        
        float sunIntensity = Mathf.Lerp(0.1f, 2.5f, sunProgress);
        
        skyboxMaterial.SetColor("_SkyColor", skyTop);
        skyboxMaterial.SetColor("_HorizonColor", skyHorizon);
        skyboxMaterial.SetColor("_GroundColor", skyGround);
        skyboxMaterial.SetColor("_SunColor", sunColor);
        skyboxMaterial.SetFloat("_SunIntensity", sunIntensity);
        skyboxMaterial.SetColor("_AtmosphereColor", atmosphereColor);
        
        RenderSettings.ambientLight = ambientColor;
        
        float sunYaw = t * 360f;
        float sunPitch = Mathf.Lerp(-60f, 60f, (Mathf.Sin(t * Mathf.PI) + 1) / 2);
        Quaternion sunRot = Quaternion.Euler(sunPitch, sunYaw, 0);
        
        sunLight.transform.rotation = sunRot;
        sunLight.color = sunColor;
        sunLight.intensity = Mathf.Lerp(0.1f, 1.5f, sunProgress);
        
        Vector3 forward = sunRot * Vector3.forward;
        Vector4 sunDir = new Vector4(-forward.x, -forward.y, -forward.z, 0);
        skyboxMaterial.SetVector("_WorldSpaceLightPos0", sunDir);
        
        if (Time.frameCount % 30 == 0)
        {
            string period = "白天";
            if (t < 0.25f || t >= 0.9f) period = "夜晚";
            else if (t < 0.45f) period = "日出";
            else if (t < 0.7f) period = "白天";
            else period = "日落";
            
            Debug.Log($"? {period} {t:F2} | 太阳角度: {sunPitch:F0}° | 太阳强度: {sunIntensity:F1}");
        }
    }
}