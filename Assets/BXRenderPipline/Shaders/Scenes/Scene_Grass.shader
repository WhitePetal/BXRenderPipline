Shader "Scene/Scene_Grass"
{
    Properties
    {
        _Control("Control (RGBA)", 2D) = "red" {}
        _BaseColor("BaseColor", Color) = (0.2, 1.0, 0.2, 1.0)
        _Color0("Color0", Color) = (0.2, 1.0, 0.2, 1.0)
        _Color1("Color1", Color) = (0.2, 1.0, 0.2, 1.0)
        _Color2("Color2", Color) = (0.2, 1.0, 0.2, 1.0)
        _Color3("Color3", Color) = (0.2, 1.0, 0.2, 1.0)
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

        Pass
        {
            Tags {"LightMode"="BXDepthNormal"}
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
                float2 terrainUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(_Terrain)
                float4 _TerrainPosition;
                float4 _TerrainSize; // terrainWidth、terrainHeight、detilMapWidth、detilMapHeight
            CBUFFER_END

            Texture2D _TerrainNormalmapTexture;
            SamplerState sampler_point_clamp;
            StructuredBuffer<float4> _DetilsPosition;

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 pos_world = _DetilsPosition[instanceID].xyz;

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
                v.vertex.xyz += 0.3 * sin((pos_world.x + pos_world.y + pos_world.z + _Time.x * 15.0)) * v.vertex.y * noiseWindDir * half3(1.0, 0.0, 1.0);
                pos_world += v.vertex.xyz;
                o.vertex = TransformWorldToHClip(pos_world);

                o.terrainUV = (pos_world.xz - _TerrainPosition.xz) / _TerrainSize.xy;

                return o;
            }

            half4 frag (v2f i) : SV_TARGET0
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n = _TerrainNormalmapTexture.Sample(sampler_point_clamp, i.terrainUV).xyz;
                half3 normal_view = mul((float3x3)UNITY_MATRIX_V, n).xyz;
                return (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(normal_view)) : float4(0.5,0.5,1.0,1.0);
            }
            ENDHLSL
        }

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

            #define BRDF_LIGHTING 1

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
                float2 terrainUV : TEXCOORD0;
                float4 pos_world : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Texture2D _Control;
            Texture2D _TerrainNormalmapTexture;
            #ifdef _REFLECT_PROBE_ONLY
                SamplerState sampler_point_clamp;
            #endif
            StructuredBuffer<float4> _DetilsPosition;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _Color0, _Color1, _Color2, _Color3;
            CBUFFER_END

            CBUFFER_START(_Terrain)
                float4 _TerrainPosition;
                float4 _TerrainSize; // terrainWidth、terrainHeight、detilMapWidth、detilMapHeight
            CBUFFER_END

            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos_world.xyz = _DetilsPosition[instanceID].xyz;
                o.pos_world.w = v.vertex.y;

                half r = random_nn(o.pos_world.xz);
                half angle = r * 6.283;
                half2 cs = half2(sin(angle), cos(angle));
                half2x2 mat = half2x2(
                    half2(cs.y, -cs.x),
                    cs
                );
                v.vertex.xz = mul(mat, v.vertex.xz);

                float3 noiseWindDir = float3(0, 0, 1);
                noiseWindDir.xz = mul(mat, noiseWindDir.xz);

                v.vertex.xyz += 0.3 * sin((o.pos_world.x + o.pos_world.y + o.pos_world.z + _Time.x * 15.0)) * v.vertex.y * noiseWindDir * half3(1.0, 0.0, 1.0);
                o.pos_world.xyz += v.vertex.xyz;
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);

                o.terrainUV = (o.pos_world.xz - _TerrainPosition.xz) / _TerrainSize.xy;

                return o;
            }

            half4 frag (v2f i) : SV_TARGET0
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 control = _Control.Sample(sampler_point_clamp, i.terrainUV);
                half3 baseCol = _Color0.rgb * control.r + _Color1.rgb * control.g + _Color2.rgb * control.b + _Color3.rgb * control.a;
                half3 albedo = lerp(baseCol, _BaseColor.rgb, i.pos_world.w);
                half3 specCol = 0.04;
                float depthEye = LinearEyeDepth(i.vertex.z);

                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                half3 n = _TerrainNormalmapTexture.Sample(sampler_point_clamp, i.terrainUV).xyz;
                half3 r = reflect(-v, n);
                half ndotv = max(0.001, dot(n, v));

                float2 uv_screen = i.vertex.xy * (_ScreenParams.zw - 1.0);
                half3 indirectDiffuse = PBR_GetIndirectDiffuseFromProbe(i.pos_world.xyz, n);
                half3 indirectSpecular = PBR_GetIndirectSpecular(specCol, r, uv_screen, ndotv, 0.7, 1.0);
                half3 diffuseColor = 0.0;
                half3 specularColor = 0.0;

                PBR_BRDF_DirectionalLighting(specCol, i.pos_world.xyz, 0.0, n, v, i.vertex.xy, ndotv, 0.7, depthEye, diffuseColor, specularColor);
                PBR_BRDF_PointLighting(specCol, i.pos_world.xyz, n, v, i.vertex.xy, ndotv, 0.7, diffuseColor, specularColor);

                diffuseColor = diffuseColor * albedo;
                specularColor *= 0.25 / ndotv;
                half3 lighting = diffuseColor + specularColor + (indirectDiffuse * albedo + indirectSpecular);

                return half4(lighting, 1.0);
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

            StructuredBuffer<float4> _DetilsPosition;

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 pos_world = _DetilsPosition[instanceID].xyz;
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

                v.vertex.xyz += 0.3 * sin((pos_world.x + pos_world.y + pos_world.z + _Time.x * 15.0)) * v.vertex.y * noiseWindDir * half3(1.0, 0.0, 1.0);
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
