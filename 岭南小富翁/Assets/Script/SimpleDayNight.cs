using UnityEngine;

public class SimpleDayNight : MonoBehaviour
{
    [Header("Day/Night Settings")]
    public float dayLength = 60f;
    public Light sunLight;
    
    [Header("Colors")]
    public Color daySky = new Color(0.25f, 0.45f, 0.8f);
    public Color nightSky = new Color(0.1f, 0.1f, 0.2f);
    public Color daySun = new Color(1f, 0.9f, 0.6f);
    public Color nightSun = new Color(0.4f, 0.15f, 0.1f);
    public Color dayAmbient = new Color(0.85f, 0.9f, 1f);
    public Color nightAmbient = new Color(0.1f, 0.1f, 0.15f);
    
    [Header("Skybox Material")]
    public Material skyboxMaterial;
    
    private float time;
    
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
    }
    
    void Update()
    {
        time += Time.deltaTime / dayLength;
        if (time > 1) time = 0;
        
        float t = time;
        float sunProgress = Mathf.Abs(Mathf.Sin(t * Mathf.PI));
        
        Color skyColor = Color.Lerp(nightSky, daySky, sunProgress);
        Color sunColor = Color.Lerp(nightSun, daySun, sunProgress);
        Color ambientColor = Color.Lerp(nightAmbient, dayAmbient, sunProgress);
        
        skyboxMaterial.SetColor("_SkyColor", skyColor);
        skyboxMaterial.SetColor("_HorizonColor", skyColor * 0.8f);
        skyboxMaterial.SetColor("_GroundColor", skyColor * 0.3f);
        skyboxMaterial.SetColor("_SunColor", sunColor);
        skyboxMaterial.SetFloat("_SunIntensity", Mathf.Lerp(0.2f, 2f, sunProgress));
        
        float sunYaw = t * 360f;
        float sunPitch = Mathf.Lerp(-60f, 60f, (Mathf.Sin(t * Mathf.PI) + 1) / 2);
        Quaternion sunRot = Quaternion.Euler(sunPitch, sunYaw, 0);
        
        sunLight.transform.rotation = sunRot;
        sunLight.color = sunColor;
        sunLight.intensity = Mathf.Lerp(0.2f, 1.5f, sunProgress);
        
        Vector3 forward = sunRot * Vector3.forward;
        Vector4 sunDir = new Vector4(-forward.x, -forward.y, -forward.z, 0);
        skyboxMaterial.SetVector("_WorldSpaceLightPos0", sunDir);
        
        RenderSettings.ambientLight = ambientColor;
        
        if (Time.frameCount % 30 == 0)
        {
            string period = "白天";
            if (t < 0.25f || t >= 0.9f) period = "夜晚";
            else if (t < 0.45f) period = "日出";
            else if (t < 0.7f) period = "白天";
            else period = "日落";
            
            Debug.Log($"? {period} {t:F2} | 太阳角度: {sunPitch:F0}°");
        }
    }
}