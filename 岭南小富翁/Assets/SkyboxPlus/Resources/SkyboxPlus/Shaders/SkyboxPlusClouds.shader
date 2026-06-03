Shader "Skybox/Skybox Plus/Clouds" {
Properties {
	[Toggle(SKYBOX_HAZE)] _SkyboxHaze("Procedural Haze", Float) = 0
	_NoiseTex ("   Noise", 2D) = "black" {}
	_HazeColor ("   Color", Color) = (1,1,1,1)
	_HazeScale ("   Scale", Range(0,1)) = 0.2
	_HazeThreshold("   Threshold", Range(0,1)) = 0
	_HazeIntensity("   Intensity", Float) = 1.0
	_HazeRotationAxis("   Rotation Axis", Vector) = (0.0,-1.0,0.0)
	_HazeRotationSpeed("   Rotation Speed", Float) = 0.2
	[Space(20)]
    [Toggle(SKYBOX_LAYER_1)] _SkyboxLayer1 ("Layer 1 (Clouds)", Float) = 0
    [NoScaleOffset] _Tex1 ("   Cubemap", Cube) = "black" {}
	_AlphaLayer1 ("   Alpha", Range(0,1)) = 1.0
    _HShiftLayer1 ("   Horizontal Shift", Range(-7, 7)) = 0
    _RotationSpeed1 ("   Horizontal Speed", Float) = 0.05
	_VShiftLayer1 ("   Vertical Shift", Range(-0.5,0.25)) = 0
	_PhaseLayer1 ("   Vertical Motion", Range(-0.1,0.1)) = 0.01
	[Space(20)]
    [Toggle(SKYBOX_LAYER_2)] _SkyboxLayer2 ("Layer 2 (Clouds)", Float) = 0
    [NoScaleOffset] _Tex2 ("   Cubemap", Cube) = "black" {}
	_AlphaLayer2 ("   Alpha", Range(0,1)) = 1.0
    _HShiftLayer2 ("   Horizontal Shift", Range(-7, 7)) = 0
    _RotationSpeed2 ("   Horizontal Speed", Float) = 0.08
	_VShiftLayer2 ("   Vertical Shift", Range(-0.5,0.25)) = 0
	_PhaseLayer2 ("   Vertical Motion", Range(-0.1,0.1)) = -0.015
	[Space(20)]
    [Toggle(SKYBOX_LAYER_3)] _SkyboxLayer3 ("Layer 3 (Clouds)", Float) = 0
    [NoScaleOffset] _Tex3 ("   Cubemap", Cube) = "black" {}
	_AlphaLayer3 ("   Alpha", Range(0,1)) = 1.0
    _HShiftLayer3 ("   Horizontal Shift", Range(-7, 7)) = 0
    _RotationSpeed3 ("   Horizontal Speed", Float) = 0.11
	_VShiftLayer3 ("   Vertical Shift", Range(-0.5,0.25)) = 0
	_PhaseLayer3 ("   Vertical Motion", Range(-0.1,0.1)) = 0.02
	[Space(20)]
    [Toggle(SKYBOX_LAYER_4)] _SkyboxLayer4 ("Layer 4 (Clouds)", Float) = 0
    [NoScaleOffset] _Tex4 ("   Cubemap", Cube) = "black" {}
	_AlphaLayer4 ("   Alpha", Range(0,1)) = 1.0
    _HShiftLayer4 ("   Horizontal Shift", Range(-7, 7)) = 0
    _RotationSpeed4 ("   Horizontal Speed", Float) = 0.14
	_VShiftLayer4 ("   Vertical Shift", Range(-0.5,0.25)) = 0
	_PhaseLayer4 ("   Vertical Motion", Range(-0.1,0.1)) = -0.025
	[Space(20)]
    _SunColor ("Sun Tint", Color) = (1,0.9568,0.8392)
    _SunSize ("Sun Size", Range(0,1)) = 0.15
	_SunFlare("Sun Flare", Range(0, 1.0)) = 0.4
	[Space(20)]
    [Toggle(SKYBOX_NIGHT)] _SkyboxNight ("Enable Night Time", Float) = 0
	[NoScaleOffset] _MoonTex ("Moon Texture", 2D) = "black" {}
    _MoonSize ("Moon Size Factor", Float) = 5
	_MoonFlare("Moon Flare", Range(0, 1.0)) = 0.03
	_StarSize ("Star Size", Range(10,1000)) = 200
	_StarDensity("Star Density", Range(0,0.35)) = 0.04
	_StarBrightness ("Star Brightness", Range(0, 1)) = 0.5
	_StarDarkColor("Star Darker Color", Color) = (1.0,0.3,0.1)
	_StarBrightColor("Star Brighter Color", Color) = (0.2,0.9,1.0)
	_StarBlinking("Star Blinking", Range(0, 1)) = 0.2
	[Space(20)]
	_SkyTint ("Sky Tint", Color) = (0.52,0.5,1)
	_SkyNightColor ("Sky Night Color", Color) = (0,0,0)
	_NightTintColor ("Night Tint Color", Color) = (0,0,0.0002)
	_Exposure ("Exposure", Float) = 1.0
	_FogColor ("Fog Color", Color) = (1,1,1)
	_FogAmount ("Fog Amount", Range(0,1)) = 0.5
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off

	Pass {

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0
		#pragma multi_compile _ SBP_CUSTOM_POSITIONS
		#pragma shader_feature_local SKYBOX_LAYER_1
		#pragma shader_feature_local SKYBOX_LAYER_2
		#pragma shader_feature_local SKYBOX_LAYER_3
		#pragma shader_feature_local SKYBOX_LAYER_4
		#pragma shader_feature_local SKYBOX_NIGHT
		#pragma shader_feature_local SKYBOX_HAZE

		#include "UnityCG.cginc"
		#include "Lighting.cginc"

		samplerCUBE _Tex1, _Tex2, _Tex3, _Tex4;
        float _RotationSpeed1, _RotationSpeed2, _RotationSpeed3, _RotationSpeed4;
		half _AlphaLayer1, _AlphaLayer2, _AlphaLayer3, _AlphaLayer4;
		float _VShiftLayer1, _VShiftLayer2, _VShiftLayer3, _VShiftLayer4;
		float _PhaseLayer1, _PhaseLayer2, _PhaseLayer3, _PhaseLayer4;
		float _HShiftLayer1, _HShiftLayer2, _HShiftLayer3, _HShiftLayer4;
		half3 _SkyTint, _SkyNightColor, _NightTintColor;
		half _Exposure;
		half3 _FogColor;
		half _FogAmount;
		float _StarSize,  _StarDensity, _StarBrightness, _StarBlinking;
		half3 _StarDarkColor, _StarBrightColor;
		half _SunSize, _SunFlare;
		half3 _SunColor;
		sampler2D _MoonTex;
		float _MoonSize;
		half _MoonFlare;
		sampler2D _NoiseTex;
		half _HazeScale;
		half4 _HazeColor;
		half _HazeIntensity, _HazeThreshold;
		float3 _HazeRotationAxis;
		float _HazeRotationSpeed;
		float4x4 _LightWL;
		float4x4 _MoonLightWL;
		float4 _WorldSpaceMoonDir;
		float3 _WorldSpaceSunDir;

		struct appdata
		{
			float4 vertex   : POSITION;
			float2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float3 uvLayer1: TEXCOORD0;
			float3 uvLayer2: TEXCOORD1;
			float3 uvLayer3: TEXCOORD2;
			float3 uvLayer4: TEXCOORD3;
			float3 ray : TEXCOORD4;
			float4 scrPos : TEXCOORD5;
			float3 hazeRay : TEXCOORD6;
			#if SBP_CUSTOM_POSITIONS
				float4 scrPosMoon : TEXCOORD7;
			#else
				float4 lightPos : TEXCOORD7;
			#endif
			UNITY_VERTEX_OUTPUT_STEREO
		};


        float3 RotateAroundYInDegrees (float3 vertex, float angle) {
            float sina, cosa;
            sincos(angle, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

		float3x3 AngleAxis3x3(float angle, float3 axis) {
		    float c, s;
			sincos(angle, s, c);

			float t = 1 - c;
			float x = axis.x;
			float y = axis.y;
			float z = axis.z;

			return float3x3(
				t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
				t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
				t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
			);
		}


		float rand3(float3 xyz) {
			return frac(sin(dot(xyz, float3(12.9898,78.233,39.789))) * 43758.5453);
		}

		float hash13(float3 p3) {
			p3  = frac(p3 * float3(.1031,.11369,.13787));
			p3 += dot(p3, p3.yzx + 19.19);
			return frac((p3.x + p3.y) * p3.z);
		}

		inline half3 BlendClouds(half3 color, samplerCUBE cube, float3 uv, half alpha, half3 skyColor) {
			half4 tex = texCUBE (cube, uv);
			tex.rgb *= skyColor;
			tex.a *= alpha;
			color = color * (1.0 - tex.a) + tex.rgb * tex.a;
			return color;
        }


		inline half3 LinearColor(half3 color) {
			#if defined(UNITY_COLORSPACE_GAMMA)
				// color properties are passed in gamma space, we need to switch to linear
				color *= color;
			#endif
			return color;
        }

		v2f vert (appdata v)
		{
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(v2f, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.ray = v.vertex.xyz;
			#if SKYBOX_LAYER_1
				o.uvLayer1 = RotateAroundYInDegrees(v.vertex.xyz, _Time.x * _RotationSpeed1 + _HShiftLayer1);
				o.uvLayer1.y += _SinTime.x * _PhaseLayer1 - _VShiftLayer1;
			#else
				o.uvLayer1 = 0;
			#endif
			#if SKYBOX_LAYER_2
				o.uvLayer2 = RotateAroundYInDegrees(v.vertex.xyz, _Time.x * _RotationSpeed2 + _HShiftLayer2);
				o.uvLayer2.y += _SinTime.x * _PhaseLayer2 - _VShiftLayer2;
			#else
				o.uvLayer2 = 0;
			#endif
			#if SKYBOX_LAYER_3
				o.uvLayer3 = RotateAroundYInDegrees(v.vertex.xyz, _Time.x * _RotationSpeed3 + _HShiftLayer3);
				o.uvLayer3.y += _SinTime.x * _PhaseLayer3 - _VShiftLayer3;
			#else
				o.uvLayer3 = 0;
			#endif
			#if SKYBOX_LAYER_4
				o.uvLayer4 = RotateAroundYInDegrees(v.vertex.xyz, _Time.x * _RotationSpeed4 + _HShiftLayer4);
				o.uvLayer4.y += _SinTime.x * _PhaseLayer4 - _VShiftLayer4;
			#else
				o.uvLayer4 = 0;
			#endif
			#if SKYBOX_HAZE
				float3x3 rotHaze = AngleAxis3x3(_Time.x * _HazeRotationSpeed, normalize(_HazeRotationAxis));
				o.hazeRay = normalize(mul(rotHaze, v.vertex.xyz));
			#else
				o.hazeRay = 0;
			#endif

			#if SBP_CUSTOM_POSITIONS
				o.scrPos = mul(_LightWL, float4(v.vertex.xyz, 0));
				o.scrPosMoon = mul(_MoonLightWL, float4(v.vertex.xyz * _WorldSpaceMoonDir.w, 0));
			#else
				o.scrPos = o.pos;
				o.lightPos = UnityObjectToClipPos(_WorldSpaceLightPos0.xyz);
			#endif

			return o;
		}

		half4 frag (v2f i) : SV_Target
		{
			float3 ray    = i.ray;

			#if SBP_CUSTOM_POSITIONS
				float3 sunDir = _WorldSpaceSunDir;
			#else
				float3 sunDir = _WorldSpaceLightPos0.xyz;
			#endif

			float3 delta  = sunDir - ray;
			float dist    = dot(delta, delta);

			// base color
			half3 skyColor = LinearColor(_SkyTint);

			// fog
			float y = abs(ray.y);
			half fog = saturate( (_FogAmount - y) / (1.0001 - _FogAmount) );
			fog *= fog;

			// sky tint
			float hy = abs(sunDir.y) + y;
			half t = saturate( (0.4 - hy) * 2.2) / (1.0 + dist * 0.8);
			skyColor.r = lerp(skyColor.r, 1.0, t);
			skyColor.b = lerp(skyColor.b, 0.0, t);

			// daylight + obscure opposite side of sky
			half daylight = saturate(1.0 + sunDir.y * 2.0 - dist * 0.03);
			skyColor = lerp(_SkyNightColor, skyColor, daylight);

			// sun
			half sunFlare = _SunFlare / pow(1.0 + dist, 64.0);

			#if SBP_CUSTOM_POSITIONS
				float2 scrDist = i.scrPos.xy * 1.8;
				float2 scrDistMoon = i.scrPosMoon.xy * 1.8;
			#else
				float2 scrDist = i.scrPos.xy/i.scrPos.w - i.lightPos.xy/i.lightPos.w;
				scrDist.x *= _ScreenParams.x/_ScreenParams.y;
				#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(SINGLE_PASS_STEREO)
					scrDist.x *= 0.5;
				#endif
				float2 scrDistMoon = scrDist;
			#endif

			half sunDist = length(scrDist);
            half sunIntensity = 1.0 - smoothstep(0.0, _SunSize, sunDist);
			sunIntensity *= 1.5;
			sunIntensity *= sunIntensity;
			half3 sunColor = LinearColor(_SunColor);
			#if SBP_CUSTOM_POSITIONS
				sunColor *= (i.scrPos.z <= 0);
			#endif
			half3 sunFlareColor = sunColor * sunFlare;
			sunColor *= sunIntensity;
			half3 moonColor = 0.0.xxx;

			#if SKYBOX_NIGHT
				// moon
				#if SBP_CUSTOM_POSITIONS
					float3 moonDir = _WorldSpaceMoonDir.xyz;
				#else
					float3 moonDir = -sunDir;
				#endif
				delta = moonDir - ray;
				dist = dot(delta, delta);
				half  moonFlare = _MoonFlare / pow(1.0 + dist, 64.0);

				if (dist < 0.3) {
					moonColor = moonFlare.xxx;
					half3 moonTex = tex2Dlod(_MoonTex, float4(scrDistMoon.xy * _MoonSize + 0.5, 0, 0)).rgb;
					moonColor += moonTex;
					#if SBP_CUSTOM_POSITIONS
						moonColor = max(0.0.xxx, moonColor - sunColor);
					#else
						sunColor = 0;
					#endif
				}

				// stars
				float3 p = ray * _StarSize;
				float br = smoothstep(1.0 - _StarDensity, 1.0, hash13(floor(p)));
				float star = smoothstep(_StarBrightness, 0., length(frac(p) - 0.5)) * br;
				star *= saturate(1.0 - saturate(frac(br * 10000.0 + _Time.w) - 0.3) * _StarBlinking);
				star = saturate(star * (1.0 - fog) - moonFlare * 25.0 - daylight * daylight * 64.0);
				half3 starColor = star * LinearColor(lerp(_StarDarkColor, _StarBrightColor, br));
				starColor *= starColor;
			#else
				half3 starColor = 0.0.xxx;
			#endif

			// final color
			half3 col = skyColor + sunColor + sunFlareColor * 0.5 + (moonColor + starColor) * saturate(ray.y + 0.2);

			// sky tint
			half3 skyTint = half3(1,1,1);
			skyTint.r = lerp(skyTint.r, 1.0, t);
			skyTint.b = lerp(skyTint.b, 0.0, t);
			skyTint = lerp(_NightTintColor, skyTint, daylight);
			
			// blend haze
			#if SKYBOX_HAZE
				float3 hazeRay = i.hazeRay;
				float3 hazeUV = hazeRay * _HazeScale;
				half n1 = tex2D(_NoiseTex, hazeUV.zy).r;
				half n2 = tex2D(_NoiseTex, hazeUV.xz).r;
				half n3 = tex2D(_NoiseTex, hazeUV.xy).r;
				float3 triW = abs(hazeRay);
				float3 weights = triW / (triW.x + triW.y + triW.z);
				half haze = dot(half3(n1, n2, n3), weights);
				haze = saturate( haze * _HazeIntensity - _HazeThreshold );
				half3 hazeColor = LinearColor(_HazeColor.rgb);
				haze *= _HazeColor.a;
				col = col * (1.0 - haze) + haze * (hazeColor * skyTint);
			#endif

			// blend clouds
			#if SKYBOX_LAYER_1
				col = BlendClouds(col, _Tex1, i.uvLayer1, _AlphaLayer1, skyTint);
			#endif
			#if SKYBOX_LAYER_2
				col = BlendClouds(col, _Tex2, i.uvLayer2, _AlphaLayer2, skyTint);
			#endif
			#if SKYBOX_LAYER_3
				col = BlendClouds(col, _Tex3, i.uvLayer3, _AlphaLayer3, skyTint);
			#endif
			#if SKYBOX_LAYER_4
				col = BlendClouds(col, _Tex4, i.uvLayer4, _AlphaLayer4, skyTint);
			#endif

			// apply fog
			col += sunFlareColor * 0.5;
			half3 fogColor = LinearColor(_FogColor);
			col = lerp(col, fogColor * skyTint, fog);

			// exposure
			col *= _Exposure;

			// gamma
			#if defined(UNITY_COLORSPACE_GAMMA)
				col = sqrt(col);
			#endif

			return half4(col, 1.0);
		}
		ENDCG
	}
}

Fallback Off
}


