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

	private const string graphicsCommandBufferName = "Graphics_Render";
#if !UNITY_EDITOR
	private string SampleName = graphicsCommandBufferName;
#endif
	private const string computesCommandBufferName = "Compute_Caclute";
	private CommandBuffer commandBufferGraphics = new CommandBuffer
	{
		name = graphicsCommandBufferName
	};

	private int width, height;
	private const float tileCountX = 32, tileCountY = 16;
	private int clusterZCount;
	private Vector4[] frustumPlanes = new Vector4[6];

	private PostProcessSettings postProcessSettings;
	private DeferredComputeSettings deferredComputeSettings;

	public Lights lights = new Lights();
	private DeferredGraphics graphicsPipline = new DeferredGraphics();
	private TerrainRenderer terrainRenderer = new TerrainRenderer();

	public ComputeBuffer tileLightingIndicesBuffer = new ComputeBuffer(32 * 16 * 256 * 256, sizeof(uint), ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
	public ComputeBuffer tileLightingDatasBuffer = new ComputeBuffer(32 * 16 * 256, sizeof(uint), ComputeBufferType.Structured, ComputeBufferMode.Dynamic);

	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing,
		DeferredComputeSettings deferredComputeSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings,
		TerrainSettings terrainSettings)
	{
		this.context = context;
		this.camera = camera;
		this.postProcessSettings = postprocessSettings;
		this.deferredComputeSettings = deferredComputeSettings;

#if UNITY_EDITOR
		PreparBuffer();
		PrepareForSceneWindow();
#endif

		if (!Cull(shadowSettings.maxShadowDistance)) return;

        SetupForRender();
		terrainRenderer.SetUp(context, commandBufferGraphics, terrainSettings);
		terrainRenderer.GenereateGrassData();
		lights.CollectLightDatas(context, cullingResults, shadowSettings, terrainRenderer);
		ClusterBasedLightCulling();

		commandBufferGraphics.BeginSample(SampleName);
		ExecuteGraphicsCommand();
		lights.Setup();

        width = camera.pixelWidth;
        height = camera.pixelHeight;

		context.SetupCameraProperties(camera);
		ExecuteGraphicsCommand();

		graphicsPipline.Setup(context, cullingResults, commandBufferGraphics, camera,
			terrainRenderer,
            width, height, 1,
            useDynamicBatching, useGPUInstancing,
            postprocessSettings);

		graphicsPipline.Render();

        CleanUp();
		Submit();
    }

	private void ExecuteGraphicsCommand()
	{
		context.ExecuteCommandBuffer(commandBufferGraphics);
		commandBufferGraphics.Clear();
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

		commandBufferGraphics.SetGlobalColor("_FogColor", postProcessSettings.atmoSettings.skyColor);
		commandBufferGraphics.SetGlobalVector("_FogInnerParams", new Vector4(
			1f / postProcessSettings.atmoSettings.innerScatterIntensity,
			postProcessSettings.atmoSettings.innerScatterDensity,
			postProcessSettings.atmoSettings.fogStartDistance
			));
		commandBufferGraphics.SetGlobalVector("_FogOuterParams", new Vector4(
			postProcessSettings.atmoSettings.outerScatterIntensity,
			1f / postProcessSettings.atmoSettings.outerScatterDensity
			));

		float aspec = camera.aspect;
		float h_half;
        if (camera.orthographic || camera.fieldOfView == 0f)
        {
			h_half = camera.orthographicSize;
        }
        else
        {
			float fov = camera.fieldOfView;
			h_half = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
		}
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

		float clusterZNumFUnlog = 1 + 2 * h_half / tileCountY;
		float clusterZNumF = Mathf.Log(clusterZNumFUnlog);
		this.clusterZCount = Mathf.CeilToInt(Mathf.Log(far / near) / clusterZNumF);

		Vector4 tileLRStart = new Vector4(-w_half, -h_half, -1);
		Vector4 tileRVec = new Vector4(w_half * 2f / tileCountX, 0, 0);
		Vector4 tileUVec = new Vector4(0, h_half * 2f / tileCountY, 0);

		commandBufferGraphics.SetGlobalVectorArray(Constants.frustumPlanesId, frustumPlanes);
		commandBufferGraphics.SetGlobalMatrix(Constants.viewPortRaysId, viewPortRays);
		commandBufferGraphics.SetGlobalVector(Constants.tileLBStartId, tileLRStart);
		commandBufferGraphics.SetGlobalVector(Constants.tileRVecId, tileRVec);
		commandBufferGraphics.SetGlobalVector(Constants.tileUVecId, tileUVec);
		commandBufferGraphics.SetGlobalVector(Constants.clusterSizeId, new Vector4(width / tileCountX, height / tileCountY, clusterZNumFUnlog, 1f / clusterZNumF));

		commandBufferGraphics.EnableShaderKeyword(Constants.reflectTypeKeywords[1]);
		Graphics.ExecuteCommandBuffer(commandBufferGraphics);
		commandBufferGraphics.Clear();

		//for (int i = 0; i < Constants.reflectTypeKeywords.Length; ++i)
		//{
		//	if (i == (int)reflectType)
		//	{
		//commandBufferGraphics.EnableShaderKeyword(Constants.reflectTypeKeywords[i]);
		//	}
		//	else
		//	{
		//		commandBufferGraphics.DisableShaderKeyword(Constants.reflectTypeKeywords[i]);
		//	}
		//}
	}

	private void ClusterBasedLightCulling()
	{
		commandBufferGraphics.SetGlobalBuffer(Constants.tileLightingDatasId, tileLightingDatasBuffer);
		commandBufferGraphics.SetGlobalBuffer(Constants.tileLightingIndicesId, tileLightingIndicesBuffer);
		commandBufferGraphics.SetComputeIntParam(deferredComputeSettings.clusterLightingCS, Constants.pointLightCountId, lights.pointLightCount);
		commandBufferGraphics.SetComputeVectorArrayParam(deferredComputeSettings.clusterLightingCS, Constants.pointLightSpheresId, lights.pointLightSpheres);
		commandBufferGraphics.SetComputeMatrixParam(deferredComputeSettings.clusterLightingCS, Constants.bxMatrixVId, camera.worldToCameraMatrix);
		commandBufferGraphics.DispatchCompute(deferredComputeSettings.clusterLightingCS, 0, (int)tileCountX, (int)tileCountY, clusterZCount);
		Graphics.ExecuteCommandBuffer(commandBufferGraphics);
		commandBufferGraphics.Clear();
	}

	private void CleanUp()
	{
		commandBufferGraphics.EndSample(SampleName);
		ExecuteGraphicsCommand();
        lights.Cleanup();
        graphicsPipline.CleanUp();
    }

	private void Submit()
	{
        context.Submit();
	}

	public void OnDispose()
    {
		tileLightingDatasBuffer.Dispose();
		tileLightingIndicesBuffer.Release();
		terrainRenderer.OnDispose();
    }
}