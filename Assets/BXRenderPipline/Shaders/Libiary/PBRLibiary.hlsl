#ifndef CUSTOME_PBR_LIBIARY
#define CUSTOME_PBR_LIBIARY

#include "Assets/BXRenderPipline/Shaders/Libiary/Shadows.hlsl"

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
    half xx = x*x;
    return xx*xx*x;
}
half PBR_SchlickFresnelFunction(half ldoth)
{
    return 0.04 + (1.0 - 0.04) * PBR_SchlickFresnel(ldoth);
}
half PBR_F0(half ndotl, half ndotv, half ldoth, half roughness)
{
    half fl = PBR_SchlickFresnel(ndotl);
    half fv = PBR_SchlickFresnel(ndotv);
    half fDiffuse90 = 0.5 + 2.0 * ldoth * ldoth * roughness;
    return lerp(1.0, fDiffuse90, fl) * lerp(1.0, fDiffuse90, fv);
}


#endif