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

	public Vector4[]
		dirLightColors = new Vector4[Constants.maxDirLightCount],
		dirLightDirections = new Vector4[Constants.maxDirLightCount],
		directionalShadowDatas = new Vector4[Constants.maxDirLightCount],
		pointLightSpheres = new Vector4[Constants.maxPointLightCount],
		pointLightColors = new Vector4[Constants.maxPointLightCount];

	public int pointLightCount;

	private ScriptableRenderContext context;
	private CullingResults cullingResults;

	private Shadows shadows = new Shadows();

	private bool useShadowMask;

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		commandBuffer.BeginSample(bufferName);
		ExecuteCommandBuffer();
		shadows.Setup(context, cullingResults, shadowSettings);

		SetupLights();
        shadows.Render(useShadowMask);
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
		for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
		{
			VisibleLight visibleLight = visibleLights[visibleLightIndex];
			LightBakingOutput lightBaking = visibleLight.light.bakingOutput;
			if (
				lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
				lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask
			)
			{
				useShadowMask = true;
			}
			if (lightBaking.lightmapBakeType == LightmapBakeType.Baked) continue;
			switch (visibleLight.lightType)
			{
				case LightType.Directional:
					if (dirLightCount < Constants.maxDirLightCount)
					{
						SetupDirectionalLight(dirLightCount++, visibleLightIndex, ref visibleLight);
					}
					break;
				case LightType.Point:
					if (pointLightCount < Constants.maxPointLightCount)
					{
						SetupPointLight(pointLightCount++, ref visibleLight);
					}
					break;
			}
			if (dirLightCount >= Constants.maxDirLightCount && pointLightCount >= Constants.maxPointLightCount) break;
		}
		commandBuffer.SetGlobalInt(Constants.dirLightCountId, dirLightCount);
		if (dirLightCount > 0)
		{
			commandBuffer.SetGlobalVectorArray(Constants.dirLightDirectionsId, dirLightDirections);
			commandBuffer.SetGlobalVectorArray(Constants.dirLightColorsId, dirLightColors);
			commandBuffer.SetGlobalVectorArray(Constants.directionalShadowDatasId, directionalShadowDatas);
		}
		commandBuffer.SetGlobalInt(Constants.pointLightCountId, pointLightCount);
		if (pointLightCount > 0)
		{
			commandBuffer.SetGlobalVectorArray(Constants.pointLightSpheresId, pointLightSpheres);
			commandBuffer.SetGlobalVectorArray(Constants.pointLightColorsId, pointLightColors);
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