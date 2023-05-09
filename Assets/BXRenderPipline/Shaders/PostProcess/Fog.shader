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
            #include "Assets/BXRenderPipline/Shaders/Libiary/Lights.hlsl"
            #include "Assets/BXRenderPipline/Shaders/Libiary/Shadows.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
                float4 ray : TEXCOORD1;
            };

            Texture2D _BXDepthNormalBuffer;
            Texture2D _LightingBuffer;
            SamplerState sampler_point_clamp;
            half4 _FogColor;
            float4 _FogOuterParams;
            float4 _FogInnerParams;

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
                half4 lightingColor = _LightingBuffer.SampleLevel(sampler_point_clamp, i.uv_screen, 0);
                float4 depthNormalData = _BXDepthNormalBuffer.SampleLevel(sampler_point_clamp, i.uv_screen, 0);
                float3 n;
                float depth;
                DecodeDepthNormal(depthNormalData, depth, n);
                float depthEye = LinearEyeDepth(depth);
                float3 pos_world = _WorldSpaceCameraPos.xyz + i.ray.xyz * depthEye;
                half3 v = normalize(i.ray.xyz);
                half3 l = _DirectionalLightDirections[0].xyz;
                half vdotl = dot(v, l);
                half3 lightCol = _DirectionalLightColors[0].rgb;

                half fog = min(1.0, _FogInnerParams.x * exp(-_FogInnerParams.y * depthEye / _FogInnerParams.z));
                half3 col = lerp(_FogColor.rgb, lightingColor.rgb, fog) + lightCol * _FogOuterParams.x * (1.0 + vdotl * vdotl) * exp(-_FogOuterParams.y * exp(-0.5 * v.y));

                return half4(col, lightingColor.a);
            }
            ENDHLSL
        }
    }
}
