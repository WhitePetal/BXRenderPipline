using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Building
{
    public enum ResourceType
    {
        None,
        Cone,
        Stone,
        Wood
    }

    public int id;
    public int width, length;
    public ResourceType resourcesType;

    public void PreviewBuilding()
    {
        Debug.Log("Preview Building");
    }
}
