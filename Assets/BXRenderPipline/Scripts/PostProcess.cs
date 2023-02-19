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
				ColorGradeMaterial.EnableKeyword(tonemappingKeywords[i]);
			}
			else
			{
				ColorGradeMaterial.DisableKeyword(tonemappingKeywords[i]);
			}
		}
		if (settings.colorGradingSettings.enable)
		{
			ColorGradeMaterial.EnableKeyword("CM_ColorGrading");
			commandBuffer.SetGlobalVector(Constants.colorAdjustmentId, new Vector4
			(
				settings.colorGradingSettings.exposure,
				settings.colorGradingSettings.contrast,
				settings.colorGradingSettings.hueShift,
				settings.colorGradingSettings.satruation
			));
			commandBuffer.SetGlobalColor(Constants.filterColorId, settings.colorGradingSettings.filterColor);
		}
		else
		{
			ColorGradeMaterial.DisableKeyword("CM_ColorGrading");
		}
		if (settings.whiteBalanceSettings.enable)
		{
			ColorGradeMaterial.EnableKeyword("CM_ColorWhiteBalance");
			commandBuffer.SetGlobalVector(Constants.colorWhiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
				settings.whiteBalanceSettings.temperature, settings.whiteBalanceSettings.tint
			));
		}
		else
		{
			ColorGradeMaterial.DisableKeyword("CM_ColorWhiteBalance");
		}
		if (settings.splitToningSettings.enable)
		{
			ColorGradeMaterial.EnableKeyword("CM_ColorSplitToning");
			Color splitColor = settings.splitToningSettings.shadows;
			splitColor.a = settings.splitToningSettings.balance * 0.01f;
			commandBuffer.SetGlobalVector(Constants.colorSplitToneShadowsId, splitColor);
			commandBuffer.SetGlobalVector(Constants.colorSplitToneHighlightsId, settings.splitToningSettings.highlights);
		}
		else
		{
			ColorGradeMaterial.DisableKeyword("CM_ColorSplitToning");
		}
		if (settings.chanelMixerSettings.enable)
		{
			ColorGradeMaterial.EnableKeyword("CM_ColorChannelMixer");
			Matrix4x4 mixer = Matrix4x4.identity;
			mixer.SetRow(0, settings.chanelMixerSettings.red);
			mixer.SetRow(1, settings.chanelMixerSettings.green);
			mixer.SetRow(2, settings.chanelMixerSettings.blue);
			commandBuffer.SetGlobalMatrix(Constants.colorChannelMixerId, mixer);
		}
		else
		{
			ColorGradeMaterial.DisableKeyword("CM_ColorChannelMixer");
		}
		if (settings.shadowsMidtoneHighlightSettings.enable)
		{
			ColorGradeMaterial.EnableKeyword("CM_ShadowsMidtoneHighlights");
			ShadowsMidtoneHighlightSettings smh = settings.shadowsMidtoneHighlightSettings;
			commandBuffer.SetGlobalColor(Constants.smhShadowsId, smh.shadows.linear);
			commandBuffer.SetGlobalColor(Constants.smhMidtonId, smh.midtone.linear);
			commandBuffer.SetGlobalColor(Constants.smhHighlightsId, smh.highlights.linear);
			commandBuffer.SetGlobalVector(Constants.smhRangeId, new Vector4(
				smh.shadowStart, smh.shadowEnd, smh.highlightsStart, smh.highlightsEnd
			));
		}
		else
		{
			ColorGradeMaterial.DisableKeyword("CM_ShadowsMidtoneHighlights");
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
