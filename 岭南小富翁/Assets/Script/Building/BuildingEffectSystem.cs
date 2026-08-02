using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingEffectSystem : MonoBehaviour
{
    public static BuildingEffectSystem Instance;

    [Header("特效显示设置")]
    public float iconOffsetY = 2.0f;
    public float rotateSpeed = 360f;
    public float fadeDuration = 0.3f;
    public float delayBetweenEffects = 0.2f;

    [Header("动画速度设置")]
    [Tooltip("建筑效果播放的动画速度")]
    public float animationSpeed = 1.0f;

    [Header("加速效果设置")]
    [Tooltip("前N个效果保持正常速度")]
    public int normalSpeedCount = 3;

    [Tooltip("每个额外效果的速度倍率")]
    public float speedMultiplierPerEffect = 1.15f;

    [Tooltip("最大速度倍率上限")]
    public float maxSpeedMultiplier = 3.0f;

    [Header("音效设置")]
    [Tooltip("音效音量(0-1)")]
    [Range(0f, 1f)]
    public float effectSoundVolume = 0.4f;

    [Header("Debug设置")]
    [Tooltip("是否开启Debug日志")]
    public bool enableDebugLog = true;

    private Queue<EffectRequest> effectQueue = new Queue<EffectRequest>();
    private bool isPlayingEffect = false;

    public bool IsPlayingEffects
    {
        get { return isPlayingEffect || effectQueue.Count > 0; }
    }

    private struct EffectRequest
    {
        public Transform buildingTransform;
        public BuildingData buildingData;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Debug 日志辅助方法
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log(message);
        }
    }

    private void DebugLogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning(message);
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError(message);
        }
    }

    public void QueueBuildingEffect(Transform buildingTransform, BuildingData buildingData)
    {
        if (buildingData == null)
        {
            DebugLogWarning("QueueBuildingEffect: buildingData 为空");
            return;
        }

        if (buildingTransform == null)
        {
            DebugLogWarning($"QueueBuildingEffect: buildingTransform 为空 for {buildingData.buildingName}");
            return;
        }

        // 检查已存在的 BuildingEffectSystem 实例
        if (Instance == null)
        {
            DebugLogWarning("BuildingEffectSystem.Instance 为空，尝试查找...");
            BuildingEffectSystem existing = FindObjectOfType<BuildingEffectSystem>();
            if (existing != null)
            {
                Instance = existing;
                DebugLog("找到已存在的 BuildingEffectSystem");
            }
            else
            {
                DebugLogError("未找到 BuildingEffectSystem，请确保场景中正确添加了 BuildingEffectSystem 组件");
                return;
            }
        }

        bool hasEffect = buildingData.effectIconPrefab != null || buildingData.effectSound != null;
        if (!hasEffect)
        {
            DebugLog($"QueueBuildingEffect: {buildingData.buildingName} 没有配置 effectIconPrefab 或 effectSound");
            return;
        }

        DebugLog($"QueueBuildingEffect: 为 {buildingData.buildingName} 添加效果到队列");

        effectQueue.Enqueue(new EffectRequest
        {
            buildingTransform = buildingTransform,
            buildingData = buildingData
        });

        if (!isPlayingEffect)
        {
            StartCoroutine(ProcessEffectQueue());
        }
    }

    private IEnumerator ProcessEffectQueue()
    {
        isPlayingEffect = true;
        int effectIndex = 0;

        while (effectQueue.Count > 0)
        {
            effectIndex++;

            float currentSpeed = GetCurrentSpeed(effectIndex);
            DebugLog($"ProcessEffectQueue: 处理第{effectIndex}个效果，播放速度={currentSpeed:F2}x");

            EffectRequest request = effectQueue.Dequeue();
            yield return StartCoroutine(PlayBuildingEffect(request.buildingTransform, request.buildingData, currentSpeed));

            if (effectQueue.Count > 0)
            {
                yield return new WaitForSeconds(delayBetweenEffects / currentSpeed);
            }
        }

        isPlayingEffect = false;
    }

    private float GetCurrentSpeed(int effectIndex)
    {
        if (effectIndex <= normalSpeedCount)
        {
            return animationSpeed;
        }

        int acceleratedIndex = effectIndex - normalSpeedCount;
        float speed = animationSpeed * Mathf.Pow(speedMultiplierPerEffect, acceleratedIndex);
        return Mathf.Min(speed, animationSpeed * maxSpeedMultiplier);
    }

    public IEnumerator PlayBuildingEffect(Transform buildingTransform, BuildingData buildingData, float speed = 1.0f)
    {
        if (buildingData == null)
        {
            yield break;
        }

        bool hasEffect = buildingData.effectIconPrefab != null || buildingData.effectSound != null;

        if (!hasEffect)
        {
            yield break;
        }

        GameObject iconInstance = null;
        Vector3 spawnPosition = buildingTransform.position + Vector3.up * iconOffsetY;

        if (buildingData.effectSound != null)
        {
            DebugLog($"BuildingEffectSystem: 自动播放自定义音效 {buildingData.buildingName}");
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayCustomClip(buildingData.effectSound, effectSoundVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(buildingData.effectSound, spawnPosition, effectSoundVolume);
            }
        }
        else if (SFXManager.Instance != null)
        {
            DebugLog($"BuildingEffectSystem: 自动播放默认音效 {buildingData.buildingName}");
            SFXManager.Instance.PlaySFX(SFXClip.EventBuffActivated, effectSoundVolume);
        }

        if (buildingData.effectIconPrefab != null)
        {
            iconInstance = Instantiate(buildingData.effectIconPrefab, spawnPosition, Quaternion.identity);

            float elapsed = 0f;
            float totalDuration = buildingData.effectDuration / speed;
            float adjustedFadeDuration = fadeDuration / speed;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime * speed;

                if (iconInstance != null)
                {
                    iconInstance.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime * speed);

                    float alpha = 1f;
                    if (elapsed < adjustedFadeDuration)
                    {
                        alpha = elapsed / adjustedFadeDuration;
                    }
                    else if (elapsed > totalDuration - adjustedFadeDuration)
                    {
                        alpha = 1 - (elapsed - (totalDuration - adjustedFadeDuration)) / adjustedFadeDuration;
                    }

                    Color color = iconInstance.GetComponent<Renderer>()?.material.color ?? Color.white;
                    color.a = alpha;
                    if (iconInstance.TryGetComponent(out Renderer renderer))
                    {
                        renderer.material.color = color;
                    }

                    float scale = Mathf.Lerp(0.5f, 1.2f, Mathf.Sin(elapsed / totalDuration * Mathf.PI));
                    iconInstance.transform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            Destroy(iconInstance);
        }
        else
        {
            yield return new WaitForSeconds(buildingData.effectDuration / speed);
        }
    }

    public void PlayBuildingEffectImmediate(Transform buildingTransform, BuildingData buildingData)
    {
        StartCoroutine(PlayBuildingEffect(buildingTransform, buildingData, animationSpeed));
    }

    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = Mathf.Max(0.1f, speed);
        DebugLog($"BuildingEffectSystem: 设置动画速度={animationSpeed}x");
    }

    public void SetNormalSpeedCount(int count)
    {
        normalSpeedCount = Mathf.Max(0, count);
        DebugLog($"BuildingEffectSystem: 设置正常速度效果数量={normalSpeedCount}");
    }

    public void SetSpeedMultiplierPerEffect(float multiplier)
    {
        speedMultiplierPerEffect = Mathf.Max(1.0f, multiplier);
        DebugLog($"BuildingEffectSystem: 设置速度倍率={speedMultiplierPerEffect}");
    }

    public void SetMaxSpeedMultiplier(float multiplier)
    {
        maxSpeedMultiplier = Mathf.Max(1.0f, multiplier);
        DebugLog($"BuildingEffectSystem: 设置最大速度倍率={maxSpeedMultiplier}");
    }

    public void SetDelayBetweenEffects(float delay)
    {
        delayBetweenEffects = Mathf.Max(0f, delay);
        DebugLog($"BuildingEffectSystem: 设置效果间隔={delayBetweenEffects}秒");
    }

    public void SetEffectSoundVolume(float volume)
    {
        effectSoundVolume = Mathf.Clamp01(volume);
        DebugLog($"BuildingEffectSystem: 设置音效音量={effectSoundVolume}");
    }

    public void ClearEffectQueue()
    {
        effectQueue.Clear();
        DebugLog($"BuildingEffectSystem: 已清空效果队列");
    }

    // Debug 日志开关接口
    public void SetDebugLogEnabled(bool enabled)
    {
        enableDebugLog = enabled;
        string status = enabled ? "已开启" : "已关闭";
        Debug.Log($"BuildingEffectSystem: Debug日志{status}");
    }

    // 获取 Debug 日志状态
    public bool IsDebugLogEnabled()
    {
        return enableDebugLog;
    }
}