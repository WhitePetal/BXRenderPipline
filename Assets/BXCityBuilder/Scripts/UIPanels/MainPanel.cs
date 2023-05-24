using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace CityBuilder
{
    public class MainPanel : UIPanel
    {
        private Text maxWoodCountText, maxStoneCountText, maxConeCountText, maxPeopleCountText;
        private Text woodCountText, stoneCountText, coneCountText, peopleCountText;
        private Slider woodCountSlider, stoneCountSlider, coneCountSlider, peopleCountSlider;

        public override void InitPanel(string name, RectTransform panel)
        {
            base.InitPanel(name, panel);

            maxWoodCountText = panel.Find("ResourcesContainer/WoodSlider/MaxAmountText").GetComponent<Text>();
            maxStoneCountText = panel.Find("ResourcesContainer/StoneSlider/MaxAmountText").GetComponent<Text>();

            woodCountText = panel.Find("ResourcesContainer/WoodSlider/CurrentAmountText").GetComponent<Text>();
            stoneCountText = panel.Find("ResourcesContainer/StoneSlider/CurrentAmountText").GetComponent<Text>();

            maxConeCountText = panel.Find("CurrencyContainer/ConeSlider/MaxAmountText").GetComponent<Text>();
            maxPeopleCountText = panel.Find("CurrencyContainer/PeopleSlider/MaxAmountText").GetComponent<Text>();

            coneCountText = panel.Find("CurrencyContainer/ConeSlider/CurrentAmountText").GetComponent<Text>();
            peopleCountText = panel.Find("CurrencyContainer/PeopleSlider/CurrentAmountText").GetComponent<Text>();

            BaseDataMgr.Instance.GetAllBaseData(out int maxWoodCount, out int maxStoneCount, out int maxConeCount, out int maxPeopleCount,
                out int woodCount, out int stoneCount, out int coneCount, out int peopleCount);

            maxWoodCountText.text = "最大: " + maxWoodCount.ToString();
            maxStoneCountText.text = "最大: " + maxStoneCount.ToString();
            maxConeCountText.text = "最大: " + maxConeCount.ToString();
            maxPeopleCountText.text = "最大: " + maxPeopleCount.ToString();

            woodCountText.text = woodCount.ToString();
            stoneCountText.text = stoneCount.ToString();
            coneCountText.text = coneCount.ToString();
            peopleCountText.text = peopleCount.ToString();


            Button saveBtn = panel.Find("BottomContainer/SaveBtn").GetComponent<Button>();
            saveBtn.onClick.AddListener(OnSaveBtnClick);

            Button buildingsBtn = panel.Find("RightListContainer/BuildingsBtn").GetComponent<Button>();
            buildingsBtn.onClick.AddListener(OnBuildingsBtnClick);

            Button leftRoteWorldViewBtn = panel.Find("BottomContainer/LeftRotateWorldViewBtn").GetComponent<Button>();
            Button rightRoteWorldViewBtn = panel.Find("BottomContainer/RightRotateWorldViewBtn").GetComponent<Button>();
            leftRoteWorldViewBtn.onClick.AddListener(OnLeftRotateWorldViewBtnClick);
            rightRoteWorldViewBtn.onClick.AddListener(OnRightRotateWorldViewBtnClick);

            Button zoomInWorldViewBtn = panel.Find("BottomContainer/ZoomInWorldVeiwBtn").GetComponent<Button>();
            Button zoomOutWorldViewBtn = panel.Find("BottomContainer/ZoomOutWorldVeiwBtn").GetComponent<Button>();
            zoomInWorldViewBtn.onClick.AddListener(OnZoomInWorldViewBtnClick);
            zoomOutWorldViewBtn.onClick.AddListener(OnZoomOutWorldViewBtnClick);

            Button changeViewBtn = panel.Find("BottomContainer/ChangeViewBtn").GetComponent<Button>();
            changeViewBtn.onClick.AddListener(OnChangeViewBtnClick);

            woodCountSlider = panel.Find("ResourcesContainer/WoodSlider").GetComponent<Slider>();
            stoneCountSlider = panel.Find("ResourcesContainer/StoneSlider").GetComponent<Slider>();
            coneCountSlider = panel.Find("CurrencyContainer/ConeSlider").GetComponent<Slider>();
            peopleCountSlider = panel.Find("CurrencyContainer/PeopleSlider").GetComponent<Slider>();

            woodCountSlider.value = (float)woodCount / maxWoodCount;
            stoneCountSlider.value = (float)stoneCount / maxStoneCount;
            coneCountSlider.value = (float)coneCount / maxConeCount;
            peopleCountSlider.value = (float)peopleCount / maxPeopleCount;
        }

        public override void OnPanelEnter()
        {
            panel.DOComplete();

            panel.gameObject.SetActive(true);
            GameManager.Instance.SetGameState(GameManager.GameState.MainPanel);
            UIMgr.Instance.FreezeAllUI();

            panel.DOScale(2f, 0f);
            panel.DOScale(1f, 0.5f).onComplete += () => UIMgr.Instance.UnFreezeAllUI();
        }

        public override void OnPanelExit()
        {
            panel.DOComplete();

            UIMgr.Instance.FreezeAllUI();
            panel.DOScale(1.2f, 0.5f).onComplete += () =>
            {
                panel.gameObject.SetActive(false);
                UIMgr.Instance.UnFreezeAllUI();
            };
        }

        public override void OnPanelRefresh()
        {
            
        }

        private void OnSaveBtnClick()
        {
            BaseDataMgr.Instance.SaveBaseData();
            MapMgr.Instance.SaveMapData();
        }

        private void OnBuildingsBtnClick()
        {
            UIMgr.Instance.EnterPanel<SelectBuildingPanel>("SelectBuildingPanel");
        }

        private void OnLeftRotateWorldViewBtnClick()
        {
            GameManager.Instance.RotateWorldViewCamera(45f);
        }

        private void OnRightRotateWorldViewBtnClick()
        {
            GameManager.Instance.RotateWorldViewCamera(-45f);
        }

        private void OnZoomInWorldViewBtnClick()
        {
            GameManager.Instance.ZoomWorldViewCamera(1);
        }

        private void OnZoomOutWorldViewBtnClick()
        {
            GameManager.Instance.ZoomWorldViewCamera(-1);
        }

        private void OnChangeViewBtnClick()
        {
            GameManager.Instance.ChangeView();
        }
    }
}
