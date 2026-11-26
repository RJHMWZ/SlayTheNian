using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    // All layers use card-local coordinates, so text, frame and art dissolve together.
    [DisallowMultipleComponent]
    public sealed class CardSurfaceEffects : MonoBehaviour
    {
        static Material surfaceMaterial;
        CardSurfaceMesh[] layers;
        RectTransform card;
        float dissolve, phase;
        bool reduced;
        public float Dissolve => dissolve;
        public float SweepPhase => reduced || dissolve > 0 ? -1 : phase;
        public RectTransform Card => card ? card : card = (RectTransform)transform;
        public Material SurfaceMaterial
        {
            get
            {
                if (!surfaceMaterial) surfaceMaterial = Resources.Load<Material>("Sleigh/Materials/CardSurface");
                return surfaceMaterial;
            }
        }

        public void Configure(int identity, bool reduceMotion)
        {
            dissolve = 0; reduced = reduceMotion;
            // Cosmetic phase must not consume the seeded gameplay RNG.
            phase = Mathf.Repeat((identity > 0 ? identity : transform.GetSiblingIndex() + 1) * .173f + .31f, 1);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas) canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            var graphics = GetComponentsInChildren<Graphic>(true);
            layers = new CardSurfaceMesh[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
            {
                layers[i] = PixelUI.Ensure<CardSurfaceMesh>(graphics[i]);
                layers[i].Bind(this);
            }
        }

        public void SetDissolve(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (dissolve == progress) return;
            dissolve = progress; DirtyLayers();
        }

        void Update()
        {
            bool next = GameRoot.Instance && GameRoot.Instance.ReduceMotion;
            if (reduced == next) return;
            reduced = next; DirtyLayers();
        }

        void DirtyLayers()
        {
            if (layers == null) return;
            foreach (var layer in layers) if (layer) layer.Refresh();
        }

        void OnDisable() { dissolve = 0; }
    }
}
