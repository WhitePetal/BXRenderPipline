using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [CreateAssetMenu(menuName = "BXCityBuilder/BuildingDataBase")]
    public class BuildingDataBase : ScriptableObject, ISerializationCallbackReceiver
    {
        public List<BuildingConfig> buildings = new List<BuildingConfig>();

        public void OnAfterDeserialize()
        {
            return;
        }

        public void OnBeforeSerialize()
        {
            for(int i = 0; i < buildings.Count; ++i)
            {
                buildings[i].id = i;
            }
        }
    }
}
