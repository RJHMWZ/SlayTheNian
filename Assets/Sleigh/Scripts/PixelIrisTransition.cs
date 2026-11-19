using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    // A saved UI graphic, rendered as pixel rows around a circular opening.
    // No render texture, shader or runtime-created mask objects are needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelIrisTransition : MaskableGraphic
    {
        [SerializeField, Range(0, 1)] float reveal = 1;
        bool fade;
        public float Reveal => reveal;

        public void SetReveal(float value, bool reducedMotion = false)
        {
            reveal = Mathf.Clamp01(value); fade = reducedMotion;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (reveal >= 1) return;
            Rect bounds = rectTransform.rect;
            Color ink = color;
            if (fade)
            {
                ink.a *= 1 - reveal;
                Box(vh, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax, ink);
                return;
            }
            if (reveal <= 0)
            {
                Box(vh, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax, ink);
                return;
            }
            Vector2 centre = bounds.center;
            float radius = (bounds.size.magnitude * .5f + 2) * reveal;
            for (float bottom = bounds.yMin; bottom < bounds.yMax; bottom += 2)
            {
                float top = Mathf.Min(bottom + 2, bounds.yMax);
                float dy = (bottom + top) * .5f - centre.y;
                float half = Mathf.Abs(dy) >= radius ? 0 :
                    Mathf.Round(Mathf.Sqrt(radius * radius - dy * dy));
                Box(vh, bounds.xMin, bottom, Mathf.Max(bounds.xMin, centre.x - half), top, ink);
                Box(vh, Mathf.Min(bounds.xMax, centre.x + half), bottom, bounds.xMax, top, ink);
            }
        }

        static void Box(VertexHelper vh, float left, float bottom, float right, float top, Color ink)
        {
            if (right <= left || top <= bottom) return;
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), ink, Vector2.zero);
            vh.AddVert(new Vector3(left, top), ink, Vector2.zero);
            vh.AddVert(new Vector3(right, top), ink, Vector2.zero);
            vh.AddVert(new Vector3(right, bottom), ink, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
