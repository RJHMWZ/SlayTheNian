using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleigh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelLabel : MaskableGraphic
    {
        public string text = ""; public float fontScale = 1; public int align; public bool shadow = true, wrap;
        static Texture2D atlas; static string chars;
        static readonly Dictionary<char, int> widths = new Dictionary<char, int>();
        List<string> cachedLines;
        string cachedText;
        float cachedWidth, cachedScale;
        bool cachedWrap;
        public override Texture mainTexture
        {
            get
            {
                if (!atlas) atlas = Resources.Load<Texture2D>("Sleigh/Art/Txt__default__0");
                if (atlas) atlas.filterMode = FilterMode.Point;
                return atlas ? atlas : Texture2D.whiteTexture;
            }
        }
        static void Init()
        {
            if (chars != null) return;
            chars = Resources.Load<TextAsset>("Sleigh/FontChars").text;
            string[] groups = { "MV", "ABCDEFGHKNOPQRSTUXYZabcdeghknopqsuvxy1234567890?^*+", "><fzJLrtz\"", "j-/I@", "il,() ", ".!:;'" };
            int[] values = { 9, 8, 7, 6, 5, 4 };
            for (int i = 0; i < groups.Length; i++) foreach (char c in groups[i]) widths[c] = values[i];
        }
        public static int CharWidth(char c) { Init(); return widths.TryGetValue(c, out int n) ? n : 10; }
        public static float Measure(string s, float scale = 1)
        { float w = 0; foreach (char c in s) w += CharWidth(c); return w * scale; }
        public static List<string> Lines(string s, float width, float scale, bool wrap)
        {
            Init(); var result = new List<string>();
            foreach (var paragraph in (s ?? "").Replace("\r", "").Split('\n'))
            {
                if (!wrap || width <= 0) { result.Add(paragraph); continue; }
                string line = "";
                foreach (var word in paragraph.Split(' '))
                {
                    if (line.Length > 0 && Measure(line + " " + word, scale) > width) { result.Add(line); line = ""; }
                    line += (line.Length > 0 ? " " : "") + word;
                    while (Measure(line, scale) > width && line.Length > 1)
                    { int cut = line.Length - 1; while (cut > 1 && Measure(line.Substring(0, cut), scale) > width) cut--; result.Add(line.Substring(0, cut)); line = line.Substring(cut); }
                }
                result.Add(line);
            }
            return result;
        }
        public void Set(string value) { if (value == text) return; text = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Init(); vh.Clear(); if (text == null) return;
            if (cachedLines == null || cachedText != text || cachedWidth != rectTransform.rect.width || cachedScale != fontScale || cachedWrap != wrap)
            { cachedText = text; cachedWidth = rectTransform.rect.width; cachedScale = fontScale; cachedWrap = wrap; cachedLines = Lines(text, cachedWidth, fontScale, wrap); }
            var lines = cachedLines;
            float top = rectTransform.rect.yMax;
            foreach (var line in lines)
            {
                float x = rectTransform.rect.xMin + (align == 1 ? (rectTransform.rect.width - Measure(line, fontScale)) * .5f : align == 2 ? rectTransform.rect.width - Measure(line, fontScale) : 0);
                foreach (var raw in line)
                {
                    char c = raw == '\u2019' || raw == '\u2018' ? '\'' : raw == '\u201c' || raw == '\u201d' ? '"' : raw == '\u2014' || raw == '\u2013' ? '-' : raw;
                    int i = chars.IndexOf(c); if (i < 0) i = chars.IndexOf('?');
                    if (c != ' ' && i >= 0)
                    {
                        var tex = mainTexture; float u0 = (i % 13) * 10f / tex.width, u1 = u0 + 10f / tex.width;
                        float v1 = 1 - (i / 13) * 12f / tex.height, v0 = v1 - 12f / tex.height;
                        if (shadow) Quad(vh, x + fontScale, top - fontScale, fontScale * 10, fontScale * 12, u0, v0, u1, v1, new Color(0.015f, .015f, .035f, color.a * .9f));
                        Quad(vh, x, top, fontScale * 10, fontScale * 12, u0, v0, u1, v1, color);
                    }
                    x += CharWidth(c) * fontScale;
                }
                top -= 12 * fontScale;
            }
        }
        static void Quad(VertexHelper vh, float x, float top, float w, float h, float u0, float v0, float u1, float v1, Color c)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x, top - h), c, new Vector2(u0, v0)); vh.AddVert(new Vector3(x, top), c, new Vector2(u0, v1));
            vh.AddVert(new Vector3(x + w, top), c, new Vector2(u1, v1)); vh.AddVert(new Vector3(x + w, top - h), c, new Vector2(u1, v0));
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 2, n + 3, n);
        }
    }

}
