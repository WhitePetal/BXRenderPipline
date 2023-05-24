using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder
{
    public class UIMgr
    {
        private static UIMgr instance;
        public static UIMgr Instance
        {
            get
            {
                if (instance == null) instance = new UIMgr();
                return instance;
            }
        }

        private RectTransform canvas;
        private CanvasGroup canvasGroup;
        private Dictionary<string, UIPanel> uiPanelPoor;
        private Stack<UIPanel> uiPanelStack;
        private UIPanel prePanel;

        public void Init()
        {
            uiPanelPoor = new Dictionary<string, UIPanel>();
            uiPanelStack = new Stack<UIPanel>();
            canvas = GameObject.Find("Canvas").GetComponent<RectTransform>();
            canvasGroup = canvas.GetComponent<CanvasGroup>();

            MainPanel mainPanel = new MainPanel();
            mainPanel.InitPanel("MainPanel", canvas.Find("MainPanel").GetComponent<RectTransform>());
            EnterPanel(mainPanel);
        }

        public void FreezeAllUI()
        {
            canvasGroup.interactable = false;
        }

        public void UnFreezeAllUI()
        {
            canvasGroup.interactable = true;
        }

        public void EnterPanel(UIPanel panel)
        {
            ExitTopPanel();
            if (GameManager.Instance.SetGameState(GameManager.GameState.ChangingUI))
            {
                uiPanelStack.Push(panel);
                uiPanelPoor[panel.name] = panel;
                panel.OnPanelEnter();
            }
        }

        public T EnterPanel<T>(string name) where T : UIPanel, new()
        {
            T panel;
            if (uiPanelPoor.ContainsKey(name))
            {
                panel = (T)uiPanelPoor[name];
            }
            else
            {
                panel = new T();
                panel.InitPanel(name, canvas.Find(name).GetComponent<RectTransform>());
            }
            EnterPanel(panel);
            return panel;
        }

        public void EnterPrePanel()
        {
            if (prePanel == null) return;
            EnterPanel(prePanel);
        }

        public void ExitTopPanel()
        {
            if (uiPanelStack.Count == 0) return;
            if (GameManager.Instance.SetGameState(GameManager.GameState.ChangingUI))
            {
                UIPanel panel = uiPanelStack.Pop();
                panel.OnPanelExit();
                prePanel = panel;
            }
        }

        public void RefreshPanel()
        {
            if (uiPanelStack.Count == 0) return;
            uiPanelStack.Peek().OnPanelRefresh();
        }
    }
}
