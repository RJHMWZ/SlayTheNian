#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sleigh.Editor
{
    public static class SleighSoundBaker
    {
        const string Folder = "Assets/Sleigh/Resources/Sleigh/Audio";
        const string EffectsFolder = "Assets/Sleigh/Audio/SFX/Generated";
        const int Rate = 48000;
        const int Version = 3;
        public static SleighAudioBank Bake(bool force = false)
        {
            var bank = AssetDatabase.LoadAssetAtPath<SleighAudioBank>(Folder + "/AudioBank.asset");
            if (bank && bank.version == Version && !force) { BindProvidedMusic(bank); BindProvidedEffects(bank); return bank; }
            Directory.CreateDirectory(EffectsFolder);
            string[] names = { "hover", "cardHover", "click", "toggle", "tick", "count", "pick", "error", "draw", "shuffle", "play", "unwrap", "hit", "crit", "whiff", "blockHit", "block", "buff", "debuff", "heal", "coin", "jingle", "bell", "death", "win", "lose", "turn", "enemyTurn", "windup", "whoosh", "spirit", "transform", "potion", "relic", "type", "line", "pop", "energy", "thud", "fire", "heartbeat", "holdTick", "page", "give", "love", "hate", "charm", "clink", "grab", "spot", "warn", "blessing", "zodiac", "firecracker" };
            var definitions = new List<SleighAudioBank.Effect>();
            foreach (string name in names)
            {
                int variants = name == "play" ? 11 : name == "cardHover" ? 7 : name == "hit" ? 4 : name == "toggle" ? 2
                    : new[] { "hover", "click", "tick", "count", "pick", "draw", "unwrap", "whiff", "blockHit", "block", "jingle", "bell", "death", "windup", "whoosh", "potion", "type", "line", "pop", "fire", "holdTick", "page", "give", "clink", "grab" }.Contains(name) ? 3 : 1;
                if (name == "win" || name == "lose") variants = 1;
                for (int i = 0; i < variants; i++)
                {
                    int parameter = name == "hit" ? new[] { 1, 6, 15, 25 }[i] : name == "bell" ? new[] { 79, 84, 96 }[i] : i;
                    if (name != "win" && name != "lose")
                        WriteWav(EffectsFolder + "/" + name + "_" + i + ".wav", FestivalSoundSynth.Render(name, parameter, i), 1, Rate);
                }
                float gate = name == "hover" ? .045f : name == "cardHover" ? .04f : name == "error" ? .12f : name == "draw" || name == "tick" ? .035f
                    : name == "count" ? .055f : name == "hit" ? .025f : name == "type" ? .045f : name == "fire" ? .09f : name == "holdTick" ? .06f : name == "click" || name == "blockHit" ? .03f
                    : name == "blessing" || name == "zodiac" || name == "firecracker" ? .12f : 0;
                definitions.Add(new SleighAudioBank.Effect { id = name, clips = new AudioClip[variants], gateSeconds = gate });
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var definition in definitions)
                for (int i = 0; i < definition.clips.Length; i++) definition.clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(EffectsFolder + "/" + definition.id + "_" + i + ".wav");
            if (!bank) { bank = ScriptableObject.CreateInstance<SleighAudioBank>(); AssetDatabase.CreateAsset(bank, Folder + "/AudioBank.asset"); }
            bank.effects = definitions.ToArray(); bank.music = bank.music ?? Array.Empty<SleighAudioBank.Song>(); bank.version = Version; BindProvidedMusic(bank); BindProvidedEffects(bank); EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets();
            Debug.Log("Sleigh audio baked: " + bank.effects.Length + " sound definitions, " + bank.effects.Sum(e => e.clips.Length) + " variations, " + bank.music.Length + " music tracks using provided audio files. No runtime synthesis.");
            return bank;
        }
        static void BindProvidedEffects(SleighAudioBank bank)
        {
            bool changed = false;
            foreach (var pair in new[] { new[] { "win", "Assets/Sleigh/Audio/SFX/Victory.mp3" }, new[] { "lose", "Assets/Sleigh/Audio/SFX/Defeat.mp3" } })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(pair[1]);
                if (!clip) throw new InvalidOperationException("Missing provided effect: " + pair[1]);
                var effect = bank.effects.First(e => e.id == pair[0]);
                if (effect.clips.Length == 1 && effect.clips[0] == clip) continue;
                effect.clips = new[] { clip }; changed = true;
            }
            if (changed) { EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets(); }
        }
        static void BindProvidedMusic(SleighAudioBank bank)
        {
            bool changed = BindSong(bank, "menu", "Assets/Sleigh/Audio/BGM/MainMenu.mp3");
            changed |= BindSong(bank, "battle", "Assets/Sleigh/Audio/BGM/NormalBattle.wav");
            changed |= BindSong(bank, "boss", "Assets/Sleigh/Audio/BGM/BossBattle.wav");
            changed |= BindSong(bank, "calm", "Assets/Sleigh/Audio/BGM/Exploration.wav");
            changed |= BindSong(bank, "end", "Assets/Sleigh/Audio/BGM/Ending.wav");
            if (changed) { EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets(); }
        }
        static bool BindSong(SleighAudioBank bank, string id, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) throw new InvalidOperationException("Missing provided music: " + path);
            var entry = bank.music.FirstOrDefault(track => track.id == id);
            if (entry != null && entry.loop == clip && !entry.intro && entry.originalLoopSamples44100 == 0) return false;
            if (entry == null)
            {
                entry = new SleighAudioBank.Song { id = id };
                bank.music = bank.music.Concat(new[] { entry }).ToArray();
            }
            entry.intro = null; entry.loop = clip; entry.originalLoopSamples44100 = 0;
            return true;
        }
        public static void WriteWav(string path, float[] samples, int channels, int rate)
        {
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)channels); writer.Write(rate); writer.Write(rate * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
                foreach (float value in samples) writer.Write((short)Math.Round(Math.Max(-1, Math.Min(1, value)) * 32767));
            }
        }
    }


}
#endif
