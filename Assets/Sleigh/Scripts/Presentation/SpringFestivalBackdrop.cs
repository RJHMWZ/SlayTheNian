using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpringFestivalBackdrop : MaskableGraphic
    {
        public enum Setting { Village, Courtyard, Market, Hearth }
        public Setting setting;
        public bool story;
        float clock, lastFrame = -1, previewTime = -1;
        bool reduced;
        static readonly Color GoldInk = Hex(0xf8c66c), Cream = Hex(0xffe9b4);

        public static SpringFestivalBackdrop Build(Transform parent, Setting setting = Setting.Village, bool story = false)
        {
            var root = Acquire(parent, "FestivalBackdrop", "Lantern-lit Spring Festival village");
            var drawing = Ensure<SpringFestivalBackdrop>(root);
            drawing.setting = setting; drawing.story = story; drawing.previewTime = -1;
            drawing.lastFrame = -1; drawing.clock = 0; drawing.color = Color.white;
            drawing.raycastTarget = false; drawing.SetVerticesDirty();
            if (setting == Setting.Village || setting == Setting.Courtyard)
            {
                Label(root, 277, 150, 86, "岁安镇", .65f, Cream, 1).shadow = false;
                Label(root, 26, 191, 43, "年货", .65f, Cream, 1).shadow = false;
                Label(root, 556, 187, 58, "迎春", .65f, Cream, 1).shadow = false;
                Label(root, 53, 234, 12, "福", .68f, GoldInk, 1).shadow = false;
                Label(root, 594, 230, 12, "春", .68f, GoldInk, 1).shadow = false;
            }
            else if (setting == Setting.Hearth) Label(root, 173, 93, 80, "福", 3.2f, GoldInk, 1).shadow = false;
            else Label(root, 18, 72, 130, "年货铺", 1.1f, GoldInk, 1).shadow = false;
            return drawing;
        }

        public void FreezePreview(float seconds, bool reduceMotion = false)
        { previewTime = seconds; clock = reduceMotion ? 0 : seconds; reduced = reduceMotion; SetVerticesDirty(); }

        void Update()
        {
            if (previewTime >= 0) return;
            reduced = GameRoot.Instance && GameRoot.Instance.ReduceMotion;
            float time = Application.isPlaying && !reduced ? Time.unscaledTime : 0;
            float frame = Mathf.Floor(time * 20);
            if (frame == lastFrame) return;
            lastFrame = frame; clock = frame / 20; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (setting == Setting.Hearth) { Hearth(vh); return; }
            for (int y = 0; y < 234; y += 3)
                Box(vh, 0, y, 640, 3, Color.Lerp(Hex(0x291728), Hex(0xb75a48), y / 234f));
            Glow(vh, 323, 174, 118, 56, Hex(0xffb85c, .055f));
            Cloud(vh, 22 + Mathf.Sin(clock * .06f) * 5, 117, .8f, Hex(0xd38578, .15f));
            Cloud(vh, 470 - Mathf.Sin(clock * .08f) * 6, 98, 1, Hex(0xdfaa8b, .13f));
            for (int i = 0; i < 15; i++)
            {
                float x = i * 48 - 16, roof = 142 + Mathf.Sin(i * 2.1f) * 17;
                Box(vh, x + 4, roof, 42, 92, Hex(0x713e45));
                Roof(vh, x, roof, 50, 14, Hex(0x492d3c), Hex(0x996052));
                for (int j = 0; j < 3; j++) Box(vh, x + 11 + j * 10, roof + 17, 4, 9, Hex(0xe5a46a, .5f));
            }
            Pagoda(vh, 324, 111);
            Box(vh, 0, 225, 640, 135, Hex(0x714343));
            for (int row = 0; row < 13; row++)
            {
                float y = 228 + row * row * .85f;
                Box(vh, 0, y, 640, 1, Hex(0xc78665, .22f));
                float width = 24 + row * 7;
                for (float x = row % 2 * width * .5f; x < 640; x += width)
                    Box(vh, x, y, 1, 3 + row * 1.65f, Hex(0x3b2b36, .3f));
            }
            Glow(vh, 320, 240, 124, 11, Hex(0xffbb68, .045f));
            House(vh, -20, 193, 144, 95, false);
            House(vh, 114, 183, 90, 63, true);
            House(vh, 445, 181, 88, 65, true);
            House(vh, 522, 189, 141, 99, false);
            Gate(vh);
            if (setting == Setting.Market) Market(vh);
            else
            {
                Stall(vh, -14, 254, 87, false);
                Stall(vh, 573, 250, 83, true);
            }
            if (setting == Setting.Village)
            {
                Villager(vh, 218, 222, Hex(0xb83b43), false);
                Villager(vh, 239, 230, Hex(0xd68c48), true);
                Villager(vh, 406, 220, Hex(0x7d5660), false);
                Villager(vh, 422, 232, Hex(0xb43f43), true);
            }
            Garland(vh);
            Lantern(vh, 35, 42, 1.45f, .3f);
            Lantern(vh, 605, 42, 1.45f, 1.7f);
            Firecrackers(vh, 11, 132, .4f);
            Firecrackers(vh, 629, 129, 2.1f);
            // Effects stay at the sides and above the play area.
            if (!reduced)
            {
                Firework(vh, 151, 92, 0, Hex(0xffcb72));
                Firework(vh, 494, 77, 2.1f, Hex(0xff9b86));
                Firework(vh, 392, 126, 4, Hex(0xffe5a1));
                Paper(vh);
            }
            for (int i = 0; i < 65; i++)
            {
                float x = Mathf.Repeat(i * 73 + 19, 640), y = 274 + Mathf.Repeat(i * 31, 82);
                Box(vh, x, y, 2 + i % 3, 1 + i % 2, i % 4 == 0 ? GoldInk : Hex(0xc64c47));
            }
            Box(vh, 0, 358, 640, 2, Hex(0x4c2934));
        }

        void House(VertexHelper vh, float x, float y, float w, float h, bool distant)
        {
            Color wall = distant ? Hex(0x9a5a50) : Hex(0xba785e);
            Box(vh, x + 8, y, w - 16, h, wall);
            Box(vh, x + 11, y + 5, w - 22, 4, Hex(0xe4a477));
            Box(vh, x + 9, y + h - 7, w - 18, 7, Hex(0x633b3d));
            Roof(vh, x - 5, y, w + 10, 26, Hex(0x452b37), Hex(0xc17c64));
            Box(vh, x + 15, y, 5, h - 5, Hex(0x63353c));
            Box(vh, x + w - 20, y, 5, h - 5, Hex(0x63353c));
            float doorX = x + w * .44f, doorW = w * .22f;
            Box(vh, doorX - 4, y + 23, doorW + 8, h - 29, Hex(0x6b3039));
            Box(vh, doorX, y + 26, doorW, h - 32, Hex(0x412d33));
            Box(vh, doorX + doorW * .5f, y + 26, 1, h - 32, Hex(0xa36651));
            for (int j = 0; j < 2; j++)
            {
                float wx = j == 0 ? x + 24 : x + w - 43;
                Window(vh, wx, y + 30, distant ? 13 : 21, distant ? 21 : 30);
            }
            for (int side = 0; side < 2; side++)
            {
                float cx = side == 0 ? doorX - 7 : doorX + doorW + 3;
                Box(vh, cx, y + 25, 4, h - 33, Hex(0xc7303f));
                for (int k = 0; k < 5; k++) Box(vh, cx + 1, y + 28 + k * (h - 42) / 5, 2, 2, GoldInk);
            }
            Box(vh, doorX - 7, y + 17, doorW + 14, 6, Hex(0xc7303f));
            Lantern(vh, x + 14, y - 7, distant ? .33f : .48f, x);
            Lantern(vh, x + w - 14, y - 7, distant ? .33f : .48f, x + 1);
            if (!distant) Diamond(vh, doorX + doorW * .5f, y + 47, 8, Hex(0xd33240));
        }

        void Window(VertexHelper vh, float x, float y, float w, float h)
        {
            Glow(vh, x + w * .5f, y + h * .5f, w * 1.15f, h * .85f, Hex(0xffbf62, .06f));
            Box(vh, x - 2, y - 2, w + 4, h + 4, Hex(0x68363c));
            Box(vh, x, y, w, h, Hex(0xf2b46a));
            Box(vh, x + 2, y + 2, w - 4, h - 4, Hex(0xffd58c));
            Box(vh, x + w * .5f, y, 2, h, Hex(0x995140));
            Box(vh, x, y + h * .5f, w, 2, Hex(0x995140));
        }

        static void Roof(VertexHelper vh, float x, float y, float width, float height, Color tile, Color edge)
        {
            for (int i = 0; i < 7; i++)
            {
                float inset = i * width * .045f;
                Box(vh, x + inset, y - i * height / 7, width - inset * 2, height / 7 + 1, tile);
                if (i % 2 == 0) Box(vh, x + inset, y - i * height / 7, width - inset * 2, 1, edge);
            }
            Box(vh, x - 3, y - 4, 8, 3, edge); Box(vh, x + width - 5, y - 4, 8, 3, edge);
            Box(vh, x, y + 2, width, 3, Hex(0x472a35));
        }

        void Gate(VertexHelper vh)
        {
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? 270 : 365;
                Box(vh, x, 161, 6, 68, Hex(0xa83342));
                Box(vh, x - 3, 226, 12, 4, Hex(0xdda16c));
                Lantern(vh, x + 3, 163, .35f, side);
            }
            Box(vh, 264, 143, 113, 20, Hex(0x80323d));
            Box(vh, 268, 146, 105, 15, Hex(0x923b43));
            Box(vh, 268, 146, 105, 1, GoldInk); Box(vh, 268, 161, 105, 1, GoldInk);
            Roof(vh, 253, 143, 135, 19, Hex(0x4d2938), Hex(0xd69a6e));
        }

        static void Pagoda(VertexHelper vh, float x, float top)
        {
            for (int i = 0; i < 3; i++)
            {
                float w = 27 + i * 13, y = top + i * 18;
                Box(vh, x - w * .5f + 6, y, w - 12, 17, Hex(0x9c5451));
                Box(vh, x - 3, y + 3, 5, 8, Hex(0xe2a16b));
                Roof(vh, x - w * .5f - 3, y, w + 6, 9, Hex(0x5a3441), Hex(0xb77a62));
            }
            Box(vh, x, top - 18, 1, 11, GoldInk);
        }

        void Garland(VertexHelper vh)
        {
            for (int x = 0; x < 640; x += 3)
            {
                if (story && x > 220 && x < 420) continue;
                float y = 28 + Mathf.Sin(x / 640f * Mathf.PI) * 31;
                Box(vh, x, y, 3, 1, Hex(0xd99666));
            }
            for (int i = 0; i < 12; i++)
            {
                float x = 84 + i * 43, y = 30 + Mathf.Sin(x / 640f * Mathf.PI) * 31;
                if (story && x > 210 && x < 430) continue;
                Lantern(vh, x, y, .32f, i * .7f);
            }
        }

        void Lantern(VertexHelper vh, float x, float y, float size, float phase)
        {
            float sway = reduced ? 0 : Mathf.Sin(clock * 1.15f + phase) * 1.8f * size;
            float cx = x + sway, cy = y + 14 * size;
            Box(vh, x, y - 10 * size, 1, 10 * size, GoldInk);
            Glow(vh, cx, cy, 28 * size, 29 * size, Hex(0xffb85f, .065f));
            Disk(vh, cx, cy + 2 * size, 13 * size, 16 * size, Hex(0x812a3e));
            Disk(vh, cx, cy, 13 * size, 16 * size, Hex(0xd83a40));
            Disk(vh, cx, cy, 9 * size, 15 * size, Hex(0xf46a48));
            Box(vh, cx - size, cy - 11 * size, 2 * size, 22 * size, Hex(0xffce7a));
            Box(vh, cx - 7 * size, cy - 16 * size, 14 * size, 3 * size, GoldInk);
            Box(vh, cx - 7 * size, cy + 14 * size, 14 * size, 3 * size, GoldInk);
            Box(vh, cx, cy + 17 * size, 1, 9 * size, GoldInk);
            Box(vh, cx - 2 * size, cy + 25 * size, 4 * size, 7 * size, Hex(0xed7952));
        }

        void Firecrackers(VertexHelper vh, float x, float y, float phase)
        {
            Box(vh, x, y, 1, 66, GoldInk);
            for (int i = 0; i < 8; i++)
            {
                float px = x + (i % 2 == 0 ? -7 : 2), py = y + i * 7;
                Box(vh, px, py, 6, 11, Hex(0xcf3641));
                Box(vh, px, py, 6, 2, GoldInk); Box(vh, px, py + 9, 6, 2, GoldInk);
            }
            if (!reduced)
            {
                float age = Mathf.Repeat(clock + phase, 2.4f);
                if (age < .9f)
                    for (int i = 0; i < 6; i++)
                        Box(vh, x + Mathf.Cos(i * 2.4f) * age * 17, y + 64 + Mathf.Sin(i * 2.4f) * age * 17, 2, 2, Hex(0xffdb88, 1 - age));
            }
        }

        void Firework(VertexHelper vh, float x, float y, float phase, Color tint)
        {
            float age = Mathf.Repeat(clock + phase, 7.2f);
            if (age < .8f)
            {
                for (int i = 0; i < 4; i++) Box(vh, x, y + 50 * (1 - age / .8f) + i * 3, 1, 2, Hex(0xffd487, .8f - i * .15f));
                return;
            }
            if (age > 3.1f) return;
            float t = (age - .8f) / 2.3f, r = 4 + 33 * (1 - Mathf.Pow(1 - t, 3));
            for (int i = 0; i < 18; i++)
            {
                float a = i * Mathf.PI / 9;
                for (int trail = 0; trail < 4; trail++)
                {
                    float radius = r - trail * 2.6f;
                    Color c = tint; c.a = (1 - t) * (1 - trail * .19f);
                    Box(vh, x + Mathf.Cos(a) * radius, y + Mathf.Sin(a) * radius + t * t * 14, trail == 0 ? 2 : 1, 2, c);
                }
            }
        }

        void Paper(VertexHelper vh)
        {
            for (int i = 0; i < 26; i++)
            {
                float x = Mathf.Repeat(i * 83 + clock * (5 + i % 3) + Mathf.Sin(clock + i) * 5, 664) - 12;
                float y = Mathf.Repeat(i * 47 + clock * (10 + i % 4), 380) - 10;
                if (x > 120 && x < 520 && y > 190) continue;
                Box(vh, x, y, i % 2 + 1, 2 + Mathf.Abs(Mathf.Sin(clock * 2 + i)) * 2, i % 4 == 0 ? GoldInk : Hex(0xe65b50, .85f));
            }
        }

        static void Villager(VertexHelper vh, float x, float y, Color coat, bool small)
        {
            float s = small ? .75f : 1;
            Box(vh, x - 3 * s, y - 18 * s, 6 * s, 6 * s, Hex(0xe4ad7f));
            Box(vh, x - 4 * s, y - 20 * s, 8 * s, 3 * s, Hex(0x472b34));
            Box(vh, x - 5 * s, y - 12 * s, 10 * s, 9 * s, coat);
            Box(vh, x - 3 * s, y - 3 * s, 2 * s, 3 * s, Hex(0x382933));
            Box(vh, x + 2 * s, y - 3 * s, 2 * s, 3 * s, Hex(0x382933));
            Box(vh, x + 6 * s, y - 12 * s, 1, 5 * s, GoldInk);
            Box(vh, x + 5 * s, y - 7 * s, 4 * s, 5 * s, Hex(0xee874f));
        }

        static void Stall(VertexHelper vh, float x, float y, float w, bool fruit)
        {
            Box(vh, x, y, w, 7, Hex(0x66333c));
            Box(vh, x + 6, y + 7, w - 12, 31, Hex(0x8c4b43));
            for (int i = 0; i < 10; i++)
            {
                float px = x + 7 + i * (w - 14) / 10;
                Box(vh, px, y - 4 - i % 3 * 2, 5, 5, fruit ? Hex(0xeea64f) : Hex(0xdd3b41));
                if (!fruit) Box(vh, px + 2, y - 3, 1, 2, GoldInk);
            }
            Box(vh, x + 3, y + 7, w - 6, 1, GoldInk);
        }

        static void Market(VertexHelper vh)
        {
            Box(vh, 171, 56, 458, 265, Hex(0x4a2935, .94f));
            for (int row = 0; row < 2; row++)
            {
                float y = 174 + row * 74;
                Box(vh, 178, y, 451, 4, Hex(0xb57953));
                Box(vh, 178, y + 4, 451, 4, Hex(0x724138));
            }
            for (int i = 0; i < 20; i++) Box(vh, 171 + i * 23, 54, 23, 12 + i % 2 * 4, i % 2 == 0 ? Hex(0xb83242) : Hex(0xe7a45d));
            Stall(vh, 9, 278, 146, false);
        }

        void Hearth(VertexHelper vh)
        {
            Box(vh, 0, 0, 640, 360, Hex(0x492732));
            for (int i = 0; i < 10; i++)
            {
                Box(vh, i * 72, 35, 5, 259, Hex(0x6b3b3b));
                Box(vh, i * 72 + 5, 42, 66, 1, Hex(0xa36348, .3f));
            }
            Box(vh, 0, 294, 640, 66, Hex(0x74483e));
            for (int i = 0; i < 6; i++) Box(vh, 0, 298 + i * 12, 640, 1, Hex(0xb97f57, .25f));
            Window(vh, 24, 92, 98, 130); Window(vh, 309, 92, 103, 130);
            Diamond(vh, 211, 112, 36, Hex(0xb52f41));
            Box(vh, 154, 161, 116, 6, Hex(0xbf835c));
            Lantern(vh, 37, 25, 1.1f, .2f); Lantern(vh, 395, 25, 1.1f, 1.8f);
            Box(vh, 449, 80, 184, 214, Hex(0x351f2b, .7f));
            Glow(vh, 320, 266, 97, 45, Hex(0xffa255, .065f));
            Disk(vh, 320, 266, 50, 11, Hex(0x3c2830));
            Box(vh, 279, 264, 82, 19, Hex(0x8e4840));
            Box(vh, 282, 282, 6, 13, Hex(0x3d2930)); Box(vh, 351, 282, 6, 13, Hex(0x3d2930));
            for (int i = 0; i < 8; i++)
            {
                float h = 8 + (reduced ? 3 : Mathf.Sin(clock * 5 + i * 2) * 5);
                Box(vh, 289 + i * 8, 263 - h, 7, h, Hex(0xed8749));
                Box(vh, 291 + i * 8, 266 - h * .6f, 3, h * .6f, GoldInk);
            }
            Disk(vh, 159, 276, 59, 17, Hex(0xa75c43));
            Box(vh, 115, 279, 8, 27, Hex(0x4a2a30)); Box(vh, 195, 279, 8, 27, Hex(0x4a2a30));
            Disk(vh, 158, 267, 23, 6, Cream); Box(vh, 146, 258, 23, 8, Hex(0xe6b877));
            Box(vh, 121, 263, 9, 8, GoldInk); Box(vh, 185, 263, 9, 8, GoldInk);
        }

        static void Cloud(VertexHelper vh, float x, float y, float s, Color tint)
        { Disk(vh, x + 25 * s, y, 26 * s, 7 * s, tint); Disk(vh, x + 49 * s, y - 5 * s, 22 * s, 11 * s, tint); Box(vh, x, y + 3 * s, 89 * s, 3 * s, tint); }

        static void Glow(VertexHelper vh, float x, float y, float rx, float ry, Color tint)
        { Disk(vh, x, y, rx, ry, tint); Disk(vh, x, y, rx * .72f, ry * .72f, tint); Disk(vh, x, y, rx * .45f, ry * .45f, tint); }

        static void Disk(VertexHelper vh, float x, float y, float rx, float ry, Color tint)
        {
            if (rx <= 0 || ry <= 0) return;
            for (float row = -ry; row < ry; row += 2)
            {
                float half = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((row + 1) / ry, 2))) * rx;
                Box(vh, x - half, y + row, half * 2, 2, tint);
            }
        }

        static void Diamond(VertexHelper vh, float x, float y, float r, Color tint)
        { for (float row = -r; row < r; row += 2) { float half = r - Mathf.Abs(row); Box(vh, x - half, y + row, half * 2, 2, tint); } }

        static void Box(VertexHelper vh, float x, float y, float w, float h, Color tint)
        {
            float left = Mathf.Max(0, Mathf.Round(x)), top = Mathf.Max(0, Mathf.Round(y));
            float right = Mathf.Min(640, Mathf.Round(x + w)), bottom = Mathf.Min(360, Mathf.Round(y + h));
            if (right <= left || bottom <= top || tint.a <= 0) return;
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(left, -top), tint, Vector2.zero); vh.AddVert(new Vector3(right, -top), tint, Vector2.zero);
            vh.AddVert(new Vector3(right, -bottom), tint, Vector2.zero); vh.AddVert(new Vector3(left, -bottom), tint, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 2, n + 3, n);
        }
    }
}
