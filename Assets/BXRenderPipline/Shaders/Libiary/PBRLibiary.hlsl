#ifndef CUSTOME_PBR_LIBIARY
#define CUSTOME_PBR_LIBIARY

#include "Assets/BXRenderPipline/Shaders/Libiary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

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
    half b = roughness / (ndoth_sqr * (rr + tan_ndoth_sqr));
    return pi_inv * b*b;
}

half PBR_G(half ndotl, half ndotv, half roughness)
{
    half rr = roughness*roughness;
    half ndotl_sqr = ndotl*ndotl;
    half ndotv_Sqr = ndotv*ndotv;

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


#endif