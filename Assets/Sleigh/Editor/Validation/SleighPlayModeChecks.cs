#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sleigh.Editor
{
    [InitializeOnLoad]
    public static class SleighPlayModeChecks
    {
        const string Active = "Sleigh.Smoke.Active", Step = "Sleigh.Smoke.Step", Stamp = "Sleigh.Smoke.Stamp";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static bool finishing;
        static SleighPlayModeChecks()
        { if (SessionState.GetBool(Active, false)) Register(); }
        static void Register()
        {
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
            EditorApplication.playModeStateChanged -= Mode; EditorApplication.playModeStateChanged += Mode;
        }
        public static void StartBatch()
        {
            if (!Application.isBatchMode || !Application.dataPath.Contains("ProjectOrganizationAudit/UnityValidation"))
                throw new InvalidOperationException("Play smoke tests run only in the isolated validation project.");
            PlayerSettings.companyName = "CodexValidation"; PlayerSettings.productName = "SlayTheNianPlaySmoke";
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SleighProjectSetup.ScenePath);
            SessionState.SetBool(Active, true); SessionState.SetBool("Sleigh.Smoke.Reloaded", false); SessionState.SetInt(Step, 0);
            SessionState.SetFloat(Stamp, (float)EditorApplication.timeSinceStartup);
            Register(); EditorApplication.EnterPlaymode();
        }
        static void Log(string message, string stack, LogType type)
        { if (!finishing && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Finish(false, message + "\n" + stack); }
        static void Mode(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active, false))
            { SessionState.SetBool(Active, false); EditorApplication.delayCall += () => EditorApplication.Exit(SessionState.GetBool("Sleigh.Smoke.Passed", false) ? 0 : 1); }
        }
        static void Advance(string message)
        {
            Debug.Log("PLAY_SMOKE: " + message);
            SessionState.SetInt(Step, SessionState.GetInt(Step, 0) + 1);
            SessionState.SetFloat(Stamp, (float)EditorApplication.timeSinceStartup);
        }
        static void Call(GameRoot root, string name, params object[] args)
        { root.GetType().GetMethod(name, Private).Invoke(root, args); }
        static Hotspot Button(GameRoot root, string caption)
        {
            var label = root.Stage.canvas.GetComponentsInChildren<PixelLabel>().FirstOrDefault(l => l.text == Chinese.Text(caption));
            if (!label) throw new Exception("Missing active button: " + caption);
            var button = label.transform.parent.GetComponentInChildren<Hotspot>();
            if (!button || button.click == null || !button.enabledAction) throw new Exception("Unbound button: " + caption);
            return button;
        }
        static void Click(Hotspot button)
        {
            Canvas.ForceUpdateCanvases();
            if (!EventSystem.current) throw new Exception("No active EventSystem");
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Debug.Log("PLAY_SMOKE raycast " + button.transform.parent.name + ": " + string.Join(", ", hits.Take(5).Select(h => h.gameObject.name)));
            if (hits.Count == 0 || hits[0].gameObject.GetComponent<Hotspot>() != button)
                throw new Exception("Button blocked by " + (hits.Count == 0 ? "empty raycast" : hits[0].gameObject.name));
            button.OnPointerClick(data);
        }
        static void Tick()
        {
            if (finishing || !SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(Stamp, 0);
                int step = SessionState.GetInt(Step, 0); var root = GameRoot.Instance;
                if (elapsed > 35) throw new Exception("Timeout step " + step + ", root=" + root + ", screen=" + root?.ScreenName + ", enabled=" + root?.enabled);
                if (elapsed < 1 || !root || root.Stage.IsTransitioning) return;
                switch (step)
                {
                    case 0:
                        if (root.ScreenName != "Title") return;
                        if (!SessionState.GetBool("Sleigh.Smoke.Reloaded", false))
                        {
                            SessionState.SetBool("Sleigh.Smoke.Reloaded", true);
                            SessionState.SetFloat(Stamp, (float)EditorApplication.timeSinceStartup);
                            Debug.Log("PLAY_SMOKE: requesting script reload while the menu is open");
                            EditorUtility.RequestScriptReload(); return;
                        }
                        if (Application.companyName != "CodexValidation" || Application.productName != "SlayTheNianPlaySmoke") throw new Exception("Unexpected PlayerPrefs scope");
                        for (int slot = 0; slot < Run.SaveSlotCount; slot++) Run.DeleteSave(slot);
                        var startLabel = root.Stage.canvas.GetComponentsInChildren<PixelLabel>().FirstOrDefault(l => l.text == Chinese.Text("NEW RUN"));
                        var startButton = startLabel ? startLabel.transform.parent.GetComponentInChildren<Hotspot>() : null;
                        // OnEnable recovery finishes on the next player frame after domain reload.
                        if (!startButton || startButton.click == null) return;
                        Debug.Log("PLAY_SMOKE runtime root enabled=" + root.enabled + ", stage=" + root.Stage.enabled);
                        Click(Button(root, "NEW RUN")); Advance("title click"); break;
                    case 1:
                        if (root.ScreenName != "CharacterSelect") return;
                        Click(Button(root, "BEGIN >")); Advance("character begin click"); break;
                    case 2:
                        Click(Button(root, "保存并开始")); Advance("save slot click"); break;
                    case 3:
                        if (root.ScreenName != "Intro") return;
                        var skip = (Action)root.GetType().GetField("skipPoem", Private).GetValue(root);
                        if (skip == null) return; skip(); Advance("intro skip"); break;
                    case 4:
                        if (root.ScreenName != "Map") return;
                        Call(root, "EnterNode", "battle"); Advance("first map battle"); break;
                    case 5:
                        if (root.ScreenName != "Combat" || (bool)root.GetType().GetField("busy", Private).GetValue(root)) return;
                        Click(Button(root, "END TURN")); Advance("end turn click"); break;
                    case 6:
                        if (root.battle.Turn < 2 || (bool)root.GetType().GetField("busy", Private).GetValue(root)) return;
                        root.StartBattle(new[] { "santa" }, "boss"); Advance("boss battle"); break;
                    case 7:
                        if (root.ScreenName == "Combat" && root.battle.Kind == "boss") Finish(true, "Title, raycasts, character choice, save panel, intro, map, ordinary battle, turn and boss startup passed.");
                        break;
                }
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }
        static void Finish(bool passed, string message)
        {
            if (finishing) return; finishing = true;
            SessionState.SetBool("Sleigh.Smoke.Passed", passed);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PlaySmokeResult.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, (passed ? "PASS: " : "FAIL: ") + message);
            Debug.Log((passed ? "PLAY_SMOKE_PASS: " : "PLAY_SMOKE_FAIL: ") + message);
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
