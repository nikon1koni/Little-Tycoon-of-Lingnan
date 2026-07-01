using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public enum PlayMode
    {
        Sequential, // 顺序播放
        Shuffle     // 乱序播放
    }

    public static MusicManager Instance;

    [Header("音乐列表")]
    public List<AudioClip> musicTracks = new List<AudioClip>();

    [Header("播放设置")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool playOnAwake = true;

    [Tooltip("播放模式：顺序播放 / 乱序播放（乱序模式下首曲也随机）")]
    public PlayMode playMode = PlayMode.Sequential;

    private AudioSource audioSource;
    private int currentTrackIndex = 0;
    private bool isPlaying = false;
    private bool hasStarted = false;
    private bool isLoading = false;
    private Coroutine loadRoutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playOnAwake && musicTracks.Count > 0)
        {
            Play();
        }
    }

    void Update()
    {
        if (isPlaying && !isLoading && audioSource != null && !audioSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    void SetupAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
    }

    public void Play()
    {
        if (musicTracks.Count == 0)
        {
            Debug.LogWarning("MusicManager: 没有添加任何音乐文件！");
            return;
        }

        if (!isPlaying)
        {
            if (!hasStarted)
            {
                hasStarted = true;
                if (playMode == PlayMode.Shuffle)
                {
                    currentTrackIndex = Random.Range(0, musicTracks.Count);
                }
            }
            isPlaying = true;
            PlayCurrentTrack();
        }
    }

    public void Pause()
    {
        if (audioSource != null && isPlaying)
        {
            audioSource.Pause();
            isPlaying = false;
            Debug.Log($"MusicManager: 暂停播放 - {GetCurrentTrackName()}");
        }
    }

    public void Resume()
    {
        if (audioSource != null && !isPlaying && musicTracks.Count > 0)
        {
            if (audioSource.clip == null)
            {
                PlayCurrentTrack();
            }
            else
            {
                audioSource.UnPause();
                isPlaying = true;
                Debug.Log($"MusicManager: 继续播放 - {GetCurrentTrackName()}");
            }
        }
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }
            isLoading = false;
            audioSource.Stop();
            isPlaying = false;
            hasStarted = false;
            Debug.Log("MusicManager: 停止播放");
        }
    }

    public void PlayNextTrack()
    {
        if (musicTracks.Count == 0) return;

        if (playMode == PlayMode.Shuffle)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, musicTracks.Count);
            } while (randomIndex == currentTrackIndex && musicTracks.Count > 1);
            currentTrackIndex = randomIndex;
        }
        else
        {
            currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Count;
        }

        PlayCurrentTrack();
    }

    public void PlayPreviousTrack()
    {
        if (musicTracks.Count == 0) return;

        currentTrackIndex--;
        if (currentTrackIndex < 0)
        {
            currentTrackIndex = musicTracks.Count - 1;
        }

        PlayCurrentTrack();
    }

    public void PlayTrackByIndex(int index)
    {
        if (index >= 0 && index < musicTracks.Count)
        {
            currentTrackIndex = index;
            PlayCurrentTrack();
        }
        else
        {
            Debug.LogWarning($"MusicManager: 无效的音乐索引 {index}，有效范围: 0-{musicTracks.Count - 1}");
        }
    }

    void PlayCurrentTrack()
    {
        if (musicTracks.Count == 0) return;

        AudioClip track = musicTracks[currentTrackIndex];
        if (track == null)
        {
            Debug.LogWarning($"MusicManager: 索引 {currentTrackIndex} 的音乐文件为空，跳过");
            PlayNextTrack();
            return;
        }

        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
        }
        loadRoutine = StartCoroutine(LoadAndPlayTrack(track));
    }

    // 异步预加载音频数据后再播放，避免切歌时在主线程同步解码导致卡顿。
    // 需在该音频的导入设置里勾选 Load In Background（Streaming 类型则边播边读，同样不卡）。
    private IEnumerator LoadAndPlayTrack(AudioClip track)
    {
        isLoading = true;

        if (track.loadState != AudioDataLoadState.Loaded)
        {
            track.LoadAudioData();
            while (track.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }
        }

        audioSource.clip = track;
        audioSource.Play();
        isPlaying = true;
        isLoading = false;
        loadRoutine = null;

        Debug.Log($"MusicManager: 播放 [{currentTrackIndex + 1}/{musicTracks.Count}] - {track.name}");
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public float GetVolume()
    {
        return volume;
    }

    public void VolumeUp()
    {
        SetVolume(volume + 0.1f);
    }

    public void VolumeDown()
    {
        SetVolume(volume - 0.1f);
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public string GetCurrentTrackName()
    {
        if (musicTracks.Count > 0 && currentTrackIndex >= 0 && currentTrackIndex < musicTracks.Count)
        {
            AudioClip track = musicTracks[currentTrackIndex];
            return track != null ? track.name : "空";
        }
        return "无音乐";
    }

    public int GetCurrentTrackIndex()
    {
        return currentTrackIndex;
    }

    public int GetTotalTracks()
    {
        return musicTracks.Count;
    }

    public void AddTrack(AudioClip track)
    {
        if (track != null && !musicTracks.Contains(track))
        {
            musicTracks.Add(track);
            Debug.Log($"MusicManager: 添加音乐 - {track.name}");
        }
    }

    public void RemoveTrack(AudioClip track)
    {
        if (track != null && musicTracks.Contains(track))
        {
            musicTracks.Remove(track);
            Debug.Log($"MusicManager: 移除音乐 - {track.name}");
        }
    }

    public void ClearAllTracks()
    {
        Stop();
        musicTracks.Clear();
        currentTrackIndex = 0;
        Debug.Log("MusicManager: 清空所有音乐");
    }

    public void ToggleShuffle()
    {
        SetPlayMode(playMode == PlayMode.Shuffle ? PlayMode.Sequential : PlayMode.Shuffle);
    }

    public void SetPlayMode(PlayMode mode)
    {
        playMode = mode;
        Debug.Log($"MusicManager: 播放模式 = {(playMode == PlayMode.Shuffle ? "乱序" : "顺序")}");
    }

    public PlayMode GetPlayMode()
    {
        return playMode;
    }

    public void TogglePlayPause()
    {
        if (isPlaying)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }
}
