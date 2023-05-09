#ifndef CUSTOME_PBR_LIBIARY
#define CUSTOME_PBR_LIBIARY

#include "Assets/BXRenderPipline/Shaders/Libiary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#ifdef LIGHTMAP_ON
    TEXTURE2D(unity_Lightmap);
    SAMPLER(samplerunity_Lightmap);

    half3 SampleLightmap(float2 lightmapUV)
    {
        return SampleSingleLightmap(
            TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightmapUV,
            float4(1.0, 1.0, 0.0, 0.0),
            #if defined(UNITY_LIGHTMAP_FULL_HDR)
                false,
            #else
                true,
            #endif
            float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
        );
    }
#endif
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

#ifndef _REFLECT_PROBE_ONLY
    Texture2D _SSRBuffer;
    SamplerState sampler_point_clamp;
#endif

#if defined(_REFLECT_PROBE_ONLY) || defined(_SSR_AND_RELFECT_PROBE)
    TEXTURECUBE(unity_SpecCube0);
    SAMPLER(samplerunity_SpecCube0);
#endif

half3 GetWorldNormalFromNormalMap(half4 normalMap, half normalScale, half3 tangent, half3 birnormal, half3 normal)
{
    half3 nor_tan = UnpackNormalScale(normalMap, normalScale);
    return normalize(half3(
        dot(half3(tangent.x, birnormal.x, normal.x), nor_tan), 
        dot(half3(tangent.y, birnormal.y, normal.y), nor_tan),
        dot(half3(tangent.z, birnormal.z, normal.z), nor_tan)
        ));
}

half PBR_D(half roughness, half ndoth)
{
    half rr = roughness * roughness;
    half ndoth_sqr = max(0.001, ndoth * ndoth);
    half tan_ndoth_sqr = (1.0 - ndoth_sqr) / ndoth_sqr;
    half b = roughness / max(0.01, ndoth_sqr * (rr + tan_ndoth_sqr));
    return pi_inv * b*b;
}

half PBR_G(half ndotl, half ndotv, half roughness)
{
    half rr = roughness*roughness;
    half ndotl_sqr = max(0.001, ndotl*ndotl);
    half ndotv_Sqr = max(0.001, ndotv*ndotv);

    half smithL = 2.0 * ndotl / (ndotl + sqrt(rr + (1.0 - rr) * ndotl_sqr));
    half smithV = 2.0 * ndotv / (ndotv + sqrt(rr + (1.0 - rr) * ndotv_Sqr));

    return smithL * smithV;
}

half PBR_SchlickFresnel(half x)
{
    half i = 1.0 - x;
    half ii = i*i;
    return ii*ii*i;
}
half3 PBR_SchlickFresnel(half3 x)
{
    half3 i = 1.0 - x;
    half3 ii = i*i;
    return ii*ii*i;
}
half PBR_SchlickFresnelFunction(half ldoth)
{
    return 0.04 + (1.0 - 0.04) * PBR_SchlickFresnel(ldoth);
}
half3 PBR_SchlickFresnelFunction(half3 SpecularColor, half LdotH){
    return SpecularColor + (1 - SpecularColor)* PBR_SchlickFresnel(LdotH);
}
half PBR_F0(half ndotl, half ndotv, half ldoth, half roughness)
{
    half fl = PBR_SchlickFresnel(ndotl);
    half fv = PBR_SchlickFresnel(ndotv);
    half fDiffuse90 = 0.5 + 2.0 * ldoth * ldoth * roughness;
    return lerp(1.0, fDiffuse90, fl) * lerp(1.0, fDiffuse90, fv);
}
half3 PBR_F0_SSS(half3 ndotl_sss, half ndotv, half ldoth, half roughness)
{
    half3 fl = PBR_SchlickFresnel(ndotl_sss);
    half fv = PBR_SchlickFresnel(ndotv);
    half fDiffuse90 = 0.5 + 2.0 * ldoth * ldoth * roughness;
    return lerp(1.0, fDiffuse90, fl) * lerp(1.0, fDiffuse90, fv);
}

