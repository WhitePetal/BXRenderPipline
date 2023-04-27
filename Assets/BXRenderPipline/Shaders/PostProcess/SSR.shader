Shader "BXDefferedShadings/SSR"
{
    SubShader
    {
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
                float4 vray : TEXCOORD1;
            };


            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                const float4 vertexs[6] = {
                    float4(1, -1, 0, 1),
                    float4(-1, 1, 0, 1),
                    float4(-1, -1, 0, 1),
                    float4(-1, 1, 0, 1),
                    float4(1, -1, 0, 1),
                    float4(1, 1, 0, 1)
                };
                const float2 uvs[6] = {
                    float2(1, 0),
                    float2(0, 1),
                    float2(0, 0),
                    float2(0, 1),
                    float2(1, 0),
                    float2(1, 1)
                };
                o.pos_clip = vertexs[vertexID];
                o.uv_screen = uvs[vertexID];
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                o.vray = _ViewPortRays[o.uv_screen.x * 2 + o.uv_screen.y];
                return o;
            }

            Texture2D _LightingBuffer;
            Texture2D _BXDepthNormalBuffer;
            SamplerState sampler_bilinear_clamp;

            const float JittersX[10] = {0.1, 0.3, 0.85, 0.12, 0.43, 0.24, 0.66, 0.93, 0.03, 0.33};
            const float JittersY[10] = {0.1, 0.3, 0.85, 0.12, 0.43, 0.24, 0.66, 0.83, 0.03, 0.33};

			half4 frag(Varyings i) : SV_TARGET0
            {
                half4 finalCol = 0.0;
                half4 depthNormal = _BXDepthNormalBuffer.SampleLevel(sampler_bilinear_clamp, i.uv_screen, 0);
                float depth_01;
                half3 n;
                DecodeDepthNormal(depthNormal, depth_01, n);
                float depthEye = depth_01 * _ProjectionParams.z;
                float3 pos_v = mul(unity_MatrixV, float4(_WorldSpaceCameraPos.xyz + i.vray.xyz * depthEye, 1.0)).xyz;
                half3 v = normalize(pos_v);
                half3 r = reflect(v, n);

                int2 uv_jitter = floor(frac(i.uv_screen * 2048) * 10);
                float jitter = (JittersX[uv_jitter.x] + JittersY[uv_jitter.y]);

                pos_v += r * jitter * 2;

                float maxDisstance = 200.0;
                float maxStep = 5;
                float stepSize = 10.0;
                float stepCount = 0;

                float2 rUV = 0.0;
                float d01 = 0.0;
                UNITY_UNROLL
                for(int i = 0; i < 5; i++)
                {
                    float3 rPos = pos_v + r * stepSize * i;
                    float4 rPosCS = mul(unity_CameraProjection, float4(rPos, 1.0));
                    rPosCS.xy /= rPosCS.w;
                    rUV = rPosCS.xy * 0.5 + 0.5;
                    float4 rDepthNormal = _BXDepthNormalBuffer.SampleLevel(sampler_bilinear_clamp, rUV, 0);
                    d01 = DecodeFloatRG(rDepthNormal.zw);
                    float depth = d01 * _ProjectionParams.z + 0.2;
                    float rDepth = -rPos.z;

                    UNITY_BRANCH
                    if (rUV.x > 0.0 && rUV.y > 0.0 && rUV.x < 1.0 && rUV.y < 1.0 && rDepth > depth && rDepth < depth + 10)
                    {
                        finalCol = _LightingBuffer.SampleLevel(sampler_bilinear_clamp, rUV, 0);
                        return finalCol * (stepCount / maxStep) * (1.0 - max(0.0, min(1.0, max(rUV.x, rUV.y)) - 0.6) / 0.4);
                    }
                    stepCount++;
                    // stepSize *= 2;
                }
                finalCol = _LightingBuffer.SampleLevel(sampler_bilinear_clamp, rUV, 0);
                return finalCol * (1.0 - max(0.0, min(1.0, max(rUV.x, rUV.y)) - 0.6) / 0.4);
			}
            ENDHLSL
        }
    }
}
