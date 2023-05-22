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
        public CinemachineVirtualCamera inWorldCam;
        private CinemachineVirtualCamera sleepCam;

        public int test;

        public GameSettings gameSettings;
        public BuildingDataBase buildingDataBase;

        public GameState gameState;

        private Vector3 worldViewCamRotAroundPoint;
        private float worldViewCamTargetRotAngle;
        private float worldViewCamHaveRotAngle;
        private float worldViewCamRotStep;
        private bool worldViewCamRoting;

        private void Awake()
        {
            this.sleepCam = inWorldCam;

            TimerMgr.Instance.Init();
            TimerMgr.Instance.StartTimer();

            for(int i = 1; i <= 4; ++i)
            {
                for(int k = 0; k < 1000; ++k)
                {
                    TimerMgr.Instance.AddFrameTask(
                        (id) =>
                        {
                            ++test;
                        },
                        1,
                        i
                        );
                }
            }

            BaseDataMgr.Instance.Init();
            MapMgr.Instance.Init(gameSettings.tileSettings);
            UIMgr.Instance.Init();
            InputMgr.Instance.Init();

            InputMgr.Instance.RegisterOnLeftMouseUpAction(OnLeftMouseUp);
            InputMgr.Instance.RegisterOnLeftMouseDownMoveAction(OnLeftMouseDownMove);
            InputMgr.Instance.RegisterOnFreeMouseLeaveScreenAction(OnFreeMouseLeaveScreen);
            InputMgr.Instance.RegisterOnFreeMouseMoveAction(OnFreeMouseMove);
        }

        // Update is called once per frame
        void Update()
        {
            InputMgr.Instance.CheckInput();
            TimerMgr.Instance.Runner();

            if(worldViewCamHaveRotAngle != worldViewCamTargetRotAngle)
            {
                float rot = worldViewCamRotStep * Time.deltaTime;
                worldViewCam.transform.RotateAround(worldViewCamRotAroundPoint, Vector3.up, rot);
                worldViewCamHaveRotAngle += rot;
                if (worldViewCamTargetRotAngle < 0 && worldViewCamHaveRotAngle < worldViewCamTargetRotAngle)
                {
                    worldViewCamHaveRotAngle = worldViewCamTargetRotAngle = 0;
                    worldViewCamRoting = false;
                }
                if (worldViewCamTargetRotAngle >= 0 && worldViewCamHaveRotAngle > worldViewCamTargetRotAngle)
                {
                    worldViewCamHaveRotAngle = worldViewCamTargetRotAngle = 0;
                    worldViewCamRoting = false;
                }
            }
        }

        public bool SetGameState(GameState state)
        {
            this.gameState = state;
            return true;
        }

        public void RotateWorldViewCamera(float roteAngle)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1));
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                worldViewCamRotAroundPoint = hitInfo.point;
                worldViewCamTargetRotAngle = worldViewCamTargetRotAngle - worldViewCamHaveRotAngle + roteAngle;
                worldViewCamRotStep = Mathf.Sign(worldViewCamTargetRotAngle - worldViewCamHaveRotAngle) * gameSettings.worldViewCameraSettings.rotateSpeed;
                worldViewCamRoting = true;
            }
        }

        public void ZoomWorldViewCamera(int zoom)
        {
            float targetOrtSize = worldViewCam.m_Lens.OrthographicSize + gameSettings.worldViewCameraSettings.zoomStpe * zoom;
            targetOrtSize = targetOrtSize < gameSettings.worldViewCameraSettings.zoomMin ? gameSettings.worldViewCameraSettings.zoomMin : targetOrtSize;
            targetOrtSize = targetOrtSize > gameSettings.worldViewCameraSettings.zoomMax ? gameSettings.worldViewCameraSettings.zoomMax : targetOrtSize;
            worldViewCam.m_Lens.OrthographicSize = targetOrtSize;
        }

        public void ChangeView()
        {
            sleepCam.MoveToTopOfPrioritySubqueue();
            sleepCam = sleepCam == worldViewCam ? inWorldCam : worldViewCam;
        }

        private void SetWorldViewCameraPos(Vector3 pos)
        {
            if (worldViewCamRoting) return;
            if (pos.x <= -10 || pos.x >= MapMgr.Instance.MapWidth + 10)
            {
                pos.x = worldViewCam.transform.position.x;
            }
            if (pos.z <= -10 || pos.z >= MapMgr.Instance.MapLength + 10)
            {
                pos.z = worldViewCam.transform.position.z;
            }
            worldViewCam.transform.position = pos;
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

        private void OnFreeMouseLeaveScreen()
        {
            switch (gameState)
            {
                case GameState.MainPanel:
                    OnFreeMouseLeaveScreen_MainPanel_BuilBuildingPanel();
                    break;
                case GameState.BuildBuilding:
                    OnFreeMouseLeaveScreen_MainPanel_BuilBuildingPanel();
                    break;
            }
        }

        private void OnFreeMouseMove()
        {
            switch (gameState)
            {
                case GameState.MainPanel:
                    break;
                case GameState.BuildBuilding:
                    OnFreeMouseMove_BuilBuildingPanel();
                    break;
            }
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
                MapMgr.Instance.BuildBuilding(Mathf.FloorToInt(mousePos.x + 0.5f), Mathf.FloorToInt(mousePos.z + 0.5f));
            }
        }
        private void OnLeftMouseDownMove_MainPanel_BuildBuildingPanel()
        {
            Vector3 camTargetPos = worldViewCam.transform.position + worldViewCam.transform.right * InputMgr.Instance.mouseMoveVec.x * Time.deltaTime * gameSettings.worldViewCameraSettings.horizontalMoveSpeed;
            camTargetPos += new Vector3(worldViewCam.transform.forward.x, 0, worldViewCam.transform.forward.z).normalized * InputMgr.Instance.mouseMoveVec.y * Time.deltaTime * gameSettings.worldViewCameraSettings.verticalMoveSpeed;
            SetWorldViewCameraPos(camTargetPos);
        }
        private void OnFreeMouseLeaveScreen_MainPanel_BuilBuildingPanel()
        {
            Vector3 camTargetPos = worldViewCam.transform.position + worldViewCam.transform.right * InputMgr.Instance.mouseLeaveScrennDir.x * Time.deltaTime * gameSettings.worldViewCameraSettings.horizontalLeaveScreenMoveSpeed;
            camTargetPos += new Vector3(worldViewCam.transform.forward.x, 0, worldViewCam.transform.forward.z).normalized * InputMgr.Instance.mouseLeaveScrennDir.y * Time.deltaTime * gameSettings.worldViewCameraSettings.verticalLevaeScreenMoveSpeed;
            SetWorldViewCameraPos(camTargetPos);
        }
        private void OnFreeMouseMove_BuilBuildingPanel()
        {
            Ray ray = Camera.main.ScreenPointToRay(InputMgr.Instance.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                Vector3 mousePos = hitInfo.point;
                MapMgr.Instance.WillBuildBuilding(Mathf.FloorToInt(mousePos.x + 0.5f), Mathf.FloorToInt(mousePos.z + 0.5f));
            }
        }
    }
}
