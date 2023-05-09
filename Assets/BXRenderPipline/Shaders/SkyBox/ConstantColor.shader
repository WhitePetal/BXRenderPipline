Shader "BXSkyBox/ConstantColor"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off 
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Assets/BXRenderPipline/Shaders/Libiary/PBRLibiary.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 pos_world : TEXCOORD0;
                half3 normal_view : TEXCOORD1;
            };

            struct FragOutput
            {
                half4 lightingBuffer : SV_TARGET0;
                half4 depthNormalBuffer : SV_TARGET1;
            };

            half4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.normal_view = normalize(mul((float3x3)unity_MatrixV, -o.pos_world));
                o.vertex = TransformWorldToHClip(o.pos_world);
                return o;
            }

            FragOutput frag (v2f i)
            {
                FragOutput output;
                half3 v = normalize(_WorldSpaceCameraPos.xyz - i.pos_world.xyz);
                output.lightingBuffer = half4(_BaseColor.rgb, 1.0);
                output.depthNormalBuffer = float4(EncodeViewNormalStereo(i.normal_view), 0.0, 0.0);
                return output;
            }
            ENDHLSL
        }
    }
}
