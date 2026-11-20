using System.Collections.Generic;
using UnityEngine;

namespace Sleigh
{
    [DisallowMultipleComponent]
    public sealed class PixelPoolBucket : MonoBehaviour
    {
        Dictionary<string, List<PixelPoolElement>> pools;
        public static int Created { get; private set; }
        public static int Reused { get; private set; }
        void Index()
        {
            if (pools != null) return;
            pools = new Dictionary<string, List<PixelPoolElement>>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var e = transform.GetChild(i).GetComponent<PixelPoolElement>();
                if (e && !string.IsNullOrEmpty(e.Kind)) Register(e);
            }
        }
        void Register(PixelPoolElement e)
        {
            if (!pools.TryGetValue(e.Kind, out var list)) { list = new List<PixelPoolElement>(); pools.Add(e.Kind, list); }
            list.Add(e);
        }
        public RectTransform Take(string kind, string displayName, GameObject prefab = null)
        {
            Index(); PixelPoolElement result = null;
            if (pools.TryGetValue(kind, out var list))
                for (int i = 0; i < list.Count; i++) if (!list[i].inUse) { result = list[i]; Reused++; break; }
            if (!result)
            {
                GameObject go = prefab ? Instantiate(prefab, transform, false) : new GameObject(displayName, typeof(RectTransform));
                if (!prefab) go.transform.SetParent(transform, false);
                result = PixelUI.Ensure<PixelPoolElement>(go.transform);
                result.Kind = kind; Register(result); Created++;
            }
            result.ResetForUse();
            if (result.name != displayName) result.name = displayName;
            result.transform.SetAsLastSibling();
            return (RectTransform)result.transform;
        }
        public void ReleaseChildren()
        {
            // No Destroy/Instantiate during a refresh. All baked children remain editable in the scene.
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i); var slot = child.GetComponent<PixelPoolElement>();
                if (slot) slot.Release(); else child.gameObject.SetActive(false);
            }
        }
        public static PixelPoolBucket On(Transform parent)
            => PixelUI.Ensure<PixelPoolBucket>(parent);
    }
}
