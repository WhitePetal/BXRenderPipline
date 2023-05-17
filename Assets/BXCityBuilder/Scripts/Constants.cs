using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public static class Constants
    {
        public const string baseDataSavedName = "BaseData";
        public const string mapDataSaveName = "MapData";



        public const int initMaxWood = 2350;
        public const int initMaxStone = 2350;
        public const int initMaxCone = 2350;
        public const int initMaxPeople = 2350;

        public const int initWood = 300;
        public const int initStone = 300;
        public const int initCone = 300;
        public const int initPeople = 300;



        public const int initMapWidth = 100;
        public const int initMapHeight = 100;
    }



    public enum ResourceType
    {
        None,
        Cone,
        People,
        Stone,
        Wood
    }
}
