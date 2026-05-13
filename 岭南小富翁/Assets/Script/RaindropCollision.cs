using UnityEngine;

public class RaindropCollision : MonoBehaviour
{
    public ParticleSystem rainParticleSystem;
    public LayerMask groundLayer;
    
    private ParticleSystem.Particle[] particles;
    private Vector3[] previousPositions;

    void Start()
    {
        if (rainParticleSystem == null)
        {
            rainParticleSystem = GetComponent<ParticleSystem>();
        }
        
        particles = new ParticleSystem.Particle[rainParticleSystem.main.maxParticles];
        previousPositions = new Vector3[rainParticleSystem.main.maxParticles];
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
                Ray ray = new Ray(previousPositions[i], currentPos - previousPositions[i]);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, Vector3.Distance(previousPositions[i], currentPos) * 1.5f, groundLayer))
                {
                    RippleManager.Instance?.AddRipple(hit.point);
                }
            }
            
            previousPositions[i] = currentPos;
        }
    }
}