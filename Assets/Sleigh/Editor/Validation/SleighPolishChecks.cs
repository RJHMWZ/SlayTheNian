#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sleigh.Editor
{
    public static class SleighPolishChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException("Presentation regression: " + message); }

        [MenuItem("Sleigh/Verify Interaction and Presentation")]
        public static void Verify()
        {
            var owner = new GameObject("Presentation regression fixture", typeof(RectTransform));
            owner.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var hot = owner.AddComponent<Hotspot>(); bool held = false; int clicks = 0;
                hot.press = value => held = value; hot.click = () => clicks++;
                var first = new PointerEventData(null) { pointerId = 1, button = PointerEventData.InputButton.Left };
                var second = new PointerEventData(null) { pointerId = 2, button = PointerEventData.InputButton.Left };
                hot.OnPointerDown(first); Require(held, "press feedback is immediate");
                hot.OnPointerUp(second); Require(held, "another finger cannot release the active press");
                // Ordinary MonoBehaviour lifecycle callbacks are not dispatched in Edit Mode.
                typeof(Hotspot).GetMethod("OnDisable", Private).Invoke(hot, null);
                Require(!held, "disabled controls release their visual state");
                hot.OnPointerDown(first); hot.OnBeginDrag(first); hot.OnPointerUp(first); hot.OnPointerClick(first);
                Require(clicks == 0, "drag release must not also click");
                hot.OnPointerDown(first); hot.OnPointerUp(first); hot.OnPointerClick(first);
                Require(clicks == 1 && !held, "a fresh touch clicks once after a drag");
                hot.enabledAction = false; hot.OnPointerEnter(first); hot.OnPointerDown(first); hot.OnPointerClick(first);
                Require(!held && !hot.hovered && clicks == 1, "disabled controls have no hover, press or click");

                var particles = owner.AddComponent<PixelParticleField>();
                particles.Burst(Vector2.zero, 2000, Color.white);
                Require(particles.ActiveCount == 768, "particle bursts stay within the fixed pool");
                particles.reduced = true;
                typeof(PixelParticleField).GetMethod("Update", Private).Invoke(particles, null);
                Require(particles.ActiveCount == 0, "reduced motion clears particles already in flight");
                particles.Burst(Vector2.zero, 10, Color.white);
                Require(particles.ActiveCount == 0, "reduced motion suppresses new particles");

                var rect = (RectTransform)owner.transform; rect.anchoredPosition = new Vector2(12, 24);
                var motion = owner.AddComponent<SleighMotion>(); motion.Configure(SleighMotion.Style.Float);
                rect.anchoredPosition += Vector2.up * 3; motion.reduceMotion = true;
                typeof(SleighMotion).GetMethod("LateUpdate", Private).Invoke(motion, null);
                Require(rect.anchoredPosition == new Vector2(12, 24), "reduced motion restores the resting position");
                Debug.Log("POLISH_REGRESSION_PASS: press lifecycle, touch ownership, drag/click separation, bounded particles and reduced-motion restoration.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        // Runs only inside the existing isolated Play Mode smoke test, before ending turn one.
        public static void VerifyCombatInput(GameRoot root)
        {
            var hand = (RectTransform)typeof(GameRoot).GetField("handRoot", Private).GetValue(root);
            var playable = new List<Hotspot>();
            foreach (var gift in root.battle.Hand.Where(root.battle.Playable))
            {
                var card = hand.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Gift " + gift.Uid + ":"));
                if (card) playable.Add(card.GetComponentInChildren<Hotspot>());
            }
            Require(playable.Count >= 2, "smoke test requires two playable cards");
            foreach (var button in playable)
            {
                var illustration = button.transform.parent.Find("Festival card illustration");
                Require(illustration && illustration.GetComponent<Image>().sprite && !illustration.GetComponent<Image>().raycastTarget,
                    "Spring Festival card artwork is bound and never blocks card input");
            }
            var point = RectTransformUtility.WorldToScreenPoint(null, playable[0].transform.position);
            var first = new PointerEventData(EventSystem.current) { pointerId = 1, position = point };
            var second = new PointerEventData(EventSystem.current) { pointerId = 2, position = point + Vector2.right * 60 };
            var ghostField = typeof(GameRoot).GetField("dragGhost", Private);
            playable[0].beginDrag(first);
            var ghost = (RectTransform)ghostField.GetValue(root);
            Require(ghost && !ghost.GetComponent<CanvasGroup>().blocksRaycasts, "drag preview never intercepts a drop target");
            Vector2 original = ghost.anchoredPosition;
            playable[1].beginDrag(second); playable[1].drag(second); playable[1].endDrag(second);
            Require((RectTransform)ghostField.GetValue(root) == ghost && ghost.anchoredPosition == original,
                "another finger cannot move or discard an active drag");
            typeof(GameRoot).GetMethod("OnApplicationFocus", Private).Invoke(root, new object[] { false });
            Require(!(RectTransform)ghostField.GetValue(root) && typeof(GameRoot).GetField("selectedGift", Private).GetValue(root) == null,
                "losing focus clears drag and targeting state");

            var targetMethod = typeof(GameRoot).GetMethod("TargetAt", Private);
            var targetAt = (Func<Vector2, Unit>)Delegate.CreateDelegate(typeof(Func<Vector2, Unit>), root, targetMethod);
            var actors = (System.Collections.IDictionary)typeof(GameRoot).GetField("actors", Private).GetValue(root);
            for (int x = 0; x < 640; x += 16)
                for (int y = 80; y < 260; y += 20)
                {
                    var pointInGame = new Vector2(x, y);
                    var expected = root.battle.Foes().Where(u => !u.Pending).LastOrDefault(u =>
                    {
                        var actor = actors[u]; if (actor == null) return false;
                        var type = actor.GetType(); float actorX = (float)type.GetField("x").GetValue(actor);
                        float width = (float)type.GetProperty("Width").GetValue(actor), height = (float)type.GetProperty("Height").GetValue(actor);
                        return Mathf.Abs(x - actorX) < Mathf.Max(32, width / 2) && y >= 220 - height && y <= 253;
                    });
                    Require(targetAt(pointInGame) == expected, "optimized target selection preserves the original hit regions and priority");
                }
            targetAt(new Vector2(450, 180));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) targetAt(new Vector2(450, 180));
            Require(GC.GetAllocatedBytesForCurrentThread() == before, "target lookup allocates no managed memory");
            Debug.Log("POLISH_COMBAT_INPUT_PASS: multi-touch drag isolation, focus cleanup, equivalent hit regions and zero-allocation target lookup.");
        }
    }
}
#endif
