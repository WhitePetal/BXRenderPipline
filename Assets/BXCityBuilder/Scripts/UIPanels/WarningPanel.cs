using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CityBuilder
{
    public class WarningPanel : UIPanel
    {
        private Text warningInfo;
        private RectTransform window;

        public override void InitPanel(string name, RectTransform panel)
        {
            base.InitPanel(name, panel);

            window = panel.Find("Window") as RectTransform;
            warningInfo = panel.Find("Window/Text").GetComponent<Text>();

            var confirmBtn = panel.Find("Window/ConfirmBtn").GetComponent<Button>();
            confirmBtn.onClick.AddListener(OnConfirmClick);
        }

        public void ShowMessage(string message)
        {
            warningInfo.text = message;
        }

        private void OnConfirmClick()
        {
            UIMgr.Instance.EnterPrePanel();
        }

        public override void OnPanelEnter()
        {
            window.DOComplete();

            UIMgr.Instance.FreezeAllUI();

            window.localScale = Vector3.zero;
            panel.gameObject.SetActive(true);
            GameManager.Instance.SetGameState(GameManager.GameState.WarningPanel);

            window.DOScale(1f, 0.5f).onComplete += () => UIMgr.Instance.UnFreezeAllUI();
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
