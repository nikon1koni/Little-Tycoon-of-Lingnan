Shader "Custom/DynamicRainRipplesURP"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Tint("Tint Color", Color) = (0.6, 1, 0.55, 0.5)
        _RippleIntensity("Ripple Intensity", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            float4 _Tint;
            float _RippleIntensity;
            #define MAX_RIPPLES 32
            float4 _RippleData[MAX_RIPPLES];
            int _RippleCount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;

                float rippleEffect = 0.0;

                for (int i = 0; i < _RippleCount; i++)
                {
                    float3 ripplePos = float3(_RippleData[i].x, 0, _RippleData[i].y);
                    float rippleRadius = _RippleData[i].z;
                    float rippleAlpha = _RippleData[i].w;

                    float dist = distance(input.positionWS.xz, ripplePos.xz);

                    if (dist < rippleRadius && rippleRadius > 0.01)
                    {
                        float normalizedDist = dist / rippleRadius;
                        float wave = sin(normalizedDist * 6.283 * 3.0) * 0.5 + 0.5;
                        wave *= (1.0 - normalizedDist);
                        wave *= rippleAlpha;

                        rippleEffect += wave;
                    }
                }

                rippleEffect = saturate(rippleEffect);

                half3 finalColor = baseColor * (1.0 - rippleEffect * _Tint.a) + 
                                   _Tint.rgb * rippleEffect * _Tint.a * _RippleIntensity;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
