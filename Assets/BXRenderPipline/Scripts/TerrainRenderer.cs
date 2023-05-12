using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

public class TerrainRenderer
{
    private ScriptableRenderContext context;
    private CommandBuffer commandBuffer;
    private TerrainSettings settings;
    private TerrainData terrainData;
    private Terrain terrain;

    private Mesh grassMesh;
    private Material grassMat;
    private uint[] grassInstanceArgs = new uint[5];

    private Mesh treeMesh;
    private Material treeSubMesh0Mat;
    private Material treeSubMesh1Mat;
    private uint[] treeSubMesh0InstanceArgs = new uint[5];
    private uint[] treeSubMesh1InstanceArgs = new uint[5];

    private Vector3 terrainPosition;

    private ComputeBuffer grassDensitys;
    private ComputeBuffer grassPositions;
    private ComputeBuffer grassInstanceArgsBuffer;

    private int treeCount;
    private ComputeBuffer treeInstances;
    private ComputeBuffer treePositions;
    private ComputeBuffer treeSubMesh0InstanceArgsBuffer;
    private ComputeBuffer treeSubMesh1InstanceArgsBuffer;

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
            grassMat.SetColor("_Color0", settings.terrainTileColor0);
            grassMat.SetColor("_Color1", settings.terrainTileColor1);
            grassMat.SetColor("_Color2", settings.terrainTileColor2);
            grassMat.SetColor("_Color3", settings.terrainTileColor3);
            grassMat.SetTexture(Constants.controlId, terrainData.GetAlphamapTexture(0));

            grassInstanceArgs[0] = grassMesh.GetIndexCount(0);
            grassInstanceArgs[1] = 1;
            grassInstanceArgs[2] = grassMesh.GetIndexStart(0);
            grassInstanceArgs[3] = grassMesh.GetBaseVertex(0);
            grassInstanceArgs[4] = 0;

            if(grassDensitys != null)
            {
                grassDensitys.Dispose();
                grassPositions.Dispose();
                grassInstanceArgsBuffer.Dispose();
            }

