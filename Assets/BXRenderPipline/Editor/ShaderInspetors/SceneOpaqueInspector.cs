using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SceneOpaqueInspector : ShaderGUI
{
	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		base.OnGUI(materialEditor, properties);

		EditorGUI.BeginChangeCheck();
		materialEditor.LightmapEmissionProperty();
		if (EditorGUI.EndChangeCheck())
		{
			foreach (Material m in materialEditor.targets)
			{
				m.globalIlluminationFlags &=
					~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
			}
		}
	}
}
