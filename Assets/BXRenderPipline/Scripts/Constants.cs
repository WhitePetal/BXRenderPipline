using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Constants
{
	public const int depthBufferIndex = 0, lightingBufferIndex = 1, baseColorBufferIndex = 2, materialDataBufferIndex = 3, depthNormalBufferIndex = 4,
		ssrBufferIndex = 5;
	public const int fxaaInputBufferIndex = 0, cameraTargetBufferIndex = 1;

	public static int depthBufferId = Shader.PropertyToID("_DepthBuffer");
	public static int lightingBufferId = Shader.PropertyToID("_LightingBuffer");
	public static int baseColorBufferId = Shader.PropertyToID("_BaseColorBuffer");
	public static int materialDataBufferId = Shader.PropertyToID("_MaterialDataBuffer");
	public static int depthNormalBufferId = Shader.PropertyToID("_BXDepthNormalBuffer");
	public static int fxaaInputBufferId = Shader.PropertyToID("_FXAAInputBuffer");
	public static int ssrBufferId = Shader.PropertyToID("_SSRBuffer");
	public static RenderTargetIdentifier depthBufferTargetId = new RenderTargetIdentifier(depthBufferId);
	public static RenderTargetIdentifier lightingBufferTargetId = new RenderTargetIdentifier(lightingBufferId);
	public static RenderTargetIdentifier baseColorBufferTargetId = new RenderTargetIdentifier(baseColorBufferId);
	public static RenderTargetIdentifier materialDataBufferTargetId = new RenderTargetIdentifier(materialDataBufferId);
	public static RenderTargetIdentifier depthNormalBufferTargetId = new RenderTargetIdentifier(depthNormalBufferId);
	public static RenderTargetIdentifier fxaaInputBufferTargetId = new RenderTargetIdentifier(fxaaInputBufferId);
	public static RenderTargetIdentifier ssrBufferTargetId = new RenderTargetIdentifier(ssrBufferId);
	public static RenderTargetIdentifier[] defferedShadingTargestsId = new RenderTargetIdentifier[4]
	{
		lightingBufferTargetId,
		baseColorBufferTargetId,
		materialDataBufferTargetId,
		depthNormalBufferTargetId
	};
	public static RenderBufferLoadAction[] defferedShadingTargetLoads = new RenderBufferLoadAction[4]
	{
		RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare
	};
	public static RenderBufferStoreAction[] defferedShadingTargetStores = new RenderBufferStoreAction[4]
	{
		RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store
	};
	public static RenderTargetBinding defferedShadingBinding = new RenderTargetBinding(defferedShadingTargestsId, defferedShadingTargetLoads, defferedShadingTargetStores, depthBufferTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

	public static int viewPortRaysId = Shader.PropertyToID("_ViewPortRays");

	public const int maxDirLightCount = 4;
	public const int maxPointLightCount = 256;

	public static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	public static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	public static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	public static int directionalShadowDatasId = Shader.PropertyToID("_DirectionalShadowDatas");

	public static int pointLightCountId = Shader.PropertyToID("_PointLightCount");
	public static int pointLightSpheresId = Shader.PropertyToID("_PointLightSpheres");
	public static int pointLightColorsId = Shader.PropertyToID("_PointLightColors");

	public static int tileLightingIndicesId = Shader.PropertyToID("_TileLightingIndices");
	public static int tileLightingDatasId = Shader.PropertyToID("_TileLightingDatas");
	public static int tileLBStartId = Shader.PropertyToID("_TileLBStart");
	public static int tileRVecId = Shader.PropertyToID("_TileRVec");
	public static int tileUVecId = Shader.PropertyToID("_TileUVec");

	public static int fxaaConfigId = Shader.PropertyToID("_FXAAConfig");

	public static int colorAdjustmentId = Shader.PropertyToID("_ColorAdjustments");
	public static int filterColorId = Shader.PropertyToID("_ColorFilter");
	public static int colorWhiteBalanceId = Shader.PropertyToID("_ColorWhiteBalance");
	public static int colorSplitToneShadowsId = Shader.PropertyToID("_ColorSplitToningShadows");
	public static int colorSplitToneHighlightsId = Shader.PropertyToID("_ColorSplitToningHighlights");
	public static int colorChannelMixerId = Shader.PropertyToID("_ColorChannelMixer");
	public static int smhShadowsId = Shader.PropertyToID("_SMHShadows");
	public static int smhMidtonId = Shader.PropertyToID("_SMHMidtones");
	public static int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
	public static int smhRangeId = Shader.PropertyToID("_SMHRange");

	public static string[] reflectTypeKeywords = new string[3]
	{
		"_SSR_ONLY", "_PROBE_ONLY", "_SSR_AND_PROBE"
	};

	public const int maxBloomPyramidLevels = 4;
	public static int[] bloomPyarmIds = new int[maxBloomPyramidLevels * 4]
	{
		Shader.PropertyToID("_BloomPyarm0"), Shader.PropertyToID("_BloomPyarm1"), Shader.PropertyToID("_BloomPyarm2"), Shader.PropertyToID("_BloomPyarm3"),
		Shader.PropertyToID("_BloomPyarm4"), Shader.PropertyToID("_BloomPyarm5"), Shader.PropertyToID("_BloomPyarm6"), Shader.PropertyToID("_BloomPyarm7"),
		Shader.PropertyToID("_BloomPyarm8"), Shader.PropertyToID("_BloomPyarm9"), Shader.PropertyToID("_BloomPyarm10"), Shader.PropertyToID("_BloomPyarm11"),
		Shader.PropertyToID("_BloomPyarm12"), Shader.PropertyToID("_BloomPyarm13"), Shader.PropertyToID("_BloomPyarm14"), Shader.PropertyToID("_BloomPyarm15"),
	};
	public static int bloomCombineId = Shader.PropertyToID("_BloomCombine");
	public static int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
	public static RenderTargetIdentifier[] bloomPyarmTargetIds = new RenderTargetIdentifier[maxBloomPyramidLevels * 4]
	{
		new RenderTargetIdentifier(bloomPyarmIds[0]),
		new RenderTargetIdentifier(bloomPyarmIds[1]),
		new RenderTargetIdentifier(bloomPyarmIds[2]),
		new RenderTargetIdentifier(bloomPyarmIds[3]),
		new RenderTargetIdentifier(bloomPyarmIds[4]),
		new RenderTargetIdentifier(bloomPyarmIds[5]),
		new RenderTargetIdentifier(bloomPyarmIds[6]),
		new RenderTargetIdentifier(bloomPyarmIds[7]),
		new RenderTargetIdentifier(bloomPyarmIds[8]),
		new RenderTargetIdentifier(bloomPyarmIds[9]),
		new RenderTargetIdentifier(bloomPyarmIds[10]),
		new RenderTargetIdentifier(bloomPyarmIds[11]),
		new RenderTargetIdentifier(bloomPyarmIds[12]),
		new RenderTargetIdentifier(bloomPyarmIds[13]),
		new RenderTargetIdentifier(bloomPyarmIds[14]),
		new RenderTargetIdentifier(bloomPyarmIds[15])
	};
	public static RenderTargetIdentifier bloomPrefilterTargetId = new RenderTargetIdentifier(bloomPrefilterId);

	public static int postprocessInputId = Shader.PropertyToID("_PostProcessInput");
	public static int copyInputId = Shader.PropertyToID("_CopyInput");
	public static int bloomInputId = Shader.PropertyToID("_BloomInput");
	public static int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
	public static int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
}
