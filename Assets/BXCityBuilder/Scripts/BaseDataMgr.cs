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
    }
}
