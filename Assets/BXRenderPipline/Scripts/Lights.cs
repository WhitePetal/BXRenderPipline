using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lights
{
	private const string bufferName = "Lights";
	private CommandBuffer commandBuffer = new CommandBuffer()
	{
		name = bufferName
	};

	private const int maxDirLightCount = 4;
	private const int maxPointLightCount = 256;

	private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	private static int directionalShadowDatasId = Shader.PropertyToID("_DirectionalShadowDatas");
	private static int pointLightCountId = Shader.PropertyToID("_PointLightCount");
	private static int pointLightSpheresId = Shader.PropertyToID("_PointLightSpheres");
	private static int pointLightColorsId = Shader.PropertyToID("_PointLightColors");
	private static int tileLightingIndicesId = Shader.PropertyToID("_TileLightingIndices");
	private static int tileLightingDatasId = Shader.PropertyToID("_TileLightingDatas");

	private static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount],
		directionalShadowDatas = new Vector4[maxDirLightCount],
		pointLightSpheres = new Vector4[maxPointLightCount],
		pointLightColors = new Vector4[maxPointLightCount];

	private static int tileLightDataCount = 0;
	public static ComputeBuffer tileLightingIndicesBuffer;
	public static ComputeBuffer tileLightingDatasBuffer;

	public int pointLightCount;

	private ScriptableRenderContext context;
	private CullingResults cullingResults;

	private Shadows shadows = new Shadows();

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		commandBuffer.BeginSample(bufferName);
		shadows.Setup(context, cullingResults, shadowSettings);

		int tileLightDataCountNow = Screen.width * Screen.height;
		if (tileLightDataCountNow != tileLightDataCount)
		{
			tileLightDataCount = tileLightDataCountNow;
			if (tileLightingIndicesBuffer != null) tileLightingIndicesBuffer.Release();
			if (tileLightingDatasBuffer != null) tileLightingDatasBuffer.Release();
			tileLightingIndicesBuffer = new ComputeBuffer(tileLightDataCount, sizeof(uint), ComputeBufferType.Constant);
			tileLightingDatasBuffer = new ComputeBuffer(tileLightDataCount / 256, sizeof(uint), ComputeBufferType.Constant);
		}

		SetupLights();
		shadows.Render();
		commandBuffer.EndSample(bufferName);
		ExecuteCommandBuffer();
	}

	private void ExecuteCommandBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}

	private void SetupLights()
	{
		NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
		int dirLightCount = 0;
		int pointLightCount = 0;
		for(int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
		{
			VisibleLight visibleLight = visibleLights[visibleLightIndex];
			switch (visibleLight.lightType)
			{
				case LightType.Directional:
					if (dirLightCount < maxDirLightCount)
					{
						SetupDirectionalLight(dirLightCount++, visibleLightIndex, ref visibleLight);
					}
					break;
				case LightType.Point:
					if(pointLightCount < maxPointLightCount)
					{
						SetupPointLight(pointLightCount++, ref visibleLight);
					}
					break;
			}
			if (dirLightCount >= maxDirLightCount && pointLightCount >= maxPointLightCount) break;
		}
		commandBuffer.SetGlobalInt(dirLightCountId, dirLightCount);
		if(dirLightCount > 0)
		{
			commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
			commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
			commandBuffer.SetGlobalVectorArray(directionalShadowDatasId, directionalShadowDatas);
		}
		commandBuffer.SetGlobalInt(pointLightCountId, pointLightCount);
		if(pointLightCount > 0)
		{
			commandBuffer.SetGlobalVectorArray(pointLightSpheresId, pointLightSpheres);
			commandBuffer.SetGlobalVectorArray(pointLightColorsId, pointLightColors);
			commandBuffer.SetGlobalBuffer(tileLightingIndicesId, tileLightingIndicesBuffer);
			commandBuffer.SetGlobalBuffer(tileLightingDatasId, tileLightingDatasBuffer);
		}
		this.pointLightCount = pointLightCount;
	}
	private void SetupDirectionalLight(int dirLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
	{
		dirLightColors[dirLightIndex] = visibleLight.finalColor;
		dirLightDirections[dirLightIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
		directionalShadowDatas[dirLightIndex] = shadows.SaveDirectionalShadows(visibleLight.light, visibleLightIndex);
	}

	private void SetupPointLight(int pointLightIndex, ref VisibleLight visibleLight)
	{
		Vector4 lightSphere = visibleLight.localToWorldMatrix.GetColumn(3);
		lightSphere.w = visibleLight.range;
		pointLightSpheres[pointLightIndex] = lightSphere;
		pointLightColors[pointLightIndex] = visibleLight.finalColor;
	}

	public void Cleanup()
	{
		shadows.Cleanup();
	}
}
