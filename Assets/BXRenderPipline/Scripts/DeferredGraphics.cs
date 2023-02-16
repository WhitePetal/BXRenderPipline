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
#if UNITY_EDITOR
			if (editorMode)
			{
				if (defferedShadingMaterial == null && shadingSettings.defferedShadingShaderEditor != null)
				{
					defferedShadingMaterial = new Material(shadingSettings.defferedShadingShaderEditor);
					defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return defferedShadingMaterial;
			}
			else
			{
				if (defferedShadingMaterial == null && shadingSettings.defferedShadingShader != null)
				{
					defferedShadingMaterial = new Material(shadingSettings.defferedShadingShader);
					defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return defferedShadingMaterial;
			}
#else
			if (defferedShadingMaterial == null && shadingSettings.defferedShadingShader != null)
			{
				defferedShadingMaterial = new Material(shadingSettings.defferedShadingShader);
				defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return defferedShadingMaterial;
#endif
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

		postProcess.Setup(postProcessSettings, editorMode);
	}

	public void Render(out GraphicsFence graphicsPiplineCompeletFence)
	{
		commandBuffer.BeginSample(commandBuffer.name);
		GenerateBuffers();
		BeginRenderPass();
		DrawGeometryGBuffer(useDynamicBatching, useGPUInstancing, useLightsPerObject);
		DrawDefferedShading();
		DrawSkyBoxAndTransparent();
		DrawUnsupportShader();
		DrawGizmosBeforePostProcess();
		DrawPostProcess();
		DrawGizmosAfterPostProcess();
		RenderToCameraTargetAndTonemapping(out graphicsPiplineCompeletFence);
		context.EndRenderPass();
	}

	public void CleanUp()
	{
		commandBuffer.ReleaseTemporaryRT(Constants.depthNormalBufferId);
	}

	private void GenerateBuffers()
	{
		int width = camera.pixelWidth;
		int height = camera.pixelHeight;
		commandBuffer.GetTemporaryRT(Constants.depthNormalBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		ExecuteBuffer();
	}

	private void BeginRenderPass()
	{
		var cameraTarget = new AttachmentDescriptor(RenderTextureFormat.ARGB32);
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

		depthNormalBufferTarget.ConfigureTarget(Constants.depthNormalBufferTargetId, false, true);
		cameraTarget.ConfigureTarget(cameraTargetId, false, true);

		var attchments = new NativeArray<AttachmentDescriptor>(6, Allocator.Temp);
		attchments[Constants.depthBufferIndex] = depthBufferTarget;
		attchments[Constants.lightingBufferIndex] = lightingBufferTarget;
		attchments[Constants.baseColorBufferIndex] = baseColorBufferTarget;
		attchments[Constants.materialDataBufferIndex] = materialDataBufferTarget;
		attchments[Constants.depthNormalBufferIndex] = depthNormalBufferTarget;
		attchments[Constants.cameraTargetIndex] = cameraTarget;

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

	private void DrawSkyBoxAndTransparent()
	{
		var renderOutputs = new NativeArray<int>(1, Allocator.Temp);
		renderOutputs[0] = Constants.lightingBufferIndex;
		context.BeginSubPass(renderOutputs);
		renderOutputs.Dispose();
		context.DrawSkybox(camera);
		context.EndSubPass();
	}

	private void DrawPostProcess()
	{

	}

	private void RenderToCameraTargetAndTonemapping(out GraphicsFence graphicsPiplineCompeletFence)
	{
		var postProcessOutput = new NativeArray<int>(1, Allocator.Temp);
		postProcessOutput[0] = Constants.cameraTargetIndex;
		var postProcessInput = new NativeArray<int>(1, Allocator.Temp);
		postProcessInput[0] = Constants.lightingBufferIndex;
		context.BeginSubPass(postProcessOutput, postProcessInput, true);
		postProcessOutput.Dispose();
		postProcessInput.Dispose();
		graphicsPiplineCompeletFence = postProcess.ColorGrade(commandBuffer);
		ExecuteBuffer();
		context.EndSubPass();
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
		context.EndSubPass();
	}

	private void DrawGizmosBeforePostProcess()
	{
		if (Handles.ShouldRenderGizmos())
		{
			var renderOutputs = new NativeArray<int>(1, Allocator.Temp);
			renderOutputs[0] = Constants.lightingBufferIndex;
			context.BeginSubPass(renderOutputs);
			renderOutputs.Dispose();
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.EndSubPass();
		}
	}

	private void DrawGizmosAfterPostProcess()
	{
		if (Handles.ShouldRenderGizmos())
		{
			var renderOutputs = new NativeArray<int>(1, Allocator.Temp);
			renderOutputs[0] = Constants.lightingBufferIndex;
			context.BeginSubPass(renderOutputs);
			renderOutputs.Dispose();
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
			context.EndSubPass();
		}
	}
#endif

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}
