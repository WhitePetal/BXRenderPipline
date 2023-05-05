Shader "BXCharacters/PBR_BRDF"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _MRATex("Metallic_Roughness_AO", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 4)) = 1
        [VectorRange(0, 1, 0.01, 1, 0, 1)]_Metallic_Roughness_AOOffset("Metallic_Roughness_AOOffset", Vector) = (1, 1, 0, 1)
        [Toggle]_EMISSION("Emission On", Int) = 0
        _EmissionMap("EmissionMap", 2D) = "white" {}
        [HDR]_EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        [ToggleOff]_RECEIVE_SHADOWS("Receive Shadows", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        // Pass
        // {
        //     Tags {"LightMode"="BXDepthNormal"}
        //     HLSLPROGRAM
        //     #pragma target 4.5
        //     #pragma multi_compile _ LOD_FADE_CROSSFADE
        //     #pragma multi_compile_instancing

        //     #pragma vertex vert
        //     #pragma fragment frag

        //     #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

        //     struct appdata
        //     {
        //         float4 vertex : POSITION;
        //         half3 normal : NORMAL;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
        //     };

        //     struct v2f
        //     {
        //         float4 vertex : SV_POSITION;
        //         float3 normal_view : TEXCOORD0;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
        //     };

        //     v2f vert (appdata v)
        //     {
        //         v2f o;
        //         UNITY_SETUP_INSTANCE_ID(v);
        //         UNITY_TRANSFER_INSTANCE_ID(v, o);
        //         float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
        //         o.vertex = TransformWorldToHClip(pos_world);
        //         half3 normal_world = TransformObjectToWorldNormal(v.normal);
        //         o.normal_view = mul((float3x3)UNITY_MATRIX_V, normal_world).xyz;

        //         return o;
        //     }

        //     half4 frag (v2f i) : SV_TARGET0
        //     {
        //         UNITY_SETUP_INSTANCE_ID(i);
        //         ClipLOD(i.vertex.xy);
        //         return (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(i.normal_view)) : float4(0.5,0.5,1.0,1.0);
        //     }
        //     ENDHLSL
        // }

        Pass
        {
            Tags {"LightMode"="BXOpaque"}
            // ZWrite Off
            // ZTest Equal
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _EMISSION_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _SSR_ONLY _REFLECT_PROBE_ONLY _SSR_AND_RELFECT_PROBE
            #pragma multi_compile_instancing

            #define BRDF_LIGHTING 1

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

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
                float3 pos_world : TEXCOORD1;
                half3 normal_world : TEXCOORD2;
                half3 tangent_world : TEXCOORD3;
                half3 binormal_world : TEXCOORD4;
                float3 normal_view : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Texture2D _MainTex, _MRATex, _NormalMap;
            SamplerState sampler_MainTex;
            #ifdef _EMISSION_ON
                Texture2D _EmissionMap;
            #endif

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Metallic_Roughness_AOOffset)
                UNITY_DEFINE_INSTANCED_PROP(half, _NormalScale)
                #ifdef _EMISSION_ON
                    UNITY_DEFINE_INSTANCED_PROP(half4, _EmissionColor)
                #endif
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos_world.xyz = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);
                o.uv = v.uv * GET_PROP(_MainTex_ST).xy + GET_PROP(_MainTex_ST).zw;
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
                ClipLOD(i.vertex.xy);
                half4 normalMap = _NormalMap.Sample(sampler_MainTex, i.uv);
                half4 mra = _MRATex.Sample(sampler_MainTex, i.uv);
                half4 baseColor = _MainTex.Sample(sampler_MainTex, i.uv);
                #ifdef _EMISSION_ON
                    half4 emissionMap = _EmissionMap.Sample(sampler_MainTex, i.uv);
                #endif
                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                half3 n = GetWorldNormalFromNormalMap(normalMap, GET_PROP(_NormalScale), i.tangent_world, i.binormal_world, i.normal_world);
                half3 r = reflect(-v, n);
                half ndotv = max(0.001, dot(n, v));

                half metallic = mra.r * GET_PROP(_Metallic_Roughness_AOOffset).x;
                half oneMinusMetallic = 1.0 - metallic;
                half roughness = mra.g * GET_PROP(_Metallic_Roughness_AOOffset).y;
                half ao = saturate(mra.b + GET_PROP(_Metallic_Roughness_AOOffset).z);
                baseColor *= GET_PROP(_Color);
                half3 albedo = baseColor.rgb * (1.0 - metallic);
                half3 specCol = lerp(0.04, baseColor.rgb, metallic * 0.5);
                float depthEye = LinearEyeDepth(i.vertex.z);

                float2 uv_screen = i.vertex.xy * (_ScreenParams.zw - 1.0);
                half3 indirectDiffuse = PBR_GetIndirectDiffuseFromProbe(i.pos_world.xyz, n);
                half3 indirectSpecular = PBR_GetIndirectSpecular(specCol, r, uv_screen, ndotv, roughness, oneMinusMetallic);
                half3 diffuseColor = 0.0;
                half3 specularColor = 0.0;

                PBR_BRDF_DirectionalLighting(specCol, i.pos_world.xyz, 0.0, n, v, i.vertex.xy, ndotv, roughness, depthEye, diffuseColor, specularColor);
                PBR_BRDF_PointLighting(specCol, i.pos_world.xyz, n, v, i.vertex.xy, ndotv, roughness, depthEye, diffuseColor, specularColor);

                diffuseColor = diffuseColor * albedo;
                specularColor *= 0.25 / ndotv;
                half3 lighting = diffuseColor + specularColor + (indirectDiffuse * albedo + indirectSpecular) * ao;
                #ifdef _EMISSION_ON
                    lighting += emissionMap.rgb * GET_PROP(_EmissionColor).rgb * emissionMap.a;
                #endif
                
                FragOutput o;
                o.lightingBuffer = half4(lighting, 1.0);
                o.depthNormalBuffer = (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(i.normal_view)) : float4(0.5,0.5,1.0,1.0);
                return o;
            }
            ENDHLSL
        }

        Pass 
        {
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
                ClipLOD(i.vertex.xy);
            }
			ENDHLSL
		}
    }
}
