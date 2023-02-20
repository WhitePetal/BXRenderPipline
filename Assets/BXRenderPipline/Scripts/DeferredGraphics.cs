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
	private DefferedShadingSettings shadingSettings;

	private PostProcess postProcess = new PostProcess();

	private bool editorMode, useDynamicBatching, useGPUInstancing, useLightsPerObject;
	private int width, height, aa;
	private RenderTargetIdentifier cameraTargetId;


#if UNITY_EDITOR
	private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

	private Material defferedShadingMaterial;
	private Material DefferedShadingMaterial
	{
		get
		{
			if (defferedShadingMaterial == null && shadingSettings.defferedShadingShader != null)
			{
				defferedShadingMaterial = new Material(shadingSettings.defferedShadingShader);
				defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return defferedShadingMaterial;
		}
	}


	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, CommandBuffer commandBuffer, Camera camera,
		int width, int height, int aa, RenderTargetIdentifier cameraTargetId,
		 bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject,
		 DefferedShadingSettings shadingSettings, PostProcessSettings postProcessSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.commandBuffer = commandBuffer;
		this.camera = camera;
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.useLightsPerObject = useLightsPerObject;
		this.shadingSettings = shadingSettings;
		this.cameraTargetId = cameraTargetId;
		this.width = width;
		this.height = height;
		this.aa = aa;

		postProcess.Setup(postProcessSettings, commandBuffer, width, height);
	}

	public void Render(out GraphicsFence graphicsFence)
	{
		commandBuffer.BeginSample(commandBuffer.name);
		GenerateBuffers();

		BeginRenderPass();
		DrawGeometryGBuffer(useDynamicBatching, useGPUInstancing, useLightsPerObject);
		DrawDefferedShading();
		DrawTransparent();
#if UNITY_EDITOR
		DrawUnsupportShader();
		DrawGizmosBeforePostProcess();
#endif
		context.EndRenderPass();

		DrawPostProcess();
		FXAA(out graphicsFence);
#if UNITY_EDITOR
		DrawGizmosAfterPostProcess();
#endif
	}

	public void CleanUp()
	{
		commandBuffer.ReleaseTemporaryRT(Constants.lightingBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.depthNormalBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.fxaaInputBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.ssrBufferId);

#if UNITY_EDITOR_OSX
		commandBuffer.ReleaseTemporaryRT(Constants.lightingBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.baseColorBufferId);
		commandBuffer.ReleaseTemporaryRT(Constants.materialDataBufferId);
#endif
		postProcess.CleanUp();
	}

	private void GenerateBuffers()
	{
		int width = camera.pixelWidth;
		int height = camera.pixelHeight;
		commandBuffer.GetTemporaryRT(Constants.lightingBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.depthNormalBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.fxaaInputBufferId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		var ssrDescriptor = new RenderTextureDescriptor(width >> 2, height >> 2, RenderTextureFormat.ARGBHalf, 0);
		ssrDescriptor.memoryless = RenderTextureMemoryless.None;
		ssrDescriptor.msaaSamples = 1;
		ssrDescriptor.sRGB = false;
		ssrDescriptor.autoGenerateMips = false;
		ssrDescriptor.enableRandomWrite = true;
		ssrDescriptor.useMipMap = true;
		ssrDescriptor.mipCount = 4;
		commandBuffer.GetTemporaryRT(Constants.ssrBufferId, ssrDescriptor, FilterMode.Bilinear);
#if UNITY_EDITOR_OSX
		commandBuffer.GetTemporaryRT(Constants.lightingBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.baseColorBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(Constants.materialDataBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
#endif
		ExecuteBuffer();
	}

	private void BeginRenderPass()
	{
		var lightingGeoBufferTarget = new AttachmentDescriptor(RenderTextureFormat.ARGBHalf);
		var lightingBufferTarget = new AttachmentDescriptor(RenderTextureFormat.ARGBHalf);
		var baseColorBufferTarget = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
		var materialDataBufferTarget = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
		var depthNormalBufferTarget = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
		var depthBufferTarget = new AttachmentDescriptor(RenderTextureFormat.Depth);

		lightingBufferTarget.ConfigureClear(Color.clear);
		baseColorBufferTarget.ConfigureClear(Color.clear);
		materialDataBufferTarget.ConfigureClear(Color.clear);
		depthNormalBufferTarget.ConfigureClear(Color.clear);
		depthBufferTarget.ConfigureClear(Color.clear, 1f, 0);

#if UNITY_EDITOR_OSX
		lightingBufferTarget.ConfigureTarget(Constants.lightingBufferId, false, true);
		baseColorBufferTarget.ConfigureTarget(Constants.baseColorBufferId, false, true);
		materialDataBufferTarget.ConfigureTarget(Constants.materialDataBufferId, false, true);
#endif
		lightingBufferTarget.ConfigureTarget(Constants.lightingBufferTargetId, false, true);
		depthNormalBufferTarget.ConfigureTarget(Constants.depthNormalBufferTargetId, false, true);

		var attchments = new NativeArray<AttachmentDescriptor>(5, Allocator.Temp);
		attchments[Constants.depthBufferIndex] = depthBufferTarget;
		attchments[Constants.lightingBufferIndex] = lightingBufferTarget;
		attchments[Constants.baseColorBufferIndex] = baseColorBufferTarget;
		attchments[Constants.materialDataBufferIndex] = materialDataBufferTarget;
		attchments[Constants.depthNormalBufferIndex] = depthNormalBufferTarget;

		context.BeginRenderPass(width, height, aa, attchments, Constants.depthBufferIndex);
		attchments.Dispose();
	}

	private void DrawGeometryGBuffer(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
	{
		var gBuffers = new NativeArray<int>(4, Allocator.Temp);
		gBuffers[0] = Constants.lightingBufferIndex;
		gBuffers[1] = Constants.baseColorBufferIndex;
		gBuffers[2] = Constants.materialDataBufferIndex;
		gBuffers[3] = Constants.depthNormalBufferIndex;
		context.BeginSubPass(gBuffers);
		gBuffers.Dispose();

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
		context.DrawSkybox(camera);
		context.EndSubPass();
	}

	private void DrawDefferedShading()
	{
		var shadingOutputs = new NativeArray<int>(1, Allocator.Temp);
		shadingOutputs[0] = Constants.lightingBufferIndex;
		var shadingInputs = new NativeArray<int>(3, Allocator.Temp);
		shadingInputs[0] = Constants.baseColorBufferIndex;
		shadingInputs[1] = Constants.materialDataBufferIndex;
		shadingInputs[2] = Constants.depthNormalBufferIndex;
		context.BeginSubPass(shadingOutputs, shadingInputs, true);
		shadingOutputs.Dispose();
		shadingInputs.Dispose();
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedShadingMaterial, 0, MeshTopology.Triangles, 6);
		ExecuteBuffer();
		context.EndSubPass();
	}

	private void DrawTransparent()
	{
		var renderOutputs = new NativeArray<int>(1, Allocator.Temp);
		renderOutputs[0] = Constants.lightingBufferIndex;
		context.BeginSubPass(renderOutputs);
		renderOutputs.Dispose();
		context.EndSubPass();
	}

	private void DrawPostProcess()
	{
		postProcess.Bloom();
		postProcess.ColorGrade();
	}

	private void FXAA(out GraphicsFence graphicsFence)
	{
		commandBuffer.SetRenderTarget(cameraTargetId);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		postProcess.FXAA();
		graphicsFence = commandBuffer.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
		ExecuteBuffer();
	}

#if UNITY_EDITOR
	private void DrawUnsupportShader()
	{
		var renderOutputs = new NativeArray<int>(1, Allocator.Temp);
		renderOutputs[0] = Constants.lightingBufferIndex;
		context.BeginSubPass(renderOutputs);
		renderOutputs.Dispose();
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
		context.EndSubPass();
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
