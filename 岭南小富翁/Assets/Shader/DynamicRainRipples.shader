Shader "Custom/DynamicRainRipples"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _RippleTex ("Ripple Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (0.6, 1, 0.55, 0.5)
        _RippleIntensity ("Ripple Intensity", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NormalMap;
            float4 _NormalMap_ST;

            sampler2D _RippleTex;
            float4 _RippleTex_ST;

            float4 _Tint;
            float _RippleIntensity;

            #define MAX_RIPPLES 32

            float4 _RippleData[MAX_RIPPLES];
            int _RippleCount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 baseColor = tex2D(_MainTex, i.uv).rgb;
                float3 normal = UnpackNormal(tex2D(_NormalMap, i.uv));

                float rippleEffect = 0;

                for (int j = 0; j < _RippleCount; j++)
                {
                    float3 ripplePos = float3(_RippleData[j].x, 0, _RippleData[j].y);
                    float rippleRadius = _RippleData[j].z;
                    float rippleAlpha = _RippleData[j].w;

                    float dist = distance(i.worldPos.xz, ripplePos.xz);
                    
                    if (dist < rippleRadius)
                    {
                        float normalizedDist = dist / rippleRadius;
                        float wave = sin(normalizedDist * 6.283 * 3) * 0.5 + 0.5;
                        wave *= (1 - normalizedDist);
                        wave *= rippleAlpha;
                        
                        rippleEffect += wave;
                    }
                }

                rippleEffect = clamp(rippleEffect, 0, 1);

                float3 finalColor = baseColor * (1 - rippleEffect * _Tint.a) + _Tint.rgb * rippleEffect * _Tint.a * _RippleIntensity;

                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}