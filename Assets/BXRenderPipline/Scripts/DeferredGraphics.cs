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
	private PostProcessSettings postProcessSettings;
	private Lights lights;
	private TerrainRenderer terrainRenderer;

	private PostProcess postProcess = new PostProcess();

	private bool useDynamicBatching, useGPUInstancing;
	private int width, height, aa;
	private int clusterZCount;

#if UNITY_EDITOR
	private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

	private Vector4[] frustumPlanes = new Vector4[6];

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, CommandBuffer commandBuffer, Camera camera,
		DeferredComputeSettings deferredComputeSettings, Lights lights, TerrainRenderer terrainRenderer,
		int width, int height, int aa,
		 bool useDynamicBatching, bool useGPUInstancing,
		 PostProcessSettings postProcessSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.commandBuffer = commandBuffer;
		this.camera = camera;
		this.deferredComputeSettings = deferredComputeSettings;
		this.postProcessSettings = postProcessSettings;
		this.lights = lights;
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

		SetFogData();
		ClusterBasedLightCulling();
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

	private void SetFogData()
    {
		commandBuffer.SetGlobalColor("_FogColor", postProcessSettings.atmoSettings.skyColor);
		commandBuffer.SetGlobalVector("_FogInnerParams", new Vector4(
			1f / postProcessSettings.atmoSettings.innerScatterIntensity,
			postProcessSettings.atmoSettings.innerScatterDensity,
			postProcessSettings.atmoSettings.fogStartDistance
			));
		commandBuffer.SetGlobalVector("_FogOuterParams", new Vector4(
			postProcessSettings.atmoSettings.outerScatterIntensity,
			1f / postProcessSettings.atmoSettings.outerScatterDensity
			));
	}

	private void ClusterBasedLightCulling()
    {
		float fov = camera.fieldOfView;
		float aspec = camera.aspect;
		float h_half = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
		float w_half = h_half * aspec;
		float near = camera.nearClipPlane;
		float far = camera.farClipPlane;

		Matrix4x4 viewPortRays = Matrix4x4.identity;
		Vector4 forward = camera.transform.forward;
		Vector4 up = camera.transform.up * h_half;
		Vector4 right = camera.transform.right * w_half;

		Vector4 lb = forward - right - up;
		Vector4 lu = forward - right + up;
		Vector4 rb = forward + right - up;
		Vector4 ru = forward + right + up;
		viewPortRays.SetRow(0, lb);
		viewPortRays.SetRow(1, lu);
		viewPortRays.SetRow(2, rb);
		viewPortRays.SetRow(3, ru);

		Vector3 nl = Vector3.Cross(lu, lb).normalized;
		Vector3 nr = Vector3.Cross(rb, ru).normalized;
		Vector3 nu = Vector3.Cross(ru, lu).normalized;
		Vector3 nd = Vector3.Cross(lb, rb).normalized;

		Vector4 camPos = camera.transform.position;

		frustumPlanes[0] = new Vector4(forward.x, forward.y, forward.z, -Vector3.Dot(camPos + forward * near, forward));
		frustumPlanes[1] = new Vector4(nl.x, nl.y, nl.z, -Vector3.Dot(camPos, nl));
		frustumPlanes[2] = new Vector4(nr.x, nr.y, nr.z, -Vector3.Dot(camPos, nr));
		frustumPlanes[3] = new Vector4(nd.x, nd.y, nd.z, -Vector3.Dot(camPos, nd));
		frustumPlanes[4] = new Vector4(nu.x, nu.y, nu.z, -Vector3.Dot(camPos, nu));
		frustumPlanes[5] = new Vector4(-forward.x, -forward.y, -forward.z, Vector3.Dot(camPos + forward * far, forward));

		float tileCountX = 32;
		float tileCountY = 16;
		float clusterZNumFUnlog = 1 + 2 * h_half / tileCountY;
		float clusterZNumF = Mathf.Log(clusterZNumFUnlog);
		this.clusterZCount = Mathf.CeilToInt(Mathf.Log(far / near) / clusterZNumF);

		Vector4 tileLRStart = new Vector4(-w_half, -h_half, -1);
		Vector4 tileRVec = new Vector4(w_half * 2f / tileCountX, 0, 0);
		Vector4 tileUVec = new Vector4(0, h_half * 2f / tileCountY, 0);

		commandBuffer.SetGlobalVectorArray(Constants.frustumPlanesId, frustumPlanes);
		commandBuffer.SetGlobalMatrix(Constants.viewPortRaysId, viewPortRays);
		commandBuffer.SetGlobalVector(Constants.tileLBStartId, tileLRStart);
		commandBuffer.SetGlobalVector(Constants.tileRVecId, tileRVec);
		commandBuffer.SetGlobalVector(Constants.tileUVecId, tileUVec);
		commandBuffer.SetGlobalVector(Constants.clusterSizeId, new Vector4(width / tileCountX, height / tileCountY, clusterZNumFUnlog, 1f / clusterZNumF));

		commandBuffer.SetComputeIntParam(deferredComputeSettings.clusterLightingCS, Constants.pointLightCountId, lights.pointLightCount);
		commandBuffer.SetComputeVectorArrayParam(deferredComputeSettings.clusterLightingCS, Constants.pointLightSpheresId, lights.pointLightSpheres);
		commandBuffer.SetComputeMatrixParam(deferredComputeSettings.clusterLightingCS, Constants.bxMatrixVId, camera.worldToCameraMatrix);
		commandBuffer.DispatchCompute(deferredComputeSettings.clusterLightingCS, 0, (int)tileCountX, (int)tileCountY, clusterZCount);
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