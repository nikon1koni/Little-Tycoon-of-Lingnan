Shader "Custom/Atmosphere"
{
    Properties
    {
        _PlanetRadius ("Planet Radius", Float) = 6371000
        _AtmosphereRadius ("Atmosphere Radius", Float) = 6471000
        _SunDirection ("Sun Direction", Vector) = (0.5, 0.5, -0.5, 0)
        _RayleighScaleHeight ("Rayleigh Scale Height", Float) = 8000
        _MieScaleHeight ("Mie Scale Height", Float) = 1200
        _MieScattering ("Mie Scattering", Float) = 0.05
        _RayleighColor ("Rayleigh Color", Color) = (0.8, 0.85, 1, 1)
        _MieColor ("Mie Color", Color) = (1, 0.9, 0.7, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            float _PlanetRadius;
            float _AtmosphereRadius;
            float3 _SunDirection;
            float _RayleighScaleHeight;
            float _MieScaleHeight;
            float _MieScattering;
            float3 _RayleighColor;
            float3 _MieColor;

            float raySphereIntersection(float3 rayOrigin, float3 rayDir, float sphereRadius)
            {
                float a = dot(rayDir, rayDir);
                float b = 2 * dot(rayOrigin, rayDir);
                float c = dot(rayOrigin, rayOrigin) - sphereRadius * sphereRadius;
                float discriminant = b * b - 4 * a * c;

                if (discriminant < 0)
                    return -1;

                float t1 = (-b - sqrt(discriminant)) / (2 * a);
                float t2 = (-b + sqrt(discriminant)) / (2 * a);

                if (t2 < 0)
                    return -1;

                return t1 > 0 ? t1 : t2;
            }

            float opticalDepth(float3 pos, float3 dir, float scaleHeight)
            {
                float t = raySphereIntersection(pos, dir, _AtmosphereRadius);
                if (t < 0)
                    return 0;

                float stepSize = t / 10.0;
                float depth = 0;
                float3 p = pos;

                for (int i = 0; i < 10; i++)
                {
                    float height = length(p) - _PlanetRadius;
                    depth += exp(-height / scaleHeight) * stepSize;
                    p += dir * stepSize;
                }

                return depth;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(o.worldPos - _WorldSpaceCameraPos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = i.viewDir;

                float t = raySphereIntersection(rayOrigin, rayDir, _AtmosphereRadius);
                if (t < 0)
                    return float4(0, 0, 0, 0);

                float3 startPos = rayOrigin;
                float3 endPos = rayOrigin + rayDir * t;

                float stepSize = t / 20.0;
                float3 p = startPos;

                float3 rayleighColor = float3(0, 0, 0);
                float3 mieColor = float3(0, 0, 0);

                float rayleighDepth = 0;
                float mieDepth = 0;

                for (int i = 0; i < 20; i++)
                {
                    float height = length(p) - _PlanetRadius;
                    float density = exp(-height / _RayleighScaleHeight);
                    rayleighDepth += density * stepSize;

                    density = exp(-height / _MieScaleHeight);
                    mieDepth += density * stepSize;

                    float3 sunDir = normalize(_SunDirection);
                    float cosAngle = dot(normalize(p), sunDir);

                    float3 inScatter = _RayleighColor * (1 + cosAngle * cosAngle) * rayleighDepth;
                    inScatter += _MieColor * pow(1 - cosAngle, 3) * mieDepth * _MieScattering;

                    rayleighColor += inScatter * density * stepSize;
                    p += rayDir * stepSize;
                }

                float3 finalColor = rayleighColor + mieColor;
                float alpha = saturate(length(finalColor));

                return float4(finalColor, alpha);
            }
            ENDCG
        }
    }
}