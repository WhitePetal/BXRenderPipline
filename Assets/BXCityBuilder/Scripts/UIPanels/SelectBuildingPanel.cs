using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Text;

namespace CityBuilder
{
    public class SelectBuildingPanel : UIPanel
    {
        private RectTransform window;

        public override void InitPanel(string name, RectTransform panel)
        {
            base.InitPanel(name, panel);

            window = panel.Find("SelectBuildingWindow").GetComponent<RectTransform>();

            Button closeBtn = panel.Find("SelectBuildingWindow/CloseWindowBtn").GetComponent<Button>();
            closeBtn.onClick.AddListener(OnCloseBtnClick);

            RectTransform content = panel.Find("SelectBuildingWindow/Scroll View/Viewport/Content").GetComponent<RectTransform>();

            GameObject buildingCardItem = ResourcesMgr.Instance.LoadAssetsAtPath<GameObject>("BXCityBuilder/Prefabs/UIItems/BuildingCardItem.prefab");
            var buildingConfigs = GameManager.Instance.buildingDataBase.buildings;
            for (int i = 0; i < buildingConfigs.Count; ++i)
            {
                BuildingConfig buildingConfig = buildingConfigs[i];
                GameObject buildingCard = GameObject.Instantiate<GameObject>(buildingCardItem, content);
                buildingCard.name = "BuildingCard_" + buildingConfig.buildingObj.name;
                Image buildingCardImg = buildingCard.transform.Find("Img").GetComponent<Image>();
                buildingCardImg.sprite = buildingConfig.buildingSprite;
                Text buildingCardText = buildingCard.transform.Find("Text").GetComponent<Text>();
                buildingCardText.text = buildingConfig.buildingDescript;
                Button buildingCardBtn = buildingCard.transform.Find("Btn").GetComponent<Button>();
                buildingCardBtn.onClick.AddListener(OnBuildingCardClick(buildingConfig.id));
            }
        }

        private void OnCloseBtnClick()
        {
            UIMgr.Instance.EnterPrePanel();
        }

        private UnityEngine.Events.UnityAction OnBuildingCardClick(int buildingID)
        {
            return () =>
            {
                UIMgr.Instance.EnterPanel<BuildBuildingPanel>("BuildBuildingPanel");
                MapMgr.Instance.SetWillBuildBuildingInfo(buildingID);
            };
        }

        public override void OnPanelEnter()
        {
            window.DOComplete();
            panel.gameObject.SetActive(true);
            GameManager.Instance.SetGameState(GameManager.GameState.SelectBuilding);
            UIMgr.Instance.FreezeAllUI();

            window.DOScale(0f, 0f);
            window.DOScale(1f, 0.5f).onComplete += () => UIMgr.Instance.UnFreezeAllUI(); ;
        }

        public override void OnPanelExit()
        {
            window.DOComplete();
            UIMgr.Instance.FreezeAllUI();
            window.DOScale(0f, 0.5f).onComplete += () =>
            {
                panel.gameObject.SetActive(false);
                UIMgr.Instance.UnFreezeAllUI();
            };
        }

        public override void OnPanelRefresh()
        {
            
        }
    }
}
