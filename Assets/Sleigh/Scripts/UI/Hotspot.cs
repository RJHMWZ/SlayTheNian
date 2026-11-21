using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleigh
{
    public class Hotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action click, enter, exit;
        public Action<bool> press;
        public Action<PointerEventData> beginDrag, drag, endDrag;
        public bool enabledAction = true;
        public bool hovered;
        bool pointerDown, wasDragged;
        int activePointer;
        public void ClearBindings()
        { ResetInput(); click = enter = exit = null; press = null; beginDrag = drag = endDrag = null; enabledAction = true; wasDragged = false; }
        void ResetInput()
        {
            bool held = pointerDown, over = hovered; pointerDown = hovered = false;
            if (held) press?.Invoke(false);
            if (over) exit?.Invoke();
        }
        void OnDisable() => ResetInput();
        void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        public void OnPointerDown(PointerEventData e)
        {
            if (!enabledAction || pointerDown || e.button != PointerEventData.InputButton.Left) return;
            activePointer = e.pointerId; pointerDown = true; wasDragged = false; press?.Invoke(true);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (!pointerDown || activePointer != e.pointerId) return;
            pointerDown = false; press?.Invoke(false);
            if (e.pointerId >= 0) { hovered = false; exit?.Invoke(); }
        }
        public void OnPointerClick(PointerEventData e) { if (enabledAction && !wasDragged && !e.dragging && e.button == PointerEventData.InputButton.Left) click?.Invoke(); }
        public void OnPointerEnter(PointerEventData e) { if (!enabledAction) return; hovered = true; enter?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; exit?.Invoke(); if (pointerDown) press?.Invoke(false); }
        public void OnBeginDrag(PointerEventData e) { wasDragged = true; press?.Invoke(false); if (enabledAction) beginDrag?.Invoke(e); }
        public void OnDrag(PointerEventData e) { if (enabledAction) drag?.Invoke(e); }
        public void OnEndDrag(PointerEventData e) { if (enabledAction) endDrag?.Invoke(e); }
    }
}
