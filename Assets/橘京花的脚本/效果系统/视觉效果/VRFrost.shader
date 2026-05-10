Shader "Custom/VRFrost"
{
    Properties
    {
        _Intensity ("Frost Intensity", Range(0,1)) = 0
        _FrostColor ("Frost Color", Color) = (0.85, 0.92, 1.0, 1.0)
        _EdgeFrost ("Edge Frost Strength", Range(0,1)) = 0.7
        _CenterFrost ("Center Frost Strength", Range(0,1)) = 0.3
        _Crystallize ("Crystallize Amount", Range(0,1)) = 0.5
        _FrostNoiseScale ("Frost Noise Scale", Range(1,20)) = 8.0
    }

    SubShader
    {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        LOD 100

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Intensity;
            float4 _FrostColor;
            float _EdgeFrost;
            float _CenterFrost;
            float _Crystallize;
            float _FrostNoiseScale;

            // Hash-based noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Value noise with smooth interpolation
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Smoothstep for organic look
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion - layered noise for frost crystals
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * valueNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                if (_Intensity < 0.001)
                    return fixed4(0,0,0,0);

                float2 uv = i.uv;

                // === Frost pattern via FBM ===
                float2 noiseUV = uv * _FrostNoiseScale;
                float frostPattern = fbm(noiseUV, 4);

                // Crystalline sharp edges (lerp between smooth and sharp)
                float sharpFrost = smoothstep(0.3, 0.5, frostPattern);
                float smoothFrost = frostPattern;
                frostPattern = lerp(smoothFrost, sharpFrost, _Crystallize);

                // === Vignette: more frost at edges ===
                float2 center = uv - 0.5;
                float dist = length(center);
                float vignette = smoothstep(0.15, 0.75, dist);
                float edgeAmount = lerp(_CenterFrost, _EdgeFrost, vignette);

                // === Combine ===
                float alpha = frostPattern * edgeAmount * _Intensity;

                // Subtle sparkle: high-frequency noise highlights
                float sparkle = smoothstep(0.85, 0.95, fbm(uv * 30.0, 2)) * _Intensity * 0.3;
                float3 color = _FrostColor.rgb + sparkle;

                return fixed4(color, saturate(alpha));
            }
            ENDCG
        }
    }
}
