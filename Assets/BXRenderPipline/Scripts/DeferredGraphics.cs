using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public class DeferredGraphics
{
	private ScriptableRenderContext context;
	private CullingResults cullingResults;
	private CommandBuffer commandBuffer;
	private Camera camera;

	private PostProcess postProcess = new PostProcess();

	private bool editorMode, useDynamicBatching, useGPUInstancing, useLightsPerObject;
	private int width, height, aa;


#if UNITY_EDITOR
	private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, CommandBuffer commandBuffer, Camera camera,
		int width, int height, int aa,
		 bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject,
		 PostProcessSettings postProcessSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.commandBuffer = commandBuffer;
		this.camera = camera;
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.useLightsPerObject = useLightsPerObject;
		this.width = width;
		this.height = height;
		this.aa = aa;

		postProcess.Setup(postProcessSettings, commandBuffer, width, height);
	}

	public void Render()
	{
		commandBuffer.BeginSample(commandBuffer.name);
		GenerateBuffers();

		DrawGeometryGBuffer(useDynamicBatching, useGPUInstancing, useLightsPerObject);
		DrawSkyboxAndTransparent();
#if UNITY_EDITOR
		DrawUnsupportShader();
		DrawGizmosBeforePostProcess();
#endif
		DrawPostProcess();
		FXAA();
#if UNITY_EDITOR
		DrawGizmosAfterPostProcess();
#endif
	}

	public void CleanUp()
	{
		commandBuffer.ReleaseTemporaryRT(Constants.depthBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.lightingBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.depthNormalBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.fxaaInputBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.ssrBufferId);

		postProcess.CleanUp();
	}

	private void GenerateBuffers()
	{
#if UNITY_EDITOR
		commandBuffer.GetTemporaryRT(Constants.depthBufferId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
#else
		commandBuffer.GetTemporaryRT(Constants.depthBufferId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Depth);
#endif
		commandBuffer.GetTemporaryRT(Constants.lightingBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.depthNormalBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.fxaaInputBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		var ssrDescriptor = new RenderTextureDescriptor(width >> 2, height >> 2, RenderTextureFormat.ARGBHalf, 0);
		ssrDescriptor.memoryless = RenderTextureMemoryless.None;
		ssrDescriptor.msaaSamples = 1;
		ssrDescriptor.sRGB = false;
		ssrDescriptor.autoGenerateMips = false;
		ssrDescriptor.enableRandomWrite = true;
		ssrDescriptor.useMipMap = true;
		ssrDescriptor.mipCount = 4;
		commandBuffer.GetTemporaryRT(Constants.ssrBufferId, ssrDescriptor, FilterMode.Bilinear);

		ExecuteBuffer();
	}

	private void DrawGeometryGBuffer(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
	{
		commandBuffer.SetRenderTarget(Constants.shadingBinding);
		CameraClearFlags clearFlags = camera.clearFlags;
		commandBuffer.ClearRenderTarget(clearFlags <= CameraClearFlags.Depth, clearFlags <= CameraClearFlags.Color, clearFlags == CameraClearFlags.SolidColor ? camera.backgroundColor.linear : Color.clear);
		ExecuteBuffer();

		PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
		// 不透明：主光渲染、GBuffer (baseColor mul shadow, and a is specular)
		SortingSettings sortingSettings = new SortingSettings(camera)
		{
			criteria = SortingCriteria.CommonOpaque
		};
		DrawingSettings drawingSettings = new DrawingSettings()
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.sortingSettings = sortingSettings;
		drawingSettings.SetShaderPassName(0, BXRenderPipline.bxShaderTagIds[0]);
		drawingSettings.SetShaderPassName(1, BXRenderPipline.bxShaderTagIds[1]);
		FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		drawingSettings.perObjectData = PerObjectData.ReflectionProbes |
			PerObjectData.Lightmaps |
			PerObjectData.ShadowMask |
			PerObjectData.OcclusionProbe |
			PerObjectData.LightProbe |
			PerObjectData.LightProbeProxyVolume |
			PerObjectData.OcclusionProbeProxyVolume |
			lightsPerObjectFlags;

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	private void DrawSkyboxAndTransparent()
	{
		context.DrawSkybox(camera);
	}

	private void DrawPostProcess()
	{
		postProcess.Bloom();
		postProcess.ColorGrade();
	}

	private void FXAA()
	{
		commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		postProcess.FXAA();
		ExecuteBuffer();
	}

#if UNITY_EDITOR
	private void DrawUnsupportShader()
	{
		DrawingSettings drawingSettings = new DrawingSettings(BXRenderPipline.legacyShaderTagIds[0], new SortingSettings(camera))
		{
			overrideMaterial = material_error
		};
		FilteringSettings filteringSettings = FilteringSettings.defaultValue;
		for (int i = 1; i < BXRenderPipline.legacyShaderTagIds.Length; ++i)
		{
			drawingSettings.SetShaderPassName(i, BXRenderPipline.legacyShaderTagIds[i]);
		}
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	private void DrawGizmosBeforePostProcess()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
		}
	}

	private void DrawGizmosAfterPostProcess()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}
#endif

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}
