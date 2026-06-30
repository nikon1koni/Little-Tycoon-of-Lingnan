
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class RaindropCollision : MonoBehaviour
{
    [Header("涟漪设置")]
    public float maxRipplesPerSecond = 20f;
    
    [Header("地面Layer")]
    public LayerMask groundLayer;
    
    private ParticleSystem rainParticleSystem;
    private float lastRippleTime;
    private float minInterval;
    
    private HashSet<int> triggeredParticles = new HashSet<int>();
    
    void Start()
    {
        rainParticleSystem = GetComponent<ParticleSystem>();
        minInterval = 1f / maxRipplesPerSecond;
    }
    
    void OnParticleCollision(GameObject other)
    {
        if ((groundLayer.value & (1 << other.layer)) == 0)
            return;
            
        if (Time.time - lastRippleTime < minInterval)
            return;
            
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[rainParticleSystem.particleCount];
        int count = rainParticleSystem.GetParticles(particles);
        
        for (int i = 0; i < count; i++)
        {
            int particleId = i;
            
            if (!triggeredParticles.Contains(particleId))
            {
                Vector3 hitPos = particles[i].position;
                hitPos.y = other.transform.position.y;
                
                RippleManager.Instance?.AddRipple(hitPos);
                triggeredParticles.Add(particleId);
                lastRippleTime = Time.time;
                
                break;
            }
        }
    }
    
    void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            triggeredParticles.Clear();
        }
    }
}
