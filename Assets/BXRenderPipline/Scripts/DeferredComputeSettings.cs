using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DeferredComputeSettings
{
	[SerializeField]
	public ComputeShader tileLightingCS;
	[SerializeField]
	public ComputeShader ssrGenerateCS;
}