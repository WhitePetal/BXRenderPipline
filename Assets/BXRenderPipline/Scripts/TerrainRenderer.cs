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
    private DetailPrototype detailPrototype;
    private Mesh grassMesh;
    private Material grassMat;
    private uint[] grassInstanceArgs = new uint[5];

    private ComputeBuffer grassDensity;
    private ComputeBuffer grassPosition;
    private ComputeBuffer grassInstanceArgsBuffer;

    public void SetUp(ScriptableRenderContext context, CommandBuffer commandBuffer, TerrainSettings terrainSettings)
    {
        if (terrainSettings.terrainData == null) return;
        this.context = context;
        this.commandBuffer = commandBuffer;
        this.settings = terrainSettings;
        this.terrainData = terrainSettings.terrainData;

        if(detailPrototype != terrainData.detailPrototypes[0])
        {
            GameObject grassPrefab = terrainData.detailPrototypes[0].prototype;
            grassMesh = grassPrefab.GetComponent<MeshFilter>().sharedMesh;
            grassMat = new Material(grassPrefab.GetComponent<MeshRenderer>().sharedMaterial);
            grassMat.SetTexture("_Control", terrainData.GetAlphamapTexture(0));

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
        if (settings.terrainData == null) return;
        commandBuffer.BeginSample(commandBuffer.name);
        ExecuteCommandBuffer();
        commandBuffer.SetComputeTextureParam(settings.grassDataCompute, 0, "_TerrainHeightmapTexture", terrainData.heightmapTexture);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, "_TerrainPosition", settings.terrainPostion);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, "_TerrainHeightmapScale", terrainData.heightmapScale);
        Vector4 terrainSize = new Vector4(terrainData.size.x, terrainData.size.z, terrainData.detailWidth, terrainData.detailHeight);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, "_TerrainSize", terrainSize);
        Vector4 detilSize = new Vector4(terrainData.detailPrototypes[0].maxWidth, terrainData.detailPrototypes[0].minWidth, terrainData.detailPrototypes[0].maxHeight, terrainData.detailPrototypes[0].minHeight);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, "_DetilSize", detilSize);
        commandBuffer.SetBufferCounterValue(grassPosition, 0);
        commandBuffer.SetComputeBufferParam(settings.grassDataCompute, 0, "_DetilsDensity", grassDensity);
        commandBuffer.SetComputeBufferParam(settings.grassDataCompute, 0, "_DetilsPosition", grassPosition);
        commandBuffer.DispatchCompute(settings.grassDataCompute, 0, Mathf.CeilToInt(terrainData.detailWidth / 8), Mathf.CeilToInt(terrainData.detailHeight / 8), 1);
        commandBuffer.EndSample(commandBuffer.name);
        ExecuteCommandBuffer();
        commandBuffer.SetGlobalBuffer("_DetilsPosition", grassPosition);
        commandBuffer.CopyCounterValue(grassPosition, grassInstanceArgsBuffer, 4);
    }

    public void DrawShadows()
    {
        if (settings.terrainData == null) return;
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 2, grassInstanceArgsBuffer);
    }

    public void DrawDepthNormal()
    {
        if (settings.terrainData == null) return;
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 0, grassInstanceArgsBuffer);
    }

    public void Draw()
    {
        if (settings.terrainData == null) return;
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 1, grassInstanceArgsBuffer);
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
