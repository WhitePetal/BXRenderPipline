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
            public Color canBuildColor;
            public Color cantBuildColor;
        }

        [System.Serializable]
        public struct BuildingSettings
        {
            public Material willBuildingOnMouseMat;
            public Material willBuildingOnTileMat;
        }

        [System.Serializable]
        public struct WorldViewCameraSettings
        {
            public float horizontalMoveSpeed;
            public float verticalMoveSpeed;
            public float horizontalLeaveScreenMoveSpeed;
            public float verticalLevaeScreenMoveSpeed;
            public float rotateSpeed;
            public float zoomStpe;
            public float zoomMin;
            public float zoomMax;
        }

        [System.Serializable]
        public struct UISettings
        {
            public Color woodTextColor;
            public Color stoneTextColor;
            public Color coneTextColor;
            public Color peopleTextColor;
        }

        public TileSettings tileSettings;

        public BuildingSettings buildingSettings;

        public WorldViewCameraSettings worldViewCameraSettings;

        public UISettings uiSettings;

    }
}
