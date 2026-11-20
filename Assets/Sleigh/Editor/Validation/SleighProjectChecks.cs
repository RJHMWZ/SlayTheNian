#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sleigh.Editor
{
    public static class SleighProjectChecks
    {
        [MenuItem("Sleigh/Verify Project Integrity")]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before verifying scene assets.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save scene changes before verifying scene assets, or run in an isolated batch project.");
            var previousActive = SceneManager.GetActiveScene();
            var opened = new List<Scene>();
            var buildScenes = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
            int objects = 0, scenes = 0, prefabs = 0;
            try
            {
                foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    string absoluteScene = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
                    if (!File.Exists(absoluteScene)) throw new InvalidOperationException("Missing scene: " + path);
                    var scene = SceneManager.GetSceneByPath(path);
                    if (!scene.isLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(scene); }
                    foreach (var root in scene.GetRootGameObjects()) objects += VerifyHierarchy(root);
                    if (buildScenes.Contains(path))
                    {
                        var stage = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SleighStage>(true)).Single();
                        Require(stage.canvas && stage.audioPlayer && stage.viewport, "baked stage bindings");
                        Require(stage.views.All(v => v.background && v.world && v.ui), "all baked screen layers");
                        Require(stage.audioPlayer.musicSources.Length == 4 && stage.audioPlayer.musicSources.All(s => s), "prebuilt music channels");
                    }
                    scenes++;
                }
                foreach (string key in new[] { "Sleigh/Prefabs/GameRoot", "Sleigh/Prefabs/ActorView", "Sleigh/Prefabs/GiftView" })
                {
                    var prefab = Resources.Load<GameObject>(key);
                    Require(prefab, "prefab resource " + key);
                    objects += VerifyHierarchy(prefab); prefabs++;
                }
                var bank = Resources.Load<SleighAudioBank>("Sleigh/Audio/AudioBank");
                Require(bank && bank.version == 3, "audio bank version");
                Require(bank.effects.All(e => e.clips.Length > 0 && e.clips.All(c => c)), "all sound effect references");
                Require(bank.music.All(s => s.loop), "all music references");
                Require(buildScenes.All(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path)), "all enabled build scenes exist");
                Debug.Log($"Project integrity passed: {scenes} scene(s), {buildScenes.Count} enabled build scene(s), {prefabs} resource prefabs, {objects} objects, audio bindings and no missing scripts/references.");
            }
            finally
            {
                if (!previousActive.IsValid() || !previousActive.isLoaded)
                    previousActive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(previousActive);
                foreach (var scene in opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static int VerifyHierarchy(GameObject root)
        {
            int count = 0;
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                count++;
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "missing script on " + transform.name);
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (!component) continue;
                    using (var serialized = new SerializedObject(component))
                    {
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                throw new InvalidOperationException("Missing reference: " + transform.name + "/" + property.propertyPath);
                    }
                }
            }
            return count;
        }

        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Project integrity: " + message); }

        static void VerifyRuntimeMaterialOwnership()
        {
            var owner = new GameObject("Material ownership verification");
            owner.hideFlags = HideFlags.HideAndDontSave;
            var source = new Material(Shader.Find("Sleigh/UI Pixel Flash"));
            try
            {
                source.SetFloat("_Flash", .25f);
                var root = owner.AddComponent<GameRoot>();
                var method = typeof(GameRoot).GetMethod("ActorMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
                var owned = (Material)method.Invoke(root, new object[] { source });
                Require(owned != source, "runtime material is an instance");
                Require((Material)method.Invoke(root, new object[] { source }) == owned, "material instance reused");
                Require((Material)method.Invoke(root, new object[] { owned }) == owned, "pooled material does not get cloned repeatedly");
                owned.SetFloat("_Flash", .75f);
                Require(source.GetFloat("_Flash") == .25f, "runtime flash does not change the asset");
                // Ordinary MonoBehaviour lifecycle callbacks do not run in Edit Mode.
                typeof(GameRoot).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, null);
                Require(!owned, "owned runtime material released on root destruction");
                UnityEngine.Object.DestroyImmediate(owner);
                Debug.Log("Runtime material ownership checks passed: reuse, source isolation and destruction cleanup.");
            }
            finally
            {
                if (owner) UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        // Run against a copied workspace to keep active scenes and PlayerPrefs undisturbed.
        public static void ValidateBatch()
        {
            try
            {
                Verify();
                VerifyRuntimeMaterialOwnership();
                SleighRuleChecks.VerifyTheme();
                SleighRuleChecks.Verify();
                SleighFestivalChecks.Verify();
                SleighViewportChecks.Check();
                SleighTransitionChecks.Check();
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "ProjectValidationResult.txt"), "PASS: integrity, material ownership, rules, theme, presentation, viewport and transitions.");
                Debug.Log("PROJECT_VALIDATION_PASS");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
#endif
