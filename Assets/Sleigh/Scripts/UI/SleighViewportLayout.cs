using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    public sealed partial class SleighStage
    {
        Vector2 layoutScreen, overlaySize = new Vector2(640, 360), overlayOrigin;
        Rect layoutSafeArea;
        float contentScale = 1;
        const string ScreenCoverName = "Full screen overlay";

        public float FitViewport(Vector2 screen, Rect safeArea)
        {
            if (!viewport || screen.x <= 0 || screen.y <= 0) return contentScale;
            float left = Mathf.Max(0, safeArea.xMin), bottom = Mathf.Max(0, safeArea.yMin);
            float right = Mathf.Min(screen.x, safeArea.xMax), top = Mathf.Min(screen.y, safeArea.yMax);
            safeArea = right > left && top > bottom ? Rect.MinMaxRect(left, bottom, right, top) : new Rect(Vector2.zero, screen);
            if (layoutScreen == screen && layoutSafeArea == safeArea) return contentScale;
            layoutScreen = screen; layoutSafeArea = safeArea;

            // Keep the authored 640x360 play area proportional and inside the safe area.
            // Fractional scales fill 16:9 screens such as 1600x900 without integer-fit bars.
            contentScale = Mathf.Min(safeArea.width / 640, safeArea.height / 360);
            viewport.localScale = Vector3.one * contentScale;
            viewport.anchoredPosition = safeArea.center - screen * .5f;
            var clip = viewport.GetComponent<RectMask2D>();
            if (clip) clip.enabled = false;

            // Extend only the scenery beyond the play area, using uniform cover scaling.
            // Characters, HUD and hit regions retain their original proportions.
            float cover = Mathf.Max(screen.x / (640 * contentScale), screen.y / (360 * contentScale));
            Vector2 offset = (screen * .5f - safeArea.center) / contentScale;
            foreach (var view in views)
            {
                view.background.localScale = Vector3.one * cover;
                view.background.anchoredPosition = new Vector2(320, -180) * (1 - cover) + offset;
            }
            overlaySize = screen / contentScale;
            overlayOrigin = new Vector2(320 - overlaySize.x * .5f, -180 + overlaySize.y * .5f) + offset;
            if (irisTransition) PlaceScreenOverlay(irisTransition.rectTransform);
            if (transitionOverlay) PlaceScreenOverlay(transitionOverlay.rectTransform);
            if (flashOverlay) PlaceScreenOverlay(flashOverlay.rectTransform);
            if (popupPool)
                foreach (var image in popupPool.GetComponentsInChildren<Image>(true))
                    if (image.name == ScreenCoverName) PlaceScreenOverlay(image.rectTransform);
            foreach (var drag in viewport.GetComponentsInChildren<MapDrag>(true)) drag.scale = contentScale;
            return contentScale;
        }

        public void CoverScreen(RectTransform rect)
        {
            rect.name = ScreenCoverName;
            PlaceScreenOverlay(rect);
        }

        void PlaceScreenOverlay(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = overlaySize; rect.anchoredPosition = overlayOrigin;
        }
    }
}
