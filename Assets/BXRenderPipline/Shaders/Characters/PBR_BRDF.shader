Shader "BXCharacters/PBR_BRDF"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _MRATex("Metallic_Roughness_AO", 2D) = "white" {}
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
                float4 pos_world : TEXCOORD1;
                half3 normal_world : TEXCOORD2;
                half3 tangent_world : TEXCOORD3;
                half3 binormal_world : TEXCOORD4;
                float3 normal_view : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Texture2D _MainTex, _MRATex;
            SamplerState sampler_MainTex;

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Metallic_Roughness_AOOffset)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos_world.xyz = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);
                o.pos_world.w = o.vertex.z / o.vertex.w;
                o.pos_world.w = Linear01Depth(o.pos_world.w); // depth linear 0-1
                o.uv = v.uv * GET_PROP(_MainTex_ST).xy + GET_PROP(_MainTex_ST).zw;
                o.normal_world = TransformObjectToWorldNormal(v.normal);
                o.tangent_world = TransformObjectToWorldDir(v.tangent.xyz);
                o.binormal_world = cross(o.normal_world, o.tangent_world) * v.tangent.w * unity_WorldTransformParams.w;
                o.normal_view = mul((float3x3)UNITY_MATRIX_V, o.normal_world).xyz;

                return o;
            }

            struct FragOutput
            {
                half4 lightingBuffer : SV_TARGET;
                half4 baseColorBuffer : SV_TARGET1;
                half4 materialDataBuffer : SV_TARGET2;
                half4 depthNormalBuffer : SV_TARGET3;
            };

            FragOutput frag (v2f i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                FragOutput output = (FragOutput)0;
                half4 mra = _MRATex.Sample(sampler_MainTex, i.uv);
                half4 baseColor = _MainTex.Sample(sampler_MainTex, i.uv);
                half3 lightColor = _DirectionalLightColors[0].xyz;
                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                half3 l = _DirectionalLightDirections[0].xyz;
                half3 h = normalize(l + v);
                half3 n = normalize(i.normal_world);
                half ndotl = max(0.0, dot(n, l));
                half ndoth = max(0.0, dot(n, h));
                half ndotv = max(0.001, dot(n, v));
                half vdoth = max(0.0, dot(v, h));
                half ldoth = max(0.0, dot(l, h));

                half metallic = mra.r * GET_PROP(_Metallic_Roughness_AOOffset).x;
                half roughness = mra.g * GET_PROP(_Metallic_Roughness_AOOffset).y;
                half ao = saturate(mra.b + GET_PROP(_Metallic_Roughness_AOOffset).z);

                baseColor *= GET_PROP(_BaseColor);
                half3 specCol = lerp(0.04, baseColor.rgb, metallic * 0.5);
                half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
                half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);

                half shadowAtten = GetDirectionalShadow(0, i.vertex.xy, i.pos_world.xyz, n, i.pos_world.w * _ProjectionParams.z);
                half3 shadowCol = lerp(_BXShadowsColor.xyz, 1.0, shadowAtten);

                half3 diffuseColor = baseColor.rgb * (1.0 - metallic) * f0 * ndotl;
                half3 indirectDiffuse = 0.2 * ao * baseColor.rgb;
                half3 specularColor = fgd * 0.25 / ndotv;

                output.lightingBuffer = half4((diffuseColor + specularColor) * shadowCol * lightColor + indirectDiffuse, 1.0);
                output.baseColorBuffer = baseColor;
                int materialFlag = 2; // 1 << 1
                #ifndef _RECEIVE_SHADOWS_OFF
                    materialFlag += 1;
                #endif
                output.materialDataBuffer = half4(metallic, roughness, ao, materialFlag / 255.0);
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
