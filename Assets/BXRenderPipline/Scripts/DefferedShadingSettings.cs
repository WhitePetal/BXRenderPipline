using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DefferedShadingSettings
{
	[SerializeField]
	public Shader defferedCombineShader;
	[SerializeField]
	public Shader defferedCombineShaderEditor;
	[SerializeField]
	public Shader defferedShadingShader;
	[SerializeField]
	public Shader defferedShadingShaderEditor;
	[SerializeField]
	public ComputeShader tileLightingCS;
}
