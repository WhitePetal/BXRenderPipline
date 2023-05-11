using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BXCityBuilder/BuildingDataBase")]
public class BuildingDataBase : ScriptableObject
{
    public List<Building> buildings;

    public BuildingDataBase()
    {
        buildings = new List<Building>();
    }
}
