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
        public PixelIrisTransition irisTransition;
        [SerializeField, Min(.05f)] float irisCloseSeconds = .38f, irisOpenSeconds = .46f;
        enum IrisPhase { Idle, Closing, Covered, Opening }
        IrisPhase irisPhase;
        Action navigateAtCover;
        float irisElapsed;
        bool irisReduced;
        public bool IsTransitioning => irisPhase != IrisPhase.Idle;
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
        public void Transition() { if (Application.isPlaying && !IsTransitioning) transitionAlpha = .85f; }

        public bool Navigate(Action next)
        {
            if (IsTransitioning || next == null) return false;
            if (!irisTransition) { next(); return true; }
            navigateAtCover = next; irisElapsed = 0; irisPhase = IrisPhase.Closing;
            irisReduced = GameRoot.Instance && GameRoot.Instance.ReduceMotion;
            transitionAlpha = 0;
            if (transitionOverlay) { transitionOverlay.color = Color.clear; transitionOverlay.raycastTarget = false; }
            irisTransition.transform.SetAsLastSibling();
            irisTransition.raycastTarget = true;
            irisTransition.SetReveal(1, irisReduced);
            return true;
        }

        void TickIris(float dt)
        {
            if (!IsTransitioning) return;
            irisElapsed += dt;
            if (irisPhase == IrisPhase.Closing)
            {
                float p = Mathf.Clamp01(irisElapsed / (irisReduced ? .12f : irisCloseSeconds));
                irisTransition.SetReveal(1 - Mathf.SmoothStep(0, 1, p), irisReduced);
                if (p >= 1) { irisPhase = IrisPhase.Covered; irisElapsed = 0; }
            }
            else if (irisPhase == IrisPhase.Covered && irisElapsed >= .06f)
            {
                // The callback runs on a later frame, after a fully covered frame.
                var next = navigateAtCover; navigateAtCover = null;
                irisPhase = IrisPhase.Opening; irisElapsed = 0;
                try { next?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex, this); }
            }
            else if (irisPhase == IrisPhase.Opening)
            {
                float p = Mathf.Clamp01(irisElapsed / (irisReduced ? .12f : irisOpenSeconds));
                irisTransition.SetReveal(Mathf.SmoothStep(0, 1, p), irisReduced);
                if (p >= 1) ResetIris();
            }
        }

        void ResetIris()
        {
            irisPhase = IrisPhase.Idle; navigateAtCover = null; irisElapsed = 0;
            if (irisTransition) { irisTransition.SetReveal(1); irisTransition.raycastTarget = false; }
        }
        void OnDisable() { ResetIris(); }
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TickIris(dt);
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
