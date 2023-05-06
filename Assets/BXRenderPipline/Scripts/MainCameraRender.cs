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

#if UNITY_EDITOR
		PreparBuffer();
		PrepareForSceneWindow();
#endif

		if (!Cull(shadowSettings.maxShadowDistance)) return;

		commandBufferGraphics.BeginSample(SampleName);
		ExecuteGraphicsCommand();
        SetupForRender();
		terrainRenderer.SetUp(context, commandBufferGraphics, terrainSettings);
		terrainRenderer.GenereateGrassData();
		lights.Setup(context, cullingResults, shadowSettings, terrainRenderer);

        width = camera.pixelWidth;
        height = camera.pixelHeight;

		context.SetupCameraProperties(camera);
		ExecuteGraphicsCommand();

		graphicsPipline.Setup(context, cullingResults, commandBufferGraphics, camera,
			deferredComputeSettings, lights, terrainRenderer,
            width, height, 1,
            useDynamicBatching, useGPUInstancing,
            postprocessSettings);

		commandBufferGraphics.SetGlobalBuffer(Constants.tileLightingDatasId, tileLightingDatasBuffer);
		commandBufferGraphics.SetGlobalBuffer(Constants.tileLightingIndicesId, tileLightingIndicesBuffer);
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
		commandBufferGraphics.EnableShaderKeyword(Constants.reflectTypeKeywords[1]);

		ExecuteGraphicsCommand();

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