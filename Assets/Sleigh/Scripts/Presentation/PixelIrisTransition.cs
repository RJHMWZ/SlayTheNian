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
        Rect paperBounds;
        Vector2 centre;
        float radius;
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
            Rect bounds = paperBounds = rectTransform.rect;
            Color ink = color;
            if (fade)
            {
                ink.a *= 1 - reveal;
                Box(vh, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax, ink);
                return;
            }
            centre = bounds.center;
            radius = (bounds.size.magnitude * .5f + 2) * reveal;
            for (float bottom = bounds.yMin; bottom < bounds.yMax; bottom += 2)
            {
                float top = Mathf.Min(bottom + 2, bounds.yMax);
                float height = Mathf.InverseLerp(bounds.yMin, bounds.yMax, bottom);
                var red = Color.Lerp(ink, PixelUI.Hex(0xef5930, ink.a), .18f + (1 - height) * .18f);
                PaperBox(vh, bounds.xMin, bottom, bounds.xMax, top, red);
            }
            // Quiet repeating brocade: diamond paper cuts alternate with stepped clouds.
            var pattern = Color.Lerp(ink, PixelUI.Hex(0xf5c65a, ink.a), .25f);
            for (int row = 0; row * 64 < bounds.height + 32; row++)
                for (int column = -1; column * 80 < bounds.width + 40; column++)
                {
                    float x = bounds.xMin + column * 80 + (row % 2) * 40 + 20;
                    float y = bounds.yMin + row * 64 + 16;
                    PaperDiamond(vh, x, y, pattern);
                    PaperCloud(vh, x + 40, y + 32, pattern);
                }
            var gold = PixelUI.Hex(0xf5c65a, ink.a);
            var trim = Color.Lerp(ink, gold, .7f);
            PaperBox(vh, bounds.xMin + 12, bounds.yMin + 12, bounds.xMax - 12, bounds.yMin + 13, trim);
            PaperBox(vh, bounds.xMin + 12, bounds.yMax - 13, bounds.xMax - 12, bounds.yMax - 12, trim);
            PaperBox(vh, bounds.xMin + 12, bounds.yMin + 12, bounds.xMin + 13, bounds.yMax - 12, trim);
            PaperBox(vh, bounds.xMax - 13, bounds.yMin + 12, bounds.xMax - 12, bounds.yMax - 12, trim);
            for (int side = 0; side < 4; side++)
            {
                float sx = side % 2 == 0 ? 1 : -1, sy = side < 2 ? 1 : -1;
                float x = sx > 0 ? bounds.xMin + 22 : bounds.xMax - 22;
                float y = sy > 0 ? bounds.yMin + 22 : bounds.yMax - 22;
                CornerMark(vh, x, y, sx * 22, sy * 2, gold);
                CornerMark(vh, x, y, sx * 2, sy * 18, gold);
                CornerMark(vh, x + sx * 7, y + sy * 7, sx * 15, sy * 2, trim);
                CornerMark(vh, x + sx * 7, y + sy * 7, sx * 2, sy * 11, trim);
            }
            // A fine gilt edge makes the opening read as a round paper-cut window.
            if (radius > 0)
                for (float bottom = bounds.yMin; bottom < bounds.yMax; bottom += 2)
                {
                    float dy = bottom + 1 - centre.y;
                    if (Mathf.Abs(dy) >= radius) continue;
                    float half = HalfOpening(bottom);
                    PaperBox(vh, centre.x - half - 2, bottom, centre.x - half, bottom + 2, gold);
                    PaperBox(vh, centre.x + half, bottom, centre.x + half + 2, bottom + 2, gold);
                }
        }

        float HalfOpening(float bottom)
        {
            float dy = bottom + 1 - centre.y;
            return Mathf.Abs(dy) >= radius ? 0 : Mathf.Round(Mathf.Sqrt(radius * radius - dy * dy));
        }

        void PaperBox(VertexHelper vh, float left, float bottom, float right, float top, Color ink)
        {
            left = Mathf.Max(left, paperBounds.xMin); right = Mathf.Min(right, paperBounds.xMax);
            bottom = Mathf.Max(bottom, paperBounds.yMin); top = Mathf.Min(top, paperBounds.yMax);
            if (right <= left || top <= bottom) return;
            if (radius <= 0) { Box(vh, left, bottom, right, top, ink); return; }
            // Clip every decorative stroke using the same pixel rows as the opening.
            float first = paperBounds.yMin + Mathf.Floor((bottom - paperBounds.yMin) / 2) * 2;
            for (float row = first; row < top; row += 2)
            {
                float half = HalfOpening(row);
                float low = Mathf.Max(bottom, row), high = Mathf.Min(top, row + 2);
                Box(vh, left, low, Mathf.Min(right, centre.x - half), high, ink);
                Box(vh, Mathf.Max(left, centre.x + half), low, right, high, ink);
            }
        }

        void PaperDiamond(VertexHelper vh, float x, float y, Color ink)
        {
            for (int row = -12; row <= 12; row += 2)
            {
                float half = 12 - Mathf.Abs(row);
                PaperBox(vh, x - half - 1, y + row, x - half + 1, y + row + 2, ink);
                PaperBox(vh, x + half - 1, y + row, x + half + 1, y + row + 2, ink);
            }
            PaperBox(vh, x - 1, y - 1, x + 1, y + 1, ink);
        }

        void PaperCloud(VertexHelper vh, float x, float y, Color ink)
        {
            PaperBox(vh, x - 19, y - 7, x + 19, y - 5, ink);
            PaperBox(vh, x - 23, y - 5, x - 19, y + 1, ink);
            PaperBox(vh, x - 19, y + 1, x - 11, y + 3, ink);
            PaperBox(vh, x - 13, y + 3, x - 11, y + 7, ink);
            PaperBox(vh, x - 11, y + 7, x + 1, y + 9, ink);
            PaperBox(vh, x + 1, y + 3, x + 3, y + 7, ink);
            PaperBox(vh, x + 3, y + 1, x + 17, y + 3, ink);
            PaperBox(vh, x + 17, y - 5, x + 21, y + 1, ink);
            PaperBox(vh, x - 11, y - 1, x + 9, y + 1, ink);
        }

        void CornerMark(VertexHelper vh, float x, float y, float width, float height, Color ink)
        { PaperBox(vh, Mathf.Min(x, x + width), Mathf.Min(y, y + height), Mathf.Max(x, x + width), Mathf.Max(y, y + height), ink); }

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
