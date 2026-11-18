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
        static readonly Color[] confettiColors = { PixelUI.Red, PixelUI.Green, PixelUI.Gold, PixelUI.White, PixelUI.Pink };
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
        // Delayed, shaped fireworks share the fixed particle pool. They never flash the whole screen.
        public void Firework(Vector2 origin, int pattern, Color tint)
        {
            if (reduced) return;
            for (int i = 0; i < 8; i++)
                FireworkDot(origin + new Vector2(0, 72 - i * 8), new Vector2(0, -70), PixelUI.Gold, .35f, i * .035f, 2, 0);
            int count = pattern == 0 ? 40 : 32;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                float speed = pattern == 0 ? 25 + (i % 5) * 7 : pattern == 3 ? 30 + (i % 4) * 5 : 42;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Color color = i % 4 == 0 ? PixelUI.White : tint;
                FireworkDot(origin, direction * speed, color, pattern == 3 ? 1.55f : 1.15f, .38f,
                    i % 3 == 0 ? 2 : 1, pattern == 3 ? 32 : 10);
                if (pattern == 2)
                    FireworkDot(origin, direction * 25, PixelUI.Gold, 1.2f, .58f, 2, 8);
                if (pattern == 0 && i % 2 == 0)
                    FireworkDot(origin, direction * (speed * .65f), PixelUI.Gold, .9f, .56f, 1, 10);
            }
            SetVerticesDirty();
        }
        void FireworkDot(Vector2 origin, Vector2 velocity, Color tint, float life, float delay, float size, float gravity)
        {
            dots[cursor] = new Dot { active = true, position = origin, velocity = velocity,
                age = -delay, life = life, size = size, gravity = gravity, floor = 10000, tint = tint };
            cursor = (cursor + 1) % dots.Length;
        }
        void Update()
        {
            float dt = Time.unscaledDeltaTime; int active = 0;
            for (int i = 0; i < dots.Length; i++)
            {
                if (!dots[i].active) continue;
                var p = dots[i]; p.age += dt;
                if (p.age < 0) { dots[i] = p; active++; continue; }
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
                var p = dots[i]; if (!p.active || p.age < 0) continue;
                float x = Mathf.Round(p.position.x), y = -Mathf.Round(p.position.y); var c = p.tint; c.a *= Mathf.Clamp01((1 - p.age / p.life) * 3);
                int v = vh.currentVertCount;
                vh.AddVert(new Vector3(x, y), c, Vector2.zero); vh.AddVert(new Vector3(x + p.size, y), c, Vector2.zero);
                vh.AddVert(new Vector3(x + p.size, y - p.size), c, Vector2.zero); vh.AddVert(new Vector3(x, y - p.size), c, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v + 2, v + 3, v);
            }
        }
    }
}
