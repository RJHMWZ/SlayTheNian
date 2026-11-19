using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    // One baked CanvasRenderer replaces hundreds of per-burst Image GameObjects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelParticleField : MaskableGraphic
    {
        struct Dot { public Vector2 position, velocity; public float age, life, size, gravity, floor; public Color tint; public bool active; }
        readonly Dot[] dots = new Dot[768];
        static readonly Color[] confettiColors = { PixelUI.Red, PixelUI.Hex(0xba273e), PixelUI.Gold, PixelUI.Hex(0xffda8c), PixelUI.Hex(0xf47752) };
        int cursor;
        public int ActiveCount { get; private set; }
        public bool reduced;
        public void ClearParticles() { for (int i = 0; i < dots.Length; i++) dots[i].active = false; ActiveCount = 0; SetVerticesDirty(); }
        public void Burst(Vector2 origin, int count, Color tint, float speed = 110, float gravity = 180, float life = .65f, bool up = false, bool floor = true)
        {
            if (reduced) return;
            for (int i = 0; i < count; i++)
            {
                float angle = up ? Random.Range(-2.6f, -.5f) : Random.Range(-Mathf.PI, Mathf.PI);
                float velocity = Random.Range(speed * .35f, speed);
                dots[cursor] = new Dot { active = true, position = origin, velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * velocity,
                    life = life * Random.Range(.7f, 1.25f), size = Random.value < .25f ? 3 : 2, gravity = gravity, floor = floor ? 232 : 10000, tint = Random.value < .25f ? PixelUI.White : tint };
                cursor = (cursor + 1) % dots.Length;
            }
            SetVerticesDirty();
        }
        public void Confetti(Vector2 origin, int count)
        {
            for (int i = 0; i < confettiColors.Length; i++) Burst(origin, count / confettiColors.Length, confettiColors[i], 160, 120, 1.6f, false, false);
        }
        void Update()
        {
            float dt = Time.unscaledDeltaTime; int active = 0;
            for (int i = 0; i < dots.Length; i++)
            {
                if (!dots[i].active) continue;
                var p = dots[i]; p.age += dt;
                if (p.age >= p.life) { dots[i].active = false; continue; }
                p.velocity.y += p.gravity * dt; p.position += p.velocity * dt;
                if (p.position.y >= p.floor && p.velocity.y > 0) { p.position.y = p.floor; p.velocity.y *= -.35f; p.velocity.x *= .7f; }
                dots[i] = p; active++;
            }
            if (active > 0 || ActiveCount > 0) SetVerticesDirty(); ActiveCount = active;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < dots.Length; i++)
            {
                var p = dots[i]; if (!p.active) continue;
                float x = Mathf.Round(p.position.x), y = -Mathf.Round(p.position.y); var c = p.tint; c.a *= Mathf.Clamp01((1 - p.age / p.life) * 3);
                int v = vh.currentVertCount;
                vh.AddVert(new Vector3(x, y), c, Vector2.zero); vh.AddVert(new Vector3(x + p.size, y), c, Vector2.zero);
                vh.AddVert(new Vector3(x + p.size, y - p.size), c, Vector2.zero); vh.AddVert(new Vector3(x, y - p.size), c, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v + 2, v + 3, v);
            }
        }
    }
}
