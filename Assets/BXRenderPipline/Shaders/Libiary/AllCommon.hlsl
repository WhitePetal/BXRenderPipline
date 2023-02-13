#ifndef _CUSTOME_ALLCOMMON_LIBIARY
#define _CUSTOME_ALLCOMMON_LIBIARY

float DotClamped(float3 a, float3 b)
{
    return saturate(dot(a, b));
}

half DotClamped(half3 a, half3 b)
{
    return saturate(dot(a, b));
}

inline half RGB2Grayscale(half3 rgb)
{
    return 0.299*rgb.r + 0.587*rgb.g + 0.114*rgb.b;
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

inline float4 EncodeDepthNormal( float depth, float3 normal) // need 8bit
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

inline half2 SignNotZero(half2 vec)
{
    return half2(vec.x >= 0.0 ? 1.0 : -1.0, vec.y >= 0.0 ? 1.0 : -1.0);
}

inline half2 EncodeNormalOct(half3 normal_world) // encode to rg half
{
    half l = dot(abs(normal_world), 1.0);
    half2 p = normal_world.xy * (1.0 / l);
    return (normal_world.z <= 0.0) ? ((1.0 - abs(p.yx)) * SignNotZero(p)) : p;
}
inline half3 DecodeNormalOct(half2 data){
    half3 v = half3(data.xy, 1.0 - abs(data.x) - abs(data.y));
    v.xy = v.z < 0 ? (1.0 - abs(v.yx)) * SignNotZero(v.xy) : v.xy;
    return normalize(v);
}

inline half4 EncodeDepthNormalWorld(float depth01, float3 normal_world) // need half
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