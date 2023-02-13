Shader "BXDefferedShadings/Shading"
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

            FRAMEBUFFER_INPUT_HALF_MS(0);
            FRAMEBUFFER_INPUT_HALF_MS(1);
            FRAMEBUFFER_INPUT_HALF_MS(2);

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
                half4 baseColor = LOAD_FRAMEBUFFER_INPUT_MS(0, 0, i.uv_screen);
                half4 materialData = LOAD_FRAMEBUFFER_INPUT_MS(1, 0, i.uv_screen);
                half4 depthNormalData = LOAD_FRAMEBUFFER_INPUT_MS(2, 0, i.uv_screen);
                int materialFlag = materialData.w * 255;
                int needShading = materialFlag >> 1;
                if(needShading == 0) return 0.0;


                int needShadowed = (materialFlag & 1);

                half3 n;
                float depth01;
                DecodeDepthNormal(depthNormalData, depth01, n);
                n = mul((float3x3)UNITY_MATRIX_I_V, n);
                float3 pos_world = _WorldSpaceCameraPos.xyz + i.vray.xyz * depth01;
                half3 v = normalize(_WorldSpaceCameraPos.xyz - pos_world);
                half ndotv = max(0.0001, dot(n, v));

                half3 specCol = lerp(0.04, baseColor.rgb, materialData.r * 0.5);

                half oneMinusMetallic = (1.0 - materialData.r);
                half ndotv_inv = 0.25 / ndotv;
                
                half3 diffuseColor = 0.0;
                half3 specularColor = 0.0;

                uint2 screenXY = i.uv_screen * _ScreenParams.xy;
                uint tileIndex = (screenXY.y / 16) * (_ScreenParams.x / 16) + (screenXY.x % 16);
                uint tileData = _TileLightingDatas[tileIndex];
                for(uint tileLightOffset = 0; tileLightOffset < tileData; ++tileLightOffset)
                {
                    uint tileLightIndex = tileIndex * 256 + tileLightOffset;
                    uint pointLightIndex = _TileLightingIndices[tileLightIndex];
                    float4 lightSphere = _PointLightSpheres[pointLightIndex];
                    half3 lightCol = _PointLightColors[pointLightIndex].xyz;

                    float3 lenV = lightSphere.xyz - pos_world;
                    half3 l = normalize(lenV);
                    half3 h = normalize(l + v);

                    half ndotl = max(0.0, dot(n, l));
                    half ndoth = max(0.0, dot(n, h));
                    half ldoth = max(0.0, dot(l, h));
                    half atten = 1.0 / dot(lenV, lenV);

                    lightCol *= atten;
                    half f0 = PBR_F0(ndotl, ndotv, ldoth, materialData.g);
                    half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, materialData.g) * PBR_D(materialData.g, ndoth);

                    diffuseColor += lightCol * f0 * ndotl;
                    specularColor += lightCol * fgd;
                }

                for(uint lightIndex = 1; lightIndex < 2; ++lightIndex)
                {
                    half3 l = _DirectionalLightDirections[lightIndex].xyz;
                    half3 lightCol = _DirectionalLightColors[lightIndex].xyz;
                    half3 h = normalize(l + v);
                    half ndotl = max(0.0, dot(n, l));
                    half ndoth = max(0.0, dot(n, h));
                    half ldoth = max(0.0, dot(l, h));
                    half f0 = PBR_F0(ndotl, ndotv, ldoth, materialData.g);
                    half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, materialData.g) * PBR_D(materialData.g, ndoth);
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
