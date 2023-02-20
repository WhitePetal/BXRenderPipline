using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DeferredCompute
{
	private Camera camera;
	private CommandBuffer commandBuffer;
	private ScriptableRenderContext context;
	private DeferredComputeSettings settings;

	private int pointLightCount;
	private Vector4[] pointLightSpheres;
	private int width, height;
	private ReflectType reflectType;

	public ComputeBuffer tileLightingIndicesBuffer = new ComputeBuffer(2048 * 2048, sizeof(uint));
	public ComputeBuffer tileLightingDatasBuffer = new ComputeBuffer(2048 * 2048 / 256, sizeof(uint));


	public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, 
		ReflectType reflectType,
		DeferredComputeSettings deferredComputeSettings,
		int width, int height, int pointLightCount, Vector4[] pointLightSpheres)
	{
		this.context = context;
		this.camera = camera;
		this.commandBuffer = commandBuffer;
		this.settings = deferredComputeSettings;
		this.pointLightCount = pointLightCount;
		this.pointLightSpheres = pointLightSpheres;
		this.width = width;
		this.height = height;
		this.reflectType = reflectType;
	}

	public void CaculateBefRender()
	{

	}

	public void CaculateAftRender(in GraphicsFence graphicsFence)
	{
		commandBuffer.WaitOnAsyncGraphicsFence(graphicsFence);
		if(reflectType != ReflectType.OnlyProbe)
		{
			GenerateSSRBuffer();
		}
		GenerateTileLightingData();
	}

	public void CleanUp()
	{
	}

	public void Dispose()
	{
		if(tileLightingDatasBuffer != null) tileLightingDatasBuffer.Dispose();
		if(tileLightingIndicesBuffer != null) tileLightingIndicesBuffer.Dispose();
	}

	private void GenerateSSRBuffer()
	{
		commandBuffer.BeginSample("GenerateSSR");
		commandBuffer.SetComputeTextureParam(settings.ssrGenerateCS, 0, "_SSRBufferMip1", Constants.ssrBufferTargetId, 1);
		commandBuffer.SetComputeTextureParam(settings.ssrGenerateCS, 0, "_SSRBufferMip2", Constants.ssrBufferTargetId, 2);
		commandBuffer.SetComputeTextureParam(settings.ssrGenerateCS, 0, "_SSRBufferMip3", Constants.ssrBufferTargetId, 3);
		int w = width >> 2;
		int h = height >> 2;
		commandBuffer.DispatchCompute(settings.ssrGenerateCS, 0, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);
		ExecuteBuffer();
		commandBuffer.EndSample("GenerateSSR");
	}

	private void GenerateTileLightingData()
	{
		if (pointLightCount == 0) return;
		commandBuffer.BeginSample("TileLightingData");
		commandBuffer.SetGlobalBuffer(Constants.tileLightingDatasId, tileLightingDatasBuffer);
		commandBuffer.SetGlobalBuffer(Constants.tileLightingIndicesId, tileLightingIndicesBuffer);
		commandBuffer.SetComputeTextureParam(settings.tileLightingCS, 0, Constants.depthNormalBufferId, Constants.depthNormalBufferTargetId);
		commandBuffer.DispatchCompute(settings.tileLightingCS, 0, Mathf.CeilToInt(width / 16f), Mathf.CeilToInt(height / 16f), 1);
		commandBuffer.EndSample("TileLightingData");
		ExecuteBuffer();
	}

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}
