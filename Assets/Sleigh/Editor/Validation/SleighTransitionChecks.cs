#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sleigh.Editor
{
    public static class SleighTransitionChecks
    {
        [MenuItem("Sleigh/Check Circular Transition")]
        public static void Check()
        {
            var owner = new GameObject("Transition verification", typeof(RectTransform), typeof(SleighStage));
            owner.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var maskObject = new GameObject("Iris mask", typeof(RectTransform), typeof(PixelIrisTransition));
                maskObject.transform.SetParent(owner.transform, false);
                var stage = owner.GetComponent<SleighStage>();
                var mask = stage.irisTransition = maskObject.GetComponent<PixelIrisTransition>();
                mask.rectTransform.sizeDelta = new Vector2(640, 360);
                var tick = typeof(SleighStage).GetMethod("TickIris", BindingFlags.Instance | BindingFlags.NonPublic);
                int switches = 0;
                Require(stage.Navigate(() => { Require(mask.Reveal == 0, "Switch must happen under full cover"); switches++; }), "Navigation starts");
                typeof(SleighStage).GetField("irisReduced", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(stage, false);
                Require(!stage.Navigate(() => switches += 100), "Repeated navigation is ignored");
                tick.Invoke(stage, new object[] { .1f });
                Require(switches == 0 && mask.Reveal > 0 && mask.Reveal < 1 && mask.raycastTarget, "Closing keeps old screen and blocks input");
                tick.Invoke(stage, new object[] { .5f });
                Require(switches == 0 && mask.Reveal == 0, "Fully covered frame precedes screen switch");
                tick.Invoke(stage, new object[] { .07f });
                Require(switches == 1 && mask.Reveal == 0, "Exactly one screen switch while covered");
                tick.Invoke(stage, new object[] { .65f });
                Require(stage.IsTransitioning && mask.Reveal == 0 && mask.raycastTarget,
                    "Screen construction and its first rendered frame must not consume the opening animation");
                for (int i = 0; i < 5; i++) tick.Invoke(stage, new object[] { 1f / 60 });
                Require(mask.Reveal > 0 && mask.Reveal < .15f, "Opening begins with a small central circle");
                float beforeStall = mask.Reveal;
                tick.Invoke(stage, new object[] { .65f });
                Require(mask.Reveal > beforeStall && mask.Reveal - beforeStall < .12f,
                    "A slow frame advances opening gradually instead of jumping to fully open");
                for (int i = 0; i < 5; i++) tick.Invoke(stage, new object[] { 1f / 60 });
                Require(mask.Reveal > 0 && mask.Reveal < 1 && mask.raycastTarget, "New screen opens while input stays blocked");
                for (int i = 0; i < 60; i++) tick.Invoke(stage, new object[] { 1f / 60 });
                Require(!stage.IsTransitioning && mask.Reveal == 1 && !mask.raycastTarget, "Opening releases input");

                var populate = typeof(PixelIrisTransition).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null);
                using (var vertices = new VertexHelper())
                {
                    mask.SetReveal(1); populate.Invoke(mask, new object[] { vertices });
                    Require(vertices.currentVertCount == 0, "Open mask leaves no visible overlay");
                    mask.SetReveal(0); populate.Invoke(mask, new object[] { vertices });
                    Require(vertices.currentVertCount > 4, "Closed mask includes the festive paper pattern");
                    mask.SetReveal(.5f); populate.Invoke(mask, new object[] { vertices });
                    Require(vertices.currentVertCount > 4 && vertices.currentVertCount <= 30000, "Patterned opening uses a bounded pixel mesh");
                    var triangles = new List<UIVertex>(); vertices.GetUIVertexStream(triangles);
                    Vector2 centre = mask.rectTransform.rect.center;
                    float clearRadius = (mask.rectTransform.rect.size.magnitude * .5f + 2) * .5f - 2;
                    for (int i = 0; i < triangles.Count; i += 3)
                    {
                        Vector2 sample = (triangles[i].position + triangles[i + 1].position + triangles[i + 2].position) / 3;
                        Require((sample - centre).sqrMagnitude >= clearRadius * clearRadius,
                            "Paper pattern and gold trim must stay outside the circular opening");
                    }
                    mask.SetReveal(.5f, true); populate.Invoke(mask, new object[] { vertices });
                    UIVertex vertex = default; vertices.PopulateUIVertex(ref vertex, 0);
                    Require(vertices.currentVertCount == 4 && vertex.color.a > 0 && vertex.color.a < 255, "Reduced motion uses a translucent full-screen fade");
                }
                Debug.Log("Circular transition checks passed: cover/switch/reveal ordering, duplicate input, input release, mesh endpoints and reduced motion.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
