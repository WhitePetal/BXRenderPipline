Shader "BXDefferedShadingsEditor/Combine"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            BlendOp Add
            Blend 0 DstColor Zero, Zero One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            TEXTURE2D(_BaseColorBuffer);
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
                half4 baseColor = SAMPLE_TEXTURE2D_LOD(_BaseColorBuffer, sampler_bilinear_clamp, i.uv_screen, 0);
                return half4(baseColor.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            BlendOp Add
            Blend 0 DstAlpha One, One Zero
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
            };

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.pos_clip = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET
            {
                return half4(1.0, 1.0, 1.0, 1.0);
            }
            ENDHLSL
        }
    }
}
