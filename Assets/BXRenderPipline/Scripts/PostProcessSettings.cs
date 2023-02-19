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
public class PostProcessSettings
{
	[SerializeField]
	public Shader colorGradeShader;
	[SerializeField]
	public Shader colorGradeShaderEditor;
	public ToneMappingType toneMappingType = ToneMappingType.Neutral;
	[SerializeField]
	public FXAASettings fxaaSettings = new FXAASettings
	{
		qualitys = FXAASettings.Qualitys.Medium,
		fixedThreshold = 0.0833f,
		relativeThreshold = 0.166f,
		subpixelBlending = 0.75f
	};
}
