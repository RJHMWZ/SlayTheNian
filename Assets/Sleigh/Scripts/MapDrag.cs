using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleigh
{
    public class MapDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public Action<float> move; public float scale = 1;
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => move?.Invoke(-e.delta.x / Mathf.Max(.1f, scale));
        public void OnEndDrag(PointerEventData e) { }
        public void OnScroll(PointerEventData e) => move?.Invoke(-e.scrollDelta.y * 60);
    }

}