half3 PBR_GetIndirectDiffuseSH(half3 n)
{
    half4 coefficients[7];
    coefficients[0] = unity_SHAr;
    coefficients[1] = unity_SHAg;
    coefficients[2] = unity_SHAb;
    coefficients[3] = unity_SHBr;
    coefficients[4] = unity_SHBg;
    coefficients[5] = unity_SHBb;
    coefficients[6] = unity_SHC;
    return max(0.0, SampleSH9(coefficients, n));
}
half3 PBR_GetIndirectDiffuseLLPV(float3 pos_world, half3 n)
{
    if (unity_ProbeVolumeParams.x) 
    {
        return SampleProbeVolumeSH4(
            TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
            pos_world, n,
            unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
            unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
        );
    }
    else
    {
        return PBR_GetIndirectDiffuseSH(n);
    }
}
half3 PBR_GetIndirectDiffuseFromProbe(float3 pos_world, half3 n)
{
    #if UNITY_LIGHT_PROBE_PROXY_VOLUME
        return PBR_GetIndirectDiffuseLLPV(pos_world, n);
    #else
        return PBR_GetIndirectDiffuseSH(n);
    #endif
}

half3 PBR_GetIndirectSpecular(half3 specCol, half3 r, float2 uv_screen, half ndotv, half roughness, half oneMinusMetallic)
{
    half3 indirectSpecular;
    #ifdef _SSR_ONLY
        indirectSpecular = _SSRBuffer.SampleLevel(sampler_point_clamp, uv_screen, roughness * 3).rgb;
    #elif defined(_REFLECT_PROBE_ONLY)
        half3 environment = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, r, roughness * 6), unity_SpecCube0_HDR);
        indirectSpecular = environment;
    #elif defined(_SSR_AND_RELFECT_PROBE)
        half4 ssrData = _SSRBuffer.SampleLevel(sampler_point_clamp, uv_screen, roughness * 3);
        half3 environment = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, r, roughness * 6), unity_SpecCube0_HDR);
        indirectSpecular = ssrData.rgb + environment;
    #else
        return 0.0;
    #endif
    return indirectSpecular * lerp(specCol,saturate(2.0 - roughness - oneMinusMetallic), PBR_SchlickFresnel(ndotv)) / (1.0 + roughness * roughness);
}

half3 ApplyFog(half3 col, half3 v, float depthEye)
{
    half fog = min(1.0, _FogInnerParams.x * exp(-_FogInnerParams.y * (depthEye - _FogInnerParams.z) / 1000));
    half3 l = _DirectionalLightDirections[0].xyz;
    half vdotl = max(0, dot(v, -l));
    half3 lightCol = _DirectionalLightColors[0].rgb;

    return lerp(_FogColor.rgb, col.rgb, fog) + lightCol * _FogOuterParams.x * (1.0 + vdotl * vdotl) * exp(-_FogOuterParams.y * exp(0.5 * v.y));
}

#if HALF_LAMBERT_LIGHTING
void HALF_LAMBERT_DirectionalLighting(float3 pos_world, half3 n, float2 pos_clip, float depthEye, inout half3 diffuseColor)
{
    half3 shadowCol = 1.0;
    #ifndef _SHADOW_MASK_ALWAYS
        half shadowDistanceStrength = GetShadowDistanceStrength(depthEye);
    #endif
    [loop]
    for(uint lightIndex = 0; lightIndex < _DirectionalLightCount; ++lightIndex)
    {
        half3 l = _DirectionalLightDirections[lightIndex].xyz;
        half3 lightColor = _DirectionalLightColors[lightIndex].xyz;
        half ndotl = saturate(dot(n, l) + 0.5);
        #ifndef _SHADOW_MASK_ALWAYS
            half shadowAtten = GetDirectionalShadow(lightIndex, pos_clip, pos_world, n, shadowDistanceStrength);
            shadowCol = lerp(_BXShadowsColor.xyz, 1.0, shadowAtten);
        #endif
        lightColor *= shadowCol;

        diffuseColor += lightColor * ndotl;
    }
}

