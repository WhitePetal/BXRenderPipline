#ifndef CUSTOME_UNITY_INPUT_INCLUDE
#define CUSTOME_UNITY_INPUT_INCLUDE


CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    float4x4 glstate_matrix_projection;
    float4x4 unity_WorldToCamera;
    float4x4 unity_CameraToWorld;
    float3 _WorldSpaceCameraPos;

    // x = 1 or -1 (-1 if projection is flipped)
    // y = near plane
    // z = far plane
    // w = 1/far plane
    float4 _ProjectionParams;

    // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1-far/near
    // y = far/near
    // z = x/far
    // w = y/far
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1+far/near
    // y = 1
    // z = x/far
    // w = 1/far
    float4 _ZBufferParams;
    
    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;

    float4 _Time;
    half _GlobalBloomThreshold;

    // Vector4 lu = forward - right + up;
    // Vector4 ru = forward + right + up;
    // Vector4 lb = forward - right - up;
    // Vector4 rb = forward + right - up;
    float4x4 _ViewPortRays;

    float4x4 unity_CameraProjection;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;

    float4 unity_LODFade;

    real4 unity_WorldTransformParams;

    real4 unity_LightData;
    real4 unity_LightIndices[2];

    float4 unity_ProbesOcclusion;

    float4 unity_SpecCube0_HDR;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    half4 unity_SHAr;
    half4 unity_SHAg;
    half4 unity_SHAb;
    half4 unity_SHBr;
    half4 unity_SHBg;
    half4 unity_SHBb;
    half4 unity_SHC;

    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;

CBUFFER_END


#endif