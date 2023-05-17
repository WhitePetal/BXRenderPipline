using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [System.Serializable]
    public class MapData : ISerializationCallbackReceiver
    {
        public int mapWidth, mapLength;
        public Tile[,] map;

        [SerializeField]
        private Tile[] serizledMap;

        public MapData(int mapWidth, int mapLength)
        {
            this.mapWidth = mapWidth;
            this.mapLength = mapLength;
            map = new Tile[mapWidth, mapLength];
        }

        public void OnBeforeSerialize()
        {
            serizledMap = new Tile[mapWidth * mapLength];
            for(int z = 0; z < mapLength; ++z)
            {
                for(int x = 0; x < mapWidth; ++x)
                {
                    serizledMap[x + z * mapWidth] = map[x, z];
                }
            }

        }

        public void OnAfterDeserialize()
        {
            map = new Tile[mapWidth, mapLength];
            for (int z = 0; z < mapLength; ++z)
            {
                for (int x = 0; x < mapWidth; ++x)
                {
                    map[x, z] = serizledMap[x + z * mapWidth];
                }
            }
        }
    }
}
