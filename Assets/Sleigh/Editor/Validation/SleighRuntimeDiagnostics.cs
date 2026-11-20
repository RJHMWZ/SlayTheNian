#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Sleigh.Editor
{
    [InitializeOnLoad]
    public static class SleighRuntimeDiagnostics
    {
        static string Request => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ProjectOrganizationAudit/runtime-diagnostics.request"));
        static SleighRuntimeDiagnostics() { if (File.Exists(Request)) EditorApplication.delayCall += Capture; }
        [MenuItem("Sleigh/Write Runtime Diagnostics")]
        public static void Capture()
        {
            var text = new StringBuilder();
            text.AppendLine("playing=" + Application.isPlaying + ", paused=" + EditorApplication.isPaused);
            text.AppendLine("scene=" + SceneManager.GetActiveScene().path);
            var scene = SceneManager.GetActiveScene();
            text.AppendLine("sceneDirty=" + scene.isDirty + ", roots=" + scene.rootCount);
            foreach (var obj in scene.GetRootGameObjects())
                text.AppendLine("sceneRoot=" + obj.name + ", active=" + obj.activeSelf + ", missingScripts=" + GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj) + ", components=" + string.Join(",", obj.GetComponents<Component>().Select(c => c ? c.GetType().FullName : "MISSING")));
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Sleigh/Scripts/Core/GameRoot.cs");
            text.AppendLine("GameRoot script=" + (script ? script.GetClass()?.AssemblyQualifiedName : "MISSING"));
            text.AppendLine("singleton=" + (bool)GameRoot.Instance + ", eventSystem=" + (bool)EventSystem.current);
            foreach (var root in Resources.FindObjectsOfTypeAll<GameRoot>().Where(r => !EditorUtility.IsPersistent(r)))
            {
                text.AppendLine("root=" + root.name + ", enabled=" + root.enabled + ", screen=" + root.ScreenName + ", stage=" + (bool)root.Stage);
                var area = root.GetType().GetField("area", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
                text.AppendLine("boundArea=" + (area != null));
                foreach (var hot in root.GetComponentsInChildren<Hotspot>().Where(h => h.gameObject.activeInHierarchy).Take(12))
                    text.AppendLine("hotspot=" + hot.transform.parent.name + ", enabled=" + hot.enabled + ", action=" + (hot.click != null));
            }
            string output = Path.ChangeExtension(Request, ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, text.ToString());
            if (File.Exists(Request)) File.Delete(Request);
            Debug.Log("Runtime diagnostics written: " + output);
        }
    }
}
#endif
