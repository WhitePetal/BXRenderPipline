using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshToTerrain : MonoBehaviour
{
    public TerrainData terrainData;
    public Texture2D heightmap;
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("terr width: " + terrainData.heightmapResolution);
        //float[,] heights = terrainData.GetHeights(0, 0, 1024, 1024);
        //for(int y = 0; y < 1024; ++y)
        //{
        //    for(int x = 0; x < 1024; ++x)
        //    {
        //        heights[x, y] = heightmap.GetPixel(x, y).r;
        //        Debug.Log("height: " + heights[x, y]);
        //    }
        //}

        //terrainData.SetHeights(0, 0, heights);
        Debug.Log("Alphamap Name: " + terrainData.GetAlphamapTexture(0).name);
    }
}
