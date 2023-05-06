using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainRenderer
{
    private ScriptableRenderContext context;
    private CommandBuffer commandBuffer;
    private TerrainSettings settings;
    private TerrainData terrainData;
    private Terrain terrain;
    private DetailPrototype detailPrototype;
    private Mesh grassMesh;
    private Material grassMat;
    private uint[] grassInstanceArgs = new uint[5];

    private Vector3 terrainPosition;

    private ComputeBuffer grassDensity;
    private ComputeBuffer grassPosition;
    private ComputeBuffer grassInstanceArgsBuffer;

    public void SetUp(ScriptableRenderContext context, CommandBuffer commandBuffer, TerrainSettings terrainSettings)
    {
        if (terrainSettings.terrainData == null) return;
        this.context = context;
        this.settings = terrainSettings;
        this.commandBuffer = commandBuffer;

        if (terrainSettings.updateAlways || this.terrainData != terrainSettings.terrainData)
        {
            terrain = GameObject.FindObjectOfType<Terrain>();
            if (terrain == null) return;
            this.terrainData = terrainSettings.terrainData;
            this.terrainPosition = terrain.transform.position;

            GameObject grassPrefab = terrainData.detailPrototypes[0].prototype;
            grassMesh = grassPrefab.GetComponent<MeshFilter>().sharedMesh;
            grassMat = new Material(grassPrefab.GetComponent<MeshRenderer>().sharedMaterial);
            grassMat.SetTexture(Constants.controlId, terrainData.GetAlphamapTexture(0));

            grassInstanceArgs[0] = grassMesh.GetIndexCount(0);
            grassInstanceArgs[1] = 1;
            grassInstanceArgs[2] = grassMesh.GetIndexStart(0);
            grassInstanceArgs[3] = grassMesh.GetBaseVertex(0);
            grassInstanceArgs[4] = 0;

            if(grassDensity != null)
            {
                grassDensity.Dispose();
                grassPosition.Dispose();
                grassInstanceArgsBuffer.Dispose();
            }

            grassDensity = new ComputeBuffer(terrainData.detailWidth * terrainData.detailHeight, 4, ComputeBufferType.Default);
            grassPosition = new ComputeBuffer(terrainData.detailWidth * terrainData.detailHeight, 4 * 4, ComputeBufferType.Append);
            grassInstanceArgsBuffer = new ComputeBuffer(1, 5 * 4, ComputeBufferType.IndirectArguments);
            grassDensity.SetData(terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, 0));
            grassInstanceArgsBuffer.SetData(grassInstanceArgs);
        }
    }

    public void GenereateGrassData()
    {
        if (terrain == null) return;
        commandBuffer.BeginSample(commandBuffer.name);
        ExecuteCommandBuffer();
        commandBuffer.SetComputeTextureParam(settings.grassDataCompute, 0, Constants.terrainHeightmapTextureId, terrainData.heightmapTexture);
        commandBuffer.SetGlobalVector(Constants.terrainPositionId, terrainPosition);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, Constants.terrainHeightmapScaleId, terrainData.heightmapScale);
        Vector4 terrainSize = new Vector4(terrainData.size.x, terrainData.size.z, terrainData.detailWidth, terrainData.detailHeight);
        commandBuffer.SetGlobalVector(Constants.terrainSizeId, terrainSize);
        Vector4 detilSize = new Vector4(terrainData.detailPrototypes[0].maxWidth, terrainData.detailPrototypes[0].minWidth, terrainData.detailPrototypes[0].maxHeight, terrainData.detailPrototypes[0].minHeight);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, Constants.detilSizeId, detilSize);
        commandBuffer.SetBufferCounterValue(grassPosition, 0);
        commandBuffer.SetComputeBufferParam(settings.grassDataCompute, 0, Constants.detilsDensityId, grassDensity);
        commandBuffer.SetComputeBufferParam(settings.grassDataCompute, 0, Constants.detilsPositionId, grassPosition);
        commandBuffer.DispatchCompute(settings.grassDataCompute, 0, Mathf.CeilToInt(terrainData.detailWidth / 8), Mathf.CeilToInt(terrainData.detailHeight / 8), 1);
        commandBuffer.EndSample(commandBuffer.name);
        ExecuteCommandBuffer();
        commandBuffer.SetGlobalBuffer(Constants.detilsPositionId, grassPosition);
        commandBuffer.CopyCounterValue(grassPosition, grassInstanceArgsBuffer, 4);
        ExecuteCommandBuffer();
    }

    public void DrawShadows(CommandBuffer commandBuffer_shadow)
    {
        if (terrain == null || !settings.grassShadowEnable) return;
        commandBuffer_shadow.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 1, grassInstanceArgsBuffer);
    }

    public void DrawDepthNormal()
    {
        if (terrain == null) return;
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 0, grassInstanceArgsBuffer);
    }

    public void Draw()
    {
        if (terrain == null) return;
        commandBuffer.SetGlobalTexture(Constants.terrainNormalTextureId, terrain.normalmapTexture);
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 0, grassInstanceArgsBuffer);
    }

    public void OnDispose()
    {
        if (grassDensity != null)
        {
            grassDensity.Dispose();
            grassPosition.Dispose();
            grassInstanceArgsBuffer.Dispose();
        }
    }

    private void ExecuteCommandBuffer()
    {
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
}
