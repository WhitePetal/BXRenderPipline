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
        private RectTransform exitConfirmPanel;
        private RectTransform exitConfirmWindow;
        private Text exitConfirmText;

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

            exitConfirmPanel = panel.Find("ExitConfirmPanel").GetComponent<RectTransform>();
            exitConfirmWindow = panel.Find("ExitConfirmPanel/Window").GetComponent<RectTransform>();
            exitConfirmText = panel.Find("ExitConfirmPanel/Window/Text").GetComponent<Text>();
        }

        private void OnBackBtnClick()
        {
            UIMgr.Instance.EnterPanel<SelectBuildingPanel>("SelectBuildingPanel");
        }

        private void OnExitBtnClick()
        {
            exitConfirmPanel.gameObject.SetActive(true);

            exitConfirmWindow.DOComplete();
            exitConfirmWindow.localScale = Vector3.zero;
            exitConfirmWindow.gameObject.SetActive(true);

            exitConfirmWindow.localScale = Vector3.zero;
            exitConfirmWindow.DOScale(1f, 0.5f).onComplete += () => UIMgr.Instance.UnFreezeAllUI();

            BaseDataMgr.Instance.GetAllCostData(out int costWood, out int costStone, out int costCone, out int costPeople);
            string woodCountText = TextUtils.GetColoredString(GameManager.Instance.gameSettings.uiSettings.woodTextColor, costWood.ToString());
            string stoneCountText = TextUtils.GetColoredString(GameManager.Instance.gameSettings.uiSettings.stoneTextColor, costStone.ToString());
            string coneCountText = TextUtils.GetColoredString(GameManager.Instance.gameSettings.uiSettings.coneTextColor, costCone.ToString());
            string peopleCountText = TextUtils.GetColoredString(GameManager.Instance.gameSettings.uiSettings.peopleTextColor, costPeople.ToString());

            string info = "  建造共花费: " + woodCountText + " 木料, " + stoneCountText + " 石料, " + coneCountText + " 金币, " + peopleCountText + " 人力\n";
            info += "  确认建造吗?";
            exitConfirmText.text = info;
        }

        public override void OnPanelEnter()
        {
            panel.DOComplete();

            panel.localScale = Vector3.one * 1.2f;
            panel.gameObject.SetActive(true);

            GameManager.Instance.SetGameState(GameManager.GameState.BuildBuilding);
            MapMgr.Instance.CancleCurSelectedTile();
            OnPanelRefresh();

            panel.DOScale(1f, 0.6f);
        }

        public override void OnPanelExit()
        {
            panel.DOComplete();

            UIMgr.Instance.FreezeAllUI();
            panel.DOScale(1.2f, 0.6f).onComplete += () =>
            {
                panel.gameObject.SetActive(false);
                UIMgr.Instance.UnFreezeAllUI();
            };
        }

        public override void OnPanelRefresh()
        {
            BaseDataMgr.Instance.GetAllBaseData(out int maxWoodCount, out int maxStoneCount, out int maxConeCount, out int maxPeopleCount,
                out int woodCount, out int stoneCount, out int coneCount, out int peopleCount);
            BaseDataMgr.Instance.GetAllCostData(out int costWood, out int costStone, out int costCone, out int costPeople);

            float costWoodValue = (float)costWood / woodCount;
            float costStoneValue = (float)costStone / stoneCount;
            float costConeValue = (float)costCone / coneCount;
            float costPeopleValue = (float)costPeople / peopleCount;

            haveWoddText.text = "拥有: " + woodCount;
            haveStoneText.text = "拥有: " + stoneCount;
            haveConeText.text = "拥有: " + coneCount;
            havePeopleText.text = "拥有: " + peopleCount;

            costWoodSlider.value = costWoodValue;
            costStoneSlider.value = costStoneValue;
            costConeSlider.value = costConeValue;
            costPeopleSlider.value = costPeopleValue;

            costWoodText.text = costWood.ToString();
            costStoneText.text = costStone.ToString();
            costConeText.text = costCone.ToString();
            costPeopleText.text = costPeople.ToString();
        }
    }
}
