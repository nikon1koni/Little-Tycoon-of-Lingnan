using UnityEngine;

public class RippleDebugger : MonoBehaviour
{
    public static RippleDebugger Instance;

    [Header("????????")]
    public GameObject ripplePrefab;
    public float rippleLifetime = 2f;
    public float rippleMaxScale = 2f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnRipple(Vector3 position)
    {
        if (ripplePrefab == null)
        {
            Debug.Log("????????¦Ë??: " + position);
            return;
        }

        GameObject ripple = Instantiate(ripplePrefab, position, Quaternion.identity);
        Destroy(ripple, rippleLifetime);

        StartCoroutine(AnimateRipple(ripple.transform));
    }

    private System.Collections.IEnumerator AnimateRipple(Transform ripple)
    {
        float startTime = Time.time;
        float startScale = 0.1f;

        while (ripple != null && Time.time - startTime < rippleLifetime)
        {
            float progress = (Time.time - startTime) / rippleLifetime;
            float currentScale = Mathf.Lerp(startScale, rippleMaxScale, progress);
            ripple.localScale = Vector3.one * currentScale;

            Renderer renderer = ripple.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1 - progress;
                renderer.material.color = color;
            }

            yield return null;
        }
    }
}
