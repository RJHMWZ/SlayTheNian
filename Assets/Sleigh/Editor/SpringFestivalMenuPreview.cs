#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sleigh.Editor
{
    public static class SpringFestivalMenuPreview
    {
        [MenuItem("Sleigh/Preview Spring Festival Menu")]
        public static void Preview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before updating the menu preview.");
            var root = UnityEngine.Object.FindObjectOfType<GameRoot>();
            if (!root || !root.Stage) throw new InvalidOperationException("Open the SleighTheSpire scene first.");
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Spring festival main menu");
            DB.Load();
            try { root.BindStage(root.Stage, true); root.ShowTitle(); }
            finally { root.BindStage(root.Stage, false); }
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Spring festival menu preview ready. Only the Title view was rebuilt; save the scene to keep the preview.");
        }
    }
}
#endif
