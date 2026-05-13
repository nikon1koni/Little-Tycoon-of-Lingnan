using UnityEngine;
using System.Collections.Generic;

public class RippleManager : MonoBehaviour
{
    public static RippleManager Instance;

    [Header("????????")]
    public int maxRipples = 32;
    public float rippleDuration = 2f;
    public float maxRippleRadius = 1.5f;
    
    [Header("????????")]
    public Material groundRainMaterial;
    
    private List<Ripple> activeRipples = new List<Ripple>();
    private Vector4[] rippleData;
    
    private const string RIPPLE_DATA_PROPERTY = "_RippleData";
    private const string RIPPLE_COUNT_PROPERTY = "_RippleCount";

    struct Ripple
    {
        public Vector3 position;
        public float startTime;
        public float duration;
        public float maxRadius;
    }

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
        
        rippleData = new Vector4[maxRipples];
    }

    void Update()
    {
        UpdateRipples();
        UpdateShaderData();
    }

    public void AddRipple(Vector3 worldPosition)
    {
        if (activeRipples.Count >= maxRipples)
        {
            activeRipples.RemoveAt(0);
        }
        
        Ripple newRipple = new Ripple();
        newRipple.position = worldPosition;
        newRipple.startTime = Time.time;
        newRipple.duration = rippleDuration;
        newRipple.maxRadius = maxRippleRadius * Random.Range(0.8f, 1.2f);
        
        activeRipples.Add(newRipple);
    }

    void UpdateRipples()
    {
        float currentTime = Time.time;
        
        for (int i = activeRipples.Count - 1; i >= 0; i--)
        {
            Ripple ripple = activeRipples[i];
            
            if (currentTime - ripple.startTime >= ripple.duration)
            {
                activeRipples.RemoveAt(i);
            }
        }
    }

    void UpdateShaderData()
    {
        if (groundRainMaterial == null) return;
        
        for (int i = 0; i < maxRipples; i++)
        {
            if (i < activeRipples.Count)
            {
                Ripple ripple = activeRipples[i];
                float progress = (Time.time - ripple.startTime) / ripple.duration;
                
                rippleData[i] = new Vector4(
                    ripple.position.x,
                    ripple.position.z,
                    ripple.maxRadius * progress,
                    1 - progress
                );
            }
            else
            {
                rippleData[i] = Vector4.zero;
            }
        }
        
        groundRainMaterial.SetVectorArray(RIPPLE_DATA_PROPERTY, rippleData);
        groundRainMaterial.SetInt(RIPPLE_COUNT_PROPERTY, activeRipples.Count);
    }
}