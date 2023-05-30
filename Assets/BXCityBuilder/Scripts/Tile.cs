using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    [System.Serializable]
    public class Tile
    {
        public enum TileState
        {
            None,
            Tree,
            Building
        }
        public Building building;
        public bool occupied;
        public TileState obstacleType;

        public int x, z;

        [System.NonSerialized]
        public GameObject tileObj;

        public Tile(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
    }

}