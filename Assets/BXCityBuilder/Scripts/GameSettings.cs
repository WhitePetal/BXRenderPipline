using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [CreateAssetMenu(menuName = "BXCityBuilder/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [System.Serializable]
        public struct TileSettings
        {
            public GameObject tileObj;
            public Color normalColor;
            public Color selectedColor;
        }

        [System.Serializable]
        public struct WorldViewCameraSettings
        {
            public float horizontalMoveSpeed;
            public float verticalMoveSpeed;
        }

        public TileSettings tileSettings;

        public WorldViewCameraSettings worldViewCameraSettings;

    }
}
