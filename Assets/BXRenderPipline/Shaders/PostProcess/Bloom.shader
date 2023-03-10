Shader "BXPostProcess/Bloom"
{
    SubShader
    {
        Pass
        {
            Name "Bloom Prefilter"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            Texture2D _LightingBuffer;
            float4 _LightingBuffer_TexelSize;
            SamplerState sampler_bilinear_clamp;
            float4 _BloomThreshold;

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.pos_clip = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                o.uv_screen = float2(
                    vertexID <= 1 ? 0.0 : 2.0,
                    vertexID == 1 ? 2.0 : 0.0
                );
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                return o;
            }

            half3 ApplyBloomThreshold(half3 col)
            {
                half brightness = max(col.r, max(col.g, col.b));
                half soft = brightness + _BloomThreshold.y;
                soft = clamp(soft, 0.0, _BloomThreshold.z);
                soft = soft * soft * _BloomThreshold.w;
                half contribution = max(soft, brightness - _BloomThreshold.x);
                contribution /= max(brightness, 0.001);
                col *= contribution;
                return col;
            }

            half4 frag(Varyings i) : SV_TARGET0
            {
                half3 color = 0.0;
                float weightSum = 0.0;
                float2 offsets[] = 
                {
                    float2(0.0, 0.0),
                    float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
                };
                [unroll(5)]
                for (int k = 0; k < 5; k++) 
                {
                    half3 c = _LightingBuffer.SampleLevel(sampler_bilinear_clamp, i.uv_screen + offsets[k] * _LightingBuffer_TexelSize.xy * 2.0, 0).rgb;
                    c = ApplyBloomThreshold(c);
                    float w = 1.0 / (Luminance(c) + 1.0);
                    color += c * w;
                    weightSum += w;
                }
                color /= weightSum;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Horizontal"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            Texture2D _BloomInput0;
            float4 _BloomInput0_TexelSize;
            SamplerState sampler_bilinear_clamp;
            // BXFRAMEBUFFER_INPUT_HALF(0, _LightingBuffer);

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.pos_clip = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                o.uv_screen = float2(
                    vertexID <= 1 ? 0.0 : 2.0,
                    vertexID == 1 ? 2.0 : 0.0
                );
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET0
            {
                half3 color = 0.0;
                float offsets[] = 
                {
                    -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
                };
                float weights[] =
                {
                    0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
                };
                UNITY_UNROLL
                for (int k = 0; k < 5; k++) 
                {
                    float offset = offsets[k] * 2.0 * _BloomInput0_TexelSize.x;
                    color += _BloomInput0.SampleLevel(sampler_bilinear_clamp, i.uv_screen + float2(offset, 0.0), 0).rgb * weights[k];
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Vertical"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            Texture2D _BloomInput0;
            float4 _BloomInput0_TexelSize;
            SamplerState sampler_bilinear_clamp;
            // BXFRAMEBUFFER_INPUT_HALF(0, _LightingBuffer);

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.pos_clip = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                o.uv_screen = float2(
                    vertexID <= 1 ? 0.0 : 2.0,
                    vertexID == 1 ? 2.0 : 0.0
                );
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET0
            {
                half3 color = 0.0;
                float offsets[] = 
                {
                    -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
                };
                float weights[] =
                {
                    0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
                };
                UNITY_UNROLL
                for (int k = 0; k < 5; k++) 
                {
                    float offset = offsets[k] * _BloomInput0_TexelSize.y;
                    color += _BloomInput0.SampleLevel(sampler_bilinear_clamp, i.uv_screen + float2(0.0, offset), 0).rgb * weights[k];
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Combine"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };

            Texture2D _BloomInput0;
            Texture2D _BloomInput1;
            SamplerState sampler_bilinear_clamp;
            SamplerState sampler_point_clamp;
            float4 _BloomInput0_TexelSize;
            half _BloomIntensity;
            // BXFRAMEBUFFER_INPUT_HALF(0, _LightingBuffer);

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.pos_clip = float4(
                    vertexID <= 1 ? -1.0 : 3.0,
                    vertexID == 1 ? 3.0 : -1.0,
                    0.0, 1.0
                );
                o.uv_screen = float2(
                    vertexID <= 1 ? 0.0 : 2.0,
                    vertexID == 1 ? 2.0 : 0.0
                );
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                return o;
            }

            half4 frag(Varyings i) : SV_TARGET0
            {
                half4 bloom = _BloomInput0.SampleLevel(sampler_bilinear_clamp, i.uv_screen + 0.5 * _BloomInput0_TexelSize.xy, 0);
                half4 lightingBuffer = _BloomInput1.SampleLevel(sampler_point_clamp, i.uv_screen, 0);
                return float4(bloom.rgb * _BloomIntensity + lightingBuffer.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
