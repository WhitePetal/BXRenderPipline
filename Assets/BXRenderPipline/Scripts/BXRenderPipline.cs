using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BXRenderPipline : RenderPipeline
{
	public static ShaderTagId[] bxShaderTagIds = new ShaderTagId[7]
	{
			new ShaderTagId("BXDepthNormal"),
			new ShaderTagId("BXOpaque"),
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
	private TerrainSettings terrainSettings;

	public BXRenderPipline(bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching,
		FrameRate frameRate,
		DeferredComputeSettings deferredComputeSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings,
		TerrainSettings terrainSettings)
	{
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.deferredComputeSettings = deferredComputeSettings;
		this.postprocessSettings = postprocessSettings;
		this.shadowSettings = shadowSettings;
		this.terrainSettings = terrainSettings;
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
				deferredComputeSettings, postprocessSettings, shadowSettings, terrainSettings);
		}
	}

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
		mainCameraRenderer.OnDispose();
    }
}