using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityBuilder
{
    public class SaveMgr
    {
        private static SaveMgr instance;
        public static SaveMgr Instance
        {
            get
            {
                if (instance == null) instance = new SaveMgr();
                return instance;
            }
        }

        private readonly static char[] encryptKeys = { 'b', 'a', 'x', 's', 'd', 'b', 't', 'w', 'h', 'q', 'f' };

        public BaseData GetBaseData()
        {
            string json;
            BaseData baseData = null;
            if(ReadFromFile(Constants.baseDataSavedName, out json))
            {
                baseData = JsonUtility.FromJson<BaseData>(json);
            }
            return baseData;
        }

        public bool SaveBaseData(BaseData baseData)
        {
            string json = JsonUtility.ToJson(baseData);
            return WriteToFile(Constants.baseDataSavedName, json);
        }

        public MapData GetMapData()
        {
            string json;
            MapData mapData = null;
            if(ReadFromFile(Constants.mapDataSaveName, out json))
            {
                mapData = JsonUtility.FromJson<MapData>(json);
            }
            return mapData;
        }

        public bool SaveMapData(MapData mapData)
        {
            string json = JsonUtility.ToJson(mapData);
            return WriteToFile(Constants.mapDataSaveName, json);
        }

        private bool WriteToFile(string name, string content)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, name);

            try
            {
                File.WriteAllText(fullPath, EncryptContent(content));
                Debug.Log("INFO: Write To File Successs " + fullPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("WARNING: Write To File Failed Path: " + fullPath + " ==== Message: " + e.Message);
            }
            return false;
        }

        private bool ReadFromFile(string name, out string content)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, name);

            try
            {
                content = File.ReadAllText(fullPath);
                content = EncryptContent(content);
                Debug.Log("INFO: Read From File Successs " + fullPath);
                return true;
            }
            catch (Exception e)
            {
                content = null;
                Debug.LogWarning("WARNING: Read From File Failed Path: " + fullPath + " ===== Message: " + e.Message);
            }
            return false;
        }

        private string EncryptContent(string content)
        {
#if UNITY_EDITOR
            return content;
#else
            char[] charAr = content.ToCharArray();
            for (int i = 0; i < charAr.Length; ++i)
            {
                charAr[i] = (char)(charAr[i] ^ encryptKeys[i % encryptKeys.Length]);
            }
            return new string(charAr);
#endif
        }
    }

}