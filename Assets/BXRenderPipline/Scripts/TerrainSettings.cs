using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSettings
{
    [SerializeField]
    public bool updateAlways;
    [SerializeField]
    public ComputeShader grassDataCompute;
    [SerializeField]
    public TerrainData terrainData;
}
