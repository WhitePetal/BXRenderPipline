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
	private CommandBuffer commandBufferCompute = new CommandBuffer
	{
		name = computesCommandBufferName
	};

	private int width, height;

	public Lights lights = new Lights();
	private DeferredGraphics graphicsPipline = new DeferredGraphics();
	private DeferredCompute computePipline = new DeferredCompute();
	public ComputeBuffer tileLightingIndicesBuffer = new ComputeBuffer(2048 * 2048, sizeof(uint));
	public ComputeBuffer tileLightingDatasBuffer = new ComputeBuffer(2048 * 2048 / 256, sizeof(uint));

	public void Render(ScriptableRenderContext context, Camera camera, bool editorMode, bool useDynamicBatching, bool useGPUInstancing,
		DeferredComputeSettings deferredComputeSettings, PostProcessSettings postprocessSettings, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;

#if UNITY_EDITOR
		PreparBuffer();
		PrepareForSceneWindow();
#endif

		if (!Cull(shadowSettings.maxShadowDistance)) return;

		commandBufferGraphics.BeginSample(SampleName);
		ExecuteGraphicsCommand();
		lights.Setup(context, cullingResults, shadowSettings);

        width = camera.pixelWidth;
        height = camera.pixelHeight;

        graphicsPipline.Setup(context, cullingResults, commandBufferGraphics, camera,
            width, height, 1,
            editorMode, useDynamicBatching, useGPUInstancing,
            postprocessSettings);
        SetupForRender();

		commandBufferGraphics.SetGlobalBuffer(Constants.tileLightingDatasId, tileLightingDatasBuffer);
		commandBufferGraphics.SetGlobalBuffer(Constants.tileLightingIndicesId, tileLightingIndicesBuffer);
		graphicsPipline.Render();

        computePipline.Setup(context, commandBufferCompute, camera, lights, deferredComputeSettings,
            width, height, lights.pointLightCount, lights.pointLightSpheres);
		commandBufferCompute.SetGlobalBuffer(Constants.tileLightingDatasId, tileLightingDatasBuffer);
		commandBufferCompute.SetGlobalBuffer(Constants.tileLightingIndicesId, tileLightingIndicesBuffer);
		computePipline.CaculateAftRender();

        CleanUp();
		Submit();
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

		float tileCountX = Mathf.Ceil(width / 16.0f);
		float tileCountY = Mathf.Ceil(height / 16.0f);
		Vector4 tileLRStart = new Vector4(-w_half, -h_half, -1);
		Vector4 tileRVec = new Vector4(w_half * 2f / tileCountX, 0, 0);
		Vector4 tileUVec = new Vector4(0, h_half * 2f / tileCountY, 0);

		commandBufferGraphics.SetGlobalMatrix(Constants.viewPortRaysId, viewPortRays);
		commandBufferGraphics.SetGlobalVector(Constants.tileLBStartId, tileLRStart);
		commandBufferGraphics.SetGlobalVector(Constants.tileRVecId, tileRVec);
		commandBufferGraphics.SetGlobalVector(Constants.tileUVecId, tileUVec);

		commandBufferGraphics.EnableShaderKeyword(Constants.reflectTypeKeywords[1]);

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
		context.SetupCameraProperties(camera);
		ExecuteGraphicsCommand();
	}

	private void CleanUp()
	{
		commandBufferGraphics.EndSample(SampleName);
		ExecuteGraphicsCommand();
        lights.Cleanup();
        graphicsPipline.CleanUp();
        computePipline.CleanUp();
    }

	private void Submit()
	{
        context.Submit();
	}

	//public void Dispose()
	//{
	//	computePipline.Dispose();
 //       commandBufferGraphics.Dispose();
	//	commandBufferCompute.Dispose();
	//}
}