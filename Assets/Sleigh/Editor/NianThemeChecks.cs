#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace Sleigh.Editor
{
    public static class NianThemeChecks
    {
        static int checks;
        static void Require(bool ok, string message) { checks++; if (!ok) throw new Exception("Nian: " + message); }
        static void Text(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Require(!Regex.IsMatch(text, @"\?{2,}"), "encoding damage: " + text);
            Require(!Regex.IsMatch(text, "圣诞|驯鹿|胡桃夹|姜饼|槲寄生|坎卜斯|克劳斯|鲁道夫|Santa|Christmas|Krampus", RegexOptions.IgnoreCase), "retired theme: " + text);
            foreach (char c in text.Where(c => !char.IsControl(c))) Require(ChinesePixelFont.HasGlyph(c), "missing glyph: " + c);
        }
        [MenuItem("Nian/Verify Theme and Compatibility")]
        public static void Verify()
        {
            checks = 0; DB.Load();
            foreach (var g in DB.All.gifts)
            {
                Text(g.name); Text(g.desc);
                var card = new Gift { Id = g.id };
                Require(!Regex.IsMatch(card.Description(), @"\{\w+\}"), "unresolved base values: " + g.id);
                card.Upgrade = true; Require(!Regex.IsMatch(card.Description(), @"\{\w+\}"), "unresolved upgrade values: " + g.id);
                Require(DB.Sprite("Gift", g.type == "friend" ? g.enemy : g.id), "card art: " + g.id);
            }
            foreach (var items in new[] { DB.All.relics, DB.All.potions, DB.All.parts })
                foreach (var item in items) { Text(item.name); Text(item.desc); }
            foreach (var e in DB.All.enemies)
            { Text(e.name); Text(e.lore); foreach (string line in e.lines) Text(line); foreach (var m in e.moves) Text(m.name); }
            foreach (var e in DB.All.events)
            { Text(e.title); Text(e.text); Require(DB.Sprite("EventArt", e.id), "event art: " + e.id); foreach (var c in e.choices) Text(c.label); }
            foreach (string character in new[] { "mrs", "tinsel", "rudolph" })
            {
                Text(DB.CharacterName(character)); Text(DB.PowerName(character)); Text(DB.PowerDesc(character));
                // Exercise serialization in memory; do not overwrite the player's real PlayerPrefs save.
                var run = Run.New(character, 10, 19880216); run.AddFriend("gingerling");
                run.AddGift("midnight", true, "battery"); run.AddPotion("elixir"); run.charmed.Add("snowman");
                run.map[0].chosen = "battle"; run.map[0].done = true; run.night = 1;
                string encoded = JsonUtility.ToJson(run); var restored = JsonUtility.FromJson<Run>(encoded);
                Require(JsonUtility.ToJson(restored) == encoded, "save roundtrip: " + character);
                Require(restored.bag.Last().Def.id == "midnight" && restored.bag.Last().Upgrade && restored.bag.Last().ToyPart == "battery", "legacy card keys");
                for (int night = 0; night < 24; night++)
                { Require(restored.map[night].options.Length > 0, "map reachability"); Text(NianTheme.Chapter(night)); }
            }
            var bank = Resources.Load<SleighAudioBank>("Sleigh/Audio/AudioBank");
            Require(bank && bank.version == 3 && bank.music.Length == 5, "new audio bank");
            foreach (var song in bank.music) Require(song.intro && song.loop && song.intro.samples > 0 && song.loop.samples > 0, "music: " + song.id);
            foreach (var effect in bank.effects) foreach (var clip in effect.clips) Require(clip && clip.samples > 0, "effect: " + effect.id);
            foreach (var f in DB.Art.Values.SelectMany(x => x)) Require(DB.Sprite(f.type, f.anim, f.frame), "sprite: " + f.path);
            Require(Shader.Find("Sleigh/UI Pixel Flash") && Shader.Find("Sleigh/UI Pixel Flash").isSupported, "actor shader");
            Directory.CreateDirectory("Artifacts/Nian");
            File.WriteAllText("Artifacts/Nian/unity-theme-checks.txt", "PASS: " + checks + " assertions. Content/glyphs/art/audio, legacy keys, in-memory save roundtrip and 24-node map.\n" + DateTime.UtcNow.ToString("u"));
            Debug.Log("Nian: " + checks + " theme and save compatibility assertions passed.");
        }
    }
}
#endif
