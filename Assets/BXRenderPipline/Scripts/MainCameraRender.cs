using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MainCameraRender
{
	private Camera camera;
	private ScriptableRenderContext context;
	private CullingResults cullingResults;

	private const string commandBufferName = "Camera Render";
	private CommandBuffer commandBuffer = new CommandBuffer
	{
		name = commandBufferName
	};

	private static int depthBufferId = Shader.PropertyToID("_DepthBuffer");
	private static int lightingBufferId = Shader.PropertyToID("_LightingBuffer");
	private static int baseColorBufferId = Shader.PropertyToID("_BaseColorBuffer");
	private static int materialDataBufferId = Shader.PropertyToID("_MaterialDataBuffer");
	private static int depthNormalBufferId = Shader.PropertyToID("_BXDepthNormalBuffer");
	private static RenderTargetIdentifier depthBufferTargetId = new RenderTargetIdentifier(depthBufferId);
	private static RenderTargetIdentifier lightingBufferTargetId = new RenderTargetIdentifier(lightingBufferId);
	private static RenderTargetIdentifier baseColorBufferTargetId = new RenderTargetIdentifier(baseColorBufferId);
	private static RenderTargetIdentifier materialDataBufferTargetId = new RenderTargetIdentifier(materialDataBufferId);
	private static RenderTargetIdentifier depthNormalBufferTargetId = new RenderTargetIdentifier(depthNormalBufferId);
	private static RenderTargetIdentifier[] defferedShadingTargestsId = new RenderTargetIdentifier[4]
	{
		lightingBufferTargetId,
		baseColorBufferTargetId,
		materialDataBufferTargetId,
		depthNormalBufferTargetId
	};
	private static RenderBufferLoadAction[] defferedShadingTargetLoads = new RenderBufferLoadAction[4]
	{
		RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare
	};
	private static RenderBufferStoreAction[] defferedShadingTargetStores = new RenderBufferStoreAction[4]
	{
		RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store
	};
	private static RenderTargetBinding defferedShadingBinding = new RenderTargetBinding(defferedShadingTargestsId, defferedShadingTargetLoads, defferedShadingTargetStores, depthBufferTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

	private bool editorMode, useDynamicBatching, useGPUInstancing, useLightsPerObject;

	private int viewPortRaysId = Shader.PropertyToID("_ViewPortRays");

	private DefferedShadingSettings defferedShadingSettings;
	private Material defferedCombineMaterial;
	private Material DefferedCombineMaterial
	{
		get
		{
#if UNITY_EDITOR
			if (editorMode)
			{
				if (defferedCombineMaterial == null && defferedShadingSettings.defferedCombineShaderEditor != null)
				{
					defferedCombineMaterial = new Material(defferedShadingSettings.defferedCombineShaderEditor);
					defferedCombineMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return defferedCombineMaterial;
			}
			else
			{
				if (defferedCombineMaterial == null && defferedShadingSettings.defferedCombineShader != null)
				{
					defferedCombineMaterial = new Material(defferedShadingSettings.defferedCombineShader);
					defferedCombineMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return defferedCombineMaterial;
			}
#else
			if (defferedCombineMaterial == null && defferedShadingSettings.defferedCombineShader != null)
			{
				defferedCombineMaterial = new Material(defferedShadingSettings.defferedCombineShader);
				defferedCombineMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return defferedCombineMaterial;
#endif
		}
	}
	private Material defferedShadingMaterial;
	private Material DefferedShadingMaterial
	{
		get
		{
#if UNITY_EDITOR
			if (editorMode)
			{
				if (defferedShadingMaterial == null && defferedShadingSettings.defferedShadingShaderEditor != null)
				{
					defferedShadingMaterial = new Material(defferedShadingSettings.defferedShadingShaderEditor);
					defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return defferedShadingMaterial;
			}
			else
			{
				if (defferedShadingMaterial == null && defferedShadingSettings.defferedShadingShader != null)
				{
					defferedShadingMaterial = new Material(defferedShadingSettings.defferedShadingShader);
					defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return defferedShadingMaterial;
			}
#else
			if (defferedShadingMaterial == null && defferedShadingSettings.defferedShadingShader != null)
			{
				defferedShadingMaterial = new Material(defferedShadingSettings.defferedShadingShader);
				defferedShadingMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return defferedShadingMaterial;
#endif
		}
	}

	private PostProcessSettings postprocessSettings;
	private Material tonemappingMaterial;
	public Material TonemappingMaterial
	{
		get
		{
#if UNITY_EDITOR
			if (editorMode)
			{
				if (tonemappingMaterial == null && postprocessSettings.tonemappingShaderEditor != null)
				{
					tonemappingMaterial = new Material(postprocessSettings.tonemappingShaderEditor);
					tonemappingMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return tonemappingMaterial;
			}
			else
			{
				if (tonemappingMaterial == null && postprocessSettings.tonemappingShader != null)
				{
					tonemappingMaterial = new Material(postprocessSettings.tonemappingShader);
					tonemappingMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return tonemappingMaterial;
			}
#else
			if (tonemappingMaterial == null && postprocessSettings.tonemappingShader != null)
			{
				tonemappingMaterial = new Material(postprocessSettings.tonemappingShader);
				tonemappingMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return tonemappingMaterial;
#endif
		}
	}

	private Lights lights = new Lights();

	public void Render(ScriptableRenderContext context, Camera camera, bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, 
		DefferedShadingSettings defferedShadingSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;
		this.editorMode = editorMode;
		this.useDynamicBatching = useDynamicBatching;
		this.useGPUInstancing = useGPUInstancing;
		this.useLightsPerObject = useLightsPerObject;
		this.defferedShadingSettings = defferedShadingSettings;
		this.postprocessSettings = postprocessSettings;

#if UNITY_EDITOR
		PreparBuffer();
		PrepareForSceneWindow();
#endif

		if (!Cull(shadowSettings.maxShadowDistance)) return;

		commandBuffer.BeginSample(SampleName);
		ExecuteBuffer();
		lights.Setup(context, cullingResults, shadowSettings);
		commandBuffer.EndSample(SampleName);
		SetupForRender();
#if UNITY_EDITOR
		if (editorMode)
		{
			ShadingInEditorMode();
		}
		else
		{
			ShadingInPlayerMode();
		}
#else
	ShadingInPlayerMode(useDynamicBatching, useGPUInstancing, useLightsPerObject);
#endif
	}

	private void ShadingInPlayerMode()
	{
		DrawGeometryGBuffer(useDynamicBatching, useGPUInstancing, useLightsPerObject);
		DrawDefferedShading();
		DrawDefferedCombine();
		DrawSkyBoxAndTransparent();
		DrawPostProcess();
		RenderToCameraTargetAndTonemapping();
		Submit();
	}

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}

	private bool Cull(float maxShadowDistance)
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
		{
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
			cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
	}

	private void SetupForRender()
	{
		context.SetupCameraProperties(camera);
		commandBuffer.BeginSample(SampleName);
	}

	private const int depthBufferIndex = 0, cameraTargetIndex = 1, lightingBufferIndex = 2, baseColorBufferIndex = 3, materialDataBufferIndex = 4, depthNormalBufferIndex = 5;

	private void DrawGeometryGBuffer(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
	{
		ExecuteBuffer();
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

		cameraTarget.ConfigureTarget(BuiltinRenderTextureType.CameraTarget, false, true);

		var attchments = new NativeArray<AttachmentDescriptor>(6, Allocator.Temp);
		attchments[depthBufferIndex] = depthBufferTarget;
		attchments[cameraTargetIndex] = cameraTarget;
		attchments[lightingBufferIndex] = lightingBufferTarget;
		attchments[baseColorBufferIndex] = baseColorBufferTarget;
		attchments[materialDataBufferIndex] = materialDataBufferTarget;
		attchments[depthNormalBufferIndex] = depthNormalBufferTarget;

		context.BeginRenderPass(camera.pixelWidth, camera.pixelHeight, 1, attchments, depthBufferIndex);
		attchments.Dispose();

		var gBuffers = new NativeArray<int>(4, Allocator.Temp);
		gBuffers[0] = lightingBufferIndex;
		gBuffers[1] = baseColorBufferIndex;
		gBuffers[2] = materialDataBufferIndex;
		gBuffers[3] = depthNormalBufferIndex;
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
		float far = camera.farClipPlane;
		float fov = camera.fieldOfView;
		float aspec = camera.aspect;
		float h_half = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad) * far;
		float w_half = h_half * aspec;
		Vector4 forward = camera.transform.forward * far;
		Vector4 up = camera.transform.up * h_half;
		Vector4 right = camera.transform.right * w_half;

		Vector4 lu = forward - right + up;
		Vector4 ru = forward + right + up;
		Vector4 lb = forward - right - up;
		Vector4 rb = forward + right - up;
		Matrix4x4 viewPortRays = new Matrix4x4();
		viewPortRays.SetRow(0, lb);
		viewPortRays.SetRow(1, lu);
		viewPortRays.SetRow(2, rb);
		viewPortRays.SetRow(3, ru);

		var shadingOutputs = new NativeArray<int>(1, Allocator.Temp);
		shadingOutputs[0] = lightingBufferIndex;
		var shadingInputs = new NativeArray<int>(2, Allocator.Temp);
		shadingInputs[0] = materialDataBufferIndex;
		shadingInputs[1] = depthNormalBufferIndex;
		context.BeginSubPass(shadingOutputs, shadingInputs, true);
		shadingOutputs.Dispose();
		shadingInputs.Dispose();
		commandBuffer.SetGlobalMatrix(viewPortRaysId, viewPortRays);
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedShadingMaterial, 0, MeshTopology.Triangles, 6);
		ExecuteBuffer();
		context.EndSubPass();
	}

	private void DrawDefferedCombine()
	{
		var combineOutputs = new NativeArray<int>(1, Allocator.Temp);
		combineOutputs[0] = lightingBufferIndex;
		var combineInputs = new NativeArray<int>(1, Allocator.Temp);
		combineInputs[0] = baseColorBufferIndex;
		context.BeginSubPass(combineOutputs, combineInputs, true);
		combineOutputs.Dispose();
		combineInputs.Dispose();
		// BlendMode为 DstColor * srcCol(baseColor) + Zero * dstCol, Zero * srcAlpha + One * dstAlpha(specular intensity)
		// 来将漫反射光累计与baseColor相乘
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedCombineMaterial, 0, MeshTopology.Triangles, 3);
		// BlendMode为 DstAlpha * srcCol(one) + One * dstCol, One * srcAlpha(one) +  Zero * dstAlpha
		// 来加上镜面反射光累计
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedCombineMaterial, 1, MeshTopology.Triangles, 3);
		// 通过这两次混合可以做到在不切换渲染目标的同时完成光累计和baseColor的Combine
		ExecuteBuffer();
		context.EndSubPass();
	}

	private void DrawSkyBoxAndTransparent()
	{
		var renderOutputs = new NativeArray<int>(1, Allocator.Temp);
		renderOutputs[0] = lightingBufferIndex;
		context.BeginSubPass(renderOutputs);
		renderOutputs.Dispose();
		context.DrawSkybox(camera);
		context.EndSubPass();
	}

	private void DrawPostProcess()
	{

	}

	private void RenderToCameraTargetAndTonemapping()
	{
		var tonemappingOutputs = new NativeArray<int>(1, Allocator.Temp);
		tonemappingOutputs[0] = cameraTargetIndex;
		var tonemappingInputs = new NativeArray<int>(1, Allocator.Temp);
		tonemappingInputs[0] = lightingBufferIndex;
		context.BeginSubPass(tonemappingOutputs, tonemappingInputs, true);
		tonemappingOutputs.Dispose();
		tonemappingInputs.Dispose();
		commandBuffer.DrawProcedural(Matrix4x4.identity, TonemappingMaterial, (int)postprocessSettings.toneMappingType, MeshTopology.Triangles, 3);
		ExecuteBuffer();
		context.EndSubPass();
		context.EndRenderPass();
	}

	private void Submit()
	{
		commandBuffer.EndSample(SampleName);
		lights.Cleanup();
		ExecuteBuffer();
		context.Submit();
	}
}
