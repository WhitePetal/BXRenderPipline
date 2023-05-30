using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        public Vector2 mouseLeaveScrennDir;

        private bool mouseLeftDownMoveing;

        private System.Action onLeftMouseUpAction;
        private System.Action onLeftMouseDownMoveAction;
        private System.Action onFreeMouseLeaveScreenAction;
        private System.Action onFreeMouseMoveAction;

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
        public void RegisterOnFreeMouseLeaveScreenAction(System.Action action)
        {
            onFreeMouseLeaveScreenAction += action;
        }
        public void RegisterOnFreeMouseMoveAction(System.Action action)
        {
            onFreeMouseMoveAction += action;
        }

        public void CheckInput()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            mousePosition = Input.mousePosition;
            bool mouseFree = true;

            if (Input.GetMouseButton(0))
            {
                mouseFree = false;
                if(Vector3.Distance(mousePrePosition, mousePosition) > 2f)
                {
                    mouseLeftDownMoveing = true;
                    if (onLeftMouseDownMoveAction != null)
                    {
                        mouseMoveVec = mousePosition - mousePrePosition;
                        onLeftMouseDownMoveAction.Invoke();
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                mouseFree = false;
                if (mouseLeftDownMoveing)
                {
                    mouseLeftDownMoveing = false;
                }
                else
                {
                    if(onLeftMouseUpAction != null) onLeftMouseUpAction.Invoke();
                }
            }

            if (mouseFree)
            {
                if(Vector3.Distance(mousePrePosition, mousePosition) > 2f)
                {
                    if (onFreeMouseMoveAction != null) onFreeMouseMoveAction.Invoke();
                }
                if(mousePosition.x < 0 || mousePosition.y < 0 || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
                {
                    if (onFreeMouseLeaveScreenAction != null)
                    {
                        float x = mousePosition.x > Screen.width ? (mousePosition.x - Screen.width) : (mousePosition.x < 0f ? mousePosition.x : 0f);
                        float y = mousePosition.y > Screen.height ? (mousePosition.y - Screen.height) : (mousePosition.y < 0f ? mousePosition.y : 0f);
                        mouseLeaveScrennDir = new Vector2(x, y).normalized;
                        onFreeMouseLeaveScreenAction.Invoke();
                    }
                }
            }

            mousePrePosition = mousePosition;
        }
    }
}
