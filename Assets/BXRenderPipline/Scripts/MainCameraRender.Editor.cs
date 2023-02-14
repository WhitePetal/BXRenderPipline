using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class MainCameraRender
{
#if UNITY_EDITOR
	private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));

	private string SampleName { get; set; }

	private void PrepareForSceneWindow()
	{
		if (camera.cameraType == CameraType.SceneView)
		{
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}

	private void PreparBuffer()
	{
		Profiler.BeginSample("Editor Only");
		commandBuffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}

	private void ShadingInEditorMode()
	{
		GenerateTileLightingData();
		GenerateBuffers_Editor();
		DrawGeometryGBuffer_Editor(useDynamicBatching, useGPUInstancing, useLightsPerObject);
		DrawDefferedShading_Editor();
		//DrawDefferedCombine_Editor();
		DrawSkyBoxAndTransparent_Editor();
		DrawUnsupportShader();
		DrawGizmosBeforePostProcess();
		DrawPostProcess_Editor();
		DrawGizmosAfterPostProcess();
		RenderToCameraTargetAndTonemapping_Editor();
		CleanUp_Editor();
		Submit();
	}

	private void GenerateBuffers_Editor()
	{
		int width = camera.pixelWidth;
		int height = camera.pixelHeight;
		commandBuffer.GetTemporaryRT(lightingBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(baseColorBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.GetTemporaryRT(materialDataBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		//commandBuffer.GetTemporaryRT(depthNormalBufferId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
#if UNITY_EDITOR_OSX
		commandBuffer.GetTemporaryRT(depthBufferId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
#else
		commandBuffer.GetTemporaryRT(depthBufferId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Depth);
#endif
    }

    private void DrawGeometryGBuffer_Editor(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
	{
		commandBuffer.SetRenderTarget(defferedShadingBinding);
		commandBuffer.ClearRenderTarget(camera.clearFlags < CameraClearFlags.Depth, true, Color.clear);
		ExecuteBuffer();

		PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
		// 不透明：主光渲染、GBuffer (baseColor mul shadow, and a is specular)
		SortingSettings sortingSettings = new SortingSettings(camera)
		{
			criteria = SortingCriteria.CommonOpaque
		};
		DrawingSettings drawingSettings = new DrawingSettings()
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.sortingSettings = sortingSettings;
		drawingSettings.SetShaderPassName(0, BXRenderPipline.bxShaderTagIds[0]);
		drawingSettings.SetShaderPassName(1, BXRenderPipline.bxShaderTagIds[1]);
		FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		drawingSettings.perObjectData = PerObjectData.ReflectionProbes |
			PerObjectData.Lightmaps |
			PerObjectData.ShadowMask |
			PerObjectData.OcclusionProbe |
			PerObjectData.LightProbe |
			PerObjectData.LightProbeProxyVolume |
			PerObjectData.OcclusionProbeProxyVolume |
			lightsPerObjectFlags;

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	private void DrawDefferedShading_Editor()
	{
#if UNITY_EDITOR_OSX
		commandBuffer.SetRenderTarget(lightingBufferTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthBufferTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		commandBuffer.SetGlobalTexture(baseColorBufferId, baseColorBufferTargetId);
		commandBuffer.SetGlobalTexture(materialDataBufferId, materialDataBufferTargetId);
		commandBuffer.SetGlobalTexture(depthNormalBufferId, depthNormalBufferTargetId);
#endif
		commandBuffer.SetGlobalMatrix(viewPortRaysId, viewPortRays);
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedShadingMaterial, 0, MeshTopology.Triangles, 6);
	}

	private void DrawDefferedCombine_Editor()
	{
		// BlendMode为 DstColor * srcCol(baseColor) + Zero * dstCol, Zero * srcAlpha + One * dstAlpha(specular intensity)
		// 来将漫反射光累计与baseColor相乘
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedCombineMaterial, 0, MeshTopology.Triangles, 3);
		// BlendMode为 DstAlpha * srcCol(one) + One * dstCol, One * srcAlpha(one) +  Zero * dstAlpha
		// 来加上镜面反射光累计
		commandBuffer.DrawProcedural(Matrix4x4.identity, DefferedCombineMaterial, 1, MeshTopology.Triangles, 3);
		// 通过这两次混合可以做到在不切换渲染目标的同时完成光累计和baseColor的Combine
	}

	private void DrawSkyBoxAndTransparent_Editor()
	{
		ExecuteBuffer();
		context.DrawSkybox(camera);
	}

	private void DrawPostProcess_Editor()
	{

	}

	private void RenderToCameraTargetAndTonemapping_Editor()
	{
		commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthBufferTargetId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		commandBuffer.ClearRenderTarget(false, true, Color.clear);
		commandBuffer.DrawProcedural(Matrix4x4.identity, TonemappingMaterial, (int)postprocessSettings.toneMappingType, MeshTopology.Triangles, 3);
	}

	private void CleanUp_Editor()
	{
		commandBuffer.ReleaseTemporaryRT(lightingBufferId);
		commandBuffer.ReleaseTemporaryRT(baseColorBufferId);
		commandBuffer.ReleaseTemporaryRT(materialDataBufferId);
		//commandBuffer.ReleaseTemporaryRT(depthNormalBufferId);
		commandBuffer.ReleaseTemporaryRT(depthBufferId);
	}

	private void DrawUnsupportShader()
	{
		DrawingSettings drawingSettings = new DrawingSettings(BXRenderPipline.legacyShaderTagIds[0], new SortingSettings(camera))
		{
			overrideMaterial = material_error
		};
		FilteringSettings filteringSettings = FilteringSettings.defaultValue;
		for (int i = 1; i < BXRenderPipline.legacyShaderTagIds.Length; ++i)
		{
			drawingSettings.SetShaderPassName(i, BXRenderPipline.legacyShaderTagIds[i]);
		}
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

	private void DrawGizmosBeforePostProcess()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
		}
	}

	private void DrawGizmosAfterPostProcess()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}
#else
	private const string SampleName = commandBufferName;
#endif
}
