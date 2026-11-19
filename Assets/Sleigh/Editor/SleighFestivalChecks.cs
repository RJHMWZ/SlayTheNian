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
using static Sleigh.PixelUI;

namespace Sleigh.Editor
{
    public static class SleighFestivalChecks
    {
        static int count;
        static string PreviewDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/FestivalPreviews"));
        static void Require(bool value, string message) { count++; if (!value) throw new Exception("Festival presentation: " + message); }

        static Mesh MeshFor(SpringFestivalBackdrop backdrop)
        {
            using (var vertices = new VertexHelper())
            {
                typeof(SpringFestivalBackdrop).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null).Invoke(backdrop, new object[] { vertices });
                var mesh = new Mesh(); vertices.FillMesh(mesh); return mesh;
            }
        }

        [MenuItem("Sleigh/Verify Festival Presentation")]
        public static void Verify()
        {
            DB.Load(); count = 0;
            var owner = new GameObject("Festival presentation verification", typeof(RectTransform));
            owner.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (SpringFestivalBackdrop.Setting setting in Enum.GetValues(typeof(SpringFestivalBackdrop.Setting)))
                {
                    Clear(owner.transform);
                    var backdrop = SpringFestivalBackdrop.Build(owner.transform, setting, true);
                    backdrop.FreezePreview(1.7f);
                    var mesh = MeshFor(backdrop);
                    try
                    {
                        Require(mesh.vertexCount > 500 && mesh.vertexCount < 60000, setting + " mesh budget");
                        Require(mesh.vertices.All(v => v.x >= 0 && v.x <= 640 && v.y >= -360 && v.y <= 0), setting + " canvas bounds");
                        Require(mesh.colors.Count(c => c.r > c.b) > mesh.vertexCount * .65f, setting + " warm palette");
                        Require(!backdrop.raycastTarget, setting + " must not block gameplay input");
                        backdrop.FreezePreview(4, true); var stillA = MeshFor(backdrop);
                        backdrop.FreezePreview(30, true); var stillB = MeshFor(backdrop);
                        try { Require(stillA.vertices.SequenceEqual(stillB.vertices) && stillA.colors32.SequenceEqual(stillB.colors32), setting + " reduced motion"); }
                        finally { UnityEngine.Object.DestroyImmediate(stillA); UnityEngine.Object.DestroyImmediate(stillB); }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                }
                foreach (string character in new[] { "mrs", "tinsel", "rudolph" })
                {
                    Clear(owner.transform);
                    var story = FestivalStoryView.Build(owner.transform, character, false, () => { }, () => { });
                    var pages = GameRoot.FestivalOpening(character);
                    Require(pages.Length == 3, character + " opening has three scenes");
                    for (int page = 0; page < pages.Length; page++)
                    {
                        story.SetPage(pages[page], page, pages.Length);
                        Require(pages[page].Length == 4, character + " four visible lines per scene");
                        foreach (var line in story.lines)
                        {
                            Require(PixelLabel.Measure(line.text, line.fontScale) <= line.rectTransform.sizeDelta.x, character + " text fits panel");
                            Require(line.text.All(c => char.IsWhiteSpace(c) || ChinesePixelFont.HasGlyph(c)), character + " story glyphs");
                        }
                    }
                    Require(pages[1].Any(s => s.Contains(DB.CharacterName(character))), character + " keeps original protagonist");
                }
                Debug.Log("Spring Festival presentation: " + count + " assertions passed, including all backgrounds, mesh budgets, input transparency, reduced motion and story layouts.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [MenuItem("Sleigh/Render Festival Previews")]
        public static void RenderPreviews()
        {
            DB.Load(); Directory.CreateDirectory(PreviewDirectory);
            var scene = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("Festival preview canvas", typeof(RectTransform), typeof(Canvas));
            var cameraObject = new GameObject("Festival preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(owner, scene); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>(); camera.scene = scene;
            var canvas = owner.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var rect = (RectTransform)owner.transform; rect.sizeDelta = new Vector2(640, 360); rect.pivot = new Vector2(0, 1);
            camera.orthographic = true; camera.orthographicSize = 180; camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(320, -180, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Hex(0x291728);
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
            try
            {
                for (int page = 0; page < 3; page++)
                {
                    Clear(owner.transform);
                    SpringFestivalBackdrop.Build(owner.transform, story: true).FreezePreview(1.7f + page * .35f);
                    var view = FestivalStoryView.Build(owner.transform, "mrs", false, () => { }, () => { });
                    view.SetPage(GameRoot.FestivalOpening("mrs")[page], page, 3);
                    Capture(camera, target, "Intro-" + (page + 1));
                }
                foreach (SpringFestivalBackdrop.Setting setting in Enum.GetValues(typeof(SpringFestivalBackdrop.Setting)))
                {
                    Clear(owner.transform);
                    SpringFestivalBackdrop.Build(owner.transform, setting).FreezePreview(2);
                    Capture(camera, target, setting.ToString());
                }
                Clear(owner.transform);
                SpringFestivalBackdrop.Build(owner.transform).FreezePreview(1.7f);
                SpringFestivalProp.Home(owner.transform, 210, 290, 1.4f, true);
                SpringFestivalProp.BlessingBag(owner.transform, 420, 170);
                Capture(camera, target, "Props");
                Debug.Log("Festival previews rendered to " + PreviewDirectory);
            }
            finally
            {
                camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static void Capture(Camera camera, RenderTexture target, string name)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                texture.ReadPixels(new UnityEngine.Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(PreviewDirectory, name + ".png"), texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
