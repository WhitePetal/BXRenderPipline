using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public static class ExtensinMethods
    {
        public static void ReplaceMaterials(this GameObject go, Material mat)
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
    }
}
