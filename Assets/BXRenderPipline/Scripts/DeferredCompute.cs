using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DeferredCompute
{
	private Camera camera;
	private Lights lights;
	private CommandBuffer commandBuffer;
	private ScriptableRenderContext context;
	private DeferredComputeSettings settings;

	private int pointLightCount;
	private Vector4[] pointLightSpheres;
	private int width, height;

	public void Setup(ScriptableRenderContext context, CommandBuffer commandBuffer, Camera camera, Lights lights,
		DeferredComputeSettings deferredComputeSettings,
		int width, int height, int pointLightCount, Vector4[] pointLightSpheres)
	{
		this.context = context;
		this.camera = camera;
		this.lights = lights;
		this.commandBuffer = commandBuffer;
		this.settings = deferredComputeSettings;
		this.pointLightCount = pointLightCount;
		this.pointLightSpheres = pointLightSpheres;
		this.width = width;
		this.height = height;
	}

	public void CaculateBefRender()
	{

	}

	public void CaculateAftRender()
	{
		//if (reflectType != ReflectType.OnlyProbe)
		//{
		//	GenerateSSRBuffer();
		//}
		GenerateTileLightingData();
	}

	public void CleanUp()
	{
		//this.tileLightingIndicesBuffer.Release();
		//this.tileLightingDatasBuffer.Release();
	}

	//public void Dispose()
	//{
	//	if (tileLightingDatasBuffer != null) tileLightingDatasBuffer.Dispose();
	//	if (tileLightingIndicesBuffer != null) tileLightingIndicesBuffer.Dispose();
	//}

	private void GenerateSSRBuffer()
	{
		commandBuffer.BeginSample("GenerateSSR");
		ExecuteBuffer();
		commandBuffer.SetComputeTextureParam(settings.ssrGenerateCS, 0, "_SSRBufferMip1", Constants.ssrBufferTargetId, 1);
		commandBuffer.SetComputeTextureParam(settings.ssrGenerateCS, 0, "_SSRBufferMip2", Constants.ssrBufferTargetId, 2);
		commandBuffer.SetComputeTextureParam(settings.ssrGenerateCS, 0, "_SSRBufferMip3", Constants.ssrBufferTargetId, 3);
		int w = width >> 2;
		int h = height >> 2;
		commandBuffer.DispatchCompute(settings.ssrGenerateCS, 0, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);
		commandBuffer.EndSample("GenerateSSR");
		ExecuteBuffer();
	}

	private void GenerateTileLightingData()
	{
		if (pointLightCount == 0) return;
		commandBuffer.BeginSample(commandBuffer.name);
		ExecuteBuffer();
		commandBuffer.SetComputeIntParam(settings.tileLightingCS, "_PointLightCount", lights.pointLightCount);
		commandBuffer.SetComputeVectorArrayParam(settings.tileLightingCS, "_PointLightSpheres", lights.pointLightSpheres);
		commandBuffer.SetComputeMatrixParam(settings.tileLightingCS, "BX_MatrixV", camera.worldToCameraMatrix);
		commandBuffer.SetComputeTextureParam(settings.tileLightingCS, 0, Constants.depthNormalBufferId, Constants.depthNormalBufferTargetId);
		commandBuffer.DispatchCompute(settings.tileLightingCS, 0, Mathf.CeilToInt(width / 16f), Mathf.CeilToInt(height / 16f), 1);
		commandBuffer.EndSample(commandBuffer.name);
		ExecuteBuffer();
	}

	private void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}
}