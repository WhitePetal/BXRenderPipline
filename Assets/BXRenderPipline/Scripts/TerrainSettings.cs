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
    public ComputeShader treeDataCompute;
    [SerializeField]
    public TerrainData terrainData;
    [SerializeField]
    public Color terrainTileColor0;
    [SerializeField]
    public Color terrainTileColor1;
    [SerializeField]
    public Color terrainTileColor2;
    [SerializeField]
    public Color terrainTileColor3;
    [SerializeField]
    public bool grassShadowEnable;
    [SerializeField]
    public float grassLODDistance0 = 10;
    [SerializeField]
    public float grassLODDistance1 = 20;
    [SerializeField]
    public float grassLODDistance2 = 40;
    [SerializeField]
    public float grassLODDistance3 = 240;
}
