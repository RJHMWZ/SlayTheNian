using UnityEngine;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh
{
    // A menu-only illustration. One mesh, no textures, particles or per-frame objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpringFestivalMenu : MaskableGraphic
    {
        public bool foreground;
        static readonly Color Cream = Hex(0xffecae), GoldInk = Hex(0xffcb55), RedInk = Hex(0xe52f36);
        [SerializeField] RectTransform[] titleGlyphs;
        [SerializeField] Vector2[] titleOrigins;
        float clock, lastFrame = -1;

        public static void Build(RectTransform background, RectTransform ui, string title)
        {
            AddDrawing(background, false);
            var titleRoot = Group(ui, "Spring festival title", 0, 0);
            Ensure<SleighMotion>(titleRoot).ResetMotion();
            var ornaments = AddDrawing(titleRoot, true);
            string text = string.IsNullOrWhiteSpace(title) ? "爆竹驱年" : title;
            float scale = Mathf.Min(6.25f, 330 / Mathf.Max(1, PixelLabel.Measure(text)));
            float width = PixelLabel.Measure(text, scale), left = (640 - width) * .5f;
            ornaments.titleGlyphs = new RectTransform[text.Length];
            ornaments.titleOrigins = new Vector2[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                float advance = PixelLabel.CharWidth(text[i]) * scale;
                float arc = Mathf.Abs(i - (text.Length - 1) * .5f) * 5;
                var glyph = Group(titleRoot, "Festival title character " + i, 0, 0, advance, 86);
                glyph.pivot = new Vector2(.5f, .5f);
                glyph.anchoredPosition = new Vector2(left + advance * .5f, -94 - arc);
                ornaments.titleGlyphs[i] = glyph; ornaments.titleOrigins[i] = glyph.anchoredPosition;
                string letter = text[i].ToString();
                Ink(glyph, 5, 9, advance, letter, scale, Hex(0x811423));
                Ink(glyph, 3, 5, advance, letter, scale, Hex(0xe76a18));
                for (int edge = 0; edge < 8; edge++)
                {
                    float angle = edge * Mathf.PI * .25f;
                    Ink(glyph, Mathf.Round(Mathf.Cos(angle) * 2), Mathf.Round(Mathf.Sin(angle) * 2), advance, letter, scale, GoldInk);
                }
                Ink(glyph, 0, 0, advance, letter, scale, Cream);
                left += advance;
            }
            ornaments.AnimateLetters(0, false);
            Label(ui, 120, 146, 400, "爆竹声中，迎春除岁", .9f, Cream, 1);
            Label(ui, 180, 166, 280, "—  新春奇遇  —", .75f, GoldInk, 1);
        }

        static void Ink(Transform parent, float x, float y, float width, string text, float scale, Color ink)
        { var label = Label(parent, x, y, width, text, scale, ink, 1); label.shadow = false; }

        public static Hotspot MenuButton(Transform parent, float y, float height, string caption, System.Action click)
            => MenuButton(parent, 240, y, 160, height, caption, click);

        public static Hotspot MenuButton(Transform parent, float x, float y, float width, float height, string caption, System.Action click, float captionScale = 1)
        {
            var frame = Acquire(parent, "FestivalButton", "Firecracker button:" + caption, x, y, width, height);
            // Scale the whole button about its centre, including the caption.
            frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition += new Vector2(width * .5f, -height * .5f);
            var art = Acquire(frame, "FirecrackerArt", "Red paper firecracker", 0, 0, width, height);
            var background = Ensure<FirecrackerButtonBackground>(art);
            background.raycastTarget = false; background.ResetInteraction(); background.SetVerticesDirty();
            Label(frame, 18, (height - 12 * captionScale) * .5f, width - 36, caption, captionScale, Cream, 1);
            var button = Hot(frame, -5, -2, width + 28, height + 4, () => { SleighAudio.Instance?.Play("click"); click?.Invoke(); });
            button.enter = () => { background.SetHovered(true); SleighAudio.Instance?.Play("hover"); };
            button.exit = () => background.SetHovered(false);
            button.press = background.SetPressed;
            return button;
        }

        static SpringFestivalMenu AddDrawing(Transform parent, bool front)
        {
            var rect = Acquire(parent, "SpringFestivalDrawing", front ? "Title embers and firecrackers" : "Spring festival illustration");
            var drawing = Ensure<SpringFestivalMenu>(rect);
            drawing.foreground = front; drawing.raycastTarget = false; drawing.color = Color.white; drawing.SetVerticesDirty();
            return drawing;
        }

        void Update()
        {
            bool reduced = GameRoot.Instance && GameRoot.Instance.ReduceMotion;
            float time = Application.isPlaying && !reduced ? Time.unscaledTime : 0;
            if (foreground) AnimateLetters(time, reduced);
            float frame = Mathf.Floor(time * 30);
            if (frame == lastFrame) return;
            lastFrame = frame; clock = frame / 30; SetVerticesDirty();
        }

        void AnimateLetters(float time, bool reduced)
        {
            if (titleGlyphs == null || titleOrigins == null) return;
            for (int i = 0; i < titleGlyphs.Length && i < titleOrigins.Length; i++)
            {
                var glyph = titleGlyphs[i]; if (!glyph) continue;
                float beat = reduced ? 0 : Mathf.Max(0, Mathf.Sin(time * 2.8f - i * .5f));
                glyph.anchoredPosition = titleOrigins[i] + Vector2.up * (beat * beat * 3.5f);
                glyph.localScale = Vector3.one * (1 + beat * .025f);
                float fan = (i - (titleGlyphs.Length - 1) * .5f) * -4;
                glyph.localRotation = Quaternion.Euler(0, 0, fan + (reduced ? 0 : Mathf.Sin(time * 1.8f + i) * 1.3f));
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (foreground) { TitleOrnaments(vh); return; }
            // Clear centre, lively edges: decoration never receives pointer events.
            for (int y = 0; y < 360; y += 4)
                Box(vh, 0, y, 640, 4, Color.Lerp(Hex(0xa90f28), Hex(0xef583c), y / 360f));
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8 + Mathf.Sin(clock * .15f) * .025f;
                Wedge(vh, new Vector2(320, 104), angle, .045f, 390, Hex(0xffb936, .035f));
            }
            Disk(vh, 320, 106, 174, 90, Hex(0xffac32, .10f));
            Disk(vh, 320, 106, 151, 79, Hex(0xffd65a, .07f));
            Box(vh, 12, 12, 616, 1, GoldInk); Box(vh, 12, 347, 616, 1, GoldInk);
            Box(vh, 12, 12, 1, 336, GoldInk); Box(vh, 627, 12, 1, 336, GoldInk);
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? 23 : 595;
                Box(vh, x, 23, 22, 2, GoldInk); Box(vh, x, 23, 2, 18, GoldInk);
                Box(vh, x + 7, 29, 15, 2, GoldInk); Box(vh, x + 7, 29, 2, 12, GoldInk);
            }
            DriftingCloud(vh, 21, 119, .85f, 6, Hex(0xffd997, .48f));
            DriftingCloud(vh, 521, 151, .9f, -8, Hex(0xffdf9e, .45f));
            DriftingCloud(vh, 145, 34, .5f, 4, Hex(0xffecae, .3f));
            DriftingCloud(vh, 439, 29, .55f, -5, Hex(0xffecae, .3f));
            Lantern(vh, 67, 43, 1.1f, .2f); Lantern(vh, 137, 24, .7f, 1.4f);
            Lantern(vh, 573, 43, 1.1f, 2.2f); Lantern(vh, 503, 24, .7f, 3.1f);
            Roof(vh, 19, 239, 132); Roof(vh, 489, 239, 132);
            Firework(vh, 74, 176, 0); Firework(vh, 564, 178, 2.7f);
            Firework(vh, 178, 230, 4.8f); Firework(vh, 465, 229, 1.5f);
            for (int i = 0; i < 18; i++)
            {
                float x = 25 + i * 35;
                float y = 27 + Mathf.Repeat(i * 47 + clock * (3 + i % 3), 280);
                if (x > 205 && x < 435 && y > 175) continue;
                Diamond(vh, x, 330 - y, i % 3 == 0 ? 2 : 1, Hex(0xffd386, .55f));
            }
            for (int i = 0; i < 7; i++) DriftingCloud(vh, -35 + i * 119, 340 + (i % 2) * 7, .95f, 10, Hex(0xf9b848));
            DancingLion(vh, 105, 284, false, 0); DancingLion(vh, 535, 284, true, 1.8f);
            for (int i = 0; i < 6; i++) DriftingCloud(vh, -55 + i * 148, 357 + (i % 2) * 4, 1.35f, -17, Cream);
            for (int i = 0; i < 22; i++)
            {
                float x = Mathf.Repeat(i * 53 + Mathf.Sin(clock + i) * 7, 640);
                float y = Mathf.Repeat(i * 37 + clock * (12 + i % 5), 390) - 15;
                if (x > 220 && x < 420) continue;
                Box(vh, x, y, i % 2 == 0 ? 3 : 2, 3 + Mathf.Sin(clock * 3 + i) * 2, i % 3 == 0 ? GoldInk : Hex(0xff9980, .85f));
            }
            // Small plum blossoms, drawn above the cloud bank.
            Blossom(vh, 36, 206); Blossom(vh, 608, 197); Blossom(vh, 168, 316); Blossom(vh, 470, 323);
        }

        void TitleOrnaments(VertexHelper vh)
        {
            // Red silk tails and a gilt ribbon frame the much larger dancing letters.
            Wedge(vh, new Vector2(182, 123), Mathf.PI, .28f, 34, Hex(0x9b1424));
            Wedge(vh, new Vector2(458, 123), 0, .28f, 34, Hex(0x9b1424));
            Box(vh, 180, 126, 280, 10, Hex(0xbd182b));
            Box(vh, 180, 126, 280, 1, GoldInk); Box(vh, 180, 135, 280, 1, GoldInk);
            for (int i = 0; i < 18; i++)
            {
                float x = 177 + i * 17;
                Flame(vh, x, 130, 5 + Mathf.Sin(clock * 6 + i * 1.7f) * 2);
            }
            Firecrackers(vh, 146, 64, .3f); Firecrackers(vh, 494, 64, 1.7f);
            for (int i = 0; i < 18; i++)
            {
                float phase = Mathf.Repeat(clock * .38f + i * .137f, 1);
                float x = 167 + i * 18 + Mathf.Sin(i * 4 + clock) * 5;
                float y = 65 - phase * 34;
                Diamond(vh, x, y, phase < .5f ? 2 : 1, Hex(0xffec9e, (1 - phase) * .9f));
            }
        }

        void Firecrackers(VertexHelper vh, float x, float y, float phase)
        {
            x += Mathf.Sin(clock * 2 + phase) * 2;
            Box(vh, x - 1, y - 8, 2, 77, GoldInk);
            for (int i = 0; i < 7; i++)
            {
                float px = x + (i % 2 == 0 ? -12 : 3), py = y + i * 9;
                Box(vh, px, py + 2, 10, 16, Hex(0x841a27));
                Box(vh, px, py, 9, 15, Hex(0xff5141));
                Box(vh, px, py, 9, 3, Cream); Box(vh, px, py + 11, 9, 3, GoldInk);
                Box(vh, px + 2, py + 4, 2, 6, Hex(0xffab55));
            }
            float flicker = Mathf.Sin(clock * 7 + phase);
            Flame(vh, x, y - 8, 8 + flicker * 2);
            for (int i = 0; i < 7; i++)
            {
                float progress = Mathf.Repeat(clock * .7f + i / 7f + phase, 1);
                float angle = i * 2.4f;
                float radius = 3 + 18 * progress;
                Diamond(vh, x + Mathf.Cos(angle) * radius, y - 10 + Mathf.Sin(angle) * radius,
                    1, Hex(0xffe9af, 1 - progress));
            }
        }

        void Lantern(VertexHelper vh, float x, float y, float scale, float phase)
        {
            x += Mathf.Sin(clock * 1.2f + phase) * 2;
            Box(vh, x, 13, 1, y - 13, GoldInk);
            Disk(vh, x, y + 21 * scale, 28 * scale, 31 * scale, Hex(0xffbd7b, .08f));
            Disk(vh, x, y + 20 * scale, 17 * scale, 21 * scale, Hex(0x9b293e));
            Disk(vh, x, y + 18 * scale, 16 * scale, 19 * scale, Hex(0xed6455));
            Disk(vh, x, y + 18 * scale, 10 * scale, 19 * scale, Hex(0xff8b65));
            Box(vh, x - scale, y + 3 * scale, 2 * scale, 30 * scale, Hex(0xffd38b));
            Box(vh, x - 9 * scale, y - scale, 18 * scale, 3 * scale, GoldInk);
            Box(vh, x - 9 * scale, y + 36 * scale, 18 * scale, 3 * scale, GoldInk);
            Box(vh, x - scale, y + 39 * scale, 2 * scale, 14 * scale, GoldInk);
            Box(vh, x - 3 * scale, y + 50 * scale, 6 * scale, 8 * scale, Hex(0xffb875));
        }

        static void Roof(VertexHelper vh, float x, float y, float width)
        {
            Box(vh, x + 14, y, width - 28, 99, Hex(0xc63134));
            for (int i = 0; i < 4; i++) Box(vh, x + i * 9, y - 7 - i * 5, width - i * 18, 6, Hex(0x85182d));
            Box(vh, x, y - 8, width, 3, GoldInk);
            for (int i = 0; i < 3; i++)
            {
                float wx = x + 24 + i * 30;
                Box(vh, wx, y + 17, 22, 30, Hex(0x87404b)); Box(vh, wx + 3, y + 20, 16, 24, Hex(0xefb67d));
                Box(vh, wx + 10, y + 20, 2, 24, Hex(0xaa6056)); Box(vh, wx + 3, y + 31, 16, 2, Hex(0xaa6056));
            }
        }

        void DancingLion(VertexHelper vh, float x, float y, bool mirror, float phase)
        {
            float beat = Mathf.Sin(clock * 2.4f + phase);
            float blinkTime = Mathf.Repeat(clock + phase, 5.2f);
            float blink = blinkTime < .18f ? Mathf.Abs(blinkTime - .09f) / .09f : 1;
            Lion(vh, x + Mathf.Sin(clock * 1.2f + phase) * 2, y - Mathf.Max(0, beat) * 4, mirror, blink);
        }

        static void Lion(VertexHelper vh, float x, float y, bool mirror, float blink)
        {
            Disk(vh, x, y + 44, 39, 14, Hex(0x9d5955, .3f));
            Disk(vh, x, y + 19, 27, 25, RedInk);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI / 8;
                Disk(vh, x + Mathf.Cos(a) * 30, y - Mathf.Sin(a) * 26, 10, 11, Cream);
            }
            Disk(vh, x - 24, y - 22, 11, 13, RedInk); Disk(vh, x + 24, y - 22, 11, 13, RedInk);
            Disk(vh, x - 24, y - 23, 5, 6, GoldInk); Disk(vh, x + 24, y - 23, 5, 6, GoldInk);
            Disk(vh, x, y, 31, 26, Hex(0xe9514b));
            Diamond(vh, x, y - 20, 7, GoldInk); Diamond(vh, x, y - 20, 3, Cream);
            for (int i = -1; i <= 1; i += 2)
            {
                Disk(vh, x + i * 14, y - 4, 12, Mathf.Max(1, 13 * blink), Cream);
                if (blink > .2f)
                {
                    Disk(vh, x + i * 14 + (mirror ? -1 : 1), y - 2, 6, Mathf.Max(1, 8 * blink), Hex(0x663447));
                    Box(vh, x + i * 14 - 2, y - 4 - 3 * blink, 3, 3 * blink, Color.white);
                }
                Disk(vh, x + i * 24, y + 11, 6, 3, Hex(0xffb18a));
            }
            Disk(vh, x, y + 17, 19, 11, Cream);
            Disk(vh, x, y + 8, 6, 4, Hex(0x9e3044));
            Box(vh, x - 7, y + 21, 14, 2, Hex(0xa05b50));
            Box(vh, x - 5, y + 28, 10, 12, GoldInk);
        }

        static void Flame(VertexHelper vh, float x, float y, float size)
        {
            Disk(vh, x, y - 2, size * .7f, size, Hex(0xff9659, .5f));
            Wedge(vh, new Vector2(x, y - size * 2), Mathf.PI * .5f, .35f, size * 2.5f, Hex(0xffb52f));
            Wedge(vh, new Vector2(x, y - size), Mathf.PI * .5f, .28f, size * 1.5f, Cream);
        }

        void DriftingCloud(VertexHelper vh, float start, float y, float scale, float speed, Color ink)
        {
            // Wrap only after the entire silhouette leaves the frame, never in the middle.
            float width = 88 * scale + 8;
            float x = Mathf.Repeat(start + clock * speed + width, 640 + width + 8) - width;
            Cloud(vh, x, y + Mathf.Sin(clock * .7f + start) * 2, scale, ink);
        }

        void Firework(VertexHelper vh, float x, float y, float phase)
        {
            float age = Mathf.Repeat(clock + phase, 6.8f);
            if (age < .8f)
            {
                float rise = age / .8f;
                for (int i = 0; i < 5; i++)
                    Diamond(vh, x, y + 58 * (1 - rise) + i * 3, 1, Hex(0xffdb79, (1 - i / 5f) * .8f));
            }
            else if (age < 2.8f)
            {
                float t = (age - .8f) / 2;
                float radius = 3 + 32 * (1 - Mathf.Pow(1 - t, 3));
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * Mathf.PI / 6;
                    for (int tail = 0; tail < 3; tail++)
                    {
                        float r = Mathf.Max(0, radius - tail * 3);
                        Diamond(vh, x + Mathf.Cos(angle) * r, y + Mathf.Sin(angle) * r + t * t * 13,
                            tail == 0 ? 1.6f : .8f, Hex(i % 3 == 0 ? 0xfff0b0 : 0xffbd39, (1 - t) * (1 - tail * .25f)));
                    }
                }
            }
        }

        static void Wedge(VertexHelper vh, Vector2 origin, float angle, float spread, float length, Color ink)
        {
            Vector2 a = origin + new Vector2(Mathf.Cos(angle - spread), Mathf.Sin(angle - spread)) * length;
            Vector2 b = origin + new Vector2(Mathf.Cos(angle + spread), Mathf.Sin(angle + spread)) * length;
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(Mathf.Clamp(origin.x, 0, 640), -Mathf.Clamp(origin.y, 0, 360)), ink, Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Clamp(a.x, 0, 640), -Mathf.Clamp(a.y, 0, 360)), ink, Vector2.zero);
            vh.AddVert(new Vector3(Mathf.Clamp(b.x, 0, 640), -Mathf.Clamp(b.y, 0, 360)), ink, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
        }
        static void Blossom(VertexHelper vh, float x, float y)
        {
            for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * .4f; Disk(vh, x + Mathf.Cos(a) * 4, y + Mathf.Sin(a) * 4, 3, 3, Hex(0xffc4a1)); }
            Diamond(vh, x, y, 2, GoldInk);
        }
        static void Cloud(VertexHelper vh, float x, float y, float s, Color ink)
        {
            Disk(vh, x + 20 * s, y, 22 * s, 9 * s, ink); Disk(vh, x + 43 * s, y - 8 * s, 19 * s, 15 * s, ink);
            Disk(vh, x + 66 * s, y - 2 * s, 19 * s, 11 * s, ink); Box(vh, x + 15 * s, y, 64 * s, 6 * s, ink);
            Box(vh, x + 26 * s, y + 10 * s, 37 * s, 2 * s, ink);
        }
        static void Disk(VertexHelper vh, float cx, float cy, float rx, float ry, Color ink)
        {
            // Horizontal strips keep every silhouette on the same pixel grid as the game.
            for (float y = -ry; y < ry; y += 2)
            {
                float half = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((y + 1) / ry, 2))) * rx;
                Box(vh, Mathf.Round(cx - half), Mathf.Round(cy + y), Mathf.Round(half * 2), 2, ink);
            }
        }
        static void Diamond(VertexHelper vh, float x, float y, float radius, Color ink)
        {
            Box(vh, Mathf.Round(x - radius), Mathf.Round(y), Mathf.Ceil(radius * 2 + 1), 1, ink);
            Box(vh, Mathf.Round(x), Mathf.Round(y - radius), 1, Mathf.Ceil(radius * 2 + 1), ink);
        }
        static void Box(VertexHelper vh, float x, float y, float w, float h, Color ink)
        {
            float right = Mathf.Min(640, x + w), bottom = Mathf.Min(360, y + h);
            x = Mathf.Max(0, x); y = Mathf.Max(0, y);
            w = right - x; h = bottom - y;
            if (w <= 0 || h <= 0) return;
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x, -y), ink, Vector2.zero); vh.AddVert(new Vector3(x + w, -y), ink, Vector2.zero);
            vh.AddVert(new Vector3(x + w, -y - h), ink, Vector2.zero); vh.AddVert(new Vector3(x, -y - h), ink, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 2, n + 3, n);
        }
    }
}
