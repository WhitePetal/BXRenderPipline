#ifndef CUSTOME_SHADOWS_LIBIARY
#define CUSTOME_SHADOWS_LIBIARY

#include "Assets/BXRenderPipline/Shaders/Libiary/Lights.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_DIRECTIONAL_SHADOW_COUNT 4
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowMap);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

CBUFFER_START(_CustomeShadows)
    half4 _BXShadowsColor;

    int _CascadeCount;
    float4x4 _DirectionalShadowMatrixs[MAX_DIRECTIONAL_SHADOW_COUNT * MAX_CASCADE_COUNT];
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeDatas[MAX_CASCADE_COUNT];
    float4 _ShadowsDistanceFade;
    float4 _ShadowMapSize;
CBUFFER_END

half SampleBakedShadows (float3 pos_world, float2 lightmapUV) 
{
    half4 mask;
	#if defined(LIGHTMAP_ON)
		mask = SAMPLE_TEXTURE2D(
			unity_ShadowMask, samplerunity_ShadowMask, lightmapUV
		);
	#else
        #if UNITY_LIGHT_PROBE_PROXY_VOLUME
            if (unity_ProbeVolumeParams.x) 
            {
                mask = SampleProbeOcclusion(
                    TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
                    pos_world, unity_ProbeVolumeWorldToObject,
                    unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
                    unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
                );
            }
            else 
            {
                mask = unity_ProbesOcclusion;
            }
        #else
            mask = unity_ProbesOcclusion;
        #endif
	#endif
    return min(mask.r, min(mask.g, min(mask.b, mask.a)));
}

half SampleDirectionalShadowMap(float3 pos_shadow)
{
    return SAMPLE_TEXTURE2D_SHADOW(
		_DirectionalShadowMap, SHADOW_SAMPLER, pos_shadow
	);
}

half FilterDirectionalShadow (float3 shadowCoord) {
	#if defined(DIRECTIONAL_FILTER_SETUP)
        // 在桌面端 weights 和 positions 需要为 float  OpenGLES3.x 则要求必须是 half
        real weights[DIRECTIONAL_FILTER_SAMPLES];
        real2 positions[DIRECTIONAL_FILTER_SAMPLES];
		float4 size = _ShadowMapSize.yyxx;
		DIRECTIONAL_FILTER_SETUP(size, shadowCoord.xy, weights, positions);
		half shadow = 0;
		for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) 
        {
			shadow += weights[i] * SampleDirectionalShadowMap(
				float3(positions[i].xy, shadowCoord.z)
			);
		}
        return shadow;
	#else
		return SampleDirectionalShadowMap(shadowCoord);
	#endif
}

half FadeShadowsStrength(float depth, float scale, float fade)
{
    return saturate((1.0 - depth * scale) * fade);
}

half GetShadowDistanceStrength(float depthView)
{
    return FadeShadowsStrength(depthView, _ShadowsDistanceFade.x, _ShadowsDistanceFade.y);
}

half GetDirectionalShadow(int lightIndex, float2 pos_clip, float3 pos_world, half3 normal_world, half shadowDistanceStrength)
{
    #ifdef _RECEIVE_SHADOWS_OFF
        return 1.0;
    #endif

    half4 shadowData = _DirectionalShadowDatas[lightIndex];
    half shadowStrength = shadowData.x * shadowDistanceStrength;
    if(shadowStrength <= 0.0) return 1.0;
    int cascadeIndex;
    half cascadeBlend = 1.0;
    for(cascadeIndex = 0; cascadeIndex < _CascadeCount; ++cascadeIndex)
    {
        float4 sphere = _CascadeCullingSpheres[cascadeIndex];
        float3 dir = pos_world - sphere.xyz;
        float dstSqr = dot(dir, dir);
        if(dstSqr < sphere.w)
        {
            half fade = FadeShadowsStrength(dstSqr, _CascadeDatas[cascadeIndex].x, _ShadowsDistanceFade.z);
            if(cascadeIndex == _CascadeCount - 1)
            {
                shadowStrength *= fade;
            }
            else
            {
                cascadeBlend = fade;
            }
            break;
        }
    }
    #if defined(_CASCADE_BLEND_DITHER)
        half dither = InterleavedGradientNoise(pos_clip.xy, 0);
    #endif

    #if defined(_CASCADE_BLEND_DITHER)
        if (cascadeBlend < dither) 
        {
            cascadeIndex += 1;
        }
    #endif
    int shadowIndex = shadowData.y + cascadeIndex;
    half3 normalBias = normal_world * _CascadeDatas[cascadeIndex].y * shadowData.z;
    float4 shadowCoord = mul(_DirectionalShadowMatrixs[shadowIndex], float4(pos_world + normalBias , 1.0));
    half shadow = FilterDirectionalShadow(shadowCoord.xyz);
    #if defined(_CASCADE_BLEND_SOFT)
        if (cascadeBlend < 1.0) {
            normalBias = normal_world * (shadowData.z * _CascadeDatas[cascadeIndex + 1].y);
            shadowCoord = mul(_DirectionalShadowMatrixs[shadowIndex + 1], float4(pos_world + normalBias, 1.0));
            shadow = lerp(FilterDirectionalShadow(shadowCoord.xyz), shadow, cascadeBlend);
        }
    #endif

    return lerp(1.0, shadow, shadowStrength);
}

#endif