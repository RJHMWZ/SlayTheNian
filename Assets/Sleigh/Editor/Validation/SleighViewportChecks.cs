#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sleigh.Editor
{
    public static class SleighViewportChecks
    {
        [MenuItem("Sleigh/Check Responsive Viewport")]
        public static void Check()
        {
            var owner = new GameObject("Viewport verification", typeof(RectTransform), typeof(SleighStage));
            owner.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var stage = owner.GetComponent<SleighStage>();
                stage.viewport = Child(owner.transform);
                stage.viewport.anchorMin = stage.viewport.anchorMax = stage.viewport.pivot = new Vector2(.5f, .5f);
                stage.viewport.gameObject.AddComponent<RectMask2D>();
                var bg = Child(Child(stage.viewport));
                stage.views = new[] { new SleighStage.View { name = "Test", background = bg } };
                stage.popupPool = Child(Child(stage.viewport));
                var cover = Child(stage.popupPool); cover.gameObject.AddComponent<Image>(); stage.CoverScreen(cover);
                var mask = Child(stage.viewport).gameObject.AddComponent<PixelIrisTransition>(); stage.irisTransition = mask;
                var ui = Child(stage.viewport);
                var sizes = new[] { new Vector2(1600, 900), new Vector2(1920, 1080), new Vector2(2400, 1080),
                    new Vector2(2560, 1080), new Vector2(2048, 1536), new Vector2(2400, 1080), new Vector2(1600, 900) };
                for (int i = 0; i < sizes.Length; i++)
                {
                    Vector2 screen = sizes[i];
                    Rect safe = i == 5 ? new Rect(96, 24, 2304, 1056) : new Rect(Vector2.zero, screen);
                    float scale = stage.FitViewport(screen, safe);
                    Vector2 centre = stage.viewport.anchoredPosition + screen * .5f;
                    Require((centre - safe.center).sqrMagnitude < .01f, "Content must follow the safe-area centre");
                    Require(640 * scale <= safe.width + .01f && 360 * scale <= safe.height + .01f, "Controls must fit inside the safe area");
                    Require(stage.viewport.localScale.x == stage.viewport.localScale.y && ui.localScale == Vector3.one,
                        "Character and UI proportions must stay uniform");
                    Require(!stage.viewport.GetComponent<RectMask2D>().enabled, "Fixed viewport must not clip the scenery");
                    float zoom = bg.localScale.x;
                    Vector2 backgroundCentre = centre + (new Vector2(-320, 180) + bg.anchoredPosition + new Vector2(320, -180) * zoom) * scale;
                    Require((backgroundCentre - screen * .5f).sqrMagnitude < .01f, "Scenery must be centred on the full screen");
                    Require(640 * zoom * scale >= screen.x - .01f && 360 * zoom * scale >= screen.y - .01f,
                        "Scenery must cover the screen without bars");
                    foreach (var overlay in new[] { mask.rectTransform, cover })
                    {
                        Vector2 overlayTopLeft = centre + (new Vector2(-320, 180) + overlay.anchoredPosition) * scale;
                        Require((overlayTopLeft - new Vector2(0, screen.y)).sqrMagnitude < .01f &&
                            (overlay.sizeDelta * scale - screen).sqrMagnitude < .01f, "Transitions and modal covers must cover the full display");
                    }
                    if (screen == new Vector2(1600, 900)) Require(Mathf.Abs(scale - 2.5f) < .001f, "1600x900 must use 2.5x rather than 2x scaling");
                }
                Debug.Log("Responsive viewport checks passed: 16:9, 20:9, ultrawide, tablet, asymmetric safe area, repeated resize and full-screen covers.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        static RectTransform Child(Transform parent)
        {
            var go = new GameObject("Test layer", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(640, 360); return rect;
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
