#ifndef CUSTOME_LIGHTS_LIBIARY
#define CUSTOME_LIGHTS_LIBIARY

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_POINT_LIGHT_COUNT 256

CBUFFER_START(_CustomeLights)
    uint _DirectionalLightCount;
    half4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalShadowDatas[MAX_DIRECTIONAL_LIGHT_COUNT];
    uint _PointLightCount;
    half4 _PointLightColors[MAX_POINT_LIGHT_COUNT];
    float4 _PointLightSpheres[MAX_POINT_LIGHT_COUNT];
CBUFFER_END

StructuredBuffer<uint> _TileLightingIndices;
StructuredBuffer<uint> _TileLightingDatas;

#endif