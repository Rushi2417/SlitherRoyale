Shader "WormCore/GlowUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.42, 0.31, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0.0, 5.0)) = 1.2
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowIntensity;
            float _PulseSpeed;
            float _PulseAmount;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + i.worldPos.x * 0.01 + i.worldPos.y * 0.01) * _PulseAmount;
                half3 glow = _GlowColor.rgb * _GlowIntensity * pulse;
                half4 final = texColor * half4(glow, _GlowColor.a);
                final.rgb += glow * 0.3;
                final.a *= _GlowColor.a;
                return final;
            }
            ENDHLSL
        }
    }

    Fallback "Unlit/Transparent"
}
