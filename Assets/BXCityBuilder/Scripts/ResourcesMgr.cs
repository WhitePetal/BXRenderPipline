using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace CityBuilder
{
    public class ResourcesMgr
    {
        private static ResourcesMgr instance;
        public static ResourcesMgr Instance
        {
            get
            {
                if (instance == null) instance = new ResourcesMgr();
                return instance;
            }
        }

        public T LoadAssetsAtPath<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            string fullPath = Path.Combine("Assets", path);
            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
#endif
        }
    }
}
