using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [System.Serializable]
    public class Building
    {
        public int id;
        public int width, length;
        public ResourceType resourceType;
        public float woodProduced, stoneProduced, coneProduced, peopleProduced;

        [System.NonSerialized]
        public GameObject buildingObj;

        public Building(int id, int width, int length, ResourceType resourceType)
        {
            this.id = id;
            this.width = width;
            this.length = length;
            this.resourceType = resourceType;
        }

        public Building(BuildingConfig config)
        {
            this.id = config.id;
            this.width = config.width;
            this.length = config.length;
            this.resourceType = config.resourceType;
        }
    }
}
