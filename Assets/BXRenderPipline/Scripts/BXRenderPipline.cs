using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BXRenderPipline : RenderPipeline
{
	public static ShaderTagId[] bxShaderTagIds = new ShaderTagId[3]
	{
			new ShaderTagId("BXCharacter"),
			new ShaderTagId("BXScene"),
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

	private bool useDynamicBatching, useGPUInstancing, useLightsPerObject, editorMode;
	private MainCameraRender mainCameraRenderer = new MainCameraRender();
	private DefferedShadingSettings defferedShadingSettings;
	private PostProcessSettings postprocessSettings;
	private ShadowSettings shadowSettings;

	public BXRenderPipline(bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching, bool useLightsPerObject, FrameRate frameRate, 
		DefferedShadingSettings defferedShadingSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings)
	{
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.useLightsPerObject = useLightsPerObject;
		this.defferedShadingSettings = defferedShadingSettings;
		this.postprocessSettings = postprocessSettings;
		this.shadowSettings = shadowSettings;
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
		GraphicsSettings.lightsUseLinearIntensity = true;
		QualitySettings.antiAliasing = 1;
		Application.targetFrameRate = (int)frameRate;
#if UNITY_EDITOR
		//InitializeForEditor();
#endif
	}

	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		for (int i = 0; i < cameras.Length; ++i)
		{
			mainCameraRenderer.Render(context, cameras[i], editorMode, useDynamicBatching, useGPUInstancing, useLightsPerObject, defferedShadingSettings, postprocessSettings, shadowSettings);
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (mainCameraRenderer.lights.tileLightingIndicesBuffer != null) mainCameraRenderer.lights.tileLightingIndicesBuffer.Release();
		if (mainCameraRenderer.lights.tileLightingDatasBuffer != null) mainCameraRenderer.lights.tileLightingDatasBuffer.Release();
		if (mainCameraRenderer.bxdepthNormalBuffer != null) mainCameraRenderer.bxdepthNormalBuffer.Release();
	}
}
