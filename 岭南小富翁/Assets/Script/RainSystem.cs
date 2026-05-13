using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainSystem : MonoBehaviour
{
    public static RainSystem Instance;

    [Header("¡£◊”…Ë÷√")]
    public float rainIntensity = 1f;
    public float windSpeed = 0f;
    public float rainHeight = 20f;
    
    [Header("≈ˆ◊≤…Ë÷√")]
    public LayerMask groundLayer;
    public bool enableCollision = true;
    
    private ParticleSystem rainParticles;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;

    void Awake()
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

    void Start()
    {
        rainParticles = GetComponent<ParticleSystem>();
        emissionModule = rainParticles.emission;
        mainModule = rainParticles.main;
        velocityModule = rainParticles.velocityOverLifetime;
        
        UpdateRainIntensity();
    }

    void Update()
    {
        UpdateWindEffect();
    }

    public void SetRainIntensity(float intensity)
    {
        rainIntensity = Mathf.Clamp01(intensity);
        UpdateRainIntensity();
    }

    void UpdateRainIntensity()
    {
        if (emissionModule.enabled)
        {
            emissionModule.rateOverTime = rainIntensity * 500f;
        }

        gameObject.SetActive(rainIntensity > 0.01f);
    }

    void UpdateWindEffect()
    {
        if (velocityModule.enabled)
        {
            velocityModule.x = windSpeed;
        }
    }

    public void ToggleRain(bool enabled)
    {
        rainIntensity = enabled ? 1f : 0f;
        UpdateRainIntensity();
    }

    public bool IsRaining()
    {
        return rainIntensity > 0.01f;
    }
}