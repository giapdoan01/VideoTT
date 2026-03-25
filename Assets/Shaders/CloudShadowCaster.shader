// Shader dùng cho mesh bóng của cloud.
// CloudShadowCaster.cs tự tạo mesh con ẩn với shader này.
// Blend One One = additive: nhiều cloud chồng nhau sẽ cộng dồn bóng tối dần.
Shader "Hidden/CloudShadowCaster"
{
    Properties
    {
        _MainTex         ("Cloud Texture", 2D)              = "white" {}
        _ShadowIntensity ("Shadow Intensity", Range(0, 1))  = 0.8
        [Header(Height Fade  low altitude  darker shadow)]
        _HeightFadeStart ("Height Fade Start", Float)       = 5.0
        _HeightFadeEnd   ("Height Fade End",   Float)       = 40.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CloudShadowPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend  One One   // additive accumulation
            ZWrite Off
            ZTest  Always
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _ShadowIntensity;
                float  _HeightFadeStart;
                float  _HeightFadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  worldY     : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS    = TransformWorldToHClip(worldPos);
                o.uv            = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldY        = worldPos.y;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;

                // cloud thấp (gần cỏ) → heightFade ~ 1 → bóng đậm
                // cloud cao (xa cỏ)   → heightFade ~ 0 → bóng nhạt / mất
                float range      = max(_HeightFadeEnd - _HeightFadeStart, 0.001);
                float heightFade = 1.0 - saturate((i.worldY - _HeightFadeStart) / range);

                half val = alpha * _ShadowIntensity * heightFade;
                return half4(val, val, val, 1.0);
            }
            ENDHLSL
        }
    }
}
