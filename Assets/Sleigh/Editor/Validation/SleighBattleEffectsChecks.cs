#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sleigh.Editor
{
    public static class SleighBattleEffectsChecks
    {
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Battle effects: " + message); }
        [MenuItem("Sleigh/Verify Battle Effects")]
        public static void Verify()
        {
            var owner = new GameObject("Battle effect verification", typeof(RectTransform));
            owner.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var field = owner.AddComponent<FestivalBattleEffects>(); field.raycastTarget = false;
                var populate = typeof(FestivalBattleEffects).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);
                var random = UnityEngine.Random.state;
                foreach (FestivalBattleEffects.Style style in Enum.GetValues(typeof(FestivalBattleEffects.Style)))
                {
                    foreach (bool flight in new[] { false, true })
                    {
                        field.ClearEffects(); field.Play(style, new Vector2(150, 180), new Vector2(450, 180), flight);
                        field.Advance(.06f);
                        using (var vertices = new VertexHelper())
                        {
                            populate.Invoke(field, new object[] { vertices });
                            var mesh = new Mesh(); vertices.FillMesh(mesh);
                            try
                            {
                                Require(mesh.vertexCount > 0 && mesh.vertexCount < 5000, style + " visible bounded geometry");
                                Require(mesh.vertices.All(v => v.x >= 4 && v.x <= 636 && v.y >= -239 && v.y <= -48), style + " keeps HUD and hand clear");
                            }
                            finally { UnityEngine.Object.DestroyImmediate(mesh); }
                        }
                        field.Advance(1); Require(field.ActiveCount == 0, style + " expires fully after a slow frame");
                    }
                }
                Require(random.Equals(UnityEngine.Random.state), "effects cannot advance the random generator");
                for (int i = 0; i < 120; i++) field.Play(FestivalBattleEffects.Style.Slash, Vector2.zero, new Vector2(450, 180));
                Require(field.ActiveCount == 32, "rapid multi-hit effects stay in the fixed pool");
                using (var vertices = new VertexHelper())
                { populate.Invoke(field, new object[] { vertices }); Require(vertices.currentVertCount < 65000, "worst-case pool stays below 16-bit vertex budget"); }
                field.reduced = true; field.Advance(.01f); Require(field.ActiveCount == 0, "reduced motion clears existing effects");
                field.Play(FestivalBattleEffects.Style.Firecracker, Vector2.zero, Vector2.one);
                Require(field.ActiveCount == 0, "reduced motion blocks new effects");
                field.reduced = false; field.Play(FestivalBattleEffects.Style.Heal, Vector2.zero, Vector2.one);
                owner.SetActive(false); Require(field.ActiveCount == 0, "disabled fields release all effect state");
                Require(FestivalBattleEffects.ForGift("sock", "atk") != FestivalBattleEffects.ForGift("icicle", "atk")
                    && FestivalBattleEffects.ForGift("mitten", "skl") != FestivalBattleEffects.ForGift("fireplace", "skl"), "different card families have distinct feedback");
                Debug.Log("BATTLE_EFFECTS_CHECKS_PASS: all effect styles, geometry bounds, fixed pool, lifetime, random isolation and reduced motion.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
#endif
