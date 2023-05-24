using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [System.Serializable]
    public class BuildingConfig
    {
        public int id;
        public int width, length;
        public ResourceType resourceType;
        public int costWood, costStone, costCone, costPeople;
        public float woodProducedPerS, stoneProducedPerS, coneProducedPerS, peopleProducedPerS;
        public GameObject buildingObj;
        public Sprite buildingSprite;
        public string buildingDescript;
    }
}
