Shader "BXPostProcess/FXAA"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma multi_compile_local FXAA_QUALITY_LOW FXAA_QUALITY_MEDIUM FXAA_QUALITY_HIGH

            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/BXRenderPipline/Shaders/Libiary/Common.hlsl"

            struct Varyings
            {
                float4 pos_clip : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
            };


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
                if(_ProjectionParams.x > 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                return o;
            }

            Texture2D _FXAAInputBuffer;
            float4 _FXAAInputBuffer_TexelSize;
            SamplerState sampler_bilinear_clamp;

            float4 _FXAAConfig;

            #if defined(FXAA_QUALITY_LOW)
                #define EXTRA_EDGE_STEPS 3
                #define EDGE_STEP_SIZES 1.5, 2.0, 2.0
                #define LAST_EDGE_STEP_GUESS 8.0
            #elif defined(FXAA_QUALITY_MEDIUM)
                #define EXTRA_EDGE_STEPS 8
                #define EDGE_STEP_SIZES 1.5, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 4.0
                #define LAST_EDGE_STEP_GUESS 8.0
            #else
            	#define EXTRA_EDGE_STEPS 10
                #define EDGE_STEP_SIZES 1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0
                #define LAST_EDGE_STEP_GUESS 8.0
            #endif

            static const float edgeStepSizes[EXTRA_EDGE_STEPS] = { EDGE_STEP_SIZES };

            struct LumaNeighborhood
            {
                half m, n, e, s, w, ne, se, sw, nw;
                half highest, lowest, range;
            };

            struct FXAAEdge 
            {
                bool isHorizontal;
                float pixelStep;
                float lumaGradient, otherLuma;
            };

            half GetLuma(float2 uv, float uoffset, float voffset)
            {
                uv += float2(uoffset, voffset) * _FXAAInputBuffer_TexelSize;
                return _FXAAInputBuffer.SampleLevel(sampler_bilinear_clamp, uv, 0).g;
            }

            LumaNeighborhood GetLumaNeighborhood(half4 originCol, float2 uv)
            {
                LumaNeighborhood luma;
                luma.m = originCol.g;
                luma.n = GetLuma(uv, 0, 1);
                luma.e = GetLuma(uv, 1, 0);
                luma.s = GetLuma(uv, 0, -1);
                luma.w = GetLuma(uv, -1, 0);
                luma.ne = GetLuma(uv, 1.0, 1.0);
                luma.se = GetLuma(uv, 1.0, -1.0);
                luma.sw = GetLuma(uv, -1.0, -1.0);
                luma.nw = GetLuma(uv, -1.0, 1.0);

                luma.highest = max(luma.m, max(luma.n, max(luma.e, max(luma.s, luma.w))));
                luma.lowest = min(luma.m, min(luma.n, min(luma.e, min(luma.s, luma.w))));
                luma.range = luma.highest - luma.lowest;
                return luma;
            }

            bool CanSkipFXAA(LumaNeighborhood luma)
            {
                return luma.range < max(_FXAAConfig.x, _FXAAConfig.y * luma.highest);
            }

            half GetSubpixelBlendFactor (LumaNeighborhood luma) 
            {
                half filter = 2.0 * (luma.n + luma.e + luma.s + luma.w);
                filter += luma.ne + luma.se + luma.sw + luma.nw;
                filter *= 0.083333;
                filter = abs(filter - luma.m);
                filter = saturate(filter / luma.range);
                filter = smoothstep(0, 1, filter);
                return filter * filter;
            }

            bool IsHorizontalEdge (LumaNeighborhood luma) 
            {
                float horizontal = abs(luma.n + luma.s - 2.0 * luma.m);
                float vertical = abs(luma.e + luma.w - 2.0 * luma.m);
                return horizontal >= vertical;
            }

            FXAAEdge GetFXAAEdge (LumaNeighborhood luma) 
            {
                FXAAEdge edge;
                edge.isHorizontal = IsHorizontalEdge(luma);
                float lumaP, lumaN;
                if (edge.isHorizontal) 
                {
                    edge.pixelStep = _FXAAInputBuffer_TexelSize.y;
                    lumaP = luma.n;
                    lumaN = luma.s;
                }
                else 
                {
                    edge.pixelStep = _FXAAInputBuffer_TexelSize.x;
                    lumaP = luma.e;
                    lumaN = luma.w;
                }
                float gradientP = abs(lumaP - luma.m);
	            float gradientN = abs(lumaN - luma.m);
                if (gradientP < gradientN) 
                {
                    edge.pixelStep = -edge.pixelStep;
                    edge.lumaGradient = gradientN;
		            edge.otherLuma = lumaN;
                }
                else 
                {
                    edge.lumaGradient = gradientP;
                    edge.otherLuma = lumaP;
                }
                return edge;
            }

            float GetEdgeBlendFactor (LumaNeighborhood luma, FXAAEdge edge, float2 uv) 
            {
                float2 edgeUV = uv;
                float2 uvStep = 0.0;
                if (edge.isHorizontal) 
                {
                    edgeUV.y += 0.5 * edge.pixelStep;
                    uvStep.x = _FXAAInputBuffer_TexelSize.x;
                }
                else 
                {
                    edgeUV.x += 0.5 * edge.pixelStep;
                    uvStep.y = _FXAAInputBuffer_TexelSize.y;
                }

                float edgeLuma = 0.5 * (luma.m + edge.otherLuma);
                float gradientThreshold = 0.25 * edge.lumaGradient;
                        
                float2 uvP = edgeUV + uvStep;
                float lumaDeltaP = GetLuma(uvP, 0, 0) - edgeLuma;
                bool atEndP = lumaDeltaP >= gradientThreshold;
                UNITY_UNROLL
                for (int i = 0; i < EXTRA_EDGE_STEPS && !atEndP; i++) 
                {
                    uvP += uvStep * edgeStepSizes[i];
                    lumaDeltaP = GetLuma(uvP, 0, 0) - edgeLuma;
                    atEndP = abs(lumaDeltaP) >= gradientThreshold;
                }
                if (!atEndP) 
                {
		            uvP += uvStep * LAST_EDGE_STEP_GUESS;
                }

                float2 uvN = edgeUV - uvStep;
                float lumaDeltaN = GetLuma(uvN, 0, 0) - edgeLuma;
                bool atEndN = lumaDeltaN >= gradientThreshold;
                UNITY_UNROLL
                for (int i = 0; i < EXTRA_EDGE_STEPS && !atEndN; i++) 
                {
                    uvN -= uvStep * edgeStepSizes[i];
                    lumaDeltaN = GetLuma(uvN, 0, 0) - edgeLuma;
                    atEndN = abs(lumaDeltaN) >= gradientThreshold;
                }
                if (!atEndN) 
                {
		            uvN += uvStep * LAST_EDGE_STEP_GUESS;
	            }

                float distanceToEndP, distanceToEndN;
                if (edge.isHorizontal) 
                {
                    distanceToEndP = uvP.x - uv.x;
                    distanceToEndN = uv.x - uvN.x;
                }
                else 
                {
                    distanceToEndP = uvP.y - uv.y;
                    distanceToEndN = uv.y - uvN.y;
                }

                float distanceToNearestEnd;
                bool deltaSign;
                if (distanceToEndP <= distanceToEndN) 
                {
                    distanceToNearestEnd = distanceToEndP;
                    deltaSign = lumaDeltaP >= 0;
                }
                else 
                {
                    distanceToNearestEnd = distanceToEndN;
                    deltaSign = lumaDeltaN >= 0;
                }

                if (deltaSign == (luma.m - edgeLuma >= 0)) 
                {
                    return 0.0;
                }
                else 
                {
                    return 0.5 - distanceToNearestEnd / (distanceToEndP + distanceToEndN);
                }
            }

            half4 frag(Varyings i) : SV_TARGET0
            {
                half4 originCol = _FXAAInputBuffer.SampleLevel(sampler_bilinear_clamp, i.uv_screen, 0);
                LumaNeighborhood luma = GetLumaNeighborhood(originCol, i.uv_screen);
                if(CanSkipFXAA(luma)) return originCol;
  
                FXAAEdge edge = GetFXAAEdge(luma);
                float blendFactor = max(GetSubpixelBlendFactor(luma), GetEdgeBlendFactor (luma, edge, i.uv_screen)) * _FXAAConfig.z;
                float2 blendUV = i.uv_screen;
                if (edge.isHorizontal) 
                {
                    blendUV.y += blendFactor * edge.pixelStep;
                }
                else 
                {
                    blendUV.x += blendFactor * edge.pixelStep;
                }

                return _FXAAInputBuffer.SampleLevel(sampler_bilinear_clamp, blendUV, 0);
            }
            ENDHLSL
        }
    }
}
