using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingEffectSystem : MonoBehaviour
{
    public static BuildingEffectSystem Instance;

    [Header("Ч����ʾ����")]
    public float iconOffsetY = 2.0f;
    public float rotateSpeed = 360f;
    public float fadeDuration = 0.3f;
    public float delayBetweenEffects = 0.2f;

    [Header("�����ٶ�����")]
    [Tooltip("�������������ٶ�")]
    public float animationSpeed = 1.0f;

    [Header("���ٶ�������")]
    [Tooltip("ǰN��Ч�����������ٶ�")]
    public int normalSpeedCount = 3;
    
    [Tooltip("ÿ������Ч�����ٶȱ���")]
    public float speedMultiplierPerEffect = 1.15f;
    
    [Tooltip("����ٶȱ�������")]
    public float maxSpeedMultiplier = 3.0f;

    [Header("��Ч����")]
[Tooltip("��Ч����0-1")]
[Range(0f, 1f)]
public float effectSoundVolume = 0.4f;

    [Header("Debug����")]
    [Tooltip("�Ƿ�����Debug��־")]
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

    // Debug��־��������
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
            DebugLogWarning("QueueBuildingEffect: buildingData Ϊ��");
            return;
        }

        if (buildingTransform == null)
        {
            DebugLogWarning($"QueueBuildingEffect: buildingTransform Ϊ�� for {buildingData.buildingName}");
            return;
        }

        // �����Ѵ��ڵ� BuildingEffectSystem ʵ��
        if (Instance == null)
        {
            DebugLogWarning("BuildingEffectSystem.Instance Ϊ�գ����Բ���...");
            BuildingEffectSystem existing = FindObjectOfType<BuildingEffectSystem>();
            if (existing != null)
            {
                Instance = existing;
                DebugLog("�ҵ��Ѵ��ڵ� BuildingEffectSystem");
            }
            else
            {
                DebugLogError("δ�ҵ� BuildingEffectSystem����ȷ������ȷ���� BuildingEffectSystem ���");
                return;
            }
        }

        bool hasEffect = buildingData.effectIconPrefab != null || buildingData.effectSound != null;
        if (!hasEffect)
        {
            DebugLog($"QueueBuildingEffect: {buildingData.buildingName} û������ effectIconPrefab �� effectSound");
            return;
        }

        DebugLog($"QueueBuildingEffect: Ϊ {buildingData.buildingName} ���Ч��������");

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
            DebugLog($"ProcessEffectQueue: �����{effectIndex}��Ч�����ٶ�={currentSpeed:F2}x");

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
            DebugLog($"BuildingEffectSystem: �����Զ�����Ч {buildingData.buildingName}");
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
            DebugLog($"BuildingEffectSystem: �����Զ�����Ч {buildingData.buildingName}");
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
        DebugLog($"BuildingEffectSystem: ���ö����ٶ�={animationSpeed}x");
    }

    public void SetNormalSpeedCount(int count)
    {
        normalSpeedCount = Mathf.Max(0, count);
        DebugLog($"BuildingEffectSystem: ���������ٶ�Ч������={normalSpeedCount}");
    }

    public void SetSpeedMultiplierPerEffect(float multiplier)
    {
        speedMultiplierPerEffect = Mathf.Max(1.0f, multiplier);
        DebugLog($"BuildingEffectSystem: �����ٶȱ���={speedMultiplierPerEffect}");
    }

    public void SetMaxSpeedMultiplier(float multiplier)
    {
        maxSpeedMultiplier = Mathf.Max(1.0f, multiplier);
        DebugLog($"BuildingEffectSystem: ��������ٶȱ���={maxSpeedMultiplier}");
    }

    public void SetDelayBetweenEffects(float delay)
    {
        delayBetweenEffects = Mathf.Max(0f, delay);
        DebugLog($"BuildingEffectSystem: ����Ч�����={delayBetweenEffects}��");
    }

    public void SetEffectSoundVolume(float volume)
    {
        effectSoundVolume = Mathf.Clamp01(volume);
        DebugLog($"BuildingEffectSystem: ������Ч����={effectSoundVolume}");
    }

    public void ClearEffectQueue()
    {
        effectQueue.Clear();
        DebugLog($"BuildingEffectSystem: ���Ч������");
    }

    // Debug��־���ƽӿ�
    public void SetDebugLogEnabled(bool enabled)
    {
        enableDebugLog = enabled;
        string status = enabled ? "������" : "�ѹر�";
        Debug.Log($"BuildingEffectSystem: Debug��־{status}");
    }

    // ��ȡDebug��־״̬
    public bool IsDebugLogEnabled()
    {
        return enableDebugLog;
    }
}