using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace CityBuilder
{
    public abstract class UIPanel
    {
        public string name;
        public RectTransform panel;

        public virtual void InitPanel(string name, RectTransform panel)
        {
            this.name = name;
            this.panel = panel;
        }

        public abstract void OnPanelEnter();
        public abstract void OnPanelExit();
        public abstract void OnPanelRefresh();
    }
}
