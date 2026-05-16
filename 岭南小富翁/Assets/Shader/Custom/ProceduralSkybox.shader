Shader "Skybox/Procedural"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (0.2, 0.4, 0.8, 1)
        _HorizonColor ("Horizon Color", Color) = (0.5, 0.6, 0.8, 1)
        _GroundColor ("Ground Color", Color) = (0.2, 0.25, 0.3, 1)
        _SunColor ("Sun Color", Color) = (1, 0.9, 0.7, 1)
        _SunIntensity ("Sun Intensity", Range(0, 5)) = 2
        _SunSize ("Sun Size", Range(0.01, 0.2)) = 0.05
        _AtmosphereColor ("Atmosphere Color", Color) = (0.8, 0.6, 0.4, 1)
        _AtmosphereThickness ("Atmosphere Thickness", Range(0, 3)) = 1
        _CloudSpeed ("Cloud Speed", Float) = 0.02
        _CloudScale ("Cloud Scale", Float) = 8
        _CloudColor ("Cloud Color", Color) = (1.0, 1.0, 1.0, 1)
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        LOD 100
        Cull Back  // 回到默认渲染

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
            };

            float4 _SkyColor;
            float4 _HorizonColor;
            float4 _GroundColor;
            float4 _SunColor;
            float _SunIntensity;
            float _SunSize;
            float4 _AtmosphereColor;
            float _AtmosphereThickness;
            float _CloudSpeed;
            float _CloudScale;
            float4 _CloudColor;
            float _CloudDensity;

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);

                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float FBM(float2 st, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(st * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }

                return value;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldPos);
                float height = dir.y;
                
                float3 skyColor = lerp(_GroundColor.rgb, _HorizonColor.rgb, saturate(height * 2));
                skyColor = lerp(skyColor, _SkyColor.rgb, saturate((height - 0.5) * 2));
                
                float sunAngle = dot(dir, _WorldSpaceLightPos0.xyz);
                float sun = smoothstep(1 - _SunSize, 1, sunAngle);
                sun *= _SunIntensity;
                
                float atmosphere = pow(1 - max(0, dir.y), _AtmosphereThickness);
                skyColor = lerp(skyColor, _AtmosphereColor.rgb, atmosphere);
                
                // 云层计算，只在天空部分渲染云
                if (height > 0.0)
                {
                    float clouds = FBM(dir.xz * _CloudScale + _Time.x * _CloudSpeed, 4);
                    clouds = smoothstep(0.4 - _CloudDensity * 0.4, 0.5 + _CloudDensity * 0.4, clouds);
                    clouds *= smoothstep(0.0, 0.7, height);  // 在地平线处云层渐隐
                    skyColor = lerp(skyColor, _CloudColor.rgb, clouds * 0.7);
                }
                
                float4 finalColor = float4(skyColor + sun * _SunColor.rgb, 1);
                return finalColor;
            }
            ENDCG
        }
    }
}