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
            };

            half4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(pos_world);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return half4(_BaseColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