            grassDensitys = new ComputeBuffer(terrainData.detailWidth * terrainData.detailHeight, 4, ComputeBufferType.Default);
            grassPositions = new ComputeBuffer(terrainData.detailWidth * terrainData.detailHeight, 16, ComputeBufferType.Append);
            grassInstanceArgsBuffer = new ComputeBuffer(1, 5 * 4, ComputeBufferType.IndirectArguments);
            grassDensitys.SetData(terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, 0));
            grassInstanceArgsBuffer.SetData(grassInstanceArgs);

            GameObject treePrefab = terrainData.treePrototypes[0].prefab;
            LOD[] treeLods = treePrefab.GetComponent<LODGroup>().GetLODs();
            Renderer treeRenderer = treeLods[0].renderers[0];
            treeMesh = treeRenderer.GetComponent<MeshFilter>().sharedMesh;
            treeSubMesh0Mat = new Material(treeRenderer.sharedMaterials[0]);
            treeSubMesh1Mat = new Material(treeRenderer.sharedMaterials[1]);

            treeSubMesh0InstanceArgs[0] = treeMesh.GetIndexCount(0);
            treeSubMesh0InstanceArgs[1] = 1;
            treeSubMesh0InstanceArgs[2] = treeMesh.GetIndexStart(0);
            treeSubMesh0InstanceArgs[3] = treeMesh.GetBaseVertex(0);
            treeSubMesh0InstanceArgs[4] = 0;

            treeSubMesh1InstanceArgs[0] = treeMesh.GetIndexCount(1);
            treeSubMesh1InstanceArgs[1] = 1;
            treeSubMesh1InstanceArgs[2] = treeMesh.GetIndexStart(1);
            treeSubMesh1InstanceArgs[3] = treeMesh.GetBaseVertex(1);
            treeSubMesh1InstanceArgs[4] = 0;

            if(treeInstances != null)
            {
                treeInstances.Dispose();
                treePositions.Dispose();
                treeSubMesh0InstanceArgsBuffer.Dispose();
                treeSubMesh1InstanceArgsBuffer.Dispose();
            }

            TreeInstance[] trees = terrainData.treeInstances;
            this.treeCount = trees.Length;

            treeInstances = new ComputeBuffer(trees.Length, 16, ComputeBufferType.Default);
            treePositions = new ComputeBuffer(trees.Length, 16, ComputeBufferType.Append);
            treeSubMesh0InstanceArgsBuffer = new ComputeBuffer(1, 5 * 4, ComputeBufferType.IndirectArguments);
            treeSubMesh1InstanceArgsBuffer = new ComputeBuffer(1, 5 * 4, ComputeBufferType.IndirectArguments);
            Vector4[] treePos = trees.Select(t =>
                {
                    Vector3 locaPos = new Vector3(t.position.x * terrainData.size.x, t.position.y * terrainData.size.y, t.position.z * terrainData.size.z);
                    Vector3 worldPos = terrain.transform.TransformPoint(locaPos);
                    return new Vector4(worldPos.x, worldPos.y, worldPos.z, 1f);
                }).ToArray();
            treeInstances.SetData(treePos);
            treeSubMesh0InstanceArgsBuffer.SetData(treeSubMesh0InstanceArgs);
            treeSubMesh1InstanceArgsBuffer.SetData(treeSubMesh1InstanceArgs);
        }
    }

    public void GenereateGrassData()
    {
        if (terrain == null) return;
        commandBuffer.SetComputeTextureParam(settings.grassDataCompute, 0, Constants.terrainHeightmapTextureId, terrainData.heightmapTexture);
        commandBuffer.SetGlobalVector(Constants.terrainPositionId, terrainPosition);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, Constants.terrainHeightmapScaleId, terrainData.heightmapScale);
        Vector4 terrainSize = new Vector4(terrainData.size.x, terrainData.size.z, terrainData.detailWidth, terrainData.detailHeight);
        commandBuffer.SetGlobalVector(Constants.terrainSizeId, terrainSize);
        Vector4 detilSize = new Vector4(terrainData.detailPrototypes[0].maxWidth, terrainData.detailPrototypes[0].minWidth, terrainData.detailPrototypes[0].maxHeight, terrainData.detailPrototypes[0].minHeight);
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, Constants.detilSizeId, detilSize);

        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, Constants.detilsLODDistances, new Vector4(settings.grassLODDistance0, settings.grassLODDistance1, settings.grassLODDistance2, settings.grassLODDistance3));
        commandBuffer.SetComputeVectorParam(settings.grassDataCompute, Constants.detilsLODStpes, new Vector4(
            settings.grassLODDistance1-settings.grassLODDistance0,
            settings.grassLODDistance2-settings.grassLODDistance1,
            settings.grassLODDistance3-settings.grassLODDistance2, 0));

        commandBuffer.SetBufferCounterValue(grassPositions, 0);
        commandBuffer.SetComputeBufferParam(settings.grassDataCompute, 0, Constants.detilsDensityId, grassDensitys);
        commandBuffer.SetComputeBufferParam(settings.grassDataCompute, 0, Constants.detilsPositionId, grassPositions);
        commandBuffer.DispatchCompute(settings.grassDataCompute, 0, Mathf.CeilToInt(terrainData.detailWidth / 8), Mathf.CeilToInt(terrainData.detailHeight / 8), 1);

        commandBuffer.SetBufferCounterValue(treePositions, 0);
        commandBuffer.SetComputeBufferParam(settings.treeDataCompute, 0, "_TreeInstances", treeInstances);
        commandBuffer.SetComputeBufferParam(settings.treeDataCompute, 0, "_TreePositions", treePositions);
        commandBuffer.DispatchCompute(settings.treeDataCompute, 0, Mathf.CeilToInt(treeCount / 8f), 1, 1);

        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        commandBuffer.SetGlobalBuffer(Constants.detilsPositionId, grassPositions);
        commandBuffer.CopyCounterValue(grassPositions, grassInstanceArgsBuffer, 4);
        commandBuffer.SetGlobalBuffer("_TreePositions", treePositions);
        commandBuffer.CopyCounterValue(treePositions, treeSubMesh0InstanceArgsBuffer, 4);
        commandBuffer.CopyCounterValue(treePositions, treeSubMesh1InstanceArgsBuffer, 4);

        Graphics.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }

    public void DrawShadows(CommandBuffer commandBuffer_shadow)
    {
        if (terrain == null) return;
        commandBuffer_shadow.DrawMeshInstancedIndirect(treeMesh, 0, treeSubMesh0Mat, 1, treeSubMesh0InstanceArgsBuffer);
        commandBuffer_shadow.DrawMeshInstancedIndirect(treeMesh, 1, treeSubMesh1Mat, 1, treeSubMesh1InstanceArgsBuffer);
        if (!settings.grassShadowEnable) return;
        commandBuffer_shadow.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 1, grassInstanceArgsBuffer);
    }

    public void DrawDepthNormal()
    {
        if (terrain == null) return;
        commandBuffer.DrawMeshInstancedIndirect(treeMesh, 0, treeSubMesh0Mat, 0, treeSubMesh0InstanceArgsBuffer);
        commandBuffer.DrawMeshInstancedIndirect(treeMesh, 1, treeSubMesh1Mat, 0, treeSubMesh1InstanceArgsBuffer);
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 0, grassInstanceArgsBuffer);
    }

    public void Draw()
    {
        if (terrain == null) return;
        commandBuffer.SetGlobalTexture(Constants.terrainNormalTextureId, terrain.normalmapTexture);
        SphericalHarmonicsL2 sh2 = RenderSettings.ambientProbe;
        Vector4 shAr = new Vector4(sh2[0, 3], sh2[0, 1], sh2[0, 2], sh2[0, 0] - sh2[0, 6]);
        Vector4 shAg = new Vector4(sh2[1, 3], sh2[1, 1], sh2[1, 2], sh2[1, 0] - sh2[1, 6]);
        Vector4 shAb = new Vector4(sh2[2, 3], sh2[2, 1], sh2[2, 2], sh2[2, 0] - sh2[2, 6]);
        Vector4 shBr = new Vector4(sh2[0, 4], sh2[0, 6], sh2[0, 5] * 3, sh2[0, 7]);
        Vector4 shBg = new Vector4(sh2[1, 4], sh2[1, 6], sh2[1, 5] * 3, sh2[1, 7]);
        Vector4 shBb = new Vector4(sh2[2, 4], sh2[2, 6], sh2[2, 5] * 3, sh2[2, 7]);
        Vector4 shC = new Vector4(sh2[0, 8], sh2[2, 8], sh2[1, 8], 1);
        commandBuffer.SetGlobalVector("unity_SHAr", shAr);
        commandBuffer.SetGlobalVector("unity_SHAg", shAg);
        commandBuffer.SetGlobalVector("unity_SHAb", shAb);
        commandBuffer.SetGlobalVector("unity_SHBr", shBr);
        commandBuffer.SetGlobalVector("unity_SHBg", shBg);
        commandBuffer.SetGlobalVector("unity_SHBb", shBb);
        commandBuffer.SetGlobalVector("unity_SHC", shC);
        commandBuffer.DrawMeshInstancedIndirect(treeMesh, 0, treeSubMesh0Mat, 0, treeSubMesh0InstanceArgsBuffer);
        commandBuffer.DrawMeshInstancedIndirect(treeMesh, 1, treeSubMesh1Mat, 0, treeSubMesh1InstanceArgsBuffer);
        commandBuffer.DrawMeshInstancedIndirect(grassMesh, 0, grassMat, 0, grassInstanceArgsBuffer);
    }

    public void OnDispose()
    {
        if (grassDensitys != null)
        {
            grassDensitys.Dispose();
            grassPositions.Dispose();
            grassInstanceArgsBuffer.Dispose();
        }

        if(treeInstances != null)
        {
            treeInstances.Dispose();
            treePositions.Dispose();
            treeSubMesh0InstanceArgsBuffer.Dispose();
            treeSubMesh1InstanceArgsBuffer.Dispose();
        }
    }

    private void ExecuteCommandBuffer()
    {
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
}
