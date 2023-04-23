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
		commandBufferGraphics.name = SampleName = camera.name;
		Profiler.EndSample();
	}
#endif
}