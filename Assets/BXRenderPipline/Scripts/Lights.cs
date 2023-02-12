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
	private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	private static int directionalShadowDatasId = Shader.PropertyToID("_DirectionalShadowDatas");

	private static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount],
		directionalShadowDatas = new Vector4[maxDirLightCount];

	private ScriptableRenderContext context;
	private CullingResults cullingResults;

	private Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		commandBuffer.BeginSample(bufferName);
		shadows.Setup(context, cullingResults, shadowSettings);
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
		for(int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
		{
			VisibleLight visibleLight = visibleLights[visibleLightIndex];
			if(visibleLight.lightType == LightType.Directional)
			{
				SetupDirectionalLight(dirLightCount++, visibleLightIndex, ref visibleLight);
				if (dirLightCount >= maxDirLightCount)
				{
					break;
				}
			}
		}
		commandBuffer.SetGlobalInt(dirLightCountId, dirLightCount);
		commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
		commandBuffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
		commandBuffer.SetGlobalVectorArray(directionalShadowDatasId, directionalShadowDatas);
	}
	private void SetupDirectionalLight(int dirLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
	{
		dirLightColors[dirLightIndex] = visibleLight.finalColor;
		dirLightDirections[dirLightIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
		directionalShadowDatas[dirLightIndex] = shadows.SaveDirectionalShadows(visibleLight.light, visibleLightIndex);
	}

	public void Cleanup()
	{
		shadows.Cleanup();
	}
}
