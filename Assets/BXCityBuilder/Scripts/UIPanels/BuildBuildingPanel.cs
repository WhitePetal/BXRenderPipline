using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CityBuilder
{
    public class BuildBuildingPanel : UIPanel
    {
        private Slider costWoodSlider, costStoneSlider, costConeSlider, costPeopleSlider;
        private Text haveWoddText, haveStoneText, haveConeText, havePeopleText;
        private Text costWoodText, costStoneText, costConeText, costPeopleText;

        public override void InitPanel(string name, RectTransform panel)
        {
            base.InitPanel(name, panel);

            Button backBtn = panel.Find("BackBtn").GetComponent<Button>();
            backBtn.onClick.AddListener(OnBackBtnClick);

            Button exitBtn = panel.Find("ExitBtn").GetComponent<Button>();
            exitBtn.onClick.AddListener(OnExitBtnClick);

            costWoodSlider = panel.Find("CostResourceContainer/WoodSlider").GetComponent<Slider>();
            costStoneSlider = panel.Find("CostResourceContainer/StoneSlider").GetComponent<Slider>();
            costConeSlider = panel.Find("CostResourceContainer/ConeSlider").GetComponent<Slider>();
            costPeopleSlider = panel.Find("CostResourceContainer/PeopleSlider").GetComponent<Slider>();

            haveWoddText = panel.Find("CostResourceContainer/WoodSlider/MaxAmountText").GetComponent<Text>();
            haveStoneText = panel.Find("CostResourceContainer/StoneSlider/MaxAmountText").GetComponent<Text>();
            haveConeText = panel.Find("CostResourceContainer/ConeSlider/MaxAmountText").GetComponent<Text>();
            havePeopleText = panel.Find("CostResourceContainer/PeopleSlider/MaxAmountText").GetComponent<Text>();

            costWoodText = panel.Find("CostResourceContainer/WoodSlider/CurrentAmountText").GetComponent<Text>();
            costStoneText = panel.Find("CostResourceContainer/StoneSlider/CurrentAmountText").GetComponent<Text>();
            costConeText = panel.Find("CostResourceContainer/ConeSlider/CurrentAmountText").GetComponent<Text>();
            costPeopleText = panel.Find("CostResourceContainer/PeopleSlider/CurrentAmountText").GetComponent<Text>();
        }

        private void OnBackBtnClick()
        {
            if(GameManager.Instance.SetGameState(GameManager.GameState.ChangingUI))
                UIMgr.Instance.EnterPanel<SelectBuildingPanel>("SelectBuildingPanel");
        }

        private void OnExitBtnClick()
        {
            if (GameManager.Instance.SetGameState(GameManager.GameState.ChangingUI))
                UIMgr.Instance.EnterPanel<MainPanel>("MainPanel");
        }

        public override void OnPanelEnter()
        {
            panel.DOComplete();

            panel.gameObject.SetActive(true);

            MapMgr.Instance.CancleCurSelectedTile();
            BaseDataMgr.Instance.GetAllBaseData(out int maxWoodCount, out int maxStoneCount, out int maxConeCount, out int maxPeopleCount,
                out int woodCount, out int stoneCount, out int coneCount, out int peopleCount);

            haveWoddText.text = "拥有: " + woodCount;
            haveStoneText.text = "拥有: " + stoneCount;
            haveConeText.text = "拥有: " + coneCount;
            havePeopleText.text = "拥有: " + peopleCount;

            panel.DOScale(1f, 0.6f).onComplete += () =>
            {
                GameManager.Instance.SetGameState(GameManager.GameState.BuildBuilding);
            };
        }

        public override void OnPanelExit()
        {
            panel.DOComplete();

            panel.gameObject.SetActive(false);
        }
    }
}
