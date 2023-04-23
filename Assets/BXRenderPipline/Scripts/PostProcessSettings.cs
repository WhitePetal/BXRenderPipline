using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToneMappingType
{
	Reinhard,
	Neutral,
	ACES
}

[System.Serializable]
public struct BloomSettings
{
	public Shader bloomShader;
	[Range(0, Constants.maxBloomPyramidLevels)]
	public int maxIterations;
	[Min(0)]
	public int downScaleLimit;

	[Min(0f)]
	public float threshold;
	[Range(0f, 1f)]
	public float thresholdKnee;
	[Min(0f)]
	public float intensity;
}

[System.Serializable]
public struct ColorGradingSettings
{
	public bool enable;
	public float exposure;
	[Range(-100f, 100f)]
	public float contrast;
	[Range(-180f, 180f)]
	public float hueShift;
	[Range(-100f, 100f)]
	public float satruation;
	public Color filterColor;
}

[System.Serializable]
public struct WhiteBalanceSettings
{
	public bool enable;
	[Range(-100f, 100f)]
	public float temperature, tint;
}

[System.Serializable]
public struct SplitToningSettings
{
	public bool enable;
	[ColorUsage(false)]
	public Color shadows, highlights;
	[Range(-100f, 100f)]
	public float balance;
}

[System.Serializable]
public struct ChanelMixerSettings
{
	public bool enable;
	public Vector3 red, green, blue;
}

[System.Serializable]
public struct ShadowsMidtoneHighlightSettings
{
	public bool enable;
	[ColorUsage(false, true)]
	public Color shadows, midtone, highlights;
	[Range(0f, 2f)]
	public float shadowStart, shadowEnd, highlightsStart, highlightsEnd;
}

[System.Serializable]
public struct FXAASettings
{
	public enum Qualitys
	{
		Low,
		Medium,
		High
	};

	public Qualitys qualitys;
	[SerializeField]
	public Shader fxaaShader;
	[Range(0.0312f, 0.0833f)]
	public float fixedThreshold;
	[Range(0.063f, 0.333f)]
	public float relativeThreshold;
	// Choose the amount of sub-pixel aliasing removal.
	// This can effect sharpness.
	//   1.00 - upper limit (softer)
	//   0.75 - default amount of filtering
	//   0.50 - lower limit (sharper, less sub-pixel aliasing removal)
	//   0.25 - almost off
	//   0.00 - completely off
	[Range(0f, 1f)]
	public float subpixelBlending;
}

[System.Serializable]
public struct FogSettings
{
	[SerializeField]
	public Shader fogShader;
}

[System.Serializable]
public class PostProcessSettings
{
	[SerializeField]
	public Shader copTexShader;
	[SerializeField]
	public Shader colorGradeShader;
	[SerializeField]
	public Shader colorGradeShaderEditor;

	public BloomSettings bloomSettings = new BloomSettings
	{
		maxIterations = 4,
		downScaleLimit = 2,
		threshold = 0.5f,
		thresholdKnee = 0.5f,
		intensity = 0.5f
	};
	public ToneMappingType toneMappingType = ToneMappingType.Neutral;
	public ColorGradingSettings colorGradingSettings = new ColorGradingSettings
	{
		exposure = 1f,
		contrast = 1f,
		hueShift = 0f,
		satruation = 1f,
		filterColor = Color.white
	};
	public WhiteBalanceSettings whiteBalanceSettings;
	public SplitToningSettings splitToningSettings = new SplitToningSettings
	{
		shadows = new Color(0.36f, 0.49f, 0.5f),
		highlights = new Color(0.92f, 0.83f, 0.59f),
		balance = 30f
	};
	public ChanelMixerSettings chanelMixerSettings = new ChanelMixerSettings
	{
		red = Vector3.right,
		green = Vector3.up,
		blue = Vector3.forward
	};
	public ShadowsMidtoneHighlightSettings shadowsMidtoneHighlightSettings = new ShadowsMidtoneHighlightSettings
	{
		shadows = new Color(.62f, .77f, .79f),
		midtone = new Color(.93f, .78f, .86f),
		highlights = new Color(1f, .9f, .79f),
		shadowStart = .0f,
		shadowEnd = .46f,
		highlightsStart = .586f,
		highlightsEnd = 1.439f
	};
	public FXAASettings fxaaSettings = new FXAASettings
	{
		qualitys = FXAASettings.Qualitys.Medium,
		fixedThreshold = 0.0833f,
		relativeThreshold = 0.166f,
		subpixelBlending = 0.75f
	};

	public FogSettings fogSettings = new FogSettings
	{

	};
}