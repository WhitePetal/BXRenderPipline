using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
	private struct ShadowedDirectionalLight
	{
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float nearPlaneOffset;
	}

	private const string bufferName = "Shadows";
	private CommandBuffer commandBuffer = new CommandBuffer()
	{
		name = bufferName
	};

	private static string[] directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};
	private static string[] cascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};
	private static string[] shadowMaskKeywords = {
		"_SHADOW_MASK_ALWAYS",
		"_SHADOW_MASK_DISTANCE"
	};

	private static int shadowsColorId = Shader.PropertyToID("_BXShadowsColor");
	private static int shadowMapSizeId = Shader.PropertyToID("_ShadowMapSize");
	private static int shadowsDistanceFadeId = Shader.PropertyToID("_ShadowsDistanceFade");
	private static int directionalShadowMapId = Shader.PropertyToID("_DirectionalShadowMap");
	private static int directionalShadowMatrixsId = Shader.PropertyToID("_DirectionalShadowMatrixs");
	private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
	private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	private static int cascadeDatasId = Shader.PropertyToID("_CascadeDatas");
	private static RenderTargetIdentifier directionalShadowMapTargetId = new RenderTargetIdentifier(directionalShadowMapId);

	private ScriptableRenderContext context;
	private CullingResults cullingResults;
	private ShadowSettings shadowSettings;

	private const int maxShadowedDirectionalLightCount = 4, maxCascadeCount = 4;

	private Vector3 CascadeRatios => new Vector3(shadowSettings.cascadeRatio1, shadowSettings.cascadeRatio2, shadowSettings.cascadeRatio3);
	private ShadowedDirectionalLight[] shadowDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
	private Matrix4x4[] directionalShadowMatrixs = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascadeCount];
	private Vector4[] cascadeCullingSpheres = new Vector4[maxCascadeCount];
	private Vector4[] cascadeDatas = new Vector4[maxCascadeCount];

	private int shadowedDirectionalLightCount;

	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.cullingResults = cullingResults;
		this.shadowSettings = shadowSettings;
		this.shadowedDirectionalLightCount = 0;
        commandBuffer.BeginSample(bufferName);
		ExecuteCommandBuffer();
	}

	private void ExecuteCommandBuffer()
	{
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}

	public Vector4 SaveDirectionalShadows(Light light, int visibleLightIndex)
	{
		Vector2 shadowData;
		if (shadowedDirectionalLightCount < maxShadowedDirectionalLightCount
			&& light.shadows != LightShadows.None && light.shadowStrength > 0f &&
			cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bound))
		{
			shadowDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight()
			{
				visibleLightIndex = visibleLightIndex,
				slopeScaleBias = light.shadowBias,
				nearPlaneOffset = light.shadowNearPlane
			};
			shadowData = new Vector4(light.shadowStrength, shadowSettings.cascadeCount * shadowedDirectionalLightCount++, light.shadowNormalBias);
		}
		else
		{
			shadowData = Vector4.zero;
		}
		return shadowData;
	}

	public void Render(bool useShadowMask)
	{
		SetKeywords(shadowMaskKeywords, useShadowMask ?
			QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 :
			-1);
		commandBuffer.SetGlobalColor(shadowsColorId, shadowSettings.shadowsColor.linear);
		if (shadowedDirectionalLightCount > 0)
		{
			SetKeywords(directionalFilterKeywords, (int)shadowSettings.shadowFilter - 1);
			SetKeywords(cascadeBlendKeywords, (int)shadowSettings.cascadeBlendMode - 1);
			RenderDirectionalShadows();
		}
		else
		{
			commandBuffer.GetTemporaryRT(directionalShadowMapId, 1, 1, (int)shadowSettings.shadowMapBits, FilterMode.Bilinear, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);
		}

		float f = 1.0f - shadowSettings.cascadeFade;
		commandBuffer.SetGlobalVector(shadowsDistanceFadeId, new Vector4(1.0f / shadowSettings.maxShadowDistance, 1.0f / shadowSettings.distanceFade,
			1.0f / (1.0f - f * f)));
		commandBuffer.SetGlobalMatrixArray(directionalShadowMatrixsId, directionalShadowMatrixs);
		commandBuffer.SetGlobalInt(cascadeCountId, shadowSettings.cascadeCount);
		commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
		commandBuffer.SetGlobalVectorArray(cascadeDatasId, cascadeDatas);
		commandBuffer.EndSample(bufferName);
        ExecuteCommandBuffer();
	}

	private void SetKeywords(string[] keywords, int enabledIndex)
	{
		for (int i = 0; i < directionalFilterKeywords.Length; i++)
		{
			if (i == enabledIndex)
			{
				commandBuffer.EnableShaderKeyword(directionalFilterKeywords[i]);
			}
			else
			{
				commandBuffer.DisableShaderKeyword(directionalFilterKeywords[i]);
			}
		}
	}

	private void RenderDirectionalShadows()
	{
		int shadowMapSize = (int)shadowSettings.shadowMapSize;
		commandBuffer.GetTemporaryRT(directionalShadowMapId, shadowMapSize, shadowMapSize, (int)shadowSettings.shadowMapBits, FilterMode.Bilinear, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);
		commandBuffer.SetRenderTarget(directionalShadowMapTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		commandBuffer.ClearRenderTarget(true, false, Color.clear);

		commandBuffer.SetGlobalVector(shadowMapSizeId, new Vector4(shadowMapSize, 1.0f / shadowMapSize));

		int cascadeCount = shadowSettings.cascadeCount;
		Vector3 cascadeRatios = CascadeRatios;
		int tileCount = shadowedDirectionalLightCount * cascadeCount;
		int split = tileCount <= 1 ? 1 : tileCount <= 4 ? 2 : 4;
		int tileSize = shadowMapSize / split;
		float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.cascadeFade);
		for (int i = 0; i < shadowedDirectionalLightCount; ++i)
		{
			int tileIndexOffset = i * cascadeCount;
			ShadowedDirectionalLight light = shadowDirectionalLights[i];
			for (int cascadeIndex = 0; cascadeIndex < cascadeCount; ++cascadeIndex)
			{
				ShadowDrawingSettings dirShadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
				cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, cascadeIndex, cascadeCount, cascadeRatios, tileSize, light.nearPlaneOffset,
					out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
				shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
				dirShadowSettings.splitData = shadowSplitData;
				if (i == 0) // 所有方向光的级联都使用同一个级联球体，因此我们只需要存储第一个光源的即可
				{
					SetCascadeData(cascadeIndex, shadowSplitData.cullingSphere, tileSize);
				}
				int tileInex = cascadeIndex + tileIndexOffset;
				directionalShadowMatrixs[tileInex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, SetTileViewport(tileInex, split, tileSize), split);
				commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
				commandBuffer.SetGlobalDepthBias(1f, 2.5f + light.slopeScaleBias);
				ExecuteCommandBuffer();
				context.DrawShadows(ref dirShadowSettings);
				commandBuffer.SetGlobalDepthBias(0f, 0f);
			}
		}
		ExecuteCommandBuffer();
	}

	private Vector2 SetTileViewport(int index, int split, int tileSize)
	{
		Vector2 offset = new Vector2(index % split, index / split);
		commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
		return offset;
	}

	private Matrix4x4 ConvertToShadowMapTileMatrix(Matrix4x4 m, Vector2 offset, int split)
	{
		if (SystemInfo.usesReversedZBuffer)
		{
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
		return m;
	}

	private void SetCascadeData(int cascadeIndex, Vector4 cullingSphere, int tileSize)
	{
		float texelSize = 2.0f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)shadowSettings.shadowFilter + 1f);
		cascadeDatas[cascadeIndex] = new Vector4(1.0f / cullingSphere.w, filterSize * 1.4142136f);
		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		cascadeCullingSpheres[cascadeIndex] = cullingSphere;
	}

	public void Cleanup()
	{
		commandBuffer.ReleaseTemporaryRT(directionalShadowMapId);
		ExecuteCommandBuffer();
	}
}