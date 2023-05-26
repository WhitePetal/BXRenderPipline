using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public static class ExtensinMethods
    {
        public static void ReplaceMaterials(this GameObject go, Material mat, bool childs = true)
        {
            if (childs)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    var mats = renderers[i].sharedMaterials;
                    for (int k = 0; k < mats.Length; ++k)
                    {
                        mats[k] = mat;
                    }
                    renderers[i].sharedMaterials = mats;
                }
            }
            else
            {
                var renderer = go.GetComponent<Renderer>();
                var mats = renderer.sharedMaterials;
                for (int k = 0; k < mats.Length; ++k)
                {
                    mats[k] = mat;
                }
                renderer.sharedMaterials = mats;
            }
        }
    }
}
