Shader "Custom/CloudLayer"
{
    Properties
    {
        _CloudScale ("Cloud Scale", Float) = 50
        _CloudSpeed ("Cloud Speed", Float) = 0.1
        _CloudDensity ("Cloud Density", Range(0, 1)) = 0.5
        _CloudHeight ("Cloud Height", Float) = 1000
        _CloudThickness ("Cloud Thickness", Float) = 500
        _CloudColor ("Cloud Color", Color) = (0.95, 0.95, 0.95, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.7, 0.7, 0.7, 0.3)
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float _CloudScale;
            float _CloudSpeed;
            float _CloudDensity;
            float _CloudHeight;
            float _CloudThickness;
            float4 _CloudColor;
            float4 _CloudShadowColor;

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

            float fbm(float2 st, int octaves)
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

            float cloudNoise(float3 pos, float time)
            {
                float2 uv = pos.xz / _CloudScale + float2(time * _CloudSpeed, 0);
                
                float noiseValue = 0;
                noiseValue += fbm(uv, 4);
                noiseValue += fbm(uv * 2 + float2(100, 0), 3) * 0.5;
                noiseValue += fbm(uv * 4 + float2(50, 50), 2) * 0.25;
                
                return noiseValue;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = o.worldPos.xz / _CloudScale;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float height = i.worldPos.y;
                
                if (height < _CloudHeight - _CloudThickness || height > _CloudHeight + _CloudThickness)
                    return float4(0, 0, 0, 0);
                
                float heightFactor = 1 - abs(height - _CloudHeight) / _CloudThickness;
                
                float noiseValue = cloudNoise(i.worldPos, _Time.y);
                float cloud = smoothstep(0.3, 0.8, noiseValue);
                cloud *= heightFactor * _CloudDensity;
                
                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                float lightFactor = max(0, dot(normalize(float3(0, 1, 0)), sunDir));
                
                float3 color = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, lightFactor);
                
                return float4(color, cloud);
            }
            ENDCG
        }
    }
}