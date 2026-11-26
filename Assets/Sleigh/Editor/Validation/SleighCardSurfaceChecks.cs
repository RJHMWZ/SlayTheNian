#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sleigh.Editor
{
    public static class SleighCardSurfaceChecks
    {
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Card surface: " + message); }

        [MenuItem("Sleigh/Verify Card Surfaces and Render Previews")]
        public static void Verify()
        {
            DB.Load();
            var scene = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("Card surface canvas", typeof(RectTransform), typeof(Canvas));
            var cameraObject = new GameObject("Card surface camera", typeof(Camera));
            var rootObject = new GameObject("Card factory", typeof(GameRoot));
            SceneManager.MoveGameObjectToScene(owner, scene);
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            var camera = cameraObject.GetComponent<Camera>(); camera.scene = scene;
            var canvas = owner.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var rect = (RectTransform)owner.transform; rect.sizeDelta = new Vector2(420, 280); rect.pivot = new Vector2(0, 1);
            camera.orthographic = true; camera.orthographicSize = 140; camera.aspect = 1.5f;
            camera.transform.position = new Vector3(210, -140, -10);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .04f, .09f, 1);
            var target = new RenderTexture(840, 560, 24); camera.targetTexture = target;
            Material preview = null;
            try
            {
                var factory = typeof(GameRoot).GetMethod("Card", BindingFlags.Instance | BindingFlags.NonPublic);
                var card = (RectTransform)factory.Invoke(rootObject.GetComponent<GameRoot>(),
                    new object[] { owner.transform, new Gift { Id = "sock", Uid = 1 }, 144f, 52f, 2f, null });
                var effect = card.gameObject.AddComponent<CardSurfaceEffects>(); effect.Configure(0, true);
                var source = effect.SurfaceMaterial;
                Require(source && source.shader.isSupported, "material is included and shader is supported");
                Require(!ShaderUtil.GetShaderMessages(source.shader).Any(m => m.severity.ToString() == "Error"), "shader compiles");
                Require((canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) != 0, "UV1 canvas channel");
                Require(card.GetComponentsInChildren<PixelLabel>().All(l => l.GetComponent<CardSurfaceMesh>()), "all text participates");
                preview = new Material(source); preview.SetFloat("_PreviewTime", 0);
                var graphics = card.GetComponentsInChildren<Graphic>();
                foreach (var graphic in graphics) graphic.material = preview;
                string output = Path.GetFullPath("Logs/CardSurfacePreviews"); Directory.CreateDirectory(output);
                card.gameObject.SetActive(false); var empty = Capture(camera, target, null);
                card.gameObject.SetActive(true);
                var states = new[] { 0f, .25f, .5f, .75f, 1f }; int previous = int.MaxValue;
                foreach (float state in states)
                {
                    effect.SetDissolve(state);
                    foreach (var graphic in graphics) graphic.material = preview;
                    var pixels = Capture(camera, target, Path.Combine(output, "Dissolve-" + Mathf.RoundToInt(state * 100) + ".png"));
                    int visible = Difference(pixels, empty);
                    Require(visible <= previous, "dissolve coverage decreases at " + state);
                    if (state == 0) Require(visible > 25000, "whole layered card is rendered");
                    if (state == .5f) Require(visible > 1000 && visible < previous, "partial noisy dissolve remains visible");
                    if (state == 1) Require(visible == 0, "frame, art, icons and text disappear completely");
                    previous = visible;
                }
                effect.Configure(0, false);
                foreach (var graphic in graphics) graphic.material = preview;
                preview.SetFloat("_PreviewTime", 4.8f * (.13f + 1 - effect.SweepPhase));
                var shine = Capture(camera, target, Path.Combine(output, "Shine.png"));
                preview.SetFloat("_PreviewTime", 4.8f * (.6f + 1 - effect.SweepPhase));
                var quiet = Capture(camera, target, Path.Combine(output, "Shine-Rest.png"));
                Require(Difference(shine, quiet) > 1000, "diagonal shine has a visible pass and a quiet interval");
                preview.SetFloat("_PreviewTime", 4.8f * (1.13f + 1 - effect.SweepPhase));
                Require(Difference(shine, Capture(camera, target, null)) < 100, "shine repeats after one period");
                effect.Configure(0, true);
                foreach (var graphic in graphics) graphic.material = preview;
                preview.SetFloat("_PreviewTime", 0); var reduced = Capture(camera, target, null);
                preview.SetFloat("_PreviewTime", 1);
                Require(Difference(reduced, Capture(camera, target, null)) == 0, "reduced motion disables periodic shine");
                Require(source.GetFloat("_PreviewTime") == -1, "shared material remains unchanged");
                effect.SetDissolve(1); card.gameObject.SetActive(false); card.gameObject.SetActive(true); effect.Configure(2, false);
                Require(effect.Dissolve == 0 && effect.SweepPhase >= 0, "pooled card starts intact");
                Require(graphics.All(g => g.material == source), "pooled layers restore and rebind the immutable material");
                Debug.Log("CARD_SURFACE_CHECKS_PASS: GPU coverage, layered text, periodic shine, reduced motion, pooling and immutable material. Previews: " + output);
            }
            finally
            {
                camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
                if (preview) UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        static Color32[] Capture(Camera camera, RenderTexture target, string path)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                if (path != null) File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture.GetPixels32();
            }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
        }

        static int Difference(Color32[] a, Color32[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++)
                if (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) > 12) count++;
            return count;
        }
    }
}
#endif
