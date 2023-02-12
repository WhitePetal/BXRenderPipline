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
    float4 _ProjectionParams;
    float4 _ZBufferParams;
    float4 _Time;
    half _GlobalBloomThreshold;

    float4x4 _ViewPortRays;
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

    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;

CBUFFER_END


#endif