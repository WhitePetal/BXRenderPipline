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

	private const string graphicsCommandBufferName = "Graphics Render";
#if !UNITY_EDITOR
	private string SampleName = graphicsCommandBufferName;
#endif
	private const string computesCommandBufferName = "Compute Caclute";
	private CommandBuffer commandBufferGraphics = new CommandBuffer
	{
		name = graphicsCommandBufferName
	};
	private CommandBuffer commandBufferCompute = new CommandBuffer
	{
		name = computesCommandBufferName
	};

	private int width, height;

	public Lights lights = new Lights();
	private PostProcess postProcess = new PostProcess();
	private DeferredGraphics graphicsPipline = new DeferredGraphics();
	private DeferredCompute computePipline = new DeferredCompute();

	private DeferredComputeSettings deferredComputeSettings;

	private Matrix4x4 viewPortRays = Matrix4x4.identity;

	private GraphicsFence graphicsPiplineCompeletFence;

	public void Render(ScriptableRenderContext context, Camera camera, bool editorMode, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, 
		DefferedShadingSettings defferedShadingSettings, DeferredComputeSettings deferredComputeSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;
		this.deferredComputeSettings = deferredComputeSettings;

#if UNITY_EDITOR
		PreparBuffer();
		PrepareForSceneWindow();
#endif

		if (!Cull(shadowSettings.maxShadowDistance)) return;

		commandBufferGraphics.BeginSample(SampleName);
		ExecuteGraphicsCommand();
		lights.Setup(context, cullingResults, shadowSettings);
		commandBufferGraphics.EndSample(SampleName);

		width = camera.pixelWidth;
		height = camera.pixelHeight;
		int aa = 1;
		RenderTargetIdentifier cameraTargetId = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
		if(camera.targetTexture != null)
		{
			width = camera.targetTexture.width;
			height = camera.targetTexture.height;
			aa = camera.targetTexture.antiAliasing;
			cameraTargetId = camera.targetTexture;
		}

		graphicsPipline.Setup(context, cullingResults, commandBufferGraphics, camera,
			width, height, aa, cameraTargetId,
			editorMode, useDynamicBatching, useGPUInstancing, useLightsPerObject,
			defferedShadingSettings, postprocessSettings);
		computePipline.Setup(context, commandBufferCompute, camera, deferredComputeSettings, 
			width, height, lights.pointLightCount, lights.pointLightSpheres);
		SetupForRender();

//#if UNITY_EDITOR
//		if (editorMode)
//		{
//			ShadingInEditorMode();
//		}
//		else
//		{
//			if (camera.cameraType == CameraType.Game)
//				ShadingInPlayerMode();
//		}
//#else
		ShadingInPlayerMode();
//#endif
	}

	private void ExecuteGraphicsCommand()
	{
		context.ExecuteCommandBuffer(commandBufferGraphics);
		commandBufferGraphics.Clear();
	}

	private void ExecuteComputeCommand()
	{
		context.ExecuteCommandBuffer(commandBufferCompute);
		commandBufferCompute.Clear();
	}

	private void ShadingInPlayerMode()
	{
		graphicsPipline.Render(out GraphicsFence ssrFence);
		computePipline.CaculateAftRender(ssrFence);
		CleanUp();
		Submit();
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
		float fov = camera.fieldOfView;
		float aspec = camera.aspect;
		float h_half = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
		float w_half = h_half * aspec;
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

		float tileCountX = width / 32.0f;
		float tileCountY = height / 32.0f;
		Vector4 tileLRStart = new Vector4(-w_half, -h_half, -1);
		Vector4 tileRVec = new Vector4(w_half / tileCountX, 0, 0);
		Vector4 tileUVec = new Vector4(0, h_half / tileCountY, 0);

		commandBufferGraphics.SetGlobalMatrix(Constants.viewPortRaysId, viewPortRays);
		commandBufferGraphics.SetGlobalVector(Constants.tileLBStartId, tileLRStart);
		commandBufferGraphics.SetGlobalVector(Constants.tileRVecId, tileRVec);
		commandBufferGraphics.SetGlobalVector(Constants.tileUVecId, tileUVec);

		context.SetupCameraProperties(camera);
		ExecuteGraphicsCommand();
	}

	private void CleanUp()
	{
		graphicsPipline.CleanUp();
		computePipline.CleanUp();
	}

	private void Submit()
	{
		commandBufferGraphics.EndSample(SampleName);
		lights.Cleanup();
		ExecuteGraphicsCommand();
		context.Submit();
	}

	public void Dispose()
	{
		computePipline.Dispose();
		commandBufferGraphics.Dispose();
		commandBufferCompute.Dispose();
	}
}
