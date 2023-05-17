using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace CityBuilder
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<GameManager>();
                    if (instance == null)
                    {
                        instance = new GameObject("GameManager").AddComponent<GameManager>();
                    }
                }
                return instance;
            }
        }

        public enum GameState
        {
            ChangingUI,
            MainPanel,
            SelectBuilding,
            BuildBuilding
        }

        public CinemachineVirtualCamera worldViewCam;

        public GameSettings gameSettings;
        public BuildingDataBase buildingDataBase;

        public GameState gameState;

        private int willBuildBuildingID;

        private void Awake()
        {
            BaseDataMgr.Instance.Init();
            MapMgr.Instance.Init(gameSettings.tileSettings);
            UIMgr.Instance.Init();
            InputMgr.Instance.Init();

            InputMgr.Instance.RegisterOnLeftMouseUpAction(OnLeftMouseUp);
            InputMgr.Instance.RegisterOnLeftMouseDownMoveAction(OnLeftMouseDownMove);
        }

        // Update is called once per frame
        void Update()
        {
            InputMgr.Instance.CheckInput();


        }

        public bool SetGameState(GameState state)
        {
            this.gameState = state;
            return true;
        }

        private void OnLeftMouseUp()
        {
            switch (gameState)
            {
                case GameState.MainPanel:
                    OnLeftMouseUp_MainPanel();
                    break;
                case GameState.BuildBuilding:
                    OnLeftMouseUp_BuildBuildingPanel();
                    break;
            }
        }

        private void OnLeftMouseDownMove()
        {
            switch (gameState)
            {
                case GameState.MainPanel:
                    OnLeftMouseDownMove_MainPanel_BuildBuildingPanel();
                    break;
                case GameState.BuildBuilding:
                    OnLeftMouseDownMove_MainPanel_BuildBuildingPanel();
                    break;
            }
        }

        public void SetWillBuildBuilding(int buildingId)
        {
            this.willBuildBuildingID = buildingId;
        }

        private void OnLeftMouseUp_MainPanel()
        {
            Ray ray = Camera.main.ScreenPointToRay(InputMgr.Instance.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Vector3 mousePos = hitInfo.point;
                MapMgr.Instance.SetSelectedTile(Mathf.FloorToInt(mousePos.x + 0.5f), Mathf.FloorToInt(mousePos.z + 0.5f));
            }
        }
        private void OnLeftMouseUp_BuildBuildingPanel()
        {
            Ray ray = Camera.main.ScreenPointToRay(InputMgr.Instance.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Vector3 mousePos = hitInfo.point;
                MapMgr.Instance.WillBuildBuilding(willBuildBuildingID, Mathf.FloorToInt(mousePos.x + 0.5f), Mathf.FloorToInt(mousePos.z + 0.5f));
            }
        }

        private void OnLeftMouseDownMove_MainPanel_BuildBuildingPanel()
        {
            Vector3 camTargetPos = worldViewCam.transform.position + worldViewCam.transform.right * InputMgr.Instance.mouseMoveVec.x * Time.deltaTime * gameSettings.worldViewCameraSettings.horizontalMoveSpeed;
            camTargetPos += new Vector3(worldViewCam.transform.forward.x, 0, worldViewCam.transform.forward.z).normalized * InputMgr.Instance.mouseMoveVec.y * Time.deltaTime * gameSettings.worldViewCameraSettings.horizontalMoveSpeed;
            if (camTargetPos.x <= -10 || camTargetPos.x >= MapMgr.Instance.MapWidth + 10)
            {
                camTargetPos.x = worldViewCam.transform.position.x;
            }
            if (camTargetPos.z <= -10 || camTargetPos.z >= MapMgr.Instance.MapLength + 10)
            {
                camTargetPos.z = worldViewCam.transform.position.z;
            }
            worldViewCam.transform.position = camTargetPos;
        }
    }
}
