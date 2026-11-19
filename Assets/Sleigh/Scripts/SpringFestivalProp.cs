using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpringFestivalProp : MaskableGraphic
    {
        public bool bag, lit;
        public static SpringFestivalProp Home(Transform parent, float x, float y, float scale, bool lit = false)
            => Build(parent, x, y, scale, false, lit);
        public static SpringFestivalProp BlessingBag(Transform parent, float x, float y)
            => Build(parent, x, y, 1, true, true);
        static SpringFestivalProp Build(Transform parent, float x, float y, float scale, bool bag, bool lit)
        {
            var root = Acquire(parent, bag ? "FestivalBlessingBag" : "FestivalHome", bag ? "Red embroidered blessing bag" : "Spring couplets and lantern home", x, y, 72, bag ? 90 : 68);
            root.pivot = new Vector2(.5f, bag ? 1 : 0); root.localScale = Vector3.one * scale;
            var prop = Ensure<SpringFestivalProp>(root); prop.bag = bag; prop.lit = lit;
            prop.color = Color.white; prop.raycastTarget = false; prop.SetVerticesDirty();
            Label(root, bag ? 17 : 30, bag ? 38 : 32, bag ? 38 : 12, "福", bag ? 2.4f : .68f, Hex(0xffd27a), 1).shadow = false;
            return prop;
        }
        public void SetLit(bool value) { lit = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (bag)
            {
                Box(vh, 22, 0, 29, 13, Hex(0x943139));
                Box(vh, 26, 2, 21, 12, Hex(0xe34a43));
                Box(vh, 21, 13, 30, 5, Hex(0xf5bd59));
                Box(vh, 12, 20, 48, 5, Hex(0xf5bd59));
                Box(vh, 7, 25, 58, 46, Hex(0x96273b));
                Box(vh, 10, 25, 52, 48, Hex(0xc63542));
                Box(vh, 15, 73, 42, 8, Hex(0x96273b));
                Box(vh, 18, 77, 36, 5, Hex(0xf5bd59));
                Box(vh, 15, 31, 2, 36, Hex(0xed6e50));
                Box(vh, 55, 31, 2, 36, Hex(0xed6e50));
                for (int i = 0; i < 4; i++) Box(vh, 25 + i * 6, 83, 2, 6, Hex(0xf5bd59));
                return;
            }
            Box(vh, 9, 21, 54, 44, Hex(0xa4614b));
            Box(vh, 12, 23, 48, 4, Hex(0xdd9d68));
            for (int row = 0; row < 6; row++)
            {
                Box(vh, 2 + row * 4, 21 - row * 3, 68 - row * 8, 4, Hex(0x532d36));
                Box(vh, 2 + row * 4, 21 - row * 3, 68 - row * 8, 1, Hex(0xbc7255));
            }
            Box(vh, 0, 20, 72, 3, Hex(0xe0a768));
            Box(vh, 28, 31, 17, 32, Hex(0x512934));
            Box(vh, 26, 30, 2, 32, Hex(0xd52f3d)); Box(vh, 45, 30, 2, 32, Hex(0xd52f3d));
            Box(vh, 26, 27, 21, 3, Hex(0xd52f3d));
            for (int i = 0; i < 5; i++)
            { Box(vh, 26, 32 + i * 6, 2, 2, Hex(0xffcf78)); Box(vh, 45, 32 + i * 6, 2, 2, Hex(0xffcf78)); }
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? 14 : 51;
                Box(vh, x, 34, 8, 13, lit ? Hex(0xffd28a) : Hex(0xbd865d));
                Box(vh, x + 3, 34, 1, 13, Hex(0x7b4239)); Box(vh, x, 40, 8, 1, Hex(0x7b4239));
                Box(vh, x + 1, 25, 1, 5, Hex(0xe7b16a));
                Box(vh, x - 1, 27, 6, 9, Hex(0xc52e3c)); Box(vh, x, 28, 3, 7, Hex(0xf59b59));
                Box(vh, x - 1, 27, 6, 1, Hex(0xffd481)); Box(vh, x + 1, 36, 1, 4, Hex(0xffd481));
            }
            Box(vh, 8, 63, 56, 4, Hex(0x6d3d36));
        }
        void Box(VertexHelper vh, float x, float y, float w, float h, Color tint)
        {
            int n = vh.currentVertCount; float left = rectTransform.rect.xMin + x, top = rectTransform.rect.yMax - y;
            vh.AddVert(new Vector3(left, top), tint, Vector2.zero); vh.AddVert(new Vector3(left + w, top), tint, Vector2.zero);
            vh.AddVert(new Vector3(left + w, top - h), tint, Vector2.zero); vh.AddVert(new Vector3(left, top - h), tint, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 2, n + 3, n);
        }
    }
}
