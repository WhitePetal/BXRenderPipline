Shader "Scene/Scene_Grass"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
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
            #pragma target 3.5
            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal_view : TEXCOORD0;
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
                pos_world += v.vertex.xyz;
                o.vertex = TransformWorldToHClip(pos_world);
                half3 normal_world = TransformObjectToWorldNormal(v.normal);
                o.normal_view = mul((float3x3)UNITY_MATRIX_V, normal_world).xyz;

                return o;
            }

            half4 frag (v2f i) : SV_TARGET0
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(i.normal_view)) : float4(0.5,0.5,1.0,1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Tags {"LightMode"="BXOpaque"}
            ZWrite Off
            ZTest Equal
            HLSLPROGRAM
            #pragma target 3.5
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
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 pos_world : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Texture2D _Control;
            StructuredBuffer<float4> _DetilsPosition;
            SamplerState sampler_point_clamp;

            v2f vert (appdata v, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos_world = _DetilsPosition[instanceID].xyz;

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
                o.pos_world += v.vertex.xyz;
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);

                return o;
            }

            struct FragOutput
            {
                half4 lightingBuffer : SV_TARGET0;
                half4 depthNormalBuffer : SV_TARGET1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color0, _Color1, _Color2, _Color3;
            CBUFFER_END

            half4 frag (v2f i) : SV_TARGET0
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 control = _Control.Sample(sampler_point_clamp, i.pos_world.xz / 256.0);
                return _Color0 * control.r + _Color1 * control.g + _Color2 * control.b + _Color3 * control.a;
                // return half4(lighting, 1.0);
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

            StructuredBuffer<float4> _DetilsPosition;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                #if UNITY_ANY_INSTANCING_ENABLED
                float3 pos_world = v.vertex.xyz + _DetilsPosition[v.instanceID].xyz;
                #else
                float3 pos_world = v.vertex.xyz;
                #endif
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
