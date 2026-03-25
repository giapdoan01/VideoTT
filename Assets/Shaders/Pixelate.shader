// Shader nội bộ cho PixelateFeature - không dùng trực tiếp trên vật thể.
Shader "Hidden/Pixelate"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest  Always
        Cull   Off

        Pass
        {
            Name "Pixelate"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Blit.hlsl cung cấp sẵn: Vert, Varyings, _BlitTexture, _BlitTexture_TexelSize

            float _PixelSize;
            float _Strength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv         = input.texcoord;
                float2 screenSize = _BlitTexture_TexelSize.zw;
                float2 gridCount  = max(floor(screenSize / max(_PixelSize, 1.0)), 1.0);

                // snap UV về tâm ô pixel gần nhất
                float2 pixelUV = (floor(uv * gridCount) + 0.5) / gridCount;

                half4 pixelColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp,  pixelUV);
                half4 origColor  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                return lerp(origColor, pixelColor, _Strength);
            }
            ENDHLSL
        }
    }
}
