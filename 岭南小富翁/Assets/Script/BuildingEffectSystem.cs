using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingEffectSystem : MonoBehaviour
{
    public static BuildingEffectSystem Instance;

    [Header("效果显示设置")]
    public float iconOffsetY = 2.0f;
    public float rotateSpeed = 360f;
    public float fadeDuration = 0.3f;
    public float delayBetweenEffects = 0.2f;

    [Header("动画速度控制")]
    [Tooltip("基础动画播放速度")]
    public float animationSpeed = 1.0f;

    [Header("队列动画加速设置")]
    [Tooltip("前N个动画以正常速度播放")]
    public int normalSpeedCount = 3;
    
    [Tooltip("每个后续动画的加速倍率")]
    public float speedMultiplierPerEffect = 1.15f;
    
    [Tooltip("最大加速倍率限制")]
    public float maxSpeedMultiplier = 3.0f;

    [Header("音效设置")]
    [Tooltip("音效播放音量0-1")]
    [Range(0f, 1f)]
    public float effectSoundVolume = 0.7f;

    [Header("Debug控制")]
    [Tooltip("是否输出Debug日志")]
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

    // Debug日志辅助方法
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

        // 尝试找到 BuildingEffectSystem 实例
        if (Instance == null)
        {
            DebugLogWarning("BuildingEffectSystem.Instance 为空，尝试查找...");
            BuildingEffectSystem existing = FindObjectOfType<BuildingEffectSystem>();
            if (existing != null)
            {
                Instance = existing;
                DebugLog("找到现有的 BuildingEffectSystem");
            }
            else
            {
                DebugLogError("找不到 BuildingEffectSystem，请确保场景中有 BuildingEffectSystem 对象");
                return;
            }
        }

        bool hasEffect = buildingData.effectIconPrefab != null || buildingData.effectSound != null;
        if (!hasEffect)
        {
            DebugLog($"QueueBuildingEffect: {buildingData.buildingName} 没有设置 effectIconPrefab 或 effectSound");
            return;
        }

        DebugLog($"QueueBuildingEffect: 将 {buildingData.buildingName} 加入效果队列");

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
            DebugLog($"ProcessEffectQueue: 播放第{effectIndex}个效果，速度={currentSpeed:F2}x");

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
            DebugLog($"BuildingEffectSystem: 播放建筑音效 {buildingData.buildingName}");
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
            DebugLog($"BuildingEffectSystem: 播放默认音效 {buildingData.buildingName}");
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
        DebugLog($"BuildingEffectSystem: 设置正常速度播放数量={normalSpeedCount}");
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
        DebugLog($"BuildingEffectSystem: 设置效果延迟={delayBetweenEffects}秒");
    }

    public void SetEffectSoundVolume(float volume)
    {
        effectSoundVolume = Mathf.Clamp01(volume);
        DebugLog($"BuildingEffectSystem: 设置音效音量={effectSoundVolume}");
    }

    public void ClearEffectQueue()
    {
        effectQueue.Clear();
        DebugLog($"BuildingEffectSystem: 清空效果队列");
    }

    // Debug日志控制接口
    public void SetDebugLogEnabled(bool enabled)
    {
        enableDebugLog = enabled;
        string status = enabled ? "已开启" : "已关闭";
        Debug.Log($"BuildingEffectSystem: Debug日志{status}");
    }

    // 获取Debug日志状态
    public bool IsDebugLogEnabled()
    {
        return enableDebugLog;
    }
}