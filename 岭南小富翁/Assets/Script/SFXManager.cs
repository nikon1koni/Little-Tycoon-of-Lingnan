using System.Collections.Generic;
using UnityEngine;

public enum SFXCategory
{
    UI,
    Character,
    Event,
    Dice
}

public enum SFXClip
{
    UIClick,
    UIHover,
    UIOpen,
    UIClose,

    PlayerJump,
    PlayerLand,
    PlayerMove,

    EventGainMoney,
    EventLoseMoney,
    EventPropertyBought,
    EventBuildingPlaced,
    EventBuildingUpgraded,
    EventGoToJail,
    EventTaxPaid,
    EventBuffActivated,

    DiceRoll,
    DiceStop
}

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("音效配置")]
    public SFXConfig config;

    [Header("音量设置")]
    [Range(0f, 1f)]
    [Tooltip("主音量")]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    [Tooltip("UI音效音量")]
    public float uiVolume = 0.8f;
    [Range(0f, 1f)]
    [Tooltip("角色音效音量")]
    public float characterVolume = 0.7f;
    [Range(0f, 1f)]
    [Tooltip("事件音效音量")]
    public float eventVolume = 0.7f;
    [Range(0f, 1f)]
    [Tooltip("骰子音效音量")]
    public float diceVolume = 0.8f;

    [Header("音效池")]
    [Tooltip("AudioSource池的大小")]
    public int poolSize = 16;

    private Dictionary<SFXClip, AudioClip> clipCache = new Dictionary<SFXClip, AudioClip>();
    private List<AudioSource> audioSources = new List<AudioSource>();
    private Dictionary<SFXClip, SFXCategory> clipCategoryMap = new Dictionary<SFXClip, SFXCategory>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        BuildClipCategoryMap();
        CreateAudioSourcePool();
        LoadAllClips();
        LoadVolumeSettings();
    }

    void BuildClipCategoryMap()
    {
        clipCategoryMap.Clear();

        clipCategoryMap[SFXClip.UIClick] = SFXCategory.UI;
        clipCategoryMap[SFXClip.UIHover] = SFXCategory.UI;
        clipCategoryMap[SFXClip.UIOpen] = SFXCategory.UI;
        clipCategoryMap[SFXClip.UIClose] = SFXCategory.UI;

        clipCategoryMap[SFXClip.PlayerJump] = SFXCategory.Character;
        clipCategoryMap[SFXClip.PlayerLand] = SFXCategory.Character;
        clipCategoryMap[SFXClip.PlayerMove] = SFXCategory.Character;

        clipCategoryMap[SFXClip.EventGainMoney] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventLoseMoney] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventPropertyBought] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventBuildingPlaced] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventBuildingUpgraded] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventGoToJail] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventTaxPaid] = SFXCategory.Event;
        clipCategoryMap[SFXClip.EventBuffActivated] = SFXCategory.Event;

        clipCategoryMap[SFXClip.DiceRoll] = SFXCategory.Dice;
        clipCategoryMap[SFXClip.DiceStop] = SFXCategory.Dice;
    }

    void CreateAudioSourcePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            audioSources.Add(source);
        }
    }

    void LoadAllClips()
    {
        if (config == null)
        {
            Debug.LogWarning("SFXManager: 未找到SFXConfig配置，音效无法加载");
            return;
        }

        clipCache.Clear();

        var entries = config.GetAllEntries();
        foreach (var entry in entries)
        {
            if (entry.clip != null)
            {
                clipCache[entry.clipType] = entry.clip;
            }
        }

        Debug.Log($"SFXManager: 成功加载 {clipCache.Count} 个音效");
    }

    AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in audioSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = false;
        audioSources.Add(newSource);
        return newSource;
    }

    float GetCategoryVolume(SFXCategory category)
    {
        float categoryVol = 1f;
        switch (category)
        {
            case SFXCategory.UI: categoryVol = uiVolume; break;
            case SFXCategory.Character: categoryVol = characterVolume; break;
            case SFXCategory.Event: categoryVol = eventVolume; break;
            case SFXCategory.Dice: categoryVol = diceVolume; break;
        }

        return masterVolume * categoryVol;
    }

    SFXCategory GetCategoryForClip(SFXClip clip)
    {
        if (clipCategoryMap.TryGetValue(clip, out SFXCategory category))
        {
            return category;
        }
        return SFXCategory.UI;
    }

    public void PlaySFX(SFXClip clip, float volumeScale = 1f)
    {
        if (clipCache.TryGetValue(clip, out AudioClip audioClip) && audioClip != null)
        {
            AudioSource source = GetAvailableSource();
            SFXCategory category = GetCategoryForClip(clip);
            float finalVolume = GetCategoryVolume(category) * volumeScale;
            source.PlayOneShot(audioClip, finalVolume);
        }
    }

    public void PlaySFXAtPosition(SFXClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clipCache.TryGetValue(clip, out AudioClip audioClip) && audioClip != null)
        {
            SFXCategory category = GetCategoryForClip(clip);
            float finalVolume = GetCategoryVolume(category) * volumeScale;
            AudioSource.PlayClipAtPoint(audioClip, position, finalVolume);
        }
    }

    public void PlayCustomClipAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip != null)
        {
            float finalVolume = masterVolume * eventVolume * volumeScale;
            AudioSource.PlayClipAtPoint(clip, position, finalVolume);
        }
    }

    public void PlayCustomClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null)
        {
            AudioSource source = GetAvailableSource();
            float finalVolume = masterVolume * eventVolume * volumeScale;
            source.clip = clip;
            source.volume = finalVolume;
            source.Play();
            Debug.Log($"SFXManager: 播放自定义音效");
        }
    }

    public void StopAllSFX()
    {
        foreach (AudioSource source in audioSources)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
    }

    public void SetCategoryVolume(SFXCategory category, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (category)
        {
            case SFXCategory.UI:
                uiVolume = volume;
                break;
            case SFXCategory.Character:
                characterVolume = volume;
                break;
            case SFXCategory.Event:
                eventVolume = volume;
                break;
            case SFXCategory.Dice:
                diceVolume = volume;
                break;
        }

        SaveVolumeSettings();
    }

    public float GetCategoryVolumeDirect(SFXCategory category)
    {
        switch (category)
        {
            case SFXCategory.UI: return uiVolume;
            case SFXCategory.Character: return characterVolume;
            case SFXCategory.Event: return eventVolume;
            case SFXCategory.Dice: return diceVolume;
            default: return 1f;
        }
    }

    void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("SFX_MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFX_UIVolume", uiVolume);
        PlayerPrefs.SetFloat("SFX_CharacterVolume", characterVolume);
        PlayerPrefs.SetFloat("SFX_EventVolume", eventVolume);
        PlayerPrefs.SetFloat("SFX_DiceVolume", diceVolume);
        PlayerPrefs.Save();
    }

    void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("SFX_MasterVolume", masterVolume);
        uiVolume = PlayerPrefs.GetFloat("SFX_UIVolume", uiVolume);
        characterVolume = PlayerPrefs.GetFloat("SFX_CharacterVolume", characterVolume);
        eventVolume = PlayerPrefs.GetFloat("SFX_EventVolume", eventVolume);
        diceVolume = PlayerPrefs.GetFloat("SFX_DiceVolume", diceVolume);
    }

    public bool IsClipLoaded(SFXClip clip)
    {
        return clipCache.ContainsKey(clip) && clipCache[clip] != null;
    }

    public void ReloadClips()
    {
        LoadAllClips();
    }
}
