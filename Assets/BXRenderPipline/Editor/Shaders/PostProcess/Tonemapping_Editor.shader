Shader "BXPostProcessEditor/Tonemapping"
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

            TEXTURE2D(_LightingBuffer);
            SAMPLER(sampler_bilinear_clamp);

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
                half4 col = SAMPLE_TEXTURE2D_LOD(_LightingBuffer, sampler_bilinear_clamp, i.uv_screen, 0);
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

            TEXTURE2D(_LightingBuffer);
            SAMPLER(sampler_bilinear_clamp);

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
                half4 col = SAMPLE_TEXTURE2D_LOD(_LightingBuffer, sampler_bilinear_clamp, i.uv_screen, 0);
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

            TEXTURE2D(_LightingBuffer);
            SAMPLER(sampler_bilinear_clamp);

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
                half4 col = SAMPLE_TEXTURE2D_LOD(_LightingBuffer, sampler_bilinear_clamp, i.uv_screen, 0);
                col.rgb = min(col.rgb, 60.0);
                return half4(AcesTonemap(unity_to_ACES(col.rgb)), 1.0);
            }
            ENDHLSL
        }
    }
}
