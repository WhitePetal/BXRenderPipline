using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcess
{
	private bool editorMode;

	private PostProcessSettings settings;

	private Material colorGradeMaterial;
	public Material ColorGradeMaterial
	{
		get
		{
#if UNITY_EDITOR
			if (editorMode)
			{
				if (colorGradeMaterial == null && settings.colorGradeShaderEditor != null)
				{
					colorGradeMaterial = new Material(settings.colorGradeShaderEditor);
					colorGradeMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return colorGradeMaterial;
			}
			else
			{
				if (colorGradeMaterial == null && settings.colorGradeShader != null)
				{
					colorGradeMaterial = new Material(settings.colorGradeShader);
					colorGradeMaterial.hideFlags = HideFlags.HideAndDontSave;
				}
				return colorGradeMaterial;
			}
#else
			if (colorGradeMaterial == null && settings.colorGradeShader != null)
			{
				colorGradeMaterial = new Material(postprocessSettings.colorGradeShader);
				colorGradeMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return colorGradeMaterial;
#endif
		}
	}

	private string[] tonemappingKeywords = new string[]
	{
		"CM_Reinhard", "CM_Neutral", "CM_ACES"
	};

	public void Setup(PostProcessSettings settings, bool editorMode)
	{
		this.settings = settings;
		this.editorMode = editorMode;
	}

	public GraphicsFence ColorGrade(CommandBuffer commandBuffer)
	{
		for (int i = 0; i < tonemappingKeywords.Length; ++i)
		{
			if (i == (int)settings.toneMappingType)
			{
				commandBuffer.EnableShaderKeyword(tonemappingKeywords[i]);
			}
			else
			{
				commandBuffer.DisableShaderKeyword(tonemappingKeywords[i]);
			}
		}
		commandBuffer.DrawProcedural(Matrix4x4.identity, ColorGradeMaterial, 0, MeshTopology.Triangles, 3);
		return commandBuffer.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.PixelProcessing);
	}
}
