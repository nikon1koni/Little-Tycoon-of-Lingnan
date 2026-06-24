using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("设置面板")]
    public GameObject settingsPanel;
    
    [Header("滑动条")]
    public Slider brightnessSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    
    [Header("按钮")]
    public Button closeButton;
    
    [Header("亮度调节")]
    public float minBrightness = 0.2f;
    public float maxBrightness = 5f;
    
    private Light[] allLights;
    private float[] originalLightIntensities;
    private float originalAmbientIntensity;
    private Color originalAmbientColor;
    
    void Awake()
    {
        AutoFindReferences();
        SaveOriginalValues();
        SetupEventListeners();
        SetupCloseButton();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    void AutoFindReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("SettingsPanel");
            if (settingsPanel == null) settingsPanel = GameObject.Find("Image");
        }
        
        if (settingsPanel != null)
        {
            if (brightnessSlider == null) brightnessSlider = FindSliderInChildren(settingsPanel.transform, "亮度");
            if (musicSlider == null) musicSlider = FindSliderInChildren(settingsPanel.transform, "音乐");
            if (sfxSlider == null) sfxSlider = FindSliderInChildren(settingsPanel.transform, "音效");
            if (closeButton == null) closeButton = FindButtonInChildren(settingsPanel.transform, "关闭");
        }
    }
    
    Slider FindSliderInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            Slider slider = child.GetComponent<Slider>();
            if (slider != null && child.name.Contains(name)) return slider;
            Slider found = FindSliderInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    Button FindButtonInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null && child.name.Contains(name)) return btn;
            Button found = FindButtonInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    void SaveOriginalValues()
    {
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalAmbientColor = RenderSettings.ambientLight;
        
        allLights = FindObjectsOfType<Light>();
        originalLightIntensities = new float[allLights.Length];
        for (int i = 0; i < allLights.Length; i++)
        {
            originalLightIntensities[i] = allLights[i].intensity;
        }
    }
    
    void SetupEventListeners()
    {
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }
    
    void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideSettingsPanel);
        }
    }
    
    public void HideSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
    
    void OnBrightnessChanged(float value)
    {
        float brightness = Mathf.Lerp(minBrightness, maxBrightness, value);
        
        RenderSettings.ambientIntensity = originalAmbientIntensity * brightness;
        RenderSettings.ambientLight = originalAmbientColor * brightness;
        
        for (int i = 0; i < allLights.Length; i++)
        {
            allLights[i].intensity = originalLightIntensities[i] * brightness;
        }
        
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.backgroundColor = cam.backgroundColor * brightness;
        }
    }
    
    void OnMusicVolumeChanged(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(value);
        }
    }
    
    void OnSFXVolumeChanged(float value)
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.SetMasterVolume(value);
        }
    }
}
