using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityBuilder
{
    public class MapMgr
    {
        private static MapMgr instance;
        public static MapMgr Instance
        {
            get
            {
                if (instance == null) instance = new MapMgr();
                return instance;
            }
        }

        private GameSettings.TileSettings tileSettings;

        private GameObject tileObj;
        private Material tileNormalMat;
        private Material tileSelectedMat;
        private Material tileCanBuildMat, tileCantBuildMat;

        private MapData mapData;

        private Tile curSelectedTile;
        private Tile curWillBuildTile;

        private BuildingConfig willBuildBuildingConfig;
        private Building willBuildBuilding;
        private List<Tile> willBuildTiles;

        public int MapWidth
        {
            get
            {
                return mapData.mapWidth;
            }
        }
        public int MapLength
        {
            get
            {
                return mapData.mapLength;
            }
        }

        public void Init(GameSettings.TileSettings tileSettings)
        {
            this.tileSettings = tileSettings;
            this.tileObj = tileSettings.tileObj;
            this.tileNormalMat = tileObj.GetComponent<MeshRenderer>().sharedMaterial;
            this.tileNormalMat.SetColor("_Color", tileSettings.normalColor);
            this.tileSelectedMat = new Material(tileNormalMat);
            this.tileSelectedMat.SetColor("_Color", tileSettings.selectedColor);
            this.tileCanBuildMat = new Material(tileNormalMat);
            this.tileCanBuildMat.SetColor("_Color", tileSettings.canBuildColor);
            this.tileCantBuildMat = new Material(tileNormalMat);
            this.tileCantBuildMat.SetColor("_Color", tileSettings.cantBuildColor);

            willBuildTiles = new List<Tile>();

            mapData = SaveMgr.Instance.GetMapData();

            if(mapData == null)
            {
                mapData = new MapData(Constants.initMapWidth, Constants.initMapHeight);
            }

            CreateLevel();
        }

        public void AddMapSize(int addWidth, int addLength)
        {
            mapData.mapWidth += addWidth;
            mapData.mapLength += addLength;
            mapData.map = new Tile[mapData.mapWidth, mapData.mapLength];
        }

        public void SaveMapData()
        {
            SaveMgr.Instance.SaveMapData(mapData);
        }

        public Tile SetSelectedTile(int x, int z)
        {
            if (!CheckTilePosVailed(x, z)) return null;
            Debug.Log("INFO: Selecte Tile Success Pos: x->" + x + "  z->" + z);
            Tile willTile = mapData.map[x, z];
            Debug.Log("WillTile: " + willTile);
            willTile.tileObj.GetComponent<MeshRenderer>().sharedMaterial = tileSelectedMat;
            if(curSelectedTile != null)
            {
                curSelectedTile.tileObj.GetComponent<MeshRenderer>().sharedMaterial = tileNormalMat;
            }
            if (curSelectedTile == willTile)
                curSelectedTile = null;
            else
                curSelectedTile = willTile;
            return curSelectedTile;
        }

        public void CancleCurSelectedTile()
        {
            if(curSelectedTile != null)
            {
                curSelectedTile.tileObj.GetComponent<MeshRenderer>().sharedMaterial = tileNormalMat;
                curSelectedTile = null;
            }
        }

        public void SetWillBuildBuildingInfo(int buildingId)
        {
            willBuildBuildingConfig = GameManager.Instance.buildingDataBase.buildings[buildingId];
            if (willBuildBuilding != null) GameObject.Destroy(this.willBuildBuilding.buildingObj);
            willBuildBuilding = new Building(willBuildBuildingConfig);
            willBuildBuilding.buildingObj = GameObject.Instantiate<GameObject>(willBuildBuildingConfig.buildingObj);
            var willBuildingOnMouseMat = GameManager.Instance.gameSettings.buildingSettings.willBuildingOnMouseMat;
            willBuildBuilding.buildingObj.ReplaceMaterials(willBuildingOnMouseMat);
        }

        public void PreviewWillBuildBuilding(int x, int z)
        {
            if (!CheckTilePosVailed(x, z)) return;

            Tile tile = mapData.map[x, z];
            MeshRenderer tileRenderer = tile.tileObj.GetComponent<MeshRenderer>();
            Material tileMat = tileRenderer.sharedMaterial;

            if(curWillBuildTile != null && curWillBuildTile != tile && curWillBuildTile != curSelectedTile)
            {
                MeshRenderer curWillBuildTileRenderer = curWillBuildTile.tileObj.GetComponent<MeshRenderer>();
                Material curWillBuildTileMat = curWillBuildTileRenderer.sharedMaterial;
                if (curWillBuildTileMat != tileNormalMat) curWillBuildTileRenderer.sharedMaterial = tileNormalMat;
            }

            if (tile.occupied)
            {
                willBuildBuilding.buildingObj.SetActive(false);
                if (tile != curSelectedTile && tileMat != tileCantBuildMat) tileRenderer.sharedMaterial = tileCantBuildMat;
            }
            else
            {
                Vector3 pos = willBuildBuilding.buildingObj.transform.position;
                willBuildBuilding.buildingObj.SetActive(true);
                willBuildBuilding.buildingObj.transform.position = new Vector3(x, pos.y, z);
                if (tile != curSelectedTile && tileMat != tileCanBuildMat) tileRenderer.sharedMaterial = tileCanBuildMat;
            }

            curWillBuildTile = tile;
        }

        public BuildingConfig WillBuildBuilding(int x, int z)
        {
            Tile tile = SetSelectedTile(x, z);
            if (tile == null || tile.occupied) return null;

            tile.building = new Building(willBuildBuildingConfig);
            tile.building.buildingObj = GameObject.Instantiate<GameObject>(willBuildBuildingConfig.buildingObj, tile.tileObj.transform);
            tile.occupied = true;
            tile.isWillBuilding = true;
            tile.willBuildIndex = willBuildTiles.Count;
            willBuildTiles.Add(tile);

            var willBuildingOnTileMat = GameManager.Instance.gameSettings.buildingSettings.willBuildingOnTileMat;
            tile.building.buildingObj.ReplaceMaterials(willBuildingOnTileMat);

            willBuildBuilding.buildingObj.SetActive(false);
            return willBuildBuildingConfig;
        }

        public void CancleLastBuildBuilding()
        {
            Tile lastWillBuildTile = willBuildTiles[willBuildTiles.Count - 1];
            lastWillBuildTile.occupied = false;
            lastWillBuildTile.isWillBuilding = false;
            GameObject.DestroyImmediate(lastWillBuildTile.building.buildingObj);
            lastWillBuildTile.building = null;
        }

        public void ConfirmBuild()
        {
            for(int i = 0; i < willBuildTiles.Count; ++i)
            {
                Tile tile = willBuildTiles[i];
                tile.isWillBuilding = false;
                int buildID = tile.building.id;
                GameObject.DestroyImmediate(tile.building.buildingObj);
                BuildingConfig buildingConfig = GameManager.Instance.buildingDataBase.buildings[buildID];
                tile.building.buildingObj = GameObject.Instantiate<GameObject>(buildingConfig.buildingObj, tile.tileObj.transform);
            }
            willBuildTiles.Clear();

            GameObject.DestroyImmediate(willBuildBuilding.buildingObj);
            willBuildBuilding = null;

            if (curWillBuildTile != null) curWillBuildTile.tileObj.ReplaceMaterials(tileNormalMat, false);
        }

        public void CancleAllWillBuild()
        {

        }

        private bool CheckTilePosVailed(int x, int z)
        {
            return x >= 0 && z >= 0 && x < mapData.mapWidth && z < mapData.mapLength;
        }

        private void CreateLevel()
        {
            GameObject tileObjsContiner = new GameObject("TileObjsContiner");
            var tiles = mapData.map;
            for (int z = 0; z < mapData.mapLength; ++z)
            {
                for (int x = 0; x < mapData.mapWidth; ++x)
                {
                    Tile tile = tiles[x, z];
                    if (tile == null) tiles[x,z] = tile = new Tile(x, z);
                    tile.tileObj = GameObject.Instantiate(tileObj, new Vector3(x, 0, z), Quaternion.identity, tileObjsContiner.transform);
                    if(tile.occupied)
                    {
                        BuildingConfig buildingConfig = GameManager.Instance.buildingDataBase.buildings[tile.building.id];
                        tile.building.buildingObj = GameObject.Instantiate(buildingConfig.buildingObj, tile.tileObj.transform);
                    }
                }
            }
        }
    }
}
