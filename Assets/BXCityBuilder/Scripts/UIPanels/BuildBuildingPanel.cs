using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CityBuilder
{
    public class BuildBuildingPanel : UIPanel
    {
        public override void InitPanel(string name, RectTransform panel)
        {
            base.InitPanel(name, panel);

            Button backBtn = panel.Find("BackBtn").GetComponent<Button>();
            backBtn.onClick.AddListener(OnBackBtnClick);

            Button exitBtn = panel.Find("ExitBtn").GetComponent<Button>();
            exitBtn.onClick.AddListener(OnExitBtnClick);
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
