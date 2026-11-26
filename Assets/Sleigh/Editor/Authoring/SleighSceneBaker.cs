#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Sleigh.PixelUI;

namespace Sleigh.Editor
{
    public static class SleighSceneBaker
    {
        public const int Version = 9;
        const string Prefabs = "Assets/Sleigh/Resources/Sleigh/Prefabs";
        const string Materials = "Assets/Sleigh/Materials";
        static RectTransform Raw(Transform parent, string name, float w = 640, float h = 360)
        {
            var o = new GameObject(name, typeof(RectTransform)); var r = o.GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.sizeDelta = new Vector2(w, h); return r;
        }
        [MenuItem("Sleigh/Bake Scene and Presentation")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before baking scene assets.");
            DB.Load(); var audioBank = SleighSoundBaker.Bake();
            Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Materials);
            string scenePath = SleighProjectSetup.ScenePath;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                if (scene.isDirty) throw new InvalidOperationException("The current unrelated scene has unsaved changes. Bake in SleighTheSpire scene.");
                scene = File.Exists(scenePath) ? EditorSceneManager.OpenScene(scenePath) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
            foreach (var previous in scene.GetRootGameObjects().Where(o => o.GetComponent<GameRoot>()).ToArray()) Undo.DestroyObjectImmediate(previous);
            var rootObject = new GameObject("GameRoot"); Undo.RegisterCreatedObjectUndo(rootObject, "Bake Sleigh presentation");
            var root = rootObject.AddComponent<GameRoot>(); var stage = rootObject.AddComponent<SleighStage>();
            var cameraObject = new GameObject("Pixel Camera", typeof(Camera), typeof(AudioListener), typeof(SleighAudioCompressor)); cameraObject.transform.SetParent(rootObject.transform, false); cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 180; camera.transform.position = new Vector3(320, -180, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Hex(0xa91b29); stage.pixelCamera = camera;
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); eventObject.transform.SetParent(rootObject.transform, false); eventObject.GetComponent<EventSystem>().pixelDragThreshold = 7;
            var canvasObject = new GameObject("Sleigh Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); canvasObject.transform.SetParent(rootObject.transform, false);
            stage.canvas = canvasObject.GetComponent<Canvas>(); stage.canvas.renderMode = RenderMode.ScreenSpaceOverlay; stage.canvas.pixelPerfect = true; stage.canvas.sortingOrder = 100;
            var black = Rect(canvasObject.transform, 0, 0, 1, 1, Hex(0xa91b29)); black.name = "Full screen background"; black.rectTransform.anchorMin = Vector2.zero; black.rectTransform.anchorMax = Vector2.one; black.rectTransform.offsetMin = black.rectTransform.offsetMax = Vector2.zero;
            stage.viewport = Raw(canvasObject.transform, "Viewport 640 x 360"); stage.viewport.anchorMin = stage.viewport.anchorMax = new Vector2(.5f, .5f); stage.viewport.pivot = new Vector2(.5f, .5f); stage.viewport.localScale = Vector3.one * 3;
            stage.backgroundLayer = Raw(stage.viewport, "BG"); stage.worldLayer = Raw(stage.viewport, "World"); stage.uiLayer = Raw(stage.viewport, "UI"); stage.effectLayer = Raw(stage.viewport, "FX"); stage.popupLayer = Raw(stage.viewport, "Pop");
            stage.worldLayer.pivot = new Vector2(.5f, .5f); stage.worldLayer.anchoredPosition = new Vector2(320, -180);
            string[] viewNames = { "Title", "CharacterSelect", "Intro", "Map", "Combat", "Reward", "LeaveGift", "Event", "Shop", "Rest", "Treasure", "Victory", "Defeat" };
            stage.views = viewNames.Select(n => new SleighStage.View { name = n, background = Raw(stage.backgroundLayer, n), world = Raw(stage.worldLayer, n), ui = Raw(stage.uiLayer, n) }).ToArray();
            stage.snowRoot = Raw(stage.backgroundLayer, "Snow (reused by all views)"); stage.snowParticles = new RectTransform[90];
            var random = new System.Random(931);
            for (int i = 0; i < stage.snowParticles.Length; i++) stage.snowParticles[i] = Rect(stage.snowRoot, random.Next(640), random.Next(360), i % 5 == 0 ? 2 : 1, i % 5 == 0 ? 2 : 1, Hex(0xf4f8ff, .25f + i % 4 * .15f)).rectTransform;
            var particleObject = Raw(stage.effectLayer, "Pooled Pixel Particles (768)"); stage.particles = particleObject.gameObject.AddComponent<PixelParticleField>(); stage.particles.raycastTarget = false;
            var battleEffects = Raw(stage.effectLayer, "Pooled Festival Battle Effects (32)");
            stage.battleEffects = battleEffects.gameObject.AddComponent<FestivalBattleEffects>(); stage.battleEffects.raycastTarget = false;
            stage.flashOverlay = Rect(stage.effectLayer, 0, 0, 640, 360, Color.clear); stage.flashOverlay.name = "Impact flash";
            stage.effectPool = Raw(stage.effectLayer, "Reusable transient visuals"); stage.popupPool = Raw(stage.popupLayer, "Reusable dialogs");
            stage.transitionOverlay = Rect(stage.popupLayer, 0, 0, 640, 360, Color.clear); stage.transitionOverlay.name = "Scene fade";
            BuildPresentationWidgets(stage);
            var irisObject = Raw(stage.popupLayer, "Circular iris transition");
            stage.irisTransition = irisObject.gameObject.AddComponent<PixelIrisTransition>();
            stage.irisTransition.color = Hex(0xb51e2d); stage.irisTransition.raycastTarget = false;
            var soundObject = new GameObject("Audio"); soundObject.transform.SetParent(rootObject.transform, false); var sound = soundObject.AddComponent<SleighAudio>(); stage.audioPlayer = sound; sound.bank = audioBank;
            sound.musicSources = new AudioSource[4]; sound.effectVoices = new AudioSource[20];
            for (int i = 0; i < 24; i++)
            {
                var go = new GameObject(i < 4 ? new[] { "Music A Intro", "Music A Loop", "Music B Intro", "Music B Loop" }[i] : "Effect Voice " + (i - 4)); go.transform.SetParent(soundObject.transform, false);
                var source = go.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.dopplerLevel = 0; source.volume = 0;
                if (i < 4) sound.musicSources[i] = source; else sound.effectVoices[i - 4] = source;
            }
            var shader = Shader.Find("Sleigh/UI Pixel Flash"); if (!shader) throw new InvalidOperationException("Pixel flash shader was not imported.");
            stage.actorFlashMaterials = new Material[16];
            for (int i = 0; i < stage.actorFlashMaterials.Length; i++)
            {
                string path = Materials + "/ActorFlash_" + i + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); } else material.shader = shader;
                material.SetFloat("_Flash", 0); stage.actorFlashMaterials[i] = material;
            }
            BuildReusablePrefabs();
            root.BindStage(stage, true); root.BakeViewsInEditor(); stage.bakeVersion = Version;
            foreach (var label in root.GetComponentsInChildren<PixelLabel>(true)) if (!label.GetComponent<CanvasGroup>()) label.gameObject.AddComponent<CanvasGroup>();
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) if (!rect.GetComponent<SleighMotion>()) { var motion = rect.gameObject.AddComponent<SleighMotion>(); motion.enabled = false; }
            PrefabUtility.SaveAsPrefabAsset(rootObject, Prefabs + "/GameRoot.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080; PlayerSettings.resizableWindow = true; PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground = true;
            if (!Application.isBatchMode) SleighDisplaySetup.FullHD();
            AssetDatabase.SaveAssets();
        }
        static void BuildReusablePrefabs()
        {
            var card = new GameObject("GiftView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); var r = card.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(66, 88); r.pivot = new Vector2(0, 1);
            var image = card.GetComponent<Image>(); image.sprite = DB.Sprite("CardFrame", "atk"); image.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(card, Prefabs + "/GiftView.prefab"); UnityEngine.Object.DestroyImmediate(card);
            var actor = new GameObject("ActorView", typeof(RectTransform)); actor.GetComponent<RectTransform>().sizeDelta = Vector2.one;
            Sprite(actor.transform, "Enemy", "gingerling", 0, 0); Sprite(actor.transform, "HeartEye", "Animation 1", -8, -60); Sprite(actor.transform, "HeartEye", "Animation 1", 8, -60);
            PrefabUtility.SaveAsPrefabAsset(actor, Prefabs + "/ActorView.prefab"); UnityEngine.Object.DestroyImmediate(actor);
        }
        static void BuildPresentationWidgets(SleighStage s)
        {
            s.arrowRoot = Raw(s.effectLayer, "Targeting curve"); s.arrowDots = new Image[14];
            for (int i = 0; i < 14; i++) s.arrowDots[i] = Sprite(s.arrowRoot, "Icon", i == 13 ? "arrow" : "node", 0, 0);
            s.reactionIcon = Sprite(s.effectLayer, "Icon", "heart", 0, 0, 2);
            s.bannerRoot = PanelBox(s.effectLayer, 100, 100, 440, 66, Gold); s.bannerRoot.name = "Turn and victory banner";
            s.bannerTitle = Label(s.bannerRoot, 8, 10, 424, "YOUR TURN", 1.6f, Gold, 1); s.bannerSubtitle = Label(s.bannerRoot, 8, 38, 424, "", .85f, White, 1);
            s.bannerRoot.gameObject.AddComponent<CanvasGroup>();
            s.bubbleRoot = PanelBox(s.effectLayer, 250, 60, 270, 62, White); s.bubbleRoot.name = "Dialogue bubble";
            s.bubbleText = Label(s.bubbleRoot, 10, 8, 250, "", .85f, White, 0, true); s.bubbleRoot.gameObject.AddComponent<CanvasGroup>();
            s.lowHealthRoot = Raw(s.effectLayer, "Low HP vignette"); s.lowHealthRoot.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < 6; i++) { float alpha = .22f - i * .035f; Rect(s.lowHealthRoot, 0, i * 3, 640, 3, Hex(0xe0443e, alpha)); Rect(s.lowHealthRoot, 0, 357 - i * 3, 640, 3, Hex(0xe0443e, alpha)); Rect(s.lowHealthRoot, i * 3, 0, 3, 360, Hex(0xe0443e, alpha)); Rect(s.lowHealthRoot, 637 - i * 3, 0, 3, 360, Hex(0xe0443e, alpha)); }
            s.bannerRoot.gameObject.SetActive(false); s.bubbleRoot.gameObject.SetActive(false); s.arrowRoot.gameObject.SetActive(false); s.reactionIcon.gameObject.SetActive(false); s.lowHealthRoot.gameObject.SetActive(false);
            s.transitionOverlay.transform.SetAsLastSibling();
        }
    }
}
#endif
