using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Constants
{
	public const int depthBufferIndex = 0, lightingBufferIndex = 1, baseColorBufferIndex = 2, materialDataBufferIndex = 3, depthNormalBufferIndex = 4, cameraTargetIndex = 5;

	public static int depthBufferId = Shader.PropertyToID("_DepthBuffer");
	public static int lightingBufferId = Shader.PropertyToID("_LightingBuffer");
	public static int baseColorBufferId = Shader.PropertyToID("_BaseColorBuffer");
	public static int materialDataBufferId = Shader.PropertyToID("_MaterialDataBuffer");
	public static int depthNormalBufferId = Shader.PropertyToID("_BXDepthNormalBuffer");
	public static RenderTargetIdentifier depthBufferTargetId = new RenderTargetIdentifier(depthBufferId);
	public static RenderTargetIdentifier lightingBufferTargetId = new RenderTargetIdentifier(lightingBufferId);
	public static RenderTargetIdentifier baseColorBufferTargetId = new RenderTargetIdentifier(baseColorBufferId);
	public static RenderTargetIdentifier materialDataBufferTargetId = new RenderTargetIdentifier(materialDataBufferId);
	public static RenderTargetIdentifier depthNormalBufferTargetId = new RenderTargetIdentifier(depthNormalBufferId);
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
}
