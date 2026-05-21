using System.Collections;
using UnityEngine;

public class BuildingEffectSystem : MonoBehaviour
{
    public static BuildingEffectSystem Instance;

    [Header("效果参数")]
    public float iconOffsetY = 2.0f;
    public float rotateSpeed = 360f;
    public float fadeDuration = 0.3f;

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