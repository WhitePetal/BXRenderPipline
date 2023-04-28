Shader "BXScenes/Scene_Terrain"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        Pass
        {
            Tags { "LightMode"="BXDepthNormal" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

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
                float3 normal_view : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(_Terrain)
                float4 _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
                float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Terrain)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
            UNITY_INSTANCING_BUFFER_END(Terrain)

            Texture2D _TerrainHeightmapTexture;
            Texture2D _TerrainNormalmapTexture;
            SamplerState sampler_TerrainNormalmapTexture;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float2 patchVertex = v.vertex.xy;
                float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);
                float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z;
                float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

                v.vertex.xz = sampleCoords * _TerrainHeightmapScale.xz;
                v.vertex.y = height * _TerrainHeightmapScale.y;
                float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(pos_world);
                half3 normal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
                // half4 tangent = half4(cross(half3(0, 0, 1), normal), 1.0);
                float3 normal_world = TransformObjectToWorldNormal(normal);
                o.normal_view = mul((float3x3)UNITY_MATRIX_V, normal_world).xyz;

                return o;
            }

            half4 frag (v2f i) : SV_TARGET0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return (i.vertex.z < (1.0-1.0/65025.0)) ? EncodeDepthNormal(i.vertex.z, normalize(i.normal_view)) : float4(0.5,0.5,1.0,1.0);
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
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #define BRDF_LIGHTING 1

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
                float2 uv : TEXCOORD0;
                float4 uvSplat01 : TEXCOORD1;
                float4 uvSplat23 : TEXCOORD2;
                float3 pos_world : TEXCOORD3;
                half3 normal_world : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Control_TexelSize;
                float4 _Control_ST;
                half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
            CBUFFER_END

            CBUFFER_START(_Terrain)
                float4 _TerrainHeightmapRecipSize;   // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
                float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
            CBUFFER_END
            
            Texture2D _TerrainHeightmapTexture;
            Texture2D _TerrainNormalmapTexture;
            SamplerState sampler_TerrainNormalmapTexture;
            Texture2D _Control, _Splat0, _Splat1, _Splat2, _Splat3;
            SamplerState sampler_Control;
            SamplerState sampler_Splat0;

            UNITY_INSTANCING_BUFFER_START(Terrain)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
            UNITY_INSTANCING_BUFFER_END(Terrain)

            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float2 patchVertex = v.vertex.xy;
                float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);
                float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z;
                float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

                v.vertex.xz = sampleCoords * _TerrainHeightmapScale.xz;
                v.vertex.y = height * _TerrainHeightmapScale.y;
                o.pos_world.xyz = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(o.pos_world.xyz);
                o.uv = sampleCoords * _TerrainHeightmapRecipSize.zw;

                half3 normal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
                // half4 tangent = half4(cross(half3(0, 0, 1), normal), 1.0);
                o.normal_world = TransformObjectToWorldNormal(normal);

                o.uvSplat01.xy = o.uv * _Splat0_ST.xy + _Splat0_ST.zw;
                o.uvSplat01.zw = o.uv * _Splat1_ST.xy + _Splat1_ST.zw;
                o.uvSplat23.xy = o.uv * _Splat2_ST.xy + _Splat2_ST.zw;
                o.uvSplat23.zw = o.uv * _Splat3_ST.xy + _Splat3_ST.zw;

                return o;
            }

            struct FragOutput
            {
                half4 lightingBuffer : SV_TARGET0;
                half4 depthNormalBuffer : SV_TARGET1;
            };

            half4 frag (v2f i) : SV_TARGET0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                float2 splatUV = (i.uv * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
                half4 splatControl = _Control.Sample(sampler_Control, splatUV);

                half4 baseColor0 = _Splat0.Sample(sampler_Splat0, i.uvSplat01.xy);
                half4 baseColor1 = _Splat1.Sample(sampler_Splat0, i.uvSplat01.zw);
                half4 baseColor2 = _Splat2.Sample(sampler_Splat0, i.uvSplat23.xy);
                half4 baseColor3 = _Splat3.Sample(sampler_Splat0, i.uvSplat23.zw);

                half4 baseColor = baseColor0 * splatControl.r + baseColor1 * splatControl.g + baseColor2 * splatControl.b + baseColor3 * splatControl.a;

                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                half3 n = normalize(i.normal_world);
                half3 r = reflect(-v, n);
                half ndotv = max(0.001, dot(n, v));

                half metallic = 0.0;
                half oneMinusMetallic = 1.0 - metallic;
                half roughness = 1.0;
                half ao = 1.0;
                half3 albedo = baseColor.rgb * (1.0 - metallic);
                half3 specCol = lerp(0.04, baseColor.rgb, metallic * 0.5);
                float depthEye = LinearEyeDepth(i.vertex.z);

                float2 uv_screen = i.vertex.xy * (_ScreenParams.zw - 1.0);
                half3 indirectDiffuse = PBR_GetIndirectDiffuseFromProbe(i.pos_world.xyz, n);
                half3 indirectSpecular = PBR_GetIndirectSpecular(specCol, r, uv_screen, ndotv, roughness, oneMinusMetallic);
                half3 diffuseColor = 0.0;
                half3 specularColor = 0.0;

                PBR_BRDF_DirectionalLighting(specCol, i.pos_world.xyz, 0.0, n, v, i.vertex.xy, ndotv, roughness, depthEye, diffuseColor, specularColor);
                PBR_BRDF_PointLighting(specCol, i.pos_world.xyz, n, v, i.vertex.xy, ndotv, roughness, diffuseColor, specularColor);

                diffuseColor = diffuseColor * albedo;
                specularColor *= 0.25 / ndotv;
                half3 lighting = diffuseColor + specularColor + (indirectDiffuse * albedo + indirectSpecular) * ao;

                return half4(lighting, 1.0);
            }
            ENDHLSL
        }
    }
}
