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
				colorGradeMaterial = new Material(settings.colorGradeShader);
				colorGradeMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return colorGradeMaterial;
#endif
		}
	}

	private Material fxaaMaterial;
	public Material FXAAMaterial
	{
		get
		{
			if(fxaaMaterial == null && settings.fxaaSettings.fxaaShader != null)
			{
				fxaaMaterial = new Material(settings.fxaaSettings.fxaaShader);
				fxaaMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return fxaaMaterial;
		}
	}

	private CommandBuffer commandBuffer;

	private string[] tonemappingKeywords = new string[]
	{
		"CM_Reinhard", "CM_Neutral", "CM_ACES"
	};
	private string[] fxaaKeywords = new string[]
	{
		"FXAA_QUALITY_LOW", "FXAA_QUALITY_MEDIUM", "FXAA_QUALITY_HIGH"
	};

	public void Setup(PostProcessSettings settings, CommandBuffer commandBuffer, bool editorMode)
	{
		this.settings = settings;
		this.editorMode = editorMode;
		this.commandBuffer = commandBuffer;
	}

	public void ColorGrade()
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
	}

	public void FXAA()
	{
		for(int i = 0; i < fxaaKeywords.Length; ++i)
		{
			if(i == (int)settings.fxaaSettings.qualitys)
			{
				FXAAMaterial.EnableKeyword(fxaaKeywords[i]);
			}
			else
			{
				FXAAMaterial.DisableKeyword(fxaaKeywords[i]);
			}
		}

		commandBuffer.SetGlobalVector(Constants.fxaaConfigId, new Vector4(
			settings.fxaaSettings.fixedThreshold, 
			settings.fxaaSettings.relativeThreshold,
			settings.fxaaSettings.subpixelBlending
			));
		commandBuffer.DrawProcedural(Matrix4x4.identity, FXAAMaterial, 0, MeshTopology.Triangles, 3);
	}
}
