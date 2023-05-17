using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    public class InputMgr
    {
        private static InputMgr instance;
        public static InputMgr Instance
        {
            get
            {
                if (instance == null) instance = new InputMgr();
                return instance;
            }
        }

        public Vector3 mousePrePosition;
        public Vector3 mousePosition;
        public Vector3 mouseMoveVec;

        private bool mouseLeftDownMoveing;

        private System.Action onLeftMouseUpAction;
        private System.Action onLeftMouseDownMoveAction;

        public void Init()
        {

        }

        public void RegisterOnLeftMouseUpAction(System.Action action)
        {
            onLeftMouseUpAction += action;
        }
        public void RegisterOnLeftMouseDownMoveAction(System.Action action)
        {
            onLeftMouseDownMoveAction += action;
        }

        public void CheckInput()
        {
            mousePosition = Input.mousePosition;

            if (Input.GetMouseButton(0))
            {
                if(Vector3.Distance(mousePrePosition, mousePosition) > 2f)
                {
                    mouseLeftDownMoveing = true;
                    mouseMoveVec = mousePosition - mousePrePosition;
                    if(onLeftMouseDownMoveAction != null) onLeftMouseDownMoveAction.Invoke();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (mouseLeftDownMoveing)
                {
                    mouseLeftDownMoveing = false;
                    return;
                }
                if(onLeftMouseUpAction != null) onLeftMouseUpAction.Invoke();
            }

            mousePrePosition = mousePosition;
        }
    }
}
