Shader "BXDefferedShadingsEditor/Shading"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            BlendOp Add
            Blend One One, Zero One
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
                float4 vray : TEXCOORD1;
            };

            TEXTURE2D(_MaterialDataBuffer);
            TEXTURE2D(_BXDepthNormalBuffer);
            TEXTURE2D(_BaseColorBuffer);
            SAMPLER(sampler_bilinear_clamp);

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

            half4 frag(Varyings i) : SV_TARGET0
            {
                half4 materialData = SAMPLE_TEXTURE2D_LOD(_MaterialDataBuffer, sampler_bilinear_clamp, i.uv_screen, 0);
                int materialFlag = materialData.w * 255;
                int needShading = materialFlag >> 1;
                if(needShading == 0) return 0.0;

                half4 depthNormalData = SAMPLE_TEXTURE2D_LOD(_BXDepthNormalBuffer, sampler_bilinear_clamp, i.uv_screen, 0);
                half4 baseColor = SAMPLE_TEXTURE2D_LOD(_BaseColorBuffer, sampler_bilinear_clamp, i.uv_screen, 0);

                int needShadowed = (materialFlag & 1);

                half3 point_light_pos = half3(1, 1, 0);
                half3 lightCol = half3(2, 0, 0);
                half3 n;
                float depth01;
                DecodeDepthNormalWorld(depthNormalData, depth01, n);
                float3 pos_world = _WorldSpaceCameraPos.xyz + i.vray.xyz * depth01;
                half3 v = normalize(_WorldSpaceCameraPos.xyz - pos_world);
                float3 lenV = point_light_pos - pos_world;
                half3 l = normalize(lenV);
                half3 h = normalize(l + v);

                half ndotv = max(0.0001, dot(n, v));
                half ndotl = max(0.0, dot(n, l));
                half ndoth = max(0.0, dot(n, h));
                half ldoth = max(0.0, dot(l, h));
                half atten = 1.0 / dot(lenV, lenV);
                lightCol *= atten;

                half3 specCol = lerp(0.04, baseColor.rgb, materialData.r * 0.5);
                half f0 = PBR_F0(ndotl, ndotv, ldoth, materialData.g);
                half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, materialData.g) * PBR_D(materialData.g, ndoth);
                
                half oneMinusMetallic = (1.0 - materialData.r);
                half ndotv_inv = 0.25 / ndotv;
                
                half3 diffuseColor = lightCol * f0 * ndotl;
                half3 specularColor = lightCol * fgd;

                for(int lightIndex = 1; lightIndex < _DirectionalLightCount; ++lightIndex)
                {
                    l = _DirectionalLightDirections[lightIndex].xyz;
                    lightCol = _DirectionalLightColors[lightIndex].xyz;
                    h = normalize(l + v);
                    ndotl = max(0.0, dot(n, l));
                    ndoth = max(0.0, dot(n, h));
                    ldoth = max(0.0, dot(l, h));
                    f0 = PBR_F0(ndotl, ndotv, ldoth, materialData.g);
                    fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, materialData.g) * PBR_D(materialData.g, ndoth);
                    half3 shadowCol = 1.0;
                    if(needShadowed == 1)
                    {
                        half shadowAtten = GetDirectionalShadow(lightIndex, i.uv_screen, pos_world.xyz, n, depth01 * _ProjectionParams.z);
                        shadowCol = lerp(_BXShadowsColor.xyz, 1.0, shadowAtten);
                    }
                    lightCol *= shadowCol;
                    diffuseColor += lightCol * f0 * ndotl;
                    specularColor += lightCol * fgd;
                }

                return half4(diffuseColor * oneMinusMetallic * baseColor.rgb + specularColor * ndotv_inv, 1.0);
            }
            ENDHLSL
        }
    }
}
