Shader "BXCharacters/PBR_BSSSDF_Skin"
{
    Properties
    {
        _LUTSSS("LUT_SSS", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _MRATex("Metallic_Roughness_AO", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 4)) = 1
        [VectorRange(0, 1, 0.01, 1, 0, 1)]_Metallic_Roughness_AOOffset("Metallic_Roughness_AOOffset", Vector) = (1, 1, 0, 1)
        [ToggleOff]_RECEIVE_SHADOWS("Receive Shadows", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        Pass
        {
            Tags {"LightMode"="BXCharacter"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_screen : TEXCOORD1;
                float4 pos_world : TEXCOORD2;
                half3 normal_world : TEXCOORD3;
                half3 tangent_world : TEXCOORD4;
                half3 binormal_world : TEXCOORD5;
                float3 normal_view : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Texture2D _LUTSSS, _MainTex, _MRATex, _NormalMap;
            SamplerState sampler_MainTex;
            SamplerState sampler_bilinear_clamp;

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Metallic_Roughness_AOOffset)
                UNITY_DEFINE_INSTANCED_PROP(half, _NormalScale)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos_world.xyz = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);
                float4 posH = o.vertex / o.vertex.w;
                o.pos_world.w = posH.z;
                o.uv = v.uv * GET_PROP(_MainTex_ST).xy + GET_PROP(_MainTex_ST).zw;
                o.uv_screen = GetScreenUV(posH).xy;
                o.normal_world = TransformObjectToWorldNormal(v.normal);
                o.tangent_world = TransformObjectToWorldDir(v.tangent.xyz);
                o.binormal_world = cross(o.normal_world, o.tangent_world) * v.tangent.w * unity_WorldTransformParams.w;
                o.normal_view = mul((float3x3)UNITY_MATRIX_V, o.normal_world).xyz;

                return o;
            }

            struct FragOutput
            {
                half4 lightingBuffer : SV_TARGET0;
                half4 depthNormalBuffer : SV_TARGET1;
            };

            FragOutput frag (v2f i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                FragOutput output = (FragOutput)0;
                half4 normalMap = _NormalMap.Sample(sampler_MainTex, i.uv);
                half4 mra = _MRATex.Sample(sampler_MainTex, i.uv);
                half4 baseColor = _MainTex.Sample(sampler_MainTex, i.uv);

                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                half3 n = GetWorldNormalFromNormalMap(normalMap, GET_PROP(_NormalScale), i.tangent_world, i.binormal_world, i.normal_world);
                half r = saturate(dot(1, fwidth(n) / fwidth(i.pos_world.xyz)) * 0.333);
                half ndotv = max(0.001, dot(n, v));

                half metallic = mra.r * GET_PROP(_Metallic_Roughness_AOOffset).x;
                half oneMinusMetallic = 1.0 - metallic;
                half roughness = mra.g * GET_PROP(_Metallic_Roughness_AOOffset).y;
                half ao = saturate(mra.b + GET_PROP(_Metallic_Roughness_AOOffset).z);
                half3 albedo = baseColor.rgb * (1.0 - metallic);
                half3 specCol = lerp(0.04, baseColor.rgb, metallic * 0.5);
                float depthEye = LinearEyeDepth(i.pos_world.w);

                half3 ndotl_sss_avg = _LUTSSS.Sample(sampler_bilinear_clamp, float2(ndotv, r)).rgb;

                half3 indirectDiffuse = 0.2 * ao * albedo;
                half3 diffuseColor = 0.0;
                half3 specularColor = 0.0;

                for(uint lightIndex = 0; lightIndex < _DirectionalLightCount; ++lightIndex)
                {
                    half3 l = _DirectionalLightDirections[lightIndex].xyz;
                    half3 lightColor = _DirectionalLightColors[lightIndex].xyz;
                    half3 h = SafeNormalize(l + v);
                    half ndotl_source = dot(n, l);
                    half ndotl = max(0.0, dot(n, l));
                    half3 ndotl_sss = _LUTSSS.Sample(sampler_bilinear_clamp, float2(ndotl_source * 0.5 + 0.5, r)).rgb;
                    half ndoth = max(0.0, dot(n, h));
                    half vdoth = max(0.0, dot(v, h));
                    half ldoth = max(0.0, dot(l, h));
                    half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
                    half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);
                    half shadowAtten = GetDirectionalShadow(lightIndex, i.uv_screen, i.pos_world.xyz, n, depthEye);
                    half3 shadowCol = lerp(_BXShadowsColor.xyz, 1.0, shadowAtten);
                    lightColor *= shadowCol;

                    diffuseColor += lightColor * f0 * ndotl_sss;
                    specularColor += lightColor * fgd;
                }

                uint2 screenXY = i.uv_screen * _ScreenParams.xy / 16.0;
                uint tileIndex = screenXY.y * _ScreenParams.x / 16.0 + screenXY.x;
                uint tileData = _TileLightingDatas[tileIndex];

                for(uint tileLightOffset = 0; tileLightOffset < min(tileData, _PointLightCount); ++tileLightOffset)
                {
                    uint tileLightIndex = tileIndex * 256 + tileLightOffset;
                    uint pointLightIndex = _TileLightingIndices[tileLightIndex];
                    float4 lightSphere = _PointLightSpheres[pointLightIndex];
                    half3 lightColor = _PointLightColors[pointLightIndex].xyz;

                    float3 lenV = lightSphere.xyz - i.pos_world.xyz;
                    half lenSqr = max(0.001, dot(lenV, lenV));
                    half3 l = lenV * rsqrt(lenSqr);
                    half3 h = SafeNormalize(l + v);

                    half ndotl = max(0.0, dot(n, l));
                    half ndoth = max(0.0, dot(n, h));
                    half ldoth = max(0.0, dot(l, h));
                    half atten =  saturate(1.0 - lenSqr / (lightSphere.w * lightSphere.w));

                    lightColor *= atten;
                    half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
                    half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);

                    diffuseColor += lightColor * f0 * ndotl_sss_avg;
                    specularColor += lightColor * fgd;
                }

                half3 indirectSpecular = 0.0;
                #ifndef _PROBE_ONLY
                    half4 ssrData = _SSRBuffer.SampleLevel(sampler_point_clamp, i.uv_screen, roughness * 3);
                    indirectSpecular = 0.5 * ssrData.rgb * lerp(specCol, saturate(2.0 - roughness - oneMinusMetallic), PBR_SchlickFresnel(ndotv)) / (1.0 + roughness * roughness);
                #endif

                diffuseColor *= albedo;
                specularColor *= 0.25 / ndotv;
                half3 lighting = (diffuseColor + specularColor) + indirectDiffuse + indirectSpecular;
                output.lightingBuffer = half4(lighting, 1.0);

                output.depthNormalBuffer = (i.pos_world.w < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.pos_world.w, normalize(i.normal_view)) : float4(0.5,0.5,1.0,1.0);
                return output;
            }
            ENDHLSL
        }

        Pass {
			Tags {"LightMode" = "ShadowCaster"}
            Cull Off
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma multi_compile_instancing
			#pragma vertex vert
			#pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
			
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(pos_world);
                #if UNITY_REVERSED_Z
                    o.vertex.z =
                        min(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    o.vertex.z =
                        max(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            void frag (v2f i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
            }
			ENDHLSL
		}
    }
}
