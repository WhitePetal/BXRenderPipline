using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMgr : MonoBehaviour
{
    public GameObject tileObj;
    public int levelWidth, levelLength;

    private Tile[,] tiles;

    private void Start()
    {
        CreateLevel();
    }

    private void CreateLevel()
    {
        GameObject tileObjsContiner = new GameObject("TileObjsContiner");
        tiles = new Tile[levelWidth, levelLength];
        for(int x = 0; x < levelWidth; ++x)
        {
            for(int z = 0; z < levelLength; ++z)
            {
                tiles[x, z] = new Tile(x, z);
                Instantiate(tileObj, new Vector3(x, 0, z), Quaternion.identity, tileObjsContiner.transform);
            }
        }
    }
}