void HALF_LAMBERT_PointLighting(float3 pos_world, half3 n, float2 pos_clip, float depthEye, inout half3 diffuseColor)
{
    uint2 tileXY = pos_clip / _ClusterSize.xy;
    uint k = log(depthEye / _ProjectionParams.y) * _ClusterSize.w;
    uint tileIndex = k * 32 * 16 + (tileXY.y * 32 + tileXY.x);
    uint tileData = _TileLightingDatas[tileIndex];
    uint tileLightIndexStart = tileIndex * 256;

    [loop]
    for(uint tileLightOffset = 0; tileLightOffset < tileData; ++tileLightOffset)
    {
        uint tileLightIndex = tileLightIndexStart + tileLightOffset;
        uint pointLightIndex = _TileLightingIndices[tileLightIndex];
        float4 lightSphere = _PointLightSpheres[pointLightIndex];
        half3 lightColor = _PointLightColors[pointLightIndex].xyz;

        float3 lenV = lightSphere.xyz - pos_world.xyz;
        float lenSqr = dot(lenV, lenV);
        float3 l = SafeNormalize(lenV);

        half ndotl = saturate(dot(n, l) + 0.5);
        half atten =  saturate(1.0 - lenSqr / (lightSphere.w * lightSphere.w));
        lightColor *= atten;
        diffuseColor += lightColor * ndotl;
    }
}
#endif

#if BRDF_LIGHTING
void PBR_BRDF_DirectionalLighting(half3 specCol, float3 pos_world, float2 lightmapUV, half3 n, half3 v, half ndotv, float2 pos_clip, half roughness, float depthEye, inout half3 diffuseColor, inout half3 specularColor)
{
    #ifndef _SHADOW_MASK_ALWAYS
        half shadowDistanceStrength = GetShadowDistanceStrength(depthEye);
    #endif
    #if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
        half baked = SampleBakedShadows(pos_world, lightmapUV);
	#endif
    #if defined(_SHADOW_MASK_ALWAYS)
        half3 shadowCol = lerp(_BXShadowsColor.xyz, 1.0, baked);
    #endif
    [loop]
    for(uint lightIndex = 0; lightIndex < _DirectionalLightCount; ++lightIndex)
    {
        half3 l = _DirectionalLightDirections[lightIndex].xyz;
        half3 lightColor = _DirectionalLightColors[lightIndex].xyz;
        half3 h = SafeNormalize(l + v);
        half ndotl = max(0.001, dot(n, l));
        half ndoth = max(0.001, dot(n, h));
        half ldoth = max(0.001, dot(l, h));
        half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
        half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);
        #ifndef _SHADOW_MASK_ALWAYS
            half shadowAtten = GetDirectionalShadow(lightIndex, pos_clip, pos_world, n, shadowDistanceStrength);
            #if defined(_SHADOW_MASK_DISTANCE)
                shadowAtten = lerp(baked, shadowAtten, shadowDistanceStrength);
            #endif
            half3 shadowCol = lerp(_BXShadowsColor.xyz, 1.0, shadowAtten);
        #endif
        lightColor *= shadowCol;

        diffuseColor += lightColor * f0 * ndotl;
        specularColor += lightColor * fgd;
    }
}

void PBR_BRDF_PointLighting(half3 specCol, float3 pos_world, half3 n, half3 v, half ndotv, float2 pos_clip, half roughness, float depthEye, inout half3 diffuseColor, inout half3 specularColor)
{
    uint2 tileXY = pos_clip / _ClusterSize.xy;
    uint k = log(depthEye / _ProjectionParams.y) * _ClusterSize.w;
    uint tileIndex = k * 32 * 16 + (tileXY.y * 32 + tileXY.x);
    uint tileData = _TileLightingDatas[tileIndex];
    uint tileLightIndexStart = tileIndex * 256;

    [loop]
    for(uint tileLightOffset = 0; tileLightOffset < tileData; ++tileLightOffset)
    {
        uint tileLightIndex = tileLightIndexStart + tileLightOffset;
        uint pointLightIndex = _TileLightingIndices[tileLightIndex];
        float4 lightSphere = _PointLightSpheres[pointLightIndex];
        half3 lightColor = _PointLightColors[pointLightIndex].xyz;

        float3 lenV = lightSphere.xyz - pos_world.xyz;
        float lenSqr = dot(lenV, lenV);
        float3 l = SafeNormalize(lenV);
        float3 h = SafeNormalize(l + v);
        
        half ndotl = max(0.0, dot(n, l));
        half ndoth = max(0.0, dot(n, h));
        half ldoth = max(0.0, dot(l, h));
        half atten =  saturate(1.0 - lenSqr / (lightSphere.w * lightSphere.w));

        lightColor *= atten;
        half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
        half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);

        diffuseColor += lightColor * f0 * ndotl;
        specularColor += lightColor * fgd;
    }
}
#endif

