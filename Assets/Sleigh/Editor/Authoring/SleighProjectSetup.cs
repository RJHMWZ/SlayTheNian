#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sleigh.Editor
{
    public class SleighAssetImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/Sleigh/Resources/Sleigh/Fonts/", StringComparison.Ordinal))
            {
                var font = (TextureImporter)assetImporter; font.textureType = TextureImporterType.Default; font.filterMode = FilterMode.Point;
                font.mipmapEnabled = false; font.textureCompression = TextureImporterCompression.Uncompressed; font.npotScale = TextureImporterNPOTScale.None;
                font.maxTextureSize = 2048; font.wrapMode = TextureWrapMode.Clamp; return;
            }
            if (!assetPath.StartsWith("Assets/Sleigh/Resources/Sleigh/Art/", StringComparison.Ordinal)) return;
            var t = (TextureImporter)assetImporter;
            // Generated atlases use explicit UV rectangles, not a single imported sprite.
            t.textureType = assetPath.EndsWith("Atlas.png", StringComparison.Ordinal) ? TextureImporterType.Default : TextureImporterType.Sprite;
            if (t.textureType == TextureImporterType.Sprite) t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = 1; t.filterMode = FilterMode.Point; t.mipmapEnabled = false;
            t.textureCompression = TextureImporterCompression.Uncompressed; t.npotScale = TextureImporterNPOTScale.None;
            t.alphaIsTransparency = true; t.maxTextureSize = 4096;
        }
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Sleigh/Audio/SFX/Generated/", StringComparison.Ordinal)) return;
            var a = (AudioImporter)assetImporter; var settings = a.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true; a.defaultSampleSettings = settings;
        }
    }
    [InitializeOnLoad]
    public static class SleighProjectSetup
    {
        public const string ScenePath = "Assets/Sleigh/Scenes/SleighTheSpire.unity";
        static SleighProjectSetup() { EditorApplication.delayCall += PrepareIfNeeded; }
        static void PrepareIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || AssetDatabase.IsAssetImportWorkerProcess()) return;
            if (!File.Exists(ScenePath) && File.Exists("Assets/Sleigh/Resources/Sleigh/Definitions.json")) Prepare();
        }
        [MenuItem("Sleigh/Prepare Native Scene and Prefabs")]
        public static void Prepare() => SleighSceneBaker.Bake();
        [MenuItem("Sleigh/Open Game Scene")]
        public static void OpenScene()
        { if (!File.Exists(ScenePath)) Prepare(); if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }
    }
}
#endif
