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

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 normal_view : TEXCOORD0;
            };

            half4 _BaseColor;

            struct FragOutput
            {
                half4 lightingBuffer : SV_TARGET0;
                half4 depthNormalBuffer : SV_TARGET1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.normal_view = normalize(mul((float3x3)unity_MatrixV, -pos_world));
                o.vertex = TransformWorldToHClip(pos_world);
                return o;
            }

            FragOutput frag (v2f i)
            {
                FragOutput output;
                output.lightingBuffer = half4(_BaseColor.rgb, 1.0);
                output.depthNormalBuffer = float4(EncodeViewNormalStereo(i.normal_view), 0.0, 0.0);
                return output;
            }
            ENDHLSL
        }
    }
}
