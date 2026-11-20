using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    // Baked into reusable UI elements. Does not create tweens, coroutines or objects per frame.
    public sealed class SleighMotion : MonoBehaviour
    {
        public enum Style { None, Float, Pulse, Swing, Slide, Sleigh, Spotlight, Sparkle }
        public Style style;
        public Vector2 origin;
        public float amplitude = 2, frequency = 2, phase, duration = .35f, delay;
        public Vector2 from;
        public bool reduceMotion;
        float born;
        RectTransform rect;
        Vector3 baseScale;
        public void ResetMotion() { style = Style.None; enabled = false; if (!rect) rect = (RectTransform)transform; born = Time.unscaledTime; }
        public void Configure(Style mode, float amount = 2, float hz = 2, float offset = 0)
        { enabled = true; rect = (RectTransform)transform; style = mode; origin = rect.anchoredPosition; baseScale = rect.localScale; amplitude = amount; frequency = hz; phase = offset; born = Time.unscaledTime; }
        void LateUpdate()
        {
            if (!rect || style == Style.None || reduceMotion || (GameRoot.Instance && GameRoot.Instance.ReduceMotion)) return;
            float t = Time.unscaledTime - born, wave = Mathf.Sin(t * frequency + phase);
            switch (style)
            {
                case Style.Float: rect.anchoredPosition = origin + Vector2.up * (wave * amplitude); break;
                case Style.Pulse: rect.localScale = baseScale * (1 + wave * amplitude); break;
                case Style.Swing: rect.localEulerAngles = new Vector3(0, 0, wave * amplitude); break;
                case Style.Sleigh: rect.anchoredPosition = new Vector2(Mathf.Repeat(origin.x + t * 40 + 300, 980) - 300, origin.y + wave * 6); break;
                case Style.Slide:
                    float p = Mathf.Clamp01((t - delay) / Mathf.Max(.01f, duration));
                    float ease = 1 - Mathf.Pow(1 - p, 3); rect.anchoredPosition = origin + from * (1 - ease);
                    if (p >= 1) style = Style.None; break;
                case Style.Spotlight:
                    var image = GetComponent<Image>(); if (image) { var c = image.color; c.a = .85f + wave * .05f; image.color = c; } break;
            }
        }
        public static SleighMotion Add(RectTransform r, Style mode, float amount = 2, float hz = 2, float phase = 0)
        { var m = PixelUI.Ensure<SleighMotion>(r); m.Configure(mode, amount, hz, phase); return m; }
    }
}
