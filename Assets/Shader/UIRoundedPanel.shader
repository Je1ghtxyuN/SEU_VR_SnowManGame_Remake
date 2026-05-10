Shader "UI/RoundedPanel"
{
    Properties
    {
        _Color ("Color", Color) = (0.078, 0.235, 0.471, 0.85)
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.08
        _EdgeSoftness ("Edge Softness", Range(0, 0.1)) = 0.01
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.02
        _BorderColor ("Border Color", Color) = (0.392, 0.784, 1.0, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
            {
            Name "RoundedPanel"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _CornerRadius;
                float _EdgeSoftness;
                float _BorderWidth;
                half4 _BorderColor;
            CBUFFER_END

            // SDF for rounded rectangle
            float RoundedRectSDF(float2 p, float2 b, float r)
            {
                float2 d = abs(p) - b + r;
                return length(max(d, 0.0)) - r;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // UV centered at (0,0), range [-0.5, 0.5]
                float2 uv = input.uv - 0.5;

                float2 halfSize = float2(0.5, 0.5);
                float radius = _CornerRadius;

                // Outer SDF
                float outerDist = RoundedRectSDF(uv, halfSize, radius);

                // Inner SDF (for border)
                float innerRadius = max(radius - _BorderWidth, 0.0);
                float2 innerHalfSize = halfSize - _BorderWidth;
                float innerDist = RoundedRectSDF(uv, innerHalfSize, innerRadius);

                // Outer edge alpha
                float outerAlpha = 1.0 - smoothstep(-_EdgeSoftness, _EdgeSoftness, outerDist);

                // Border mask
                float borderMask = smoothstep(-_EdgeSoftness, _EdgeSoftness, innerDist);

                // Compose: body color inside border, border color on edge
                half4 bodyColor = _Color;
                half4 finalColor = lerp(bodyColor, _BorderColor, borderMask);
                finalColor.a *= outerAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "UI/Default"
}
