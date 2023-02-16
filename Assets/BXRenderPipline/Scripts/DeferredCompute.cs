using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DeferredCompute
{
	private CommandBuffer commandBuffer;
	private ScriptableRenderContext context;
	private DeferredComputeSettings deferredComputeSettings;

	private int pointLightCount;
	private Vector4[] pointLightSpheres;
	private int width, height;

	public ComputeBuffer tileLightingIndicesBuffer = new ComputeBuffer(2048 * 2048, sizeof(uint));
	public ComputeBuffer tileLightingDatasBuffer = new ComputeBuffer(2048 * 2048 / 256, sizeof(uint));


	public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, DeferredComputeSettings deferredComputeSettings,
		int width, int height, int pointLightCount, Vector4[] pointLightSpheres)
	{
		this.context = context;
		this.commandBuffer = commandBuffer;
		this.deferredComputeSettings = deferredComputeSettings;
		this.pointLightCount = pointLightCount;
		this.pointLightSpheres = pointLightSpheres;
		this.width = width;
		this.height = height;
	}

	public void CaculateBefRender()
	{

	}

	public void CaculateAftRender(in GraphicsFence graphicsPiplineCompeletFence)
	{
		GenerateTileLightingData(in graphicsPiplineCompeletFence);
	}

	public void CleanUp()
	{
	}

	public void Dispose()
	{
		if(tileLightingDatasBuffer != null) tileLightingDatasBuffer.Dispose();
		if(tileLightingIndicesBuffer != null) tileLightingIndicesBuffer.Dispose();
	}

	private void GenerateTileLightingData(in GraphicsFence graphicsPiplineCompeletFence)
	{
		if (pointLightCount == 0) return;
		commandBuffer.WaitOnAsyncGraphicsFence(graphicsPiplineCompeletFence);
		commandBuffer.BeginSample("TileLightingData");
		commandBuffer.SetGlobalBuffer(Constants.tileLightingDatasId, tileLightingDatasBuffer);
		commandBuffer.SetGlobalBuffer(Constants.tileLightingIndicesId, tileLightingIndicesBuffer);
		commandBuffer.SetComputeTextureParam(deferredComputeSettings.tileLightingCS, 0, Constants.depthNormalBufferId, Constants.depthNormalBufferTargetId);
		commandBuffer.DispatchCompute(deferredComputeSettings.tileLightingCS, 0, Mathf.CeilToInt(width / 16f), Mathf.CeilToInt(height / 16f), 1);
		commandBuffer.EndSample("TileLightingData");
		ExecuteBuffer();
	}

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}
