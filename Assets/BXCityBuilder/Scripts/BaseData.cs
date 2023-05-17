using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [System.Serializable]
    public class BaseData
    {
        public int maxWood = 2350;
        public int maxStone = 2350;
        public int maxCone = 2350;
        public int maxPeople = 2350;

        public int wood;
        public int stone;
        public int cone;
        public int pepole;

        public BaseData(int initMaxWood, int initMaxStone, int initMaxCone, int initMaxPeople,
            int initWood, int initStone, int initCone, int initPeople)
        {
            this.maxWood = initMaxWood;
            this.maxStone = initMaxStone;
            this.maxCone = initMaxCone;
            this.maxPeople = initMaxPeople;

            this.wood = initWood;
            this.stone = initStone;
            this.cone = initCone;
            this.pepole = initPeople;
        }
    }
}
