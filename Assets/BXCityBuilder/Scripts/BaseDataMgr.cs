using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public class BaseDataMgr
    {
        private static BaseDataMgr instance;
        public static BaseDataMgr Instance
        {
            get
            {
                if (instance == null) instance = new BaseDataMgr();
                return instance;
            }
        }

        private BaseData baseData;

        private int willCostWood, willCostStone, willCostCone, willCostPeople;

        public void Init()
        {
            baseData = SaveMgr.Instance.GetBaseData();

            if (baseData == null)
            {
                baseData = new BaseData(Constants.initMaxWood, Constants.initMaxStone, Constants.initMaxCone, Constants.initMaxPeople,
                    Constants.initWood, Constants.initStone, Constants.initCone, Constants.initPeople);
            }
        }

        public void GetAllBaseData(out int maxWood, out int maxStone, out int maxCone, out int maxPeople,
            out int wood, out int stone, out int cone, out int people)
        {
            maxWood = baseData.maxWood;
            maxStone = baseData.maxStone;
            maxCone = baseData.maxCone;
            maxPeople = baseData.maxPeople;

            wood = baseData.wood;
            stone = baseData.stone;
            cone = baseData.cone;
            people = baseData.pepole;
        }

        public void GetAllCostData(out int costWood, out int costStone, out int costCone, out int costPeople)
        {
            costWood = willCostWood;
            costStone = willCostStone;
            costCone = willCostCone;
            costPeople = willCostPeople;
        }

        public int GetMaxWoodCount()
        {
            return baseData.maxWood;
        }

        public int GetWoodCount()
        {
            return baseData.wood;
        }

        public void AddWoodCount(int addCount)
        {
            baseData.wood += addCount;
        }

        public void SaveBaseData()
        {
            SaveMgr.Instance.SaveBaseData(baseData);
        }

        public bool WillBuildBuilding(BuildingConfig config, out int outofWood, out int outofStone, out int outofCone, out int outofPeople)
        {
            int targetCostWood = willCostWood + config.costWood;
            int targetCostStone = willCostStone + config.costStone;
            int targetCostCone = willCostCone + config.costCone;
            int targetCostPeople = willCostPeople + config.costPeople;

            outofWood = targetCostWood - baseData.wood;
            outofStone = targetCostStone - baseData.stone;
            outofCone = targetCostCone - baseData.cone;
            outofPeople = targetCostPeople - baseData.pepole;

            if(outofWood > 0 || outofStone > 0 || outofCone > 0 || outofPeople > 0)
            {
                return false;
            }

            willCostWood = targetCostWood;
            willCostStone = targetCostStone;
            willCostCone = targetCostCone;
            willCostPeople = targetCostPeople;
            return true;
        }

        public void ConfirmBuild()
        {
            baseData.wood -= willCostWood;
            baseData.stone -= willCostStone;
            baseData.cone -= willCostCone;
            baseData.pepole -= willCostPeople;

            willCostWood = 0;
            willCostStone = 0;
            willCostCone = 0;
            willCostPeople = 0;
        }

        public void CancleAllWillBuild()
        {

        }
    }
}
