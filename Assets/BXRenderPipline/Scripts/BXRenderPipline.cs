using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BXRenderPipline : RenderPipeline
{
	public static ShaderTagId[] bxShaderTagIds = new ShaderTagId[7]
	{
			new ShaderTagId("BXCharacter"),
			new ShaderTagId("BXScene"),
			new ShaderTagId("BXCharacterAlphaDepth"),
			new ShaderTagId("BXCharacterAlpha"),
			new ShaderTagId("BXSceneAlphaDepth"),
			new ShaderTagId("BXSceneAlpha"),
			new ShaderTagId("BXEffect")
	};
	public static ShaderTagId[] legacyShaderTagIds = new ShaderTagId[]
	{
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	private bool useDynamicBatching, useGPUInstancing, editorMode;
	private MainCameraRender mainCameraRenderer = new MainCameraRender();
	private DeferredComputeSettings deferredComputeSettings;
	private PostProcessSettings postprocessSettings;
	private ShadowSettings shadowSettings;

	public BXRenderPipline(bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching,
		FrameRate frameRate,
		DeferredComputeSettings deferredComputeSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings)
	{
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.deferredComputeSettings = deferredComputeSettings;
		this.postprocessSettings = postprocessSettings;
		this.shadowSettings = shadowSettings;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
		GraphicsSettings.lightsUseLinearIntensity = true;
		QualitySettings.antiAliasing = 1;
		Application.targetFrameRate = (int)frameRate;
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		for (int i = 0; i < cameras.Length; ++i)
		{
			mainCameraRenderer.Render(context, cameras[i], editorMode, useDynamicBatching, useGPUInstancing,
				deferredComputeSettings, postprocessSettings, shadowSettings);
		}
	}

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
		mainCameraRenderer.tileLightingDatasBuffer.Release();
		mainCameraRenderer.tileLightingIndicesBuffer.Release();
    }
}