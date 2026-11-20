using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleigh
{
    public class Hotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action click, enter, exit;
        public Action<PointerEventData> beginDrag, drag, endDrag;
        public bool enabledAction = true;
        public bool hovered;
        public void ClearBindings()
        { click = enter = exit = null; beginDrag = drag = endDrag = null; enabledAction = true; hovered = false; }
        public void OnPointerClick(PointerEventData e) { if (enabledAction && !e.dragging && e.button == PointerEventData.InputButton.Left) click?.Invoke(); }
        public void OnPointerEnter(PointerEventData e) { hovered = true; enter?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; exit?.Invoke(); }
        public void OnBeginDrag(PointerEventData e) { if (enabledAction) beginDrag?.Invoke(e); }
        public void OnDrag(PointerEventData e) { if (enabledAction) drag?.Invoke(e); }
        public void OnEndDrag(PointerEventData e) { if (enabledAction) endDrag?.Invoke(e); }
    }
}
