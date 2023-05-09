using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcess
{
	private int width, height;
	private int bloomPyarmCount;

	private PostProcessSettings settings;

	private Material copyTexMaterial;
	private Material CopyTexMaterial
	{
		get
		{
			if (copyTexMaterial == null && settings.copTexShader != null)
			{
				copyTexMaterial = new Material(settings.copTexShader);
				copyTexMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return copyTexMaterial;
		}
	}

	private Material bloomMaterial;
	public Material BloomMaterial
	{
		get
		{
			if (bloomMaterial == null && settings.bloomSettings.bloomShader != null)
			{
				bloomMaterial = new Material(settings.bloomSettings.bloomShader);
				bloomMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return bloomMaterial;
		}
	}

	private Material colorGradeMaterial;
	private Material ColorGradeMaterial
	{
		get
		{
			if (colorGradeMaterial == null && settings.colorGradeShader != null)
			{
				colorGradeMaterial = new Material(settings.colorGradeShader);
				colorGradeMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return colorGradeMaterial;
		}
	}

	private Material fxaaMaterial;
	private Material FXAAMaterial
	{
		get
		{
			if (fxaaMaterial == null && settings.fxaaSettings.fxaaShader != null)
			{
				fxaaMaterial = new Material(settings.fxaaSettings.fxaaShader);
				fxaaMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return fxaaMaterial;
		}
	}

	private Material fogMaterial;
	private Material FogMaterial
	{
		get
		{
			if (fogMaterial == null && settings.atmoSettings.fogShader != null)
			{
				fogMaterial = new Material(settings.atmoSettings.fogShader);
				fogMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			return fogMaterial;
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

	public void Setup(PostProcessSettings settings, CommandBuffer commandBuffer, int width, int height)
	{
		this.settings = settings;
		this.width = width;
		this.height = height;
		this.commandBuffer = commandBuffer;
	}

	public void CleanUp()
	{
		//commandBuffer.ReleaseTemporaryRT(Constants.fogFinalBufferId);
		for (int i = 0; i < bloomPyarmCount - 1; ++i)
		{
			commandBuffer.ReleaseTemporaryRT(Constants.bloomPyarmIds[i]);
		}
		commandBuffer.ReleaseTemporaryRT(Constants.bloomPrefilterId);
	}

	public void Copy(RenderTargetIdentifier from, RenderTargetIdentifier to)
	{
		commandBuffer.SetRenderTarget(to);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		commandBuffer.SetGlobalTexture(Constants.copyInputId, from);
		commandBuffer.DrawProcedural(Matrix4x4.identity, CopyTexMaterial, 0, MeshTopology.Triangles, 3);
	}

	public void Fog()
	{
		commandBuffer.GetTemporaryRT(Constants.fogFinalBufferId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.SetRenderTarget(Constants.fogFinalBufferTargetId);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		commandBuffer.SetGlobalColor("_FogColor", settings.atmoSettings.skyColor);
		commandBuffer.SetGlobalVector("_FogInnerParams", new Vector4(
			1f / settings.atmoSettings.innerScatterIntensity,
			settings.atmoSettings.innerScatterDensity,
			settings.atmoSettings.fogStartDistance
			));
		commandBuffer.SetGlobalVector("_FogOuterParams", new Vector4(
			settings.atmoSettings.outerScatterIntensity,
			1f / settings.atmoSettings.outerScatterDensity
			));
		commandBuffer.DrawProcedural(Matrix4x4.identity, FogMaterial, 0, MeshTopology.Quads, 4);
	}

	private void BloomBlur(RenderTargetIdentifier from, RenderTargetIdentifier to, int pass, bool clear = false)
	{
		commandBuffer.SetRenderTarget(to);
		if (clear) commandBuffer.ClearRenderTarget(true, true, Color.clear);
		commandBuffer.SetGlobalTexture(Constants.bloomInput0Id, from);
		commandBuffer.DrawProcedural(Matrix4x4.identity, BloomMaterial, pass, MeshTopology.Triangles, 3);
	}

	public void Bloom()
	{
		BloomSettings bloomSettings = settings.bloomSettings;
		int bloomWidth = width >> 1;
		int bloomHeight = height >> 1;
		commandBuffer.GetTemporaryRT(Constants.bloomPyarmIds[0], bloomWidth, bloomHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		Vector4 threshold;
		threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
		threshold.y = threshold.x * bloomSettings.thresholdKnee;
		threshold.z = 2f * threshold.y;
		threshold.w = 0.25f / (threshold.y + 0.00001f);
		threshold.y -= threshold.x;
		commandBuffer.SetGlobalVector(Constants.bloomThresholdId, threshold);
		commandBuffer.SetRenderTarget(Constants.bloomPyarmTargetIds[0]);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		commandBuffer.DrawProcedural(Matrix4x4.identity, BloomMaterial, 0, MeshTopology.Triangles, 3);

		int fromIndex = 0, toIndex = 0;
		RenderTargetIdentifier fromId = Constants.bloomPyarmTargetIds[0], toId, midId;
		bloomPyarmCount = 1;
		int i;
		for (i = 0; i < bloomSettings.maxIterations * 2; i += 2)
		{
			if (bloomWidth < bloomSettings.downScaleLimit || bloomHeight < bloomSettings.downScaleLimit) break;
			commandBuffer.GetTemporaryRT(Constants.bloomPyarmIds[bloomPyarmCount], bloomWidth, bloomHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
			commandBuffer.GetTemporaryRT(Constants.bloomPyarmIds[bloomPyarmCount + 1], bloomWidth, bloomHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);

			fromIndex = toIndex;
			toIndex += 2;
			fromId = Constants.bloomPyarmTargetIds[fromIndex];
			toId = Constants.bloomPyarmTargetIds[toIndex];
			midId = Constants.bloomPyarmTargetIds[toIndex - 1];
			BloomBlur(fromId, midId, 1);
			BloomBlur(midId, toId, 2);

			bloomWidth = bloomWidth >> 1;
			bloomHeight = bloomHeight >> 1;
			bloomPyarmCount += 2;
		}


		commandBuffer.SetGlobalFloat(Constants.bloomIntensityId, 1f);
		for (i -= 1; i > 0 && fromIndex >= 0; --i)
		{
			int midIndex = toIndex - 1;
			toId = Constants.bloomPyarmTargetIds[toIndex];
			midId = Constants.bloomPyarmTargetIds[midIndex];
			fromId = Constants.bloomPyarmTargetIds[fromIndex];
			commandBuffer.SetRenderTarget(fromId);
			commandBuffer.SetGlobalTexture(Constants.bloomInput0Id, toId);
			commandBuffer.SetGlobalTexture(Constants.bloomInput1Id, midId);
			commandBuffer.DrawProcedural(Matrix4x4.identity, BloomMaterial, 3, MeshTopology.Triangles, 3);
			toIndex = fromIndex;
			fromIndex -= 2;
		}

		commandBuffer.GetTemporaryRT(Constants.bloomPrefilterId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None);
		commandBuffer.SetRenderTarget(Constants.bloomPrefilterTargetId);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		commandBuffer.SetGlobalFloat(Constants.bloomIntensityId, bloomSettings.intensity);
		commandBuffer.SetGlobalTexture(Constants.bloomInput0Id, fromId);
		commandBuffer.SetGlobalTexture(Constants.bloomInput1Id, Constants.lightingBufferTargetId);
		commandBuffer.DrawProcedural(Matrix4x4.identity, BloomMaterial, 3, MeshTopology.Triangles, 3);
	}

	public void ColorGrade()
	{
		commandBuffer.SetRenderTarget(Constants.fxaaInputBufferTargetId);
		commandBuffer.ClearRenderTarget(true, true, Color.clear);
		commandBuffer.SetGlobalTexture(Constants.postprocessInputId, Constants.bloomPrefilterTargetId);
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
		for (int i = 0; i < fxaaKeywords.Length; ++i)
		{
			if (i == (int)settings.fxaaSettings.qualitys)
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