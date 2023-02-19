#ifndef _CUSTOME_COLOR_MANAGER
#define _CUSTOME_COLOR_MANAGER

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// x: exposure
// y: constrast
// z: hue
// w: saturation
half4 _ColorAdjustments;
half4 _ColorFilter;
half4 _ColorWhiteBalance;
// rgb: shadowsCol 
// a: balance
half4 _ColorSplitToningShadows;
half4 _ColorSplitToningHighlights;
float4x4 _ColorChannelMixer;
half4 _SMHShadows, _SMHMidtones, _SMHHighlights, _SMHRange;

half LuminaceCM(half3 col)
{
    #ifdef CM_ACES
        return AcesLuminance(col);
    #else
        return Luminance(col);
    #endif
}

half3 ColorGradePostExposure(half3 col)
{
    return col * _ColorAdjustments.x;
}

half3 ColorGradingContrast(half3 col)
{
    #ifdef CM_ACES
        col = ACES_to_ACEScc(unity_to_ACES(col));
    #else
        col = LinearToLogC(col);
    #endif
    col = (col - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
    #ifdef CM_ACES
        return max(0.0, ACES_to_ACEScg(ACEScc_to_ACES(col)));
    #else
        return max(LogCToLinear(col), 0.0);
    #endif
}

half3 ColorGradeColorFilter(half3 col)
{
    return col * _ColorFilter.rgb;
}

half3 ColorGradingSaturation(half3 col)
{
    half luminance = LuminaceCM(col);
    return max(0.0, (col - luminance) * _ColorAdjustments.w + luminance);
}

half3 ColorGradeingChannelMixer(half3 col)
{
    return max(0.0, mul((float3x3)_ColorChannelMixer, col));
}

half3 ColorGradingHueShift(half3 col)
{
    col = RgbToHsv(col);
    half hue = col.x + _ColorAdjustments.z;
    col.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(col);
}

half3 ColorGradeSplitToning(half3 col)
{
    col = PositivePow(col, 1.0 / 2.2);
    half t = saturate(LuminaceCM(col) - _ColorSplitToningShadows.w);
    // return t;
    half3 shadows = lerp(0.5, _ColorSplitToningShadows.rgb, 1.0 - t);
    half3 highlights = lerp(0.5, _ColorSplitToningHighlights.rgb, t);
    col = SoftLight(col, shadows);
    col = SoftLight(col, highlights);
    return PositivePow(col, 2.2);
}

float3 ColorGradingShadowsMidtonesHighlights (float3 color) 
{
	float luminance = LuminaceCM(color);
	float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
	float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
	float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
	return
		color * _SMHShadows.rgb * shadowsWeight +
		color * _SMHMidtones.rgb * midtonesWeight +
		color * _SMHHighlights.rgb * highlightsWeight;
}

half3 ColorGradeWhiteBalance(half3 col)
{
    col = LinearToLMS(col);
    col *= _ColorWhiteBalance.rgb;
    return LMSToLinear(col);
}

half3 ReinhardTonemapping(half3 col)
{
    col /= col + 1.0;
    return col;
}
half3 NeutralTonemapping(half3 col)
{
    return NeutralTonemap(col.rgb);
}


half3 ColorGrade(half3 col)
{
    col = min(col.rgb, 60.0);
#ifdef CM_ColorGrading
    col = ColorGradePostExposure(col);
    col = ColorGradingContrast(col);
    col = ColorGradeColorFilter(col);
#elif defined(CM_ACES)
    col = ACES_to_ACEScg(unity_to_ACES(col));
#endif
#ifdef CM_ColorSplitToning
    col = ColorGradeSplitToning(col);
#endif
#ifdef CM_ColorChannelMixer
    col = ColorGradeingChannelMixer(col);
#endif
#ifdef CM_ShadowsMidtoneHighlights
    col = ColorGradingShadowsMidtonesHighlights(col);
#endif
#ifdef CM_ColorGrading
    col = ColorGradingHueShift(col);
    col = ColorGradingSaturation(col);
#endif
#ifdef CM_ColorWhiteBalance
    col = ColorGradeWhiteBalance(col);
#endif

#if defined(CM_Reinhard)
    return ReinhardTonemapping(col);
#elif defined(CM_Neutral)
    return NeutralTonemapping(col);
#endif

    return AcesTonemap(ACEScg_to_ACES(col));
}

#endif