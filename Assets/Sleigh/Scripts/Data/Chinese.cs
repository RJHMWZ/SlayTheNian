using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sleigh
{
    public static class Chinese
    {
        [Serializable] public sealed class Entry { public string source, text; }
        [Serializable] public sealed class Catalog { public Entry[] entries; }
        static Dictionary<string, string> entries, cache;
        static Regex phrases;
        static readonly HashSet<string> untranslated = new HashSet<string>();
        public static IEnumerable<string> Untranslated => untranslated;
        public static IEnumerable<string> Translations { get { Load(); return entries.Values; } }
        static void Load()
        {
            if (entries != null) return;
            var asset = Resources.Load<TextAsset>("Sleigh/Localization/zh-CN");
            if (!asset) throw new InvalidOperationException("Missing Simplified Chinese localization catalog.");
            entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in JsonUtility.FromJson<Catalog>(asset.text).entries) entries[e.source] = e.text;
            cache = new Dictionary<string, string>(StringComparer.Ordinal);
            phrases = new Regex(string.Join("|", entries.Keys.OrderByDescending(k => k.Length).Select(k =>
                (IsLatin(k[0]) ? "(?<![A-Za-z])" : "") + Regex.Escape(k) + (IsLatin(k[k.Length-1]) ? "(?![A-Za-z])" : ""))), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        static bool IsLatin(char c) => c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z';
        public static string Text(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            Load();
            if (entries.TryGetValue(value, out var direct)) return direct;
            if (cache.TryGetValue(value, out var found)) return found;
            bool ascii = false;
            foreach (char c in value) if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z') { ascii = true; break; }
            if (!ascii) return value;
            string translated = phrases.Replace(value, m => entries[m.Value]);
            if (Regex.IsMatch(Regex.Replace(translated, @"\{[A-Za-z0-9]+\}", ""), "[A-Za-z]{2,}")) untranslated.Add(translated);
            // Cache formatted UI text; cap it so long runs cannot retain unbounded combat counters.
            if (cache.Count > 4096) cache.Clear();
            cache[value] = translated; return translated;
        }
        public static void Localize(DefinitionBundle data)
        {
            foreach (var g in data.gifts) { g.name = Text(g.name); g.desc = Text(g.desc); }
            foreach (var list in new[] { data.relics, data.potions, data.parts }) foreach (var d in list) { d.name = Text(d.name); d.desc = Text(d.desc); }
            foreach (var e in data.enemies)
            { e.name = Text(e.name); e.lore = Text(e.lore); for (int i = 0; i < e.lines.Length; i++) e.lines[i] = Text(e.lines[i]); foreach (var m in e.moves) m.name = Text(m.name); }
            foreach (var e in data.events)
            { e.title = Text(e.title); e.text = Text(e.text); foreach (var c in e.choices) c.label = Text(c.label); }
        }
    }
}
