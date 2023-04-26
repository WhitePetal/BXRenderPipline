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
	private DeferredComputeSettings deferredComputeSettings;
	private Lights lights;

	private PostProcess postProcess = new PostProcess();

	private bool editorMode, useDynamicBatching, useGPUInstancing;
	private int width, height, aa;


#if UNITY_EDITOR
	private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, CommandBuffer commandBuffer, Camera camera,
		DeferredComputeSettings deferredComputeSettings, Lights lights,
		int width, int height, int aa,
		 bool editorMode, bool useDynamicBatching, bool useGPUInstancing,
		 PostProcessSettings postProcessSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.commandBuffer = commandBuffer;
		this.camera = camera;
		this.deferredComputeSettings = deferredComputeSettings;
		this.lights = lights;
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.width = width;
		this.height = height;
		this.aa = aa;

		postProcess.Setup(postProcessSettings, commandBuffer, width, height);
	}

	public void Render()
	{
		commandBuffer.BeginSample(commandBuffer.name);
		ExecuteBuffer();
        GenerateBuffers();

        DrawGeometryGBuffer(useDynamicBatching, useGPUInstancing);
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
		commandBuffer.ReleaseTemporaryRT(Constants.depthBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.lightingBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.depthNormalBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.fxaaInputBufferId);
		//commandBuffer.ReleaseTemporaryRT(Constants.ssrBufferId);

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

	private void DrawGeometryGBuffer(bool useDynamicBatching, bool useGPUInstancing)
	{
		//commandBuffer.SetRenderTarget(Constants.shadingBinding);

		CameraClearFlags clearFlags = camera.clearFlags;
		var depthNormalBuffer = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
		var depthBuffer = new AttachmentDescriptor(RenderTextureFormat.Depth);

		depthNormalBuffer.ConfigureClear(Color.clear);
		depthBuffer.ConfigureClear(Color.clear, 1f, 0);
		depthNormalBuffer.ConfigureTarget(Constants.depthNormalBufferTargetId, false, true);
		depthBuffer.ConfigureTarget(Constants.depthBufferTargetId, false, true);

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

		var attachments = new NativeArray<AttachmentDescriptor>(2, Allocator.Temp);
		const int depthBufferIndex = 0, depthNormalBufferIndex = 1;
		attachments[depthBufferIndex] = depthBuffer;
		attachments[depthNormalBufferIndex] = depthNormalBuffer;
		context.BeginRenderPass(width, height, 1, attachments, depthBufferIndex);
		attachments.Dispose();

		var depthNormalTarget = new NativeArray<int>(1, Allocator.Temp);
		depthNormalTarget[0] = depthNormalBufferIndex;
		context.BeginSubPass(depthNormalTarget);
		depthNormalTarget.Dispose();
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
		context.EndSubPass();
		context.EndRenderPass();

		ExecuteBuffer();
		commandBuffer.SetComputeIntParam(deferredComputeSettings.tileLightingCS, "_PointLightCount", lights.pointLightCount);
		commandBuffer.SetComputeVectorArrayParam(deferredComputeSettings.tileLightingCS, "_PointLightSpheres", lights.pointLightSpheres);
		commandBuffer.SetComputeMatrixParam(deferredComputeSettings.tileLightingCS, "BX_MatrixV", camera.worldToCameraMatrix);
		commandBuffer.SetComputeTextureParam(deferredComputeSettings.tileLightingCS, 0, Constants.depthNormalBufferId, Constants.depthNormalBufferTargetId);
		commandBuffer.DispatchCompute(deferredComputeSettings.tileLightingCS, 0, Mathf.CeilToInt(width / 16f), Mathf.CeilToInt(height / 16f), 1);
		ExecuteBuffer();

		var lightingBuffer = new AttachmentDescriptor(RenderTextureFormat.ARGBHalf);
		lightingBuffer.ConfigureClear(clearFlags == CameraClearFlags.SolidColor ? camera.backgroundColor.linear : Color.clear, 1f, 0);
		lightingBuffer.ConfigureTarget(Constants.lightingBufferTargetId, false, true);
		depthBuffer = new AttachmentDescriptor(RenderTextureFormat.Depth);
		depthBuffer.ConfigureTarget(Constants.depthBufferTargetId, true, true);

		attachments = new NativeArray<AttachmentDescriptor>(2, Allocator.Temp);
		const int lightingBufferIndex = 1;
		attachments[depthBufferIndex] = depthBuffer;
		attachments[lightingBufferIndex] = lightingBuffer;
		context.BeginRenderPass(width, height, 1, attachments, depthBufferIndex);
		attachments.Dispose();
		drawingSettings.SetShaderPassName(0, BXRenderPipline.bxShaderTagIds[1]);
		var lightingBufferTarget = new NativeArray<int>(1, Allocator.Temp);
		lightingBufferTarget[0] = lightingBufferIndex;
		context.BeginSubPass(lightingBufferTarget);
		lightingBufferTarget.Dispose();
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
		context.DrawSkybox(camera);
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		drawingSettings.SetShaderPassName(0, BXRenderPipline.bxShaderTagIds[2]);
		drawingSettings.SetShaderPassName(1, BXRenderPipline.bxShaderTagIds[3]);
		drawingSettings.SetShaderPassName(2, BXRenderPipline.bxShaderTagIds[4]);
		drawingSettings.SetShaderPassName(3, BXRenderPipline.bxShaderTagIds[5]);
		drawingSettings.SetShaderPassName(4, BXRenderPipline.bxShaderTagIds[6]);
		filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
		context.EndSubPass();
		context.EndRenderPass();
		ExecuteBuffer();
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