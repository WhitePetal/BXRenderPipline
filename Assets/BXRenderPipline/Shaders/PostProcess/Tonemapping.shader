Shader "BXPostProcess/Tonemapping"
{
    SubShader
    {
        // Reinhard Tonemapping
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            FRAMEBUFFER_INPUT_HALF(0);

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
                half4 col = LOAD_FRAMEBUFFER_INPUT(0, i.uv_screen);
                col = min(col, 60);
                col /= col + 1.0;
                return half4(col.rgb, 1.0);
            }
            ENDHLSL
        }

        // Neutral Tonemapping
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            FRAMEBUFFER_INPUT_HALF(0);

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

            half4 frag(Varyings i) : SV_TARGET
            {
                half4 col = LOAD_FRAMEBUFFER_INPUT(0, i.uv_screen);
                col.rgb = min(col.rgb, 60.0);
	            col.rgb = NeutralTonemap(col.rgb);
                return half4(col.rgb, 1.0);
            }
            ENDHLSL
        }

        // ACES Tonemapping
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            FRAMEBUFFER_INPUT_HALF(0);

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

            half4 frag(Varyings i) : SV_TARGET
            {
                half4 col = LOAD_FRAMEBUFFER_INPUT(0, i.uv_screen);
                col.rgb = min(col.rgb, 60.0);
                return half4(AcesTonemap(unity_to_ACES(col.rgb)), 1.0);
            }
            ENDHLSL
        }
    }
}
