using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSettings
{
    [SerializeField]
    public ComputeShader grassDataCompute;
    [SerializeField]
    public TerrainData terrainData;
    [SerializeField]
    public Vector3 terrainPostion;
}
