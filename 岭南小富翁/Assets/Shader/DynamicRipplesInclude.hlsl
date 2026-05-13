#ifndef DYNAMIC_RIPPLES_INCLUDE_H
#define DYNAMIC_RIPPLES_INCLUDE_H

#define MAX_RIPPLES 32

float CalculateDynamicRipple(float3 positionWS, float4 rippleData[MAX_RIPPLES], float rippleCount)
{
    float effect = 0.0;
    int count = (int)rippleCount;
    
    [loop]
    for (int i = 0; i < count; i++)
    {
        float3 ripplePos = float3(rippleData[i].x, 0, rippleData[i].y);
        float rippleRadius = rippleData[i].z;
        float rippleAlpha = rippleData[i].w;
        
        float dist = distance(positionWS.xz, ripplePos.xz);
        
        if (dist < rippleRadius && rippleRadius > 0.01)
        {
            float normalizedDist = dist / rippleRadius;
            float wave = sin(normalizedDist * 6.283 * 3.0) * 0.5 + 0.5;
            wave *= (1.0 - normalizedDist);
            wave *= rippleAlpha;
            
            effect += wave;
        }
    }
    
    return saturate(effect);
}

#endif
