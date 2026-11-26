using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FirecrackerButtonBackground : MaskableGraphic
    {
        float highlight;
        bool hovered, pressed;
        RectTransform buttonRoot;
        Vector3 restingScale;

        protected override void OnEnable()
        {
            base.OnEnable();
            buttonRoot = transform.parent as RectTransform;
            restingScale = buttonRoot ? buttonRoot.localScale : Vector3.one;
            ResetInteraction();
        }

        protected override void OnDisable()
        {
            ResetInteraction();
            base.OnDisable();
        }

        public void ResetInteraction()
        {
            hovered = pressed = false; highlight = 0;
            if (buttonRoot) buttonRoot.localScale = restingScale;
            SetVerticesDirty();
        }

        public void SetHovered(bool value)
        {
            hovered = value;
        }

        public void SetPressed(bool value)
        {
            pressed = value;
            if (buttonRoot) buttonRoot.localScale = restingScale * (GameRoot.Instance && GameRoot.Instance.ReduceMotion ? 1 : pressed ? .97f : 1 + highlight * .04f);
        }

        void Update()
        {
            bool reduced = GameRoot.Instance && GameRoot.Instance.ReduceMotion;
            float target = hovered || pressed ? 1 : 0;
            float value = reduced ? target : Mathf.MoveTowards(highlight, target, Time.unscaledDeltaTime * 9);
            if (buttonRoot) buttonRoot.localScale = restingScale * (reduced ? 1 : pressed ? .97f : 1 + value * .04f);
            if (Mathf.Abs(value - highlight) < .001f) return;
            highlight = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float w = rectTransform.rect.width, h = rectTransform.rect.height, cy = h * .5f;
            var outline = Hex(0x751329);
            var paper = Color.Lerp(Hex(0xc62432), Hex(0xef432d), highlight);
            var gold = Color.Lerp(Hex(0xf4b73f), Hex(0xffe498), highlight);
            var light = Hex(0xffdf7e);

            Disc(vh, 6, cy + 2, 10, cy, outline); Box(vh, 6, 2, w - 12, h, outline);
            Disc(vh, w - 6, cy + 2, 10, cy, outline);
            Disc(vh, 6, cy, 9, cy, gold); Box(vh, 6, 0, w - 12, h, paper);
            Disc(vh, w - 6, cy, 10, cy, gold);
            Box(vh, 8, 1, w - 22, 2, Hex(0xffa15c));
            Box(vh, 8, 3, w - 22, 2, Hex(0xff6850));
            Box(vh, 8, h - 5, w - 22, 2, Hex(0xad1b2e));
            Box(vh, 8, h - 3, w - 22, 2, Hex(0x8f182b));

            Box(vh, 8, 0, 8, h, gold); Box(vh, 9, 1, 2, h - 2, light);
            Box(vh, 15, 1, 1, h - 2, Hex(0xbf792d));
            Box(vh, w - 20, 0, 8, h, gold); Box(vh, w - 19, 1, 2, h - 2, light);
            Box(vh, w - 13, 1, 1, h - 2, Hex(0xbf792d));
            Diamond(vh, 22, cy, 2, gold); Diamond(vh, w - 26, cy, 2, gold);

            Disc(vh, 4, cy, 4, Mathf.Max(1, cy - 3), Hex(0xd68c38));
            Disc(vh, 4, cy, 2, Mathf.Max(1, cy - 5), Hex(0xf8ce71));
            Disc(vh, w - 5, cy, 6, Mathf.Max(1, cy - 2), Hex(0x9f2630));
            Disc(vh, w - 5, cy, 4, Mathf.Max(1, cy - 4), Hex(0xf2ac51));
            Disc(vh, w - 5, cy, 2, Mathf.Max(1, cy - 6), Hex(0xa94a30));

            Box(vh, w - 3, cy - 1, 11, 2, Hex(0x81382c));
            Box(vh, w + 6, cy - 4, 4, 3, Hex(0xf2cd84));
            Box(vh, w + 9, cy - 7, 4, 3, Hex(0xa7723a));
            Box(vh, w + 12, cy - 9, 5, 2, Hex(0xffdc88));
            float sx = w + 18, sy = cy - 9;
            Diamond(vh, sx, sy, 2 + highlight, Hex(0xffd254));
            Box(vh, sx, sy, 1, 1, Hex(0xfff1ba));
            if (highlight > .1f)
            {
                Box(vh, sx - 5, sy - 4, 2, 1, light);
                Box(vh, sx + 4, sy + 3, 2, 1, light);
                Box(vh, sx + 3, sy - 5, 1, 2, light);
            }
        }

        static void Disc(VertexHelper vh, float x, float y, float rx, float ry, Color ink)
        {
            for (float row = -ry; row < ry; row++)
            {
                float half = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((row + .5f) / ry, 2))) * rx;
                Box(vh, Mathf.Round(x - half), Mathf.Round(y + row), Mathf.Round(half * 2), 1, ink);
            }
        }
        static void Diamond(VertexHelper vh, float x, float y, float radius, Color ink)
        {
            for (float row = -radius; row <= radius; row++)
            {
                float half = radius - Mathf.Abs(row);
                Box(vh, x - half, y + row, half * 2 + 1, 1, ink);
            }
        }
        static void Box(VertexHelper vh, float x, float y, float w, float h, Color ink)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x, -y), ink, Vector2.zero); vh.AddVert(new Vector3(x + w, -y), ink, Vector2.zero);
            vh.AddVert(new Vector3(x + w, -y - h), ink, Vector2.zero); vh.AddVert(new Vector3(x, -y - h), ink, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 2, n + 3, n);
        }
    }
}
