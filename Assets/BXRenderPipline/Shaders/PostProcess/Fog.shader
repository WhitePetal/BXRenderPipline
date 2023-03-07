Shader "BXPostProcess/Fog"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Assets/BXRenderPipline/Shaders/Libiary/Shadows.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
                float4 ray : TEXCOORD1;
            };

            Texture2D _BXDepthNormalBuffer;
            SamplerState sampler_point_clamp;

            Varyings vert (uint vertexID : SV_VertexID)
            {
                Varyings o;
                const float4 vertexs[4] = 
                {
                    float4(-1.0, -1.0, 0.0, 1.0),
                    float4(1.0, -1.0, 0.0, 1.0),
                    float4(1.0, 1.0, 0.0, 1.0),
                    float4(-1.0, 1.0, 0.0, 1.0)
                };
                const float2 uvs[4] = 
                {
                    float2(0.0, 0.0),
                    float2(1.0, 0.0),
                    float2(1.0, 1.0),
                    float2(0.0, 1.0)
                };
                const uint rayIds[4] = {1, 3, 2, 0};
                o.pos_clip = vertexs[vertexID];
                o.uv_screen = uvs[vertexID];
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                o.ray = _ViewPortRays[rayIds[vertexID]];
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float4 depthNormalData = _BXDepthNormalBuffer.SampleLevel(sampler_point_clamp, i.uv_screen, 0);
                float3 n;
                float depth;
                DecodeDepthNormal(depthNormalData, depth, n);
                float depthEye = LinearEyeDepth(depth);

                float3 ray = normalize(i.ray.xyz);

                half3 l = _DirectionalLightDirections[0].xyz;
                half3 lightColor = _DirectionalLightColors[0].xyz;
                half vdotl = max(0.0, dot(ray, l));

                float stepF = pow(2, 20) + 1.0;
                float stepSize = depthEye / stepF;
                half result = 0.0;
                float3 curPoint = _WorldSpaceCameraPos.xyz;
                [unroll(20)]
                for(int k = 0; k < 20; ++k)
                {
                    float3 pos_world = curPoint + ray.xyz * stepSize;
                    curPoint = pos_world;
                    stepSize *= 2.0;
                    half shadowAtten = GetDirectionalShadow(0, i.uv_screen * _ScreenParams.xy, pos_world, n, 1.0);
                    result += shadowAtten * 0.06 * vdotl;
                }
                return half4(result * lightColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Prefilter"
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

            Texture2D _LightingBuffer, _FogLightingBuffer;
            float4 _FogLightingBuffer_TexelSize;
            SamplerState sampler_bilinear_clamp;
            SamplerState sampler_point_clamp;

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
                half3 color = _LightingBuffer.SampleLevel(sampler_point_clamp, i.uv_screen, 0).rgb + _FogLightingBuffer.SampleLevel(sampler_bilinear_clamp, i.uv_screen + _FogLightingBuffer_TexelSize.xy * 2.0, 0).rgb;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
