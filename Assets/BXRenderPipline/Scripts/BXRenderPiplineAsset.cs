using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum FrameRate
{
	FPS60 = 60,
	FPS30 = 30
}

public enum ReflectType
{
	OnlySSR,
	OnlyProbe,
	ProbeAndSSR
}

[CreateAssetMenu(menuName = "Rendering/BXPipline")]
public class BXRenderPiplineAsset : RenderPipelineAsset
{
	public bool editorMode = true;
	public bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatching = true;
	public FrameRate frameRate = FrameRate.FPS60;
	public ReflectType reflectType = ReflectType.OnlySSR;
	[SerializeField]
	public ShadowSettings shadowSettings;
	[SerializeField]
	public DeferredComputeSettings deferredComputeSettings;
	[SerializeField]
	public PostProcessSettings processSettings;

	protected override RenderPipeline CreatePipeline()
	{
		return new BXRenderPipline(editorMode, useDynamicBatching, useGPUInstancing, useSRPBatching, frameRate,
			reflectType, deferredComputeSettings, processSettings, shadowSettings);
	}
}
