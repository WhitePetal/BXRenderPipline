using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum FrameRate
{
	FPS60 = 60,
	FPS30 = 30
}

[CreateAssetMenu(menuName = "Rendering/BXPipline")]
public class BXRenderPiplineAsset : RenderPipelineAsset
{
	public bool editorMode = true;
	public bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatching = true, useLightsPerObject = true;
	public FrameRate frameRate = FrameRate.FPS60;
	[SerializeField]
	public DefferedShadingSettings defferedShadingSettings;
	[SerializeField]
	public PostProcessSettings processSettings;
	[SerializeField]
	public ShadowSettings shadowSettings;

	protected override RenderPipeline CreatePipeline()
	{
		return new BXRenderPipline(editorMode, useDynamicBatching, useGPUInstancing, useSRPBatching, useLightsPerObject, frameRate, defferedShadingSettings, processSettings, shadowSettings);
	}
}
