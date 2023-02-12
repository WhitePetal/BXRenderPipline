#ifndef CUSTOME_LIGHTS_LIBIARY
#define CUSTOME_LIGHTS_LIBIARY

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomeLights)
    int _DirectionalLightCount;
    half4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalShadowDatas[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

#endif