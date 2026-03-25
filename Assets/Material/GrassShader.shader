Shader "Custom/Grass_Flat_Shadow_BaseColor"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _ShadowStrength     ("Shadow Strength",     Range(0,1)) = 0.6
        _ShadowFadeDistance ("Shadow Fade Distance", Float)      = 15.0
        _ShadowDepthScale   ("Shadow Depth Scale",   Float)      = 50.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // GPU Instancing - bắt buộc để Terrain Detail render được
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling lodfade

            // nhận shadow từ vật thể khác
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // CBUFFER tách riêng - không đặt chung với instancing buffer
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _ShadowStrength;
                float _ShadowFadeDistance;
                float _ShadowDepthScale;
            CBUFFER_END

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
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                // alpha clip
                clip(tex.a - _Cutoff);

                // màu phẳng, không diffuse / normal lighting
                half3 color = tex.rgb * _BaseColor.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                Light mainLight = GetMainLight(shadowCoord);
                half shadow = mainLight.shadowAttenuation;

                // Khoảng cách từ vật đổ bóng đến cỏ, đọc từ shadow map.
                // Dùng shadowCoord.xy trực tiếp - KHÔNG chia .w vì cascade shadow
                // lưu cascade index trong .w, chia sẽ cho UV sai.
                float receiverDepth = shadowCoord.z;
                float casterDepth   = SAMPLE_TEXTURE2D_LOD(
                    _MainLightShadowmapTexture, sampler_LinearClamp, shadowCoord.xy, 0).r;

                // Chênh lệch depth trong NDC × _ShadowDepthScale ≈ khoảng cách world
                // _ShadowDepthScale nên xấp xỉ bằng chiều cao shadow frustum của scene
                #if UNITY_REVERSED_Z
                    float depthDiff = saturate(casterDepth - receiverDepth);
                #else
                    float depthDiff = saturate(receiverDepth - casterDepth);
                #endif

                float casterDist    = depthDiff * _ShadowDepthScale;
                float fadeFactor    = 1.0 - saturate(casterDist / max(_ShadowFadeDistance, 0.001));
                half effectiveStrength = _ShadowStrength * fadeFactor;

                shadow = lerp(1.0, shadow, effectiveStrength);
                color *= shadow;

                return half4(color, 1.0);
            }

            ENDHLSL
        }

        // không có ShadowCaster pass → mesh không tự cast shadow lên chính nó
    }
}