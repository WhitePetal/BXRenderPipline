using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace CityBuilder
{
    public class MainPanel : UIPanel
    {
        private Text maxWoodText, maxStoneText, maxConeText, maxPeopleText;
        private Text woodText, stoneText, coneText, peopleText;

        private Button saveBtn, buildingsBtn;

        public override void InitPanel(string name, RectTransform panel)
        {
            base.InitPanel(name, panel);

            maxWoodText = panel.Find("ResourcesContainer/WoodSlider/MaxAmountText").GetComponent<Text>();
            maxStoneText = panel.Find("ResourcesContainer/StoneSlider/MaxAmountText").GetComponent<Text>();

            woodText = panel.Find("ResourcesContainer/WoodSlider/CurrentAmountText").GetComponent<Text>();
            stoneText = panel.Find("ResourcesContainer/StoneSlider/CurrentAmountText").GetComponent<Text>();

            maxConeText = panel.Find("CurrencyContainer/ConeSlider/MaxAmountText").GetComponent<Text>();
            maxPeopleText = panel.Find("CurrencyContainer/PeopleSlider/MaxAmountText").GetComponent<Text>();

            coneText = panel.Find("CurrencyContainer/ConeSlider/CurrentAmountText").GetComponent<Text>();
            peopleText = panel.Find("CurrencyContainer/PeopleSlider/CurrentAmountText").GetComponent<Text>();

            BaseDataMgr.Instance.GetAllBaseData(out int maxWood, out int maxStone, out int maxCone, out int maxPeople,
                out int wood, out int stone, out int cone, out int people);

            maxWoodText.text = maxWood.ToString();
            maxStoneText.text = maxStone.ToString();
            maxConeText.text = maxCone.ToString();
            maxPeopleText.text = maxPeople.ToString();

            woodText.text = wood.ToString();
            stoneText.text = stone.ToString();
            coneText.text = cone.ToString();
            peopleText.text = people.ToString();


            saveBtn = panel.Find("BottomContainer/SaveBtn").GetComponent<Button>();
            saveBtn.onClick.AddListener(OnSaveBtnClick);

            buildingsBtn = panel.Find("RightListContainer/BuildingsBtn").GetComponent<Button>();
            buildingsBtn.onClick.AddListener(OnBuildingsBtnClick);
        }

        public override void OnPanelEnter()
        {
            panel.DOComplete();

            panel.gameObject.SetActive(true);
            panel.DOScale(2f, 0f);
            panel.DOScale(1f, 0.5f).onComplete += () =>
            {
                GameManager.Instance.SetGameState(GameManager.GameState.MainPanel);
            };
        }

        public override void OnPanelExit()
        {
            panel.DOComplete();

            panel.DOScale(1.2f, 0.5f).onComplete += () => panel.gameObject.SetActive(false);
        }

        private void OnSaveBtnClick()
        {
            BaseDataMgr.Instance.SaveBaseData();
            MapMgr.Instance.SaveMapData();
        }

        private void OnBuildingsBtnClick()
        {
            if (GameManager.Instance.SetGameState(GameManager.GameState.ChangingUI))
                UIMgr.Instance.EnterPanel<SelectBuildingPanel>("SelectBuildingPanel");
        }
    }
}
