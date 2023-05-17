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

        public void PreviewBuilding()
        {
            Debug.Log("Preview Building");
        }
    }
}
