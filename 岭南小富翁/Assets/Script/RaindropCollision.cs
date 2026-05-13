using UnityEngine;

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
            
        // 获取碰撞位置
        // 获取粒子碰撞事件
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[rainParticleSystem.particleCount];
        int count = rainParticleSystem.GetParticles(particles);
        
        // 简单处理：取第一个碰撞点
        // 实际上OnParticleCollision已经保证是真的碰撞了
        if (count > 0)
        {
            // 这里我们用物体的位置近似（或者可以用更精确的计算）
            // 为了简单，我们就直接触发
            // 如果你想要精确的位置，可以用下面这段：
            /*
            Collider collider = other.GetComponent<Collider>();
            if (collider != null)
            {
                Vector3 pos = collider.ClosestPoint(transform.position);
                RippleManager.Instance?.AddRipple(pos);
            }
            */
            
            // 为了简单演示，我们直接触发（位置后面再优化）
            RippleManager.Instance?.AddRipple(other.transform.position);
            lastRippleTime = Time.time;
            Debug.Log($"Raindrop hit: {other.name}");
        }
    }
}
