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
        const int Rate = 48000;
        public static SleighAudioBank Bake(bool force = false)
        {
            var bank = AssetDatabase.LoadAssetAtPath<SleighAudioBank>(Folder + "/AudioBank.asset");
            if (bank && bank.version == 2 && !force) return bank;
            Directory.CreateDirectory(Folder + "/Effects"); Directory.CreateDirectory(Folder + "/Music");
            string[] names = { "hover", "cardHover", "click", "toggle", "tick", "count", "pick", "error", "draw", "shuffle", "play", "unwrap", "hit", "crit", "whiff", "blockHit", "block", "buff", "debuff", "heal", "coin", "jingle", "bell", "death", "win", "lose", "turn", "enemyTurn", "windup", "whoosh", "spirit", "transform", "potion", "relic", "type", "line", "pop", "energy", "thud", "fire", "heartbeat", "holdTick", "page", "give", "love", "hate", "charm", "clink", "grab", "spot", "warn" };
            var definitions = new List<SleighAudioBank.Effect>();
            foreach (string name in names)
            {
                int variants = name == "play" ? 11 : name == "cardHover" ? 7 : name == "hit" ? 4 : name == "toggle" ? 2
                    : new[] { "hover", "click", "tick", "count", "pick", "draw", "unwrap", "whiff", "blockHit", "block", "jingle", "bell", "death", "windup", "whoosh", "potion", "type", "line", "pop", "fire", "holdTick", "page", "give", "clink", "grab" }.Contains(name) ? 3 : 1;
                for (int i = 0; i < variants; i++)
                {
                    int parameter = name == "hit" ? new[] { 1, 6, 15, 25 }[i] : name == "bell" ? new[] { 79, 84, 96 }[i] : i;
                    WriteWav(Folder + "/Effects/" + name + "_" + i + ".wav", Render(name, parameter, i), 1, Rate);
                }
                float gate = name == "hover" ? .045f : name == "cardHover" ? .04f : name == "error" ? .12f : name == "draw" || name == "tick" ? .035f
                    : name == "count" ? .055f : name == "hit" ? .025f : name == "type" ? .045f : name == "fire" ? .09f : name == "holdTick" ? .06f : name == "click" || name == "blockHit" ? .03f : 0;
                definitions.Add(new SleighAudioBank.Effect { id = name, clips = new AudioClip[variants], gateSeconds = gate });
            }
            string[] musicNames = { "calm", "battle", "boss", "end" }; int[] samples44100 = { 920348, 641455, 579945, 504000 };
            var tracks = new List<SleighAudioBank.Song>();
            for (int i = 0; i < musicNames.Length; i++)
            {
                string name = musicNames[i]; var source = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/" + name + ".ogg");
                source.LoadAudioData(); var pcm = new float[source.samples * source.channels];
                if (!source.GetData(pcm, 0)) throw new InvalidOperationException("Cannot decode original music: " + name);
                int loopFrames = (int)Math.Round(samples44100[i] / 44100.0 * source.frequency), introFrames = (int)Math.Round(loopFrames * .5);
                if (introFrames + loopFrames > source.samples) throw new InvalidOperationException("Original song is shorter than its loop window: " + name);
                var intro = new float[introFrames * source.channels]; var loop = new float[loopFrames * source.channels];
                Array.Copy(pcm, intro, intro.Length); Array.Copy(pcm, intro.Length, loop, 0, loop.Length);
                WriteWav(Folder + "/Music/" + name + "_intro.wav", intro, source.channels, source.frequency);
                WriteWav(Folder + "/Music/" + name + "_loop.wav", loop, source.channels, source.frequency);
                tracks.Add(new SleighAudioBank.Song { id = name, originalLoopSamples44100 = samples44100[i] });
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var definition in definitions)
                for (int i = 0; i < definition.clips.Length; i++) definition.clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Effects/" + definition.id + "_" + i + ".wav");
            foreach (var track in tracks)
            { track.intro = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Music/" + track.id + "_intro.wav"); track.loop = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Music/" + track.id + "_loop.wav"); }
            if (!bank) { bank = ScriptableObject.CreateInstance<SleighAudioBank>(); AssetDatabase.CreateAsset(bank, Folder + "/AudioBank.asset"); }
            bank.effects = definitions.ToArray(); bank.music = tracks.ToArray(); bank.version = 2; EditorUtility.SetDirty(bank); AssetDatabase.SaveAssets();
            Debug.Log("Sleigh audio baked: " + bank.effects.Length + " original sound definitions, " + bank.effects.Sum(e => e.clips.Length) + " variations, four sample-accurate music loops. No runtime synthesis.");
            return bank;
        }
        static double Midi(int m) => 440 * Math.Pow(2, (m - 69) / 12.0);
        static double Blep(double phase, double dt)
        { if (phase < dt) { double x = phase / dt; return x + x - x * x - 1; } if (phase > 1 - dt) { double x = (phase - 1) / dt; return x * x + x + x + 1; } return 0; }
        static double Envelope(double time, double duration, double attack, double peak)
        { return time <= attack ? .0001 * Math.Exp(Math.Log(Math.Max(.0002, peak) / .0001) * time / attack)
                : Math.Max(.0002, peak) * Math.Exp(Math.Log(.0001 / Math.Max(.0002, peak)) * (time - attack) / Math.Max(.01, duration - attack)); }
        static float[] Render(string name, int value, int variation)
        {
            var random = new System.Random(14761 + variation * 271 + name.Sum(c => c));
            double R() => random.NextDouble(); double J(double v = .06) => 1 + (R() * 2 - 1) * v;
            var pcm = new double[Rate * 3]; int end = 1;
            void Tone(double hz, double dur = .1, double vol = .15, double delay = 0, string type = "square", double hz2 = 0, double attack = .004)
            {
                hz = Math.Max(20, hz); double phase = 0; int start = (int)Math.Round(delay * Rate), length = (int)Math.Ceiling((dur + .05) * Rate);
                end = Math.Max(end, start + length);
                for (int i = 0; i < length && start + i < pcm.Length; i++)
                {
                    double t = i / (double)Rate, frequency = hz2 > 0 ? hz * Math.Pow(Math.Max(20, hz2) / hz, Math.Min(1, t / dur)) : hz;
                    double dt = frequency / Rate, wave;
                    if (type == "sine") wave = Math.Sin(phase * Math.PI * 2);
                    else if (type == "triangle") { double p = (phase + .25) % 1; wave = 1 - 4 * Math.Abs(p - .5); }
                    else if (type == "sawtooth") wave = 2 * phase - 1 - Blep(phase, dt);
                    else wave = (phase < .5 ? 1 : -1) + Blep(phase, dt) - Blep((phase + .5) % 1, dt);
                    double envelope = t > dur ? .0001 * Math.Max(0, 1 - (t - dur) / .05) : Envelope(t, dur, attack, vol);
                    pcm[start + i] += wave * envelope; phase = (phase + dt) % 1;
                }
            }
            void Noise(double dur = .1, double vol = .15, double delay = 0, string type = "lowpass", double hz = 2000, double hz2 = 0, double q = 1)
            {
                int start = (int)Math.Round(delay * Rate), length = (int)Math.Ceiling((dur + .05) * Rate); end = Math.Max(end, start + length);
                double b0 = 0, b1 = 0, b2 = 0, a1 = 0, a2 = 0, z1 = 0, z2 = 0;
                for (int i = 0; i < length && start + i < pcm.Length; i++)
                {
                    double t = i / (double)Rate;
                    if (i % 16 == 0)
                    {
                        double f = hz2 > 0 ? hz * Math.Pow(hz2 / hz, Math.Min(1, t / dur)) : hz;
                        double w = 2 * Math.PI * Math.Max(20, Math.Min(Rate * .48, f)) / Rate, sin = Math.Sin(w), cos = Math.Cos(w), alpha = sin / (2 * q), a0 = 1 + alpha;
                        if (type == "highpass") { b0 = (1 + cos) * .5 / a0; b1 = -(1 + cos) / a0; b2 = b0; }
                        else if (type == "bandpass") { b0 = alpha / a0; b1 = 0; b2 = -alpha / a0; }
                        else { b0 = (1 - cos) * .5 / a0; b1 = (1 - cos) / a0; b2 = b0; }
                        a1 = -2 * cos / a0; a2 = (1 - alpha) / a0;
                    }
                    double input = R() * 2 - 1, filtered = b0 * input + z1; z1 = b1 * input - a1 * filtered + z2; z2 = b2 * input - a2 * filtered;
                    pcm[start + i] += filtered * (t > dur ? .0001 * Math.Max(0, 1 - (t - dur) / .05) : Envelope(t, dur, .003, vol));
                }
            }
            void Notes(int root, int[] intervals, double step, double duration, double volume, string waveform)
            { for (int i = 0; i < intervals.Length; i++) Tone(Midi(root + intervals[i]), duration, volume, step * i, waveform); }
            void Bell(int midi) { double f = Midi(midi); Tone(f, 1.2, .12, 0, "sine"); Tone(f * 2.76, .5, .035, 0, "sine"); Tone(f * 5.4, .25, .02, 0, "sine"); }
            void Jingle() { for (int i = 0; i < 7; i++) Noise(.06, .06, i * .05 + R() * .02, "bandpass", 7000 + R() * 3000, 0, 6); }
            switch (name)
            {
                case "hover": Tone(1500 * J(), .025, .03); break;
                case "cardHover": Tone(Midi(79 + value % 7 * 2), .04, .035, 0, "triangle"); break;
                case "click": Tone(660 * J(), .06, .1, 0, "square", 990); Tone(1320, .05, .05, .03, "triangle"); break;
                case "toggle": Tone(value > 0 ? 700 : 500, .07, .1, 0, "triangle", value > 0 ? 1100 : 350); Noise(.03, .05, 0, "highpass", 5000); break;
                case "tick": Tone(1800 * J(.02), .015, .035); break;
                case "count": Tone(1400 * J(.04), .02, .03, 0, "triangle"); break;
                case "pick": Tone(520 * J(), .07, .14, 0, "triangle", 780); Noise(.05, .05, 0, "highpass", 4000); break;
                case "error": Tone(140, .08, .12); Tone(110, .1, .12, .09); break;
                case "draw": Noise(.07, .09, 0, "bandpass", 3500 * J(.2), 6000, .8); break;
                case "shuffle": for (int i = 0; i < 6; i++) Noise(.05, .07, i * .04, "bandpass", 3000 + i * 300); break;
                case "play": double pitch = Math.Pow(2, Math.Min(value, 10) / 12.0); Noise(.12, .1, 0, "bandpass", 2000, 6000, .7); Tone(440 * pitch * J(.02), .1, .11, 0, "triangle", 880 * pitch); break;
                case "unwrap": for (int i = 0; i < 4; i++) Noise(.04, .09, i * .03, "highpass", 2500 + R() * 3000); Tone(1046, .2, .07, .08, "sine"); break;
                case "hit": double hit = Math.Min(1, value / 25.0); Noise(.09 + hit * .12, .22 + hit * .2, 0, "lowpass", 4000, 300); Tone(200 * J() - hit * 60, .1 + hit * .12, .16 + hit * .1, 0, "square", 50); if (hit > .6) Tone(60, .35, .35, 0, "sine", 30); break;
                case "crit": Tone(90, .45, .4, 0, "sine", 30); Noise(.35, .25, 0, "lowpass", 6000, 200); Tone(1600, .25, .06, 0, "triangle", 400); break;
                case "whiff": Noise(.12, .06, 0, "bandpass", 1200, 4000, 2); break;
                case "blockHit": Tone(1800 * J(), .06, .06, 0, "square", 1500); Tone(2700 * J(), .12, .05, 0, "triangle"); break;
                case "block": Tone(700, .08, .13, 0, "triangle", 900); Tone(1050, .14, .09, .05, "triangle"); break;
                case "buff": Notes(72, new[] { 0, 4, 7, 12 }, .045, .1, .09, "triangle"); break;
                case "debuff": Notes(60, new[] { 0, -3, -6 }, .06, .12, .07, "square"); break;
                case "heal": Notes(79, new[] { 0, 7, 12, 16, 19 }, .05, .25, .06, "sine"); break;
                case "coin": Tone(988, .06, .09); Tone(1319, .18, .09, .06); break;
                case "jingle": Jingle(); break;
                case "bell": Bell(value); break;
                case "death": Noise(.5, .22, 0, "lowpass", 3000, 100); Tone(400, .5, .1, 0, "sawtooth", 60); break;
                case "win": Notes(67, new[] { 0, 4, 7, 12, 16, 19, 24 }, .07, .16, .07, "square"); Jingle(); break;
                case "lose": Notes(64, new[] { 0, -2, -5, -9, -12 }, .22, .4, .11, "triangle"); break;
                case "turn": Bell(79); Tone(Midi(86), .5, .05, .1, "sine"); break;
                case "enemyTurn": Tone(Midi(50), .25, .05, 0, "sawtooth"); Tone(Midi(49), .3, .05, .15, "sawtooth"); break;
                case "windup": Tone(180, .18, .05, 0, "sawtooth", 420); break;
                case "whoosh": Noise(.3, .1, 0, "bandpass", 400, 3000, 1.2); break;
                case "spirit": Notes(76, new[] { 0, 4, 7, 11, 14 }, .07, .4, .06, "sine"); Jingle(); break;
                case "transform": Noise(1.6, .3, 0, "lowpass", 200, 2000, 3); Tone(110, 1.6, .15, 0, "sawtooth", 40); Tone(55, 1.6, .1); break;
                case "potion": for (int i = 0; i < 5; i++) Tone(300 + R() * 400, .06, .07, i * .05, "sine", 800); break;
                case "relic": Notes(72, new[] { 0, 7, 12, 16 }, .03, .6, .05, "triangle"); Jingle(); break;
                case "type": Tone(700 + R() * 300, .02, .02); break;
                case "line": Tone(Midi(84 + (int)(R() * 5)), .6, .03, 0, "sine"); break;
                case "pop": Tone(800 * J(), .06, .09, 0, "sine", 1300); break;
                case "energy": Tone(880, .12, .07, 0, "triangle", 1760); break;
                case "thud": Tone(90, .25, .28, 0, "sine", 40); Noise(.15, .12, 0, "lowpass", 600); break;
                case "fire": Noise(.08, .03, 0, "bandpass", 900 + R() * 1500, 0, 2); break;
                case "heartbeat": Tone(60, .12, .25, 0, "sine", 45); Tone(55, .14, .2, .16, "sine", 40); break;
                case "holdTick": Tone(400 + value * 300, .03, .05, 0, "triangle"); break;
                case "page": Noise(.1, .06, 0, "bandpass", 2500, 5000); break;
                case "give": Tone(660 * J(), .12, .08, 0, "sine", 1320); Tone(990, .18, .05, .06, "triangle"); break;
                case "love": Notes(81, new[] { 0, 4, 7 }, .05, .18, .07, "sine"); break;
                case "hate": Tone(220, .18, .06, 0, "sawtooth", 140); break;
                case "charm": Notes(72, new[] { 0, 4, 7, 12, 16, 19, 24 }, .06, .35, .07, "sine"); Bell(96); Jingle(); break;
                case "clink": Tone(2200 * J(.08), .12, .06, 0, "sine"); Tone(3300 * J(.08), .08, .03, .02, "sine"); break;
                case "grab": Tone(900 * J(), .05, .06, 0, "triangle", 1400); break;
                case "spot": Noise(.25, .06, 0, "bandpass", 1200, 300, 2); Tone(110, .2, .12, 0, "sine", 80); break;
                case "warn": Tone(Midi(62), .3, .08, 0, "triangle"); Tone(Midi(57), .4, .08, .18, "triangle"); break;
                default: throw new InvalidOperationException("Missing original sound: " + name);
            }
            var output = new float[Math.Min(end, pcm.Length)]; for (int i = 0; i < output.Length; i++) output[i] = (float)pcm[i]; return output;
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
