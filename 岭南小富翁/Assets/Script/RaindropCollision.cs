using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class RaindropCollision : MonoBehaviour
{
    [Header("涟漪设置")]
    public float maxRipplesPerSecond = 5f;
    
    [Header("地面Layer")]
    public LayerMask groundLayer;
    
    private ParticleSystem rainParticleSystem;
    private float lastRippleTime;
    private float minInterval;
    
    // 记录已经触发过涟漪的粒子ID
    private HashSet<int> triggeredParticles = new HashSet<int>();
    
    void Start()
    {
        rainParticleSystem = GetComponent<ParticleSystem>();
        minInterval = 1f / maxRipplesPerSecond;
    }
    
    void OnParticleCollision(GameObject other)
    {
        // 检查碰撞的物体是否是地面
        if ((groundLayer.value & (1 << other.layer)) == 0)
            return;
            
        // 检查频率限制
        if (Time.time - lastRippleTime < minInterval)
            return;
            
        // 获取所有粒子
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[rainParticleSystem.particleCount];
        int count = rainParticleSystem.GetParticles(particles);
        
        // 找一个合适的粒子触发
        for (int i = 0; i < count; i++)
        {
            // 检查这个粒子是否已经触发过涟漪
            int particleId = i; // 直接用索引作为粒子ID
            
            if (!triggeredParticles.Contains(particleId))
            {
                // 没触发过！就这个粒子了！
                // 获取碰撞位置（用粒子当前位置）
                Vector3 hitPos = particles[i].position;
                
                // 稍微调整Y坐标到地面高度
                hitPos.y = other.transform.position.y;
                
                RippleManager.Instance?.AddRipple(hitPos);
                triggeredParticles.Add(particleId);
                lastRippleTime = Time.time;
                Debug.Log($"Raindrop hit: {other.name} at {hitPos}");
                
                // 找到一个就退出
                break;
            }
        }
    }
    
    void Update()
    {
        // 清理太久之前的粒子记录（防止内存泄漏）
        // 每帧检查有点费，不过粒子不多没关系
        // 这里简单处理：每5秒清空一次
        if (Time.frameCount % 300 == 0)
        {
            triggeredParticles.Clear();
        }
    }
}
