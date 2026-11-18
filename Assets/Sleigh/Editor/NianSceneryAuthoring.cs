#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sleigh.Editor
{
    public static class NianSceneryAuthoring
    {
        [MenuItem("Nian/Apply Scenic Backgrounds and Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before saving the scenic layout.");
            var root = UnityEngine.Object.FindObjectOfType<GameRoot>();
            if (!root || !root.Stage) throw new InvalidOperationException("Open the existing game scene.");
            var menu = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Bg1.png");
            if (!menu) throw new InvalidOperationException("Missing the user's Assets/Bg1.png.");
            var scene = root.gameObject.scene;
            Directory.CreateDirectory("Temp/Nian/BeforeSceneryRefresh");
            EditorSceneManager.SaveScene(scene, "Temp/Nian/BeforeSceneryRefresh/BeforeScenery.unity", true);
            root.Stage.titleArtwork = menu;
            // Rebuild the existing UI views, retaining scene roots, camera, audio, materials and gameplay IDs.
            root.BindStage(root.Stage, true); root.BakeViewsInEditor();
            EditorUtility.SetDirty(root.Stage);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Sleigh/Resources/Sleigh/Prefabs/GameRoot.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Nian: Bg1 linked directly; character, map and battle scenic layouts saved. Existing materials and audio retained.");
        }
    }
}
#endif
