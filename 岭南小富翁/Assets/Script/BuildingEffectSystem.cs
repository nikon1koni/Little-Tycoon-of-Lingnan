using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingEffectSystem : MonoBehaviour
{
    public static BuildingEffectSystem Instance;

    [Header("????????")]
    public float iconOffsetY = 2.0f;
    public float rotateSpeed = 360f;
    public float fadeDuration = 0.3f;
    public float delayBetweenEffects = 0.2f;

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

    public void QueueBuildingEffect(Transform buildingTransform, BuildingData buildingData)
    {
        if (buildingData == null)
        {
            Debug.LogWarning("QueueBuildingEffect: buildingData ???");
            return;
        }

        if (buildingTransform == null)
        {
            Debug.LogWarning($"QueueBuildingEffect: buildingTransform ??? for {buildingData.buildingName}");
            return;
        }

        // ??? BuildingEffectSystem ??????
        if (Instance == null)
        {
            Debug.LogWarning("BuildingEffectSystem.Instance ?????????????...");
            BuildingEffectSystem existing = FindObjectOfType<BuildingEffectSystem>();
            if (existing != null)
            {
                Instance = existing;
                Debug.Log("????????? BuildingEffectSystem");
            }
            else
            {
                Debug.LogError("?????? BuildingEffectSystem??????????????? BuildingEffectSystem ????");
                return;
            }
        }

        bool hasEffect = buildingData.effectIconPrefab != null || buildingData.effectSound != null;
        if (!hasEffect)
        {
            Debug.Log($"QueueBuildingEffect: {buildingData.buildingName} ??????? effectIconPrefab ?? effectSound");
            return;
        }

        Debug.Log($"QueueBuildingEffect: ???? {buildingData.buildingName} ??????????");

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

        while (effectQueue.Count > 0)
        {
            EffectRequest request = effectQueue.Dequeue();
            yield return StartCoroutine(PlayBuildingEffect(request.buildingTransform, request.buildingData));

            if (effectQueue.Count > 0)
            {
                yield return new WaitForSeconds(delayBetweenEffects);
            }
        }

        isPlayingEffect = false;
    }

    public IEnumerator PlayBuildingEffect(Transform buildingTransform, BuildingData buildingData)
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

        if (buildingData.effectSound != null && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFXAtPosition(SFXClip.EventBuffActivated, spawnPosition);
            AudioSource.PlayClipAtPoint(buildingData.effectSound, spawnPosition, 0.7f);
        }

        if (buildingData.effectIconPrefab != null)
        {
            iconInstance = Instantiate(buildingData.effectIconPrefab, spawnPosition, Quaternion.identity);
            
            float elapsed = 0f;
            float totalDuration = buildingData.effectDuration;
            float halfDuration = totalDuration / 2;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                
                if (iconInstance != null)
                {
                    iconInstance.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
                    
                    float alpha = 1f;
                    if (elapsed < fadeDuration)
                    {
                        alpha = elapsed / fadeDuration;
                    }
                    else if (elapsed > totalDuration - fadeDuration)
                    {
                        alpha = 1 - (elapsed - (totalDuration - fadeDuration)) / fadeDuration;
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
            yield return new WaitForSeconds(buildingData.effectDuration);
        }
    }

    public void PlayBuildingEffectImmediate(Transform buildingTransform, BuildingData buildingData)
    {
        StartCoroutine(PlayBuildingEffect(buildingTransform, buildingData));
    }
}