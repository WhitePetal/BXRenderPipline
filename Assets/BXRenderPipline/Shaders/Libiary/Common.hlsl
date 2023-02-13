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

float DotClamped(float3 a, float3 b)
{
    return saturate(dot(a, b));
}

half DotClamped(half3 a, half3 b)
{
    return saturate(dot(a, b));
}

void ClipLOD(float2 pos_clip, half fade)
{
    #if defined(LOD_FADE_CROSSFADE)
        half dither = InterleavedGradientNoise(pos_clip, 0.0);
        clip(fade + (fade < 0.0 ? dither : -dither));
    #endif
}

inline half RGB2Grayscale(half3 rgb)
{
    return 0.299*rgb.r + 0.587*rgb.g + 0.114*rgb.b;
}

inline float Linear01Depth( float z )
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}

// Encoding/decoding [0..1) floats into 8 bit/channel RG. Note that 1.0 will not be encoded properly.
inline float2 EncodeFloatRG( float v )
{
    float2 kEncodeMul = float2(1.0, 255.0);
    float kEncodeBit = 1.0/255.0;
    float2 enc = kEncodeMul * v;
    enc = frac (enc);
    enc.x -= enc.y * kEncodeBit;
    return enc;
}
inline float DecodeFloatRG( float2 enc )
{
    float2 kDecodeDot = float2(1.0, 1/255.0);
    return dot( enc, kDecodeDot );
}

// Encoding/decoding view space normals into 2D 0..1 vector
inline float2 EncodeViewNormalStereo( float3 n )
{
    float kScale = 1.7777;
    float2 enc;
    enc = n.xy / (n.z+1);
    enc /= kScale;
    enc = enc*0.5+0.5;
    return enc;
}
inline float3 DecodeViewNormalStereo( float4 enc4 )
{
    float kScale = 1.7777;
    float3 nn = enc4.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    float3 n;
    n.xy = g*nn.xy;
    n.z = g-1;
    return n;
}

inline float4 EncodeDepthNormal( float depth, float3 normal )
{
    float4 enc;
    enc.xy = EncodeViewNormalStereo (normal);
    enc.zw = EncodeFloatRG(depth);
    return enc;
}
inline void DecodeDepthNormal( float4 enc, out float depth, out float3 normal )
{
    depth = DecodeFloatRG(enc.zw);
    normal = DecodeViewNormalStereo(enc);
}

inline half2 SignNotZero(half2 xy){
    return xy >= 0 ? 1:-1;
}

inline half2 EncodeNormalOct(half3 normal_world){
    half l = dot(abs(normal_world),1);
    half3 normalOct = normal_world * rcp(l); 
    return normal_world.z > 0 ? normalOct.xy : (1-abs(normalOct.yx)*SignNotZero(normalOct.xy));
}
inline half3 DecodeNormalOct(half2 data){
    half3 v = half3(data.xy,1 - abs(data.x) - abs(data.y));
    v.xy = v.z <= 0 ? (SignNotZero(v.xy) * (1 - abs(v.yx))) : v.xy;
    return normalize(v);
}

inline half4 EncodeDepthNormalWorld(float depth01, float3 normal_world)
{
    half4 enc;
    enc.xy = EncodeNormalOct(normal_world);
    enc.zw = EncodeFloatRG(depth01);
    return enc;
}
inline void DecodeDepthNormalWorld(half4 data, out float depth, out half3 normal)
{
    depth = DecodeFloatRG(data.zw);
    normal = DecodeNormalOct(data.xy);
}

#endif