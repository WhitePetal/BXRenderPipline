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
public class PostProcessSettings
{
	[SerializeField]
	public Shader tonemappingShader;
	[SerializeField]
	public Shader tonemappingShaderEditor;
	public ToneMappingType toneMappingType = ToneMappingType.Neutral;
}
