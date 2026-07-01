Shader "WormCore/GlowUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.42, 0.31, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0.5, 4.0)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.2
        _IsBoosting ("Is Boosting", Range(0, 1)) = 0
        _BoostColor ("Boost Color", Color) = (1, 0.42, 0.36, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogCoord : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlowColor;
                float _GlowIntensity;
                float _PulseSpeed;
                float _IsBoosting;
                float4 _BoostColor;
                float _Alpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogCoord = ComputeFogFactor(output.positionHCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Pulse factor: oscillates between 0.7 and 1.0
                float pulse = 0.7 + 0.3 * sin(_Time.y * _PulseSpeed * 6.28318);
                // Boosting = faster, brighter pulse
                float boostPulse = 0.6 + 0.4 * sin(_Time.y * _PulseSpeed * 3.0 * 6.28318);
                float pulseFactor = lerp(pulse, boostPulse, _IsBoosting);

                // Blend between normal glow and boost glow
                float4 activeColor = lerp(_GlowColor, _BoostColor, _IsBoosting);

                // Final color: texture tinted by glow color, amplified by intensity + pulse
                half4 col = texColor * input.color;
                col.rgb = col.rgb * activeColor.rgb * _GlowIntensity * pulseFactor;
                col.a = col.a * _Alpha;

                // Apply fog
                col.rgb = MixFog(col.rgb, input.fogCoord);

                return col;
            }
            ENDHLSL
        }
    }

    // Fallback for non-URP builds
    Fallback "Sprites/Default"
}
