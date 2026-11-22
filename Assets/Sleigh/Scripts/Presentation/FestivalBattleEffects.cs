using UnityEngine;
using UnityEngine.UI;

namespace Sleigh
{
    // Fixed-size, unscaled-time effects. One canvas mesh, no textures, materials or per-effect objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FestivalBattleEffects : MaskableGraphic
    {
        public enum Style { Firecracker, Slash, Pierce, Frost, Gong, Sweep, Guard, Heal, Blessing, Seal, Summon }
        struct Effect
        {
            public bool active, travelling;
            public Style style;
            public Vector2 from, to;
            public float age, life, strength;
            public int variant;
        }
        readonly Effect[] effects = new Effect[32];
        int cursor, sequence;
        public int ActiveCount { get; private set; }
        public bool reduced;
        public static Style ForGift(string id, string type)
        {
            switch (id)
            {
                case "snowball": case "icicle": case "flurry": case "frostbreath": case "snowfall": return Style.Frost;
                case "bellslam": case "offkey": case "jingle": return Style.Gong;
                case "canejab": case "birch": return Style.Pierce;
                case "sledsweep": case "stampede": case "tinselwhip": return Style.Sweep;
                case "chimney": case "midnight": case "nutchomp": return Style.Slash;
                case "mitten": case "scarf": case "snowfort": case "bigbow": case "doublewrap": case "wrapspree": case "tinselarmor": case "globe": return Style.Guard;
                case "fireplace": case "milkcookies": case "kiss": case "cocoasip": return Style.Heal;
                case "checktwice": return Style.Seal;
                case "starfall": return Style.Blessing;
                default: return type == "friend" ? Style.Summon : type == "atk" ? Style.Firecracker : Style.Blessing;
            }
        }
        public static Color Ink(Style style)
        {
            switch (style)
            {
                case Style.Frost: return PixelUI.Hex(0x93e4e0);
                case Style.Guard: return PixelUI.Hex(0xffd26a);
                case Style.Heal: return PixelUI.Hex(0xa8e6a0);
                case Style.Seal: return PixelUI.Hex(0xdd8cc4);
                case Style.Summon: return PixelUI.Hex(0xffa088);
                case Style.Slash: case Style.Pierce: return PixelUI.Hex(0xffe6aa);
                default: return PixelUI.Hex(0xffa34e);
            }
        }
        public void Play(Style style, Vector2 from, Vector2 to, bool travelling = false, float strength = 1)
        {
            if (reduced) return;
            if (!effects[cursor].active) ActiveCount++;
            effects[cursor] = new Effect { active = true, style = style, from = from, to = to, travelling = travelling,
                life = travelling ? .12f : style == Style.Guard || style == Style.Heal || style == Style.Summon ? .52f : .32f,
                strength = Mathf.Clamp(strength, .75f, 1.4f), variant = sequence++ };
            cursor = (cursor + 1) % effects.Length; SetVerticesDirty();
        }
        public void ClearEffects()
        {
            for (int i = 0; i < effects.Length; i++) effects[i].active = false;
            ActiveCount = 0; SetVerticesDirty();
        }
        public void Advance(float dt)
        {
            if (ActiveCount == 0) return;
            if (reduced) { ClearEffects(); return; }
            int active = 0;
            for (int i = 0; i < effects.Length; i++)
            {
                if (!effects[i].active) continue;
                effects[i].age += Mathf.Max(0, dt);
                if (effects[i].age >= effects[i].life) effects[i].active = false;
                else active++;
            }
            ActiveCount = active; SetVerticesDirty();
        }
        void Update() => Advance(Time.unscaledDeltaTime);
        protected override void OnDisable() { ClearEffects(); base.OnDisable(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < effects.Length; i++)
            {
                var e = effects[i]; if (!e.active) continue;
                float t = e.age / e.life;
                var ink = Ink(e.style); ink.a = Mathf.Min(1, (1 - t) * 3);
                if (e.travelling) Flight(vh, e, t, ink);
                else Impact(vh, e, t, ink);
            }
        }
        static void Box(VertexHelper vh, float x, float y, float w, float h, Color c)
        {
            // Keep effects off the top HUD and the hand / battle controls.
            float left = Mathf.Clamp(Mathf.Round(x), 4, 636), right = Mathf.Clamp(Mathf.Round(x + w), 4, 636);
            float top = Mathf.Clamp(Mathf.Round(y), 48, 239), bottom = Mathf.Clamp(Mathf.Round(y + h), 48, 239);
            if (right <= left || bottom <= top || c.a <= 0) return;
            int v = vh.currentVertCount;
            vh.AddVert(new Vector3(left, -top), c, Vector2.zero); vh.AddVert(new Vector3(right, -top), c, Vector2.zero);
            vh.AddVert(new Vector3(right, -bottom), c, Vector2.zero); vh.AddVert(new Vector3(left, -bottom), c, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v + 2, v + 3, v);
        }
        static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(a, b) / 3), 1, 32);
            for (int i = 0; i <= n; i++) { var p = Vector2.Lerp(a, b, i / (float)n); Box(vh, p.x - width / 2, p.y - width / 2, width, width, c); }
        }
        static void Ring(VertexHelper vh, Vector2 p, float radius, Color c, float squash = 1)
        {
            if (radius < 2) return;
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI / 16;
                Box(vh, p.x + Mathf.Cos(a) * radius - 1, p.y + Mathf.Sin(a) * radius * squash - 1, 3, 3, c);
            }
        }
        static void Star(VertexHelper vh, Vector2 p, float size, Color c)
        { Box(vh, p.x - 1, p.y - size, 3, size * 2 + 1, c); Box(vh, p.x - size, p.y - 1, size * 2 + 1, 3, c); Box(vh, p.x - 2, p.y - 2, 5, 5, PixelUI.White); }
        static Vector2 OnArc(Effect e, float t)
        {
            var p = Vector2.Lerp(e.from, e.to, t);
            p.y -= Mathf.Sin(t * Mathf.PI) * (e.style == Style.Pierce ? 0 : 20);
            return p;
        }
        static void Flight(VertexHelper vh, Effect e, float t, Color ink)
        {
            for (int i = 5; i >= 0; i--)
            {
                float q = Mathf.Max(0, t - i * .035f); var p = OnArc(e, q); var tail = ink; tail.a *= 1 - i / 7f;
                Box(vh, p.x - 2, p.y - 2, 5 - i * .45f, 5 - i * .45f, tail);
            }
            var head = OnArc(e, t);
            if (e.style == Style.Firecracker)
            {
                Box(vh, head.x - 4, head.y - 6, 8, 12, PixelUI.Red);
                Box(vh, head.x - 4, head.y - 5, 8, 2, PixelUI.Gold);
                Star(vh, head + new Vector2(1, -9), 3, ink);
            }
            else if (e.style == Style.Frost) Star(vh, head, 6, ink);
            else Stroke(vh, head - (e.to - e.from).normalized * 14, head + (e.to - e.from).normalized * 6, 3, ink);
        }
        static void Impact(VertexHelper vh, Effect e, float t, Color ink)
        {
            var p = e.to; float size = e.strength; float open = 1 - Mathf.Pow(1 - t, 3);
            if (e.style == Style.Guard)
            {
                float r = (24 + open * 8) * size; var glow = ink; glow.a *= .45f;
                Ring(vh, p, r, glow); Ring(vh, p, r - 5, ink);
                var paper = PixelUI.Red; paper.a = ink.a;
                for (int y = -12; y <= 12; y += 3) Box(vh, p.x - (12 - Mathf.Abs(y)), p.y + y - 1, (12 - Mathf.Abs(y)) * 2 + 3, 3, paper);
                Box(vh, p.x - 5, p.y - 6, 3, 13, ink); Box(vh, p.x - 6, p.y - 3, 12, 3, ink);
                Box(vh, p.x + 3, p.y - 7, 3, 15, ink); Box(vh, p.x, p.y + 4, 8, 3, ink);
                return;
            }
            if (e.style == Style.Heal || e.style == Style.Blessing || e.style == Style.Summon)
            {
                Ring(vh, new Vector2(p.x, 222), 16 + open * 22, ink, .24f);
                for (int i = 0; i < 6; i++)
                {
                    float phase = Mathf.Repeat(t + i * .17f, 1); float x = p.x + Mathf.Sin(i * 2.4f) * 23;
                    var c = ink; c.a *= Mathf.Sin(phase * Mathf.PI);
                    Star(vh, new Vector2(x, 220 - phase * 62), i % 2 == 0 ? 4 : 2, c);
                }
                if (e.style == Style.Summon) Ring(vh, p, 16 + open * 28, ink);
                return;
            }
            if (e.style == Style.Seal)
            {
                float y = p.y - 17 * (1 - open); var red = PixelUI.Red; red.a = ink.a;
                Box(vh, p.x - 9, y - 16, 18, 32, red);
                Box(vh, p.x - 6, y - 13, 12, 2, ink); Box(vh, p.x - 1, y - 9, 3, 22, ink);
                for (int i = 0; i < 3; i++) Box(vh, p.x - 5, y - 7 + i * 7, 11, 2, ink);
                Ring(vh, p, 18 + open * 17, ink); return;
            }
            if (e.style == Style.Gong)
            {
                for (int i = 0; i < 3; i++) Ring(vh, p, (8 + open * 36 - i * 9) * size, ink, .8f);
                if (t < .3f) Star(vh, p, 12 * size, ink);
                return;
            }
            if (e.style == Style.Firecracker)
            {
                var paper = PixelUI.Red; paper.a = ink.a;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4 + e.variant * .31f;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var start = p + dir * (5 + open * 11) * size;
                    var end = p + dir * (12 + open * 31) * size;
                    Stroke(vh, start, end, 4, paper);
                    Stroke(vh, start, end, 2, ink);
                    Box(vh, end.x - 2, end.y - 2, i % 2 == 0 ? 5 : 3, i % 2 == 0 ? 3 : 5, paper);
                }
                if (t < .55f) Star(vh, p, (14 - t * 20) * size, ink);
                return;
            }
            if (e.style == Style.Slash || e.style == Style.Pierce || e.style == Style.Sweep)
            {
                float sign = e.variant % 2 == 0 ? 1 : -1;
                Vector2 a = e.style == Style.Pierce ? new Vector2(-28, 4) : new Vector2(-24, 22 * sign);
                Vector2 b = e.style == Style.Pierce ? new Vector2(28, -4) : new Vector2(24, -22 * sign);
                if (e.style == Style.Sweep) { a = new Vector2(-43, 10); b = new Vector2(43, -10); }
                var end = Vector2.Lerp(a, b, Mathf.Min(1, open * 1.6f));
                Stroke(vh, p + a * size, p + end * size, 6 * (1 - t) + 1, ink);
                Stroke(vh, p + a * size, p + end * size, 2, new Color(1, .98f, .86f, ink.a));
            }
            else Ring(vh, p, (7 + open * 25) * size, ink);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4 + e.variant * .7f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float distance = (8 + open * 25) * size;
                if (e.style == Style.Frost) Stroke(vh, p + dir * 5, p + dir * distance, 2, ink);
                else Box(vh, p.x + dir.x * distance, p.y + dir.y * distance, 3, i % 2 == 0 ? 5 : 3, i % 2 == 0 ? ink : new Color(1, .9f, .6f, ink.a));
            }
            if (t < .24f) Star(vh, p, (10 - t * 22) * size, ink);
        }
    }
}
