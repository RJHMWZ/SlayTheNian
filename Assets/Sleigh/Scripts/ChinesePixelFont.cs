using System;
using System.Collections.Generic;
using UnityEngine;
namespace Sleigh
{
    public static class ChinesePixelFont
    {
        [Serializable] public sealed class Glyph { public int code, advance; }
        [Serializable] public sealed class Atlas { public int cell, columns, width, height; public Glyph[] glyphs; }
        static Atlas data;
        static Texture2D texture;
        static Dictionary<char,int> indices;
        public static Texture2D Texture { get { Load(); return texture; } }
        static void Load()
        {
            if (data != null) return;
            data = JsonUtility.FromJson<Atlas>(Resources.Load<TextAsset>("Sleigh/Fonts/Chinese12").text);
            texture = Resources.Load<Texture2D>("Sleigh/Fonts/Chinese12"); texture.filterMode = FilterMode.Point;
            indices = new Dictionary<char,int>(data.glyphs.Length);
            for (int i=0;i<data.glyphs.Length;i++) indices[(char)data.glyphs[i].code]=i;
        }
        public static bool HasGlyph(char c) { Load(); return indices.ContainsKey(c); }
        public static int Advance(char c)
        { Load(); return indices.TryGetValue(c,out var i) ? data.glyphs[i].advance : 12; }
        public static Rect UV(char c)
        {
            Load(); if (!indices.TryGetValue(c,out var i)) i=indices['□'];
            return new Rect(i%data.columns*data.cell/(float)data.width,1-(i/data.columns+1)*data.cell/(float)data.height,data.cell/(float)data.width,data.cell/(float)data.height);
        }
    }
}