#if BSSSDFSKIN_LIGHTING
void PBR_BSSSDFSkin_DirectionalLighting(half3 specCol, float3 pos_world, half3 n, half3 v, float2 pos_clip, half ndotv, half r, half roughness, float depthEye, inout half3 diffuseColor, inout half3 specularColor)
{
    half shadowDistanceStrength = GetShadowDistanceStrength(depthEye);
    #if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
        half baked = SampleBakedShadows(pos_world, lightmapUV);
	#endif
    #if defined(_SHADOW_MASK_ALWAYS)
        half3 shadowCol = lerp(_BXShadowsColor.xyz, 1.0, baked);
    #endif
    [loop]
    for(uint lightIndex = 0; lightIndex < _DirectionalLightCount; ++lightIndex)
    {
        half3 l = _DirectionalLightDirections[lightIndex].xyz;
        half3 lightColor = _DirectionalLightColors[lightIndex].xyz;
        half3 h = SafeNormalize(l + v);
        half ndotl_source = dot(n, l);
        half ndotl = max(0.001, ndotl_source);
        half3 ndotl_sss = _LUTSSS.Sample(sampler_bilinear_clamp, float2(ndotl_source * 0.5 + 0.5, r)).rgb;
        half ndoth = max(0.001, dot(n, h));
        half ldoth = max(0.001, dot(l, h));
        half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
        half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);
        #ifndef _SHADOW_MASK_ALWAYS
            half shadowAtten = GetDirectionalShadow(lightIndex, pos_clip, pos_world, n, shadowDistanceStrength);
            #if defined(_SHADOW_MASK_DISTANCE)
                shadowAtten = lerp(baked, shadowAtten, shadowDistanceStrength);
            #endif
            half3 shadowCol = lerp(_BXShadowsColor.xyz, 1.0, shadowAtten);
        #endif
        lightColor *= shadowCol;

        diffuseColor += lightColor * f0 * ndotl_sss;
        specularColor += lightColor * fgd;
    }
}

void PBR_BSSSDFSkin_PointLighting(half3 specCol, half3 ndotl_sss_avg, float3 pos_world, half3 n, half3 v, float2 pos_clip, half ndotv, half roughness, float depthEye, inout half3 diffuseColor, inout half3 specularColor)
{
    uint2 tileXY = pos_clip / _ClusterSize.xy;
    uint k = log(depthEye / _ProjectionParams.y) * _ClusterSize.w;
    uint tileIndex = k * 32 * 16 + (tileXY.y * 32 + tileXY.x);
    uint tileData = _TileLightingDatas[tileIndex];
    uint tileLightIndexStart = tileIndex * 256;

    [loop]
    for(uint tileLightOffset = 0; tileLightOffset < min(tileData, _PointLightCount); ++tileLightOffset)
    {
        uint tileLightIndex = tileLightIndexStart + tileLightOffset;
        uint pointLightIndex = _TileLightingIndices[tileLightIndex];
        float4 lightSphere = _PointLightSpheres[pointLightIndex];
        half3 lightColor = _PointLightColors[pointLightIndex].xyz;

        float3 lenV = lightSphere.xyz - pos_world.xyz;
        float lenSqr = dot(lenV, lenV);
        float3 l = SafeNormalize(lenV);
        float3 h = SafeNormalize(l + v);

        half ndotl = max(0.001, dot(n, l));
        half ndoth = max(0.001, dot(n, h));
        half ldoth = max(0.001, dot(l, h));
        half atten =  saturate(1.0 - lenSqr / (lightSphere.w * lightSphere.w));

        lightColor *= atten;
        half f0 = PBR_F0(ndotl, ndotv, ldoth, roughness);
        half3 fgd = PBR_SchlickFresnelFunction(specCol, ldoth) * PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth);

        diffuseColor += lightColor * f0 * ndotl_sss_avg * ndotl;
        specularColor += lightColor * fgd;
    }
}
#endif

#endif