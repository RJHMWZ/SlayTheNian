#if UNITY_EDITOR
using System;

namespace Sleigh.Editor
{
    // Offline modal/percussion synthesis. The player only loads the resulting WAV assets.
    public static class FestivalSoundSynth
    {
        public const int SampleRate = 48000;
        public static float[] Render(string name, int value, int variation)
        {
            int seed = 23571 + variation * 7919;
            foreach (char c in name) seed = unchecked(seed * 31 + c);
            var random = new System.Random(seed);
            var pcm = new double[SampleRate * 4]; int end = 1;
            double Pitch(int note) => 440 * Math.Pow(2, (note - 69) / 12.0);
            double Jitter() => .96 + random.NextDouble() * .08;
            void Add(double delay, double duration, Func<double, double> sample)
            {
                int start = (int)(delay * SampleRate), length = Math.Max(2, (int)(duration * SampleRate));
                end = Math.Max(end, Math.Min(pcm.Length, start + length));
                for (int i = 0; i < length && start + i < pcm.Length; i++)
                {
                    double t = i / (double)SampleRate;
                    double edge = Math.Min(1, i / 24.0) * Math.Min(1, (length - 1 - i) / 120.0);
                    pcm[start + i] += sample(t) * edge;
                }
            }
            void Modal(double hz, double duration, double gain, double delay, bool metal)
            {
                double[] ratios = metal ? new[] { 1.0, 1.47, 2.09, 2.56, 3.58, 4.73 }
                    : new[] { 1.0, 2.76, 5.4, 8.93 };
                hz *= Jitter();
                Add(delay, duration, t =>
                {
                    double signal = 0;
                    for (int k = 0; k < ratios.Length; k++)
                    {
                        double decay = Math.Exp(-(metal ? 5 + k * 1.2 : 7 + k * 3) * t / duration);
                        double shimmer = metal ? .32 * Math.Sin(2 * Math.PI * 13 * t) * Math.Exp(-8 * t) : 0;
                        signal += Math.Sin(2 * Math.PI * hz * ratios[k] * t + shimmer) * decay / (1 + k * 1.7);
                    }
                    return gain * signal;
                });
            }
            void Wood(double hz = 1450, double gain = .2, double delay = 0, double duration = .07)
                => Modal(hz, duration, gain, delay, false);
            void Gong(double hz = 430, double gain = .18, double delay = 0, double duration = .5)
                => Modal(hz, duration, gain, delay, true);
            void Pluck(int note, double gain = .15, double delay = 0, double duration = .45)
            {
                double hz = Pitch(note) * (.998 + random.NextDouble() * .004);
                Add(delay, duration, t =>
                {
                    double signal = 0;
                    for (int k = 1; k <= 8; k++)
                        signal += Math.Sin(2 * Math.PI * hz * k * t) * Math.Exp(-(4 + k * .8) * t / duration) / (k * k);
                    return signal * gain;
                });
            }
            void Notes(int root, int[] intervals, double step = .065, double gain = .12, double delay = 0)
            { for (int i = 0; i < intervals.Length; i++) Pluck(root + intervals[i], gain, delay + step * i); }
            void Paper(double duration = .12, double gain = .15, double delay = 0, bool airy = false)
            {
                double fast = 0, slow = 0;
                Add(delay, duration, t =>
                {
                    double n = random.NextDouble() * 2 - 1;
                    fast += (n - fast) * (airy ? .25 : .65); slow += (n - slow) * .035;
                    double folds = .55 + .45 * Math.Sin(2 * Math.PI * (airy ? 5 : 37) * t);
                    return (fast - slow) * gain * Math.Sin(Math.PI * t / duration) * folds;
                });
            }
            void Drum(double gain = .3, double delay = 0, double low = 100, double duration = .3)
            {
                double noise = 0;
                Add(delay, duration, t =>
                {
                    noise += ((random.NextDouble() * 2 - 1) - noise) * .18;
                    double phase = 2 * Math.PI * (low * t + low * .9 * (1 - Math.Exp(-35 * t)) / 35);
                    return gain * ((Math.Sin(phase) + .25 * Math.Sin(phase * 1.59)) * Math.Exp(-7 * t / duration)
                        + noise * .7 * Math.Exp(-90 * t));
                });
            }
            void Crack(double gain = .2, double delay = 0)
            {
                double low = 0;
                Add(delay, .11, t =>
                {
                    double n = random.NextDouble() * 2 - 1; low += (n - low) * .12;
                    return gain * ((n - low) * Math.Exp(-105 * t) + .5 * low * Math.Exp(-45 * t));
                });
                Drum(gain * .45, delay, 170, .07);
            }
            void Crackers(int count = 5, double gain = .14, double delay = 0)
            { for (int i = 0; i < count; i++) Crack(gain * (.65 + random.NextDouble() * .35), delay + i * .042 + random.NextDouble() * .012); }
            void Roar()
            {
                double low = 0;
                Add(0, 2.1, t =>
                {
                    low += ((random.NextDouble() * 2 - 1) - low) * .06;
                    double phase = 2 * Math.PI * (64 * t - 7 * t * t) + .5 * Math.Sin(2 * Math.PI * 5 * t);
                    return .24 * (Math.Sin(phase) + .4 * Math.Sin(phase * 2) + low) * Math.Min(1, t / .12) * Math.Exp(-1.6 * t);
                });
                Drum(.32, 0, 75, .5); Drum(.2, .32, 85, .4); Gong(150, .12, .1, 1.6);
            }
            int[] pentatonic = { 0, 2, 4, 7, 9 };
            switch (name)
            {
                case "hover": Wood(2100, .075, 0, .035); break;
                case "cardHover": Pluck(74 + pentatonic[value % 5] + value / 5 * 12, .065, 0, .14); break;
                case "click": Wood(1350, .23); Drum(.065, .006, 310, .06); break;
                case "toggle": Wood(value == 0 ? 980 : 1620, .18); Wood(value == 0 ? 680 : 2100, .1, .045); break;
                case "tick": Wood(2400, .065, 0, .025); break;
                case "count": Modal(2600, .055, .07, 0, true); break;
                case "pick": Paper(.07, .12); Pluck(74 + variation * 2, .12, .015, .2); break;
                case "error": Wood(520, .17); Wood(390, .14, .09); break;
                case "draw": Paper(.105, .22); Wood(1800, .045, .01, .03); break;
                case "shuffle": for (int i = 0; i < 6; i++) Paper(.055, .14, i * .035); break;
                case "play": Paper(.12, .21); Wood(1050 + value * 85, .14); Pluck(62 + pentatonic[value % 5] + value / 5 * 12, .09, .018, .22); break;
                case "unwrap": for (int i = 0; i < 3; i++) Paper(.09, .15, i * .05); Notes(74, new[] { 0, 7 }, .08, .1, .1); break;
                case "hit": double strength = Math.Min(1, value / 25.0); Drum(.22 + strength * .15, 0, 150 - strength * 65, .18 + strength * .16); Wood(750, .08); if (strength > .6) Crack(.13, .008); break;
                case "crit": Drum(.36, 0, 72, .45); Gong(320, .13, .012, .38); Crackers(3, .16, .02); break;
                case "whiff": Paper(.15, .18, 0, true); break;
                case "blockHit": Gong(820 + variation * 60, .2, 0, .2); Wood(2400, .1, .003); break;
                case "block": Gong(1050, .13, 0, .32); Pluck(81, .1, .025, .25); break;
                case "buff": Notes(74, new[] { 0, 4, 7 }, .055, .13); Paper(.1, .07); break;
                case "debuff": Gong(310, .15, 0, .3); Wood(580, .13, .04); break;
                case "heal": Notes(74, new[] { 0, 2, 4, 7, 9 }, .07, .12); Gong(1350, .075, .25, .6); break;
                case "coin": Gong(1850, .12, 0, .18); Gong(2670, .065, .04, .15); break;
                case "jingle": Crackers(3, .055); Wood(1800, .06, .1); break;
                case "bell": Gong(Pitch(value), .14, 0, .55); break;
                case "death": Paper(.45, .2, 0, true); Drum(.17, .02, 80, .35); Gong(220, .1, .05, .65); break;
                case "win": Notes(74, new[] { 0, 4, 7, 9, 12 }, .1, .13); Crackers(); break;
                case "lose": Notes(62, new[] { 0, -3, -5, -10 }, .18, .12); Gong(180, .08, .4, .7); break;
                case "turn": Wood(1400, .15); Wood(1900, .12, .05); Gong(950, .1, .08, .3); break;
                case "enemyTurn": Drum(.2, 0, 110, .22); Wood(600, .12, .11); break;
                case "windup": Paper(.16, .2, 0, true); Drum(.07, .1, 210, .08); break;
                case "whoosh": Paper(.24, .22, 0, true); break;
                case "spirit": Notes(74, new[] { 0, 2, 7, 9, 12 }, .06, .11); Gong(1600, .08, .28, .45); break;
                case "transform": Roar(); break;
                case "potion": Wood(900, .12); for (int i = 0; i < 4; i++) { Pluck(62 + pentatonic[i], .08, .05 + i * .055, .12); Paper(.04, .05, .06 + i * .055); } break;
                case "relic": Gong(720, .1, 0, .7); Notes(74, new[] { 0, 7, 9, 12 }, .075, .13); break;
                case "type": Wood(1600 + variation * 200, .035, 0, .024); break;
                case "line": Pluck(81 + variation * 2, .055, 0, .3); break;
                case "pop": Crack(.14); Pluck(86, .08, .025, .16); break;
                case "energy": Wood(1700, .12); Notes(74, new[] { 0, 7 }, .055, .1); break;
                case "thud": Drum(.34, 0, 65, .38); Wood(440, .08); break;
                case "fire": Crack(.018); Paper(.075, .018, .015); break;
                case "heartbeat": Drum(.19, 0, 53, .13); Drum(.13, .17, 60, .12); break;
                case "holdTick": Wood(1100 + value * 180, .11, 0, .045); break;
                case "page": Paper(.16, .18); break;
                case "give": Paper(.16, .14); Notes(74, new[] { 0, 4, 9 }, .065, .1, .035); break;
                case "love": Notes(81, new[] { 0, 2, 7 }, .065, .12); break;
                case "hate": Wood(420, .17); Gong(260, .09, .03, .25); break;
                case "charm": Notes(74, new[] { 0, 2, 4, 7, 9, 12 }, .075, .13); Crackers(4, .065, .3); break;
                case "clink": Gong(2300 * Jitter(), .12, 0, .2); break;
                case "grab": Paper(.08, .12); Wood(1600, .13, .01); break;
                case "spot": Drum(.17, 0, 100, .24); Paper(.2, .12, .02, true); break;
                case "warn": Wood(680, .15); Wood(680, .13, .14); Gong(280, .06, .05, .35); break;
                case "blessing": Paper(.2, .15); Notes(74, new[] { 0, 4, 7, 9, 12 }, .085, .14, .03); Gong(1450, .1, .3, .6); break;
                case "zodiac": Drum(.24); Drum(.16, .08, 170, .15); Drum(.18, .16, 150, .16); Gong(650, .16, .24, .65); Notes(74, new[] { 0, 7, 12 }, .07, .1, .24); break;
                case "firecracker": Crackers(8, .22); Drum(.16, .04, 130, .25); break;
                default: throw new InvalidOperationException("Missing festival sound recipe: " + name);
            }
            // Remove DC, preserve quiet UI/ambient sounds, and leave headroom for simultaneous effects.
            double mean = 0; for (int i = 0; i < end; i++) mean += pcm[i]; mean /= end;
            double peak = 0; for (int i = 0; i < end; i++) { pcm[i] -= mean; peak = Math.Max(peak, Math.Abs(pcm[i])); }
            double scale = peak > .72 ? .72 / peak : 1;
            var output = new float[end];
            for (int i = 0; i < end; i++) output[i] = (float)(pcm[i] * scale * Math.Min(1, i / 96.0) * Math.Min(1, (end - 1 - i) / 240.0));
            return output;
        }
    }
}
#endif
