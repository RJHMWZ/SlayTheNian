using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    public sealed class SleighStage : MonoBehaviour
    {
        [Serializable] public class View { public string name; public RectTransform background, world, ui; }
        public Canvas canvas;
        public Camera pixelCamera;
        public RectTransform viewport, backgroundLayer, worldLayer, uiLayer, effectLayer, popupLayer, effectPool, popupPool, snowRoot;
        public View[] views;
        public RectTransform[] snowParticles;
        public PixelParticleField particles;
        public Image flashOverlay, transitionOverlay;
        public SleighAudio audioPlayer;
        public Material[] actorFlashMaterials;
        public RectTransform arrowRoot, bannerRoot, bubbleRoot, lowHealthRoot;
        public Image[] arrowDots;
        public Image reactionIcon;
        public PixelLabel bannerTitle, bannerSubtitle, bubbleText;
        public int bakeVersion;
        float flashAlpha, flashFade = 5, transitionAlpha, shake, punch;
        Color flashColor;
        public View Show(string name)
        {
            View selected = null;
            foreach (var v in views)
            { bool active = v.name == name; v.background.gameObject.SetActive(active); v.world.gameObject.SetActive(active); v.ui.gameObject.SetActive(active); if (active) selected = v; }
            if (selected == null) throw new InvalidOperationException("Scene has no baked view: " + name);
            if (snowRoot) snowRoot.gameObject.SetActive(false);
            if (particles) particles.ClearParticles(); shake = punch = 0;
            if (arrowRoot) arrowRoot.gameObject.SetActive(false); if (reactionIcon) reactionIcon.gameObject.SetActive(false);
            if (bannerRoot) bannerRoot.gameObject.SetActive(false); if (bubbleRoot) bubbleRoot.gameObject.SetActive(false); if (lowHealthRoot) lowHealthRoot.gameObject.SetActive(false);
            worldLayer.anchoredPosition = new Vector2(320, -180); worldLayer.localScale = Vector3.one;
            return selected;
        }
        public void Flash(Color color, float alpha = .2f, float seconds = .2f)
        { flashColor = color; flashAlpha = alpha; flashFade = alpha / Mathf.Max(.01f, seconds); }
        public void Impact(float strength, float zoom = .025f) { shake = Mathf.Max(shake, strength); punch = Mathf.Max(punch, zoom); }
        public void Transition() { if (Application.isPlaying) transitionAlpha = .85f; }
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool reduced = GameRoot.Instance && GameRoot.Instance.ReduceMotion;
            if (particles) particles.reduced = reduced;
            flashAlpha = Mathf.Max(0, flashAlpha - dt * flashFade); transitionAlpha = Mathf.Max(0, transitionAlpha - dt * 5);
            if (flashOverlay) flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, reduced ? 0 : flashAlpha);
            if (transitionOverlay) { transitionOverlay.color = new Color(0, 0, 0, transitionAlpha); transitionOverlay.raycastTarget = transitionAlpha > .12f; }
            shake *= Mathf.Exp(-14 * dt); punch *= Mathf.Exp(-10 * dt);
            if (worldLayer)
            {
                worldLayer.anchoredPosition = new Vector2(320, -180) + (reduced ? Vector2.zero : new Vector2(Mathf.Sin(Time.unscaledTime * 91), Mathf.Cos(Time.unscaledTime * 77)) * shake);
                worldLayer.localScale = Vector3.one * (1 + (reduced ? 0 : punch));
            }
        }
    }
}
