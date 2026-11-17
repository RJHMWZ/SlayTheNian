using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    [DisallowMultipleComponent]
    public sealed class PixelPoolElement : MonoBehaviour
    {
        [SerializeField] string kind;
        [NonSerialized] public bool inUse;
        [NonSerialized] public int generation;
        public string Kind { get => kind; set => kind = value; }
        public void ResetForUse()
        {
            generation++; inUse = true;
            var rect = (RectTransform)transform;
            rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
            var cg = GetComponent<CanvasGroup>(); if (cg) { cg.alpha = 1; cg.blocksRaycasts = true; cg.interactable = true; }
            var hot = GetComponent<Hotspot>(); if (hot) hot.ClearBindings();
            var motion = GetComponent<SleighMotion>(); if (motion) motion.ResetMotion();
            var bucket = GetComponent<PixelPoolBucket>(); if (bucket) bucket.ReleaseChildren();
            gameObject.SetActive(true);
        }
        public void Release()
        {
            generation++; inUse = false;
            var hot = GetComponent<Hotspot>(); if (hot) hot.ClearBindings();
            var bucket = GetComponent<PixelPoolBucket>(); if (bucket) bucket.ReleaseChildren();
            gameObject.SetActive(false);
        }
    }
}
