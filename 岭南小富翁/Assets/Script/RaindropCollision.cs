using UnityEngine;

public class RaindropCollision : MonoBehaviour
{
    [Header("??????")]
    public ParticleSystem rainParticleSystem;
    public LayerMask groundLayer;
    
    [Header("????????")]
    [Tooltip("??????????????????")]
    public float maxRipplesPerSecond = 5f;
    
    [Tooltip("???????????????????? (0-1)")]
    [Range(0f, 1f)]
    public float rippleChance = 0.1f;
    
    private ParticleSystem.Particle[] particles;
    private Vector3[] previousPositions;
    private bool[] hasTriggered;
    
    private float lastRippleTime;
    private float minInterval;

    void Start()
    {
        if (rainParticleSystem == null)
        {
            rainParticleSystem = GetComponent<ParticleSystem>();
        }
        
        particles = new ParticleSystem.Particle[rainParticleSystem.main.maxParticles];
        previousPositions = new Vector3[rainParticleSystem.main.maxParticles];
        hasTriggered = new bool[rainParticleSystem.main.maxParticles];
        
        minInterval = 1f / maxRipplesPerSecond;
    }

    void Update()
    {
        if (rainParticleSystem == null) return;

        int particleCount = rainParticleSystem.GetParticles(particles);
        
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 currentPos = particles[i].position;
            
            if (i < previousPositions.Length && previousPositions[i] != Vector3.zero)
            {
                if (!hasTriggered[i])
                {
                    Ray ray = new Ray(previousPositions[i], currentPos - previousPositions[i]);
                    RaycastHit hit;
                    
                    if (Physics.Raycast(ray, out hit, Vector3.Distance(previousPositions[i], currentPos) * 1.5f, groundLayer))
                    {
                        if (CanSpawnRipple())
                        {
                            hasTriggered[i] = true;
                            RippleManager.Instance?.AddRipple(hit.point);
                        }
                    }
                }
            }
            
            previousPositions[i] = currentPos;
            
            if (particles[i].remainingLifetime <= 0)
            {
                hasTriggered[i] = false;
            }
        }
    }
    
    private bool CanSpawnRipple()
    {
        if (Time.time - lastRippleTime < minInterval)
            return false;
            
        if (Random.value > rippleChance)
            return false;
            
        lastRippleTime = Time.time;
        return true;
    }
}
