Shader "BXDefferedShadings/Combine"
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
                half4 baseColor = LOAD_FRAMEBUFFER_INPUT(0, i.uv_screen);
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
