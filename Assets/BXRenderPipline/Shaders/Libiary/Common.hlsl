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

#if defined(PLATFORM_SUPPORTS_NATIVE_RENDERPASS)
    #define BXFRAMEBUFFER_INPUT_FLOAT(idx, texName) FRAMEBUFFER_INPUT_FLOAT(idx)
    #define BXFRAMEBUFFER_INPUT_HALF(idx, texName) FRAMEBUFFER_INPUT_HALF(idx)
    #define BXFRAMEBUFFER_INPUT_INT(idx, texName) FRAMEBUFFER_INPUT_INT(idx)
    #define BXFRAMEBUFFER_INPUT_UINT(idx, texName) FRAMEBUFFER_INPUT_UINT(idx)

    #define BXLOAD_FRAMEBUFFER_INPUT(idx, texName, uv) LOAD_FRAMEBUFFER_INPUT(idx, uv)
#else
    #if defined(SHADER_API_METAL)
        #define BXFRAMEBUFFER_INPUT_FLOAT(idx, texName) TEXTURE2D_FLOAT(texName); SAMPLER(sampler_##texName)
        #define BXFRAMEBUFFER_INPUT_HALF(idx, texName) TEXTURE2D_HALF(texName); SAMPLER(sampler_##texName)
        #define BXFRAMEBUFFER_INPUT_INT(idx, texName) TEXTURE2D_INT(texName); SAMPLER(sampler_##texName)
        #define BXFRAMEBUFFER_INPUT_UINT(idx, texName) TEXTURE2D_UINT(texName); SAMPLER(sampler_##texName)

        #define BXLOAD_FRAMEBUFFER_INPUT(idx, texName, uv) texName.Sample(sampler_##texName, float2(uv.x, 1.0 - uv.y))
    #else
        #define BXFRAMEBUFFER_INPUT_FLOAT(idx, texName) TEXTURE2D_FLOAT(_UnityFBInput##idx); SAMPLER(sampler_UnityFBInput##idx)
        #define BXFRAMEBUFFER_INPUT_HALF(idx, texName) TEXTURE2D_HALF(_UnityFBInput##idx); SAMPLER(sampler_UnityFBInput##idx)
        #define BXFRAMEBUFFER_INPUT_INT(idx, texName) TEXTURE2D_INT(_UnityFBInput##idx); SAMPLER(sampler_UnityFBInput##idx)
        #define BXFRAMEBUFFER_INPUT_UINT(idx, texName) TEXTURE2D_UINT(_UnityFBInput##idx); SAMPLER(sampler_UnityFBInput##idx)

        #define BXLOAD_FRAMEBUFFER_INPUT(idx, texName, uv) _UnityFBInput##idx.Sample(sampler_UnityFBInput##idx, float2(uv.x, 1.0 - uv.y))
    #endif
#endif

#include "Assets/BXRenderPipline/Shaders/Libiary/AllCommon.hlsl"

void ClipLOD(float2 pos_clip, half fade)
{
    #if defined(LOD_FADE_CROSSFADE)
        half dither = InterleavedGradientNoise(pos_clip, 0.0);
        clip(fade + (fade < 0.0 ? dither : -dither));
    #endif
}

float4 GetScreenUV(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

inline float Linear01Depth( float z )
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}

inline float LinearEyeDepth( float z )
{
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}
#endif