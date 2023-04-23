Shader "BXPostProcess/ColorManager"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma multi_compile_local __ CM_ColorGrading    
            #pragma multi_compile_local __ CM_ColorSplitToning
            #pragma multi_compile_local __ CM_ShadowsMidtoneHighlights
            #pragma multi_compile_local __ CM_ColorChannelMixer
            #pragma multi_compile_local __ CM_ColorWhiteBalance
            #pragma multi_compile_local CM_ACES CM_Reinhard CM_Neutral
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Assets/BXRenderPipline/Shaders/Libiary/ColorManager.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            Texture2D _PostProcessInput;
            SamplerState sampler_point_clamp;
            // BXFRAMEBUFFER_INPUT_HALF(0, _LightingBuffer);

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.pos_clip = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                o.uv_screen = float2(
                    vertexID <= 1 ? 0.0 : 2.0,
                    vertexID == 1 ? 2.0 : 0.0
                );
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET0
            {
                half4 col = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv_screen, 0);
                return half4(ColorGrade(col.rgb), 1.0);
            }
            ENDHLSL
        }
    }
}
