using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShadowMapSize
{
	_256 = 256,
	_512 = 512,
	_1024 = 1024,
	_2048 = 2048,
	_4086 = 4096
};

public enum ShadowMapBits
{
	_16 = 16,
	_24 = 24,
	_32 = 32
};

public enum ShadowFilterMode
{
	PCF2x2, PCF3x3, PCF5x5, PCF7x7
};

public enum CascadeBlendMode
{
	Hard, Soft, Dither
}

[System.Serializable]
public class ShadowSettings
{
	[Min(0.001f)]
	public float maxShadowDistance = 100;
	[Range(0.001f, 1f)]
	public float distanceFade = 0.1f;
	public Color shadowsColor = new Color(.5f, .5f, .5f);
	public ShadowMapBits shadowMapBits = ShadowMapBits._24;
	public ShadowMapSize shadowMapSize = ShadowMapSize._2048;
	public ShadowFilterMode shadowFilter = ShadowFilterMode.PCF2x2;
	public CascadeBlendMode cascadeBlendMode = CascadeBlendMode.Dither;
	[Range(1, 4)]
	public int cascadeCount = 4;
	[Range(0f, 1f)]
	public float cascadeRatio1 = 0.1f, cascadeRatio2 = 0.25f, cascadeRatio3 = 0.5f;
	[Range(0.1f, 1f)]
	public float cascadeFade = 0.1f;
}