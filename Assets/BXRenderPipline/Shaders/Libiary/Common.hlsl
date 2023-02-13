#ifndef CUSTOME_COMMON_LIBIARY
#define CUSTOME_COMMON_LIBIARY

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define pi 3.1415926535
#define pi_inv 0.31830989

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#define GET_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

// #define TRANSFORM_TEX(tex,name) (tex.xy * GET_PROP(name##_ST).xy + GET_PROP(name##_ST).zw)

#include "Assets/BXRenderPipline/Shaders/Libiary/AllCommon.hlsl"

void ClipLOD(float2 pos_clip, half fade)
{
    #if defined(LOD_FADE_CROSSFADE)
        half dither = InterleavedGradientNoise(pos_clip, 0.0);
        clip(fade + (fade < 0.0 ? dither : -dither));
    #endif
}

inline float Linear01Depth( float z )
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}
#endif