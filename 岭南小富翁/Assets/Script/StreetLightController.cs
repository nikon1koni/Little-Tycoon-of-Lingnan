using UnityEngine;

public class StreetLightController : MonoBehaviour
{
    [Header("Light Reference")]
    [Tooltip("拖拽点光源子对象到这里")]
    public Light pointLight;
    
    [Header("Light Settings")]
    [Tooltip("夜晚时的目标亮度")]
    public float targetIntensity = 2.0f;
    
    [Tooltip("渐变过渡时间（秒）")]
    public float fadeDuration = 2.0f;
    
    [Tooltip("夜晚开始时间 (0-1, 0=午夜)")]
    [Range(0f, 1f)] public float nightStart = 0.85f;
    
    [Tooltip("夜晚结束时间 (0-1, 0=午夜)")]
    [Range(0f, 1f)] public float nightEnd = 0.35f;
    
    [Header("Position Offset (避免遮挡)")]
    [Tooltip("是否启用位置偏移")]
    public bool usePositionOffset = true;
    
    [Tooltip("点光源相对父对象的偏移位置")]
    public Vector3 lightOffset = new Vector3(0, 0.5f, 0);
    
    [Header("Light Render Settings")]
    [Tooltip("是否忽略自身模型遮挡")]
    public bool ignoreSelfShadow = true;
    
    [Tooltip("是否使用阴影")]
    public bool useShadows = true;
    
    private float currentIntensity;
    private float targetIntensityValue;
    private SimpleDayNight dayNightSystem;
    private Vector3 originalLocalPosition;
    private bool initialized = false;
    
    void Awake()
    {
        if (pointLight == null)
        {
            pointLight = GetComponentInChildren<Light>();
            if (pointLight != null)
            {
                Debug.Log($"自动找到子对象点光源: {pointLight.name}");
            }
        }
        
        if (pointLight != null)
        {
            currentIntensity = pointLight.intensity;
            targetIntensityValue = currentIntensity;
            originalLocalPosition = pointLight.transform.localPosition;
        }
    }
    
    void Start()
    {
        dayNightSystem = FindObjectOfType<SimpleDayNight>();
        if (dayNightSystem == null)
        {
            Debug.LogWarning("未找到 SimpleDayNight 组件，路灯将使用模拟时间");
        }
        
        if (pointLight == null)
        {
            Debug.LogError("未设置点光源！请在 Inspector 中拖拽点光源子对象到 Point Light 字段");
            enabled = false;
            return;
        }
        
        ApplyLightSettings();
        initialized = true;
    }
    
    void ApplyLightSettings()
    {
        if (pointLight == null) return;
        
        if (usePositionOffset)
        {
            pointLight.transform.localPosition = originalLocalPosition + lightOffset;
        }
        
        pointLight.shadows = useShadows ? LightShadows.Soft : LightShadows.None;
        
        if (ignoreSelfShadow)
        {
            Transform parentTransform = transform;
            MeshRenderer[] renderers = parentTransform.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.receiveShadows = false;
            }
        }
    }
    
    void Update()
    {
        if (!initialized) return;
        
        float timeOfDay = GetTimeOfDay();
        UpdateLightState(timeOfDay);
    }
    
    float GetTimeOfDay()
    {
        if (dayNightSystem != null)
        {
            return dayNightSystem.GetTime();
        }
        
        return Mathf.PingPong(Time.time / 60f, 1f);
    }
    
    void UpdateLightState(float time)
    {
        bool isNight = IsNightTime(time);
        
        if (isNight)
        {
            targetIntensityValue = targetIntensity;
        }
        else
        {
            targetIntensityValue = 0f;
        }
        
        if (fadeDuration > 0)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensityValue, Time.deltaTime / fadeDuration);
        }
        else
        {
            currentIntensity = targetIntensityValue;
        }
        
        if (pointLight != null)
        {
            pointLight.intensity = currentIntensity;
        }
    }
    
    bool IsNightTime(float time)
    {
        return time >= nightStart || time < nightEnd;
    }
    
    public void SetNightTime(float start, float end)
    {
        nightStart = Mathf.Clamp01(start);
        nightEnd = Mathf.Clamp01(end);
    }
    
    public bool IsCurrentlyNight()
    {
        return IsNightTime(GetTimeOfDay());
    }
    
    void OnValidate()
    {
        if (pointLight != null && Application.isPlaying)
        {
            ApplyLightSettings();
        }
    }
}