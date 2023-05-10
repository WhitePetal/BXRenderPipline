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
	private TerrainRenderer terrainRenderer;

	private PostProcess postProcess = new PostProcess();

	private bool useDynamicBatching, useGPUInstancing;
	private int width, height, aa;

#if UNITY_EDITOR
	private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, CommandBuffer commandBuffer, Camera camera,
		TerrainRenderer terrainRenderer,
		int width, int height, int aa,
		 bool useDynamicBatching, bool useGPUInstancing,
		 PostProcessSettings postProcessSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.commandBuffer = commandBuffer;
		this.camera = camera;
		this.terrainRenderer = terrainRenderer;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.width = width;
		this.height = height;
		this.aa = aa;

		if(RenderSettings.skybox != postProcessSettings.atmoSettings.skyBox || postProcessSettings.atmoSettings.skyBox.GetColor("_BaseColor") != postProcessSettings.atmoSettings.skyColor.gamma)
        {
			postProcessSettings.atmoSettings.skyBox.SetColor("_BaseColor", postProcessSettings.atmoSettings.skyColor.gamma);
			RenderSettings.skybox = postProcessSettings.atmoSettings.skyBox;
        }

		postProcess.Setup(postProcessSettings, commandBuffer, width, height);
	}

	public void Render()
	{
		commandBuffer.BeginSample(commandBuffer.name);
		ExecuteBuffer();

		GenerateBuffers();

		DrawGeometry(useDynamicBatching, useGPUInstancing);
#if UNITY_EDITOR
        DrawUnsupportShader();
		DrawGizmosBeforePostProcess();
#endif
		DrawPostProcess();
#if UNITY_EDITOR
		DrawGizmosAfterPostProcess();
#endif
		commandBuffer.EndSample(commandBuffer.name);
		ExecuteBuffer();
	}

	public void CleanUp()
	{
		commandBuffer.ReleaseTemporaryRT(Constants.lightingBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.depthNormalBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.fxaaInputBufferId);
		//commandBuffer.ReleaseTemporaryRT(Constants.ssrBufferId);

		postProcess.CleanUp();
	}

	private void GenerateBuffers()
	{
		commandBuffer.GetTemporaryRT(Constants.lightingBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.depthNormalBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.fxaaInputBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		//var ssrDescriptor = new RenderTextureDescriptor(width >> 2, height >> 2, RenderTextureFormat.ARGBHalf, 0);
		//ssrDescriptor.memoryless = RenderTextureMemoryless.None;
		//ssrDescriptor.msaaSamples = 1;
		//ssrDescriptor.sRGB = false;
		//ssrDescriptor.autoGenerateMips = false;
		//ssrDescriptor.enableRandomWrite = true;
		//ssrDescriptor.useMipMap = true;
		//ssrDescriptor.mipCount = 4;
		//commandBuffer.GetTemporaryRT(Constants.ssrBufferId, ssrDescriptor, FilterMode.Bilinear);

		ExecuteBuffer();
	}

	private void DrawGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		CameraClearFlags clearFlags = camera.clearFlags;
		var lightingBuffer = new AttachmentDescriptor(RenderTextureFormat.ARGBHalf);
		var depthNormalBuffer = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
		var depthBuffer = new AttachmentDescriptor(RenderTextureFormat.Depth);

		lightingBuffer.ConfigureClear(clearFlags == CameraClearFlags.SolidColor ? camera.backgroundColor.linear : Color.clear, 1f, 0);
		depthNormalBuffer.ConfigureClear(Color.clear);
		depthBuffer.ConfigureClear(Color.clear, 1f, 0);

		lightingBuffer.ConfigureTarget(Constants.lightingBufferTargetId, false, true);
		depthNormalBuffer.ConfigureTarget(Constants.depthNormalBufferTargetId, false, true);

		PerObjectData lightsPerObjectFlags = PerObjectData.None;
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
		FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		drawingSettings.perObjectData = PerObjectData.ReflectionProbes |
			PerObjectData.Lightmaps |
			PerObjectData.ShadowMask |
			PerObjectData.OcclusionProbe |
			PerObjectData.LightProbe |
			PerObjectData.LightProbeProxyVolume |
			PerObjectData.OcclusionProbeProxyVolume |
			lightsPerObjectFlags;

		var attachments = new NativeArray<AttachmentDescriptor>(3, Allocator.Temp);
		const int depthBufferIndex = 0, lightingBufferIndex = 1, depthNormalBufferIndex = 2;
		attachments[depthBufferIndex] = depthBuffer;
		attachments[lightingBufferIndex] = lightingBuffer;
		attachments[depthNormalBufferIndex] = depthNormalBuffer;
		context.BeginRenderPass(width, height, 1, attachments, depthBufferIndex);
		attachments.Dispose();

		var shadingBuffers = new NativeArray<int>(2, Allocator.Temp);
		shadingBuffers[0] = lightingBufferIndex;
		shadingBuffers[1] = depthNormalBufferIndex;
		context.BeginSubPass(shadingBuffers);
		shadingBuffers.Dispose();

		terrainRenderer.Draw();
        ExecuteBuffer();

		drawingSettings.SetShaderPassName(0, BXRenderPipline.bxShaderTagIds[0]);
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        drawingSettings.SetShaderPassName(0, BXRenderPipline.bxShaderTagIds[1]);
        drawingSettings.SetShaderPassName(1, BXRenderPipline.bxShaderTagIds[2]);
        drawingSettings.SetShaderPassName(2, BXRenderPipline.bxShaderTagIds[3]);
        drawingSettings.SetShaderPassName(3, BXRenderPipline.bxShaderTagIds[4]);
        drawingSettings.SetShaderPassName(4, BXRenderPipline.bxShaderTagIds[5]);
        filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        ExecuteBuffer();
		context.EndSubPass();
		context.EndRenderPass();
	}

	private void DrawPostProcess()
	{
        //postProcess.Fog();
        postProcess.Bloom();
        postProcess.ColorGrade();
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