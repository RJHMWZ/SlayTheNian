using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleigh
{
    public static class PixelUI
    {
        public static T Ensure<T>(Component owner) where T : Component
        { var component = owner.GetComponent<T>(); return component ? component : owner.gameObject.AddComponent<T>(); }
        public static readonly Color White = Hex(0xfff8ec), Gold = Hex(0xf6c94f), Muted = Hex(0x95a2c6), Pink = Hex(0xff8fb1), Ice = Hex(0x7fd3ff), Green = Hex(0x7be08f), Red = Hex(0xe0443e), Panel = Hex(0x141a36), Outline = Hex(0x0b0b16);
        public static Color Hex(int c, float a = 1) => new Color((c >> 16 & 255) / 255f, (c >> 8 & 255) / 255f, (c & 255) / 255f, a);
        public static RectTransform Group(Transform parent, string name, float x = 0, float y = 0, float w = 640, float h = 360)
            => Acquire(parent, "Group", name, x, y, w, h);
        public static RectTransform Acquire(Transform parent, string kind, string name, float x = 0, float y = 0, float w = 640, float h = 360, GameObject prefab = null)
        {
            var r = PixelPoolBucket.On(parent).Take(kind, name, prefab);
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, -y); return r;
        }
        public static Image Rect(Transform p, float x, float y, float w, float h, Color c, bool raycast = false, string name = "Pixel")
        { var r = Acquire(p, "Rect", name, x, y, w, h); var im = PixelUI.Ensure<Image>(r); im.sprite = null; im.type = Image.Type.Simple; im.color = c; im.raycastTarget = raycast; return im; }
        public static Image Sprite(Transform p, string type, string anim, float x, float y, float scale = 1, bool originalPivot = true, float width = -1, float height = -1)
        {
            var f = DB.Frame(type, anim); var sp = DB.Sprite(type, anim); if (f == null || !sp) return null;
            float w = width > 0 ? width : f.width * scale, h = height > 0 ? height : f.height * scale;
            var r = Acquire(p, "Sprite", type + ":" + anim, x, y, w, h); if (originalPivot) r.pivot = new Vector2(f.pivotX, 1 - f.pivotY);
            var im = PixelUI.Ensure<Image>(r); im.sprite = sp; im.type = Image.Type.Simple; im.color = Color.white; im.raycastTarget = false; return im;
        }
        public static PixelLabel Label(Transform p, float x, float y, float w, string text, float scale = 1, Color? color = null, int align = 0, bool wrap = false)
        {
            var r = Acquire(p, "Label", "Text:" + (text ?? ""), x, y, w, 16 + PixelLabel.Lines(text, w, scale, wrap).Count * 12 * scale);
            var l = PixelUI.Ensure<PixelLabel>(r); l.text = text; l.fontScale = scale; l.color = color ?? White; l.align = align; l.wrap = wrap; l.shadow = true; l.raycastTarget = false; l.SetVerticesDirty(); return l;
        }
        public static RectTransform PanelBox(Transform p, float x, float y, float w, float h, Color? trim = null)
        {
            var r = Acquire(p, "Panel", "Panel", x, y, w, h); Rect(r, 0, 0, w, h, Outline); Rect(r, 1, 1, w - 2, h - 2, trim ?? Hex(0x54618c));
            Rect(r, 3, 3, w - 6, h - 6, Panel); Rect(r, 4, 4, w - 8, 1, Hex(0x253262)); return r;
        }
        public static Hotspot Hot(Transform p, float x, float y, float w, float h, Action click, Action enter = null, Action exit = null)
        {
            var r = Acquire(p, "Hotspot", "Hotspot", x, y, w, h);
            var im = PixelUI.Ensure<Image>(r); im.sprite = null; im.color = Color.clear; im.raycastTarget = true;
            var hsp = PixelUI.Ensure<Hotspot>(im); hsp.ClearBindings(); hsp.click = click; hsp.enter = enter; hsp.exit = exit; return hsp;
        }
        public static Hotspot Button(Transform p, float x, float y, float w, float h, string label, Action click, Color? c = null, bool enabled = true)
        {
            var r = Acquire(p, "Button", "Button:" + label, x, y, w, h);
            Rect(r, 0, 2, w, h, Outline); var im = Rect(r, 0, 0, w, h, c ?? Hex(0x253262), true);
            Rect(r, 1, 1, w - 2, 1, Hex(0xffffff, .16f)); Rect(r, 1, h - 2, w - 2, 1, Hex(0x000000, .3f));
            Label(r, 2, (h - 12 * .8f) / 2, w - 4, label, .8f, enabled ? White : Muted, 1);
            var hot = PixelUI.Ensure<Hotspot>(im); hot.ClearBindings(); hot.enabledAction = enabled; hot.click = () => { SleighAudio.Instance?.Play("click"); click?.Invoke(); };
            Color normal = im.color; if (!enabled) im.color = normal * new Color(.5f, .5f, .5f, 1);
            hot.enter = () => { if (enabled) { im.color = Color.Lerp(normal, Color.white, .18f); SleighAudio.Instance?.Play("hover"); } };
            hot.exit = () => { if (im && enabled) im.color = normal; }; return hot;
        }
        public static void Clear(Transform t)
        { if (t) PixelPoolBucket.On(t).ReleaseChildren(); }
        public static void Release(Component c)
        { if (!c) return; var slot = c.GetComponent<PixelPoolElement>(); if (slot) slot.Release(); else c.gameObject.SetActive(false); }
    }
}
