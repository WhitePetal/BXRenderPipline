Shader "Scene/Scene_Tree"
{
    Properties
    {
        _Color("BaseColor", Color) = (0.2, 1.0, 0.2, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        HLSLINCLUDE
            float random_nn(float2 pos)
            {
                return frac(sin(dot(pos, float2(12.9898, 78.233))) * 43748.543);
            }

            
        ENDHLSL

        // Pass
        // {
        //     Tags {"LightMode"="BXDepthNormal"}
        //     HLSLPROGRAM
        //     #pragma target 4.5
        //     #pragma multi_compile_instancing

        //     #pragma vertex vert
        //     #pragma fragment frag

        //     #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

        //     struct appdata
        //     {
        //         float4 vertex : POSITION;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
        //     };

        //     struct v2f
        //     {
        //         float4 vertex : SV_POSITION;
        //         float2 terrainUV : TEXCOORD0;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
        //     };

        //     CBUFFER_START(_Terrain)
        //         float4 _TerrainPosition;
        //         float4 _TerrainSize; // terrainWidth、terrainHeight、detilMapWidth、detilMapHeight
        //     CBUFFER_END

        //     Texture2D _TerrainNormalmapTexture;
        //     SamplerState sampler_point_clamp;
        //     StructuredBuffer<float4> _DetilsPosition;

        //     v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
        //     {
        //         v2f o;
        //         UNITY_SETUP_INSTANCE_ID(v);
        //         UNITY_TRANSFER_INSTANCE_ID(v, o);
        //         float3 pos_world = _DetilsPosition[instanceID].xyz;

        //         half r = random_nn(pos_world.xz);
        //         half angle = r * 6.283;
        //         half2 cs = half2(sin(angle), cos(angle));
        //         half2x2 mat = half2x2(
        //             half2(cs.y, -cs.x),
        //             cs
        //         );

        //         v.vertex.xz = mul(mat, v.vertex.xz);
        //         float3 noiseWindDir = float3(0, 0, 1);
        //         noiseWindDir.xz = mul(mat, noiseWindDir.xz);
        //         v.vertex.xyz += 0.3 * sin((pos_world.x + pos_world.y + pos_world.z + _Time.x * 15.0)) * v.vertex.y * noiseWindDir * half3(1.0, 0.0, 1.0);
        //         pos_world += v.vertex.xyz;
        //         o.vertex = TransformWorldToHClip(pos_world);

        //         o.terrainUV = (pos_world.xz - _TerrainPosition.xz) / _TerrainSize.xy;

        //         return o;
        //     }

        //     half4 frag (v2f i) : SV_TARGET0
        //     {
        //         UNITY_SETUP_INSTANCE_ID(i);
        //         half3 n = _TerrainNormalmapTexture.Sample(sampler_point_clamp, i.terrainUV).xyz * 2 - 1;
        //         half3 normal_view = mul((float3x3)UNITY_MATRIX_V, n).xyz;
        //         return (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(normal_view)) : float4(0.5,0.5,1.0,1.0);
        //     }
        //     ENDHLSL
        // }

        Pass
        {
            Tags {"LightMode"="BXOpaque"}
            // ZWrite Off
            // ZTest Equal
            HLSLPROGRAM
            #pragma target 4.5
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _SSR_ONLY _REFLECT_PROBE_ONLY _SSR_AND_RELFECT_PROBE
            #pragma multi_compile_instancing

            #define HALF_LAMBERT_LIGHTING 1

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 pos_world : TEXCOORD0;
                float3 normal_world : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            StructuredBuffer<float4> _TreePositions;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos_world.xyz = _TreePositions[instanceID].xyz;
                o.pos_world.w = v.vertex.y * v.vertex.y;

                half r = random_nn(o.pos_world.xz);
                half angle = r * 6.283;
                half2 cs = half2(sin(angle), cos(angle));
                half2x2 mat = half2x2(
                    half2(cs.y, -cs.x),
                    cs
                );
                // v.vertex.xz = mul(mat, v.vertex.xz);
                o.normal_world = v.normal;
                o.normal_world.xz = mul(mat, o.normal_world.xz);

                float3 noiseWindDir = float3(0, 0, 1);
                noiseWindDir.xz = mul(mat, noiseWindDir.xz);

                // v.vertex.xyz += 0.3 * sin((o.pos_world.x + o.pos_world.y + o.pos_world.z + _Time.x * 15.0)) * v.vertex.y * noiseWindDir * half3(1.0, 0.0, 1.0);
                o.pos_world.xyz += v.vertex.xyz;
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);

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
                half3 albedo = _Color.rgb;
                float depthEye = LinearEyeDepth(i.vertex.z);

                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                half3 n = normalize(i.normal_world);
                half ndotv = max(0.001, dot(n, v));

                float2 uv_screen = i.vertex.xy * (_ScreenParams.zw - 1.0);
                half3 indirectDiffuse = PBR_GetIndirectDiffuseFromProbe(i.pos_world.xyz, n);
                half3 diffuseColor = 0.0;
                
                HALF_LAMBERT_DirectionalLighting(i.pos_world.xyz, n, i.vertex.xy, depthEye, diffuseColor);
                HALF_LAMBERT_PointLighting(i.pos_world.xyz, n, i.vertex.xy, depthEye, diffuseColor);

                diffuseColor = diffuseColor * albedo;
                half3 lighting = diffuseColor + indirectDiffuse * albedo;

                FragOutput o;
                o.lightingBuffer = half4(ApplyFog(lighting, v, depthEye), 1.0);
                half3 normal_view = mul((float3x3)UNITY_MATRIX_V, n).xyz;
                o.depthNormalBuffer = (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(normal_view)) : float4(0.5,0.5,1.0,1.0);
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
			#pragma target 4.5
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

            StructuredBuffer<float4> _TreePositions;

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 pos_world = _TreePositions[instanceID].xyz;
                half r = random_nn(pos_world.xz);
                half angle = r * 6.283;
                half2 cs = half2(sin(angle), cos(angle));
                half2x2 mat = half2x2(
                    half2(cs.y, -cs.x),
                    cs
                );
                v.vertex.xz = mul(mat, v.vertex.xz);
                float3 noiseWindDir = float3(0, 0, 1);
                noiseWindDir.xz = mul(mat, noiseWindDir.xz);

                // v.vertex.xyz += 0.3 * sin((pos_world.x + pos_world.y + pos_world.z + _Time.x * 15.0)) * v.vertex.y * noiseWindDir * half3(1.0, 0.0, 1.0);
                pos_world.xyz += v.vertex.xyz;

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
