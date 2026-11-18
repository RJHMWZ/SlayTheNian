#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Sleigh.Editor
{
    /// <summary>Deterministic visual previews. Uses isolated runs; never calls Run.Save or writes PlayerPrefs.</summary>
    [InitializeOnLoad]
    public static class NianVisualReview
    {
        static NianVisualReview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode) return;
                var root = UnityEngine.Object.FindObjectOfType<GameRoot>();
                if (root && root.IsBaking) { root.ShowTitle(); root.BindStage(root.Stage, false); }
            };
        }
        public static string Show(string screen)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Review in Edit mode to protect the live run.");
            var root = UnityEngine.Object.FindObjectOfType<GameRoot>();
            if (!root) throw new InvalidOperationException("Open the native game scene first.");
            root.BindStage(root.Stage, true); Ledger.SuppressNotes = true;
            try
            {
                root.run = Run.New("mrs", 0, 20261118); root.run.cookies = 300; root.run.night = 6;
                root.run.AddGift("midnight", true); root.run.AddRelic("nutjaw");
                if (screen == "Title") root.ShowTitle();
                else if (screen == "Map") root.ShowMap();
                else if (screen == "Battle") root.StartBattle(new[] { "general", "creeper", "ornament" }, "elite");
                else if (screen == "Boss") { root.run.night = 23; root.StartBattle(new[] { "santa" }, "boss"); }
                else if (screen == "Characters") Invoke(root, "ShowCharacters");
                else if (screen == "Event") Invoke(root, "ShowEvent", DB.Events["carolers"]);
                else if (screen == "Shop") Invoke(root, "ShowShop", true);
                else if (screen == "Rest") Invoke(root, "ShowRest");
                else if (screen == "Treasure") Invoke(root, "ShowTreasure");
                else if (screen == "Victory") Invoke(root, "ShowEnding", true, true);
                else if (screen == "Defeat") Invoke(root, "ShowEnding", false, false);
                else throw new ArgumentException(screen);
                Canvas.ForceUpdateCanvases();
                foreach (var l in root.GetComponentsInChildren<PixelLabel>(false))
                    if (System.Text.RegularExpressions.Regex.IsMatch(l.text, "圣诞|驯鹿|胡桃夹|姜饼|槲寄生|坎卜斯|克劳斯|鲁道夫|\\?{2,}"))
                        throw new Exception("Unreviewed label: " + l.text);
                return root.ScreenName;
            }
            finally { Ledger.SuppressNotes = false; }
        }
        static void Invoke(GameRoot root, string name, params object[] args)
        { typeof(GameRoot).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(root, args); }
    }
}
#endif
