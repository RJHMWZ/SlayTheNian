using System.Collections.Generic;
using UnityEngine;

namespace Sleigh
{
    public sealed class SleighAudio : MonoBehaviour
    {
        public static SleighAudio Instance;
        public SleighAudioBank bank;
        [Tooltip("Two prebuilt pairs: intro A / loop A / intro B / loop B.")]
        public AudioSource[] musicSources;
        public AudioSource[] effectVoices;
        [Range(0, 1)] public float master = .8f, musicVolume = .45f, effectsVolume = .8f;
        public bool mute;
        readonly Dictionary<string, SleighAudioBank.Effect> effects = new Dictionary<string, SleighAudioBank.Effect>();
        readonly Dictionary<string, SleighAudioBank.Song> songs = new Dictionary<string, SleighAudioBank.Song>();
        readonly Dictionary<string, double> last = new Dictionary<string, double>();
        readonly float[] gains = new float[2], from = new float[2], to = new float[2], fadeTime = new float[2], fadeDuration = new float[2];
        string song;
        int channel = -1, voice;
        float duck = 1, duckTarget = 1;
        public string CurrentSong => song;
        public int RuntimeGeneratedClipCount => 0;
        void Awake()
        {
            Instance = this;
            if (!bank) bank = Resources.Load<SleighAudioBank>("Sleigh/Audio/AudioBank");
            if (!bank || musicSources == null || musicSources.Length != 4 || effectVoices == null || effectVoices.Length == 0)
            { Debug.LogError("Sleigh audio was not baked into the scene. Run Sleigh/Bake Scene and Presentation.", this); enabled = false; return; }
            foreach (var e in bank.effects) effects[e.id] = e;
            foreach (var m in bank.music) songs[m.id] = m;
            master = PlayerPrefs.GetFloat("Sleigh.Master", .8f); musicVolume = PlayerPrefs.GetFloat("Sleigh.Music", .45f);
            effectsVolume = PlayerPrefs.GetFloat("Sleigh.SFX", .8f); mute = PlayerPrefs.GetInt("Sleigh.Mute", 0) != 0;
            foreach (var s in musicSources) Configure(s); foreach (var s in effectVoices) Configure(s); Apply();
        }
        static void Configure(AudioSource s)
        { s.playOnAwake = false; s.spatialBlend = 0; s.dopplerLevel = 0; s.pitch = 1; s.panStereo = 0; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public void Apply()
        {
            PlayerPrefs.SetFloat("Sleigh.Master", master); PlayerPrefs.SetFloat("Sleigh.Music", musicVolume);
            PlayerPrefs.SetFloat("Sleigh.SFX", effectsVolume); PlayerPrefs.SetInt("Sleigh.Mute", mute ? 1 : 0); SetVolumes();
        }
        void SetVolumes()
        {
            if (musicSources == null || musicSources.Length != 4) return;
            float volume = mute ? 0 : master;
            for (int i = 0; i < musicSources.Length; i++) if (musicSources[i]) musicSources[i].volume = gains[i / 2] * musicVolume * volume * duck;
            if (effectVoices != null) foreach (var source in effectVoices) if (source) source.volume = volume * effectsVolume;
        }
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < 2; i++)
            {
                fadeTime[i] += dt; gains[i] = Mathf.Lerp(from[i], to[i], fadeDuration[i] <= 0 ? 1 : Mathf.Clamp01(fadeTime[i] / fadeDuration[i]));
                if (to[i] == 0 && gains[i] <= 0 && musicSources != null && musicSources.Length == 4)
                { musicSources[i * 2].Stop(); musicSources[i * 2 + 1].Stop(); }
            }
            duck = Mathf.Lerp(duck, duckTarget, 1 - Mathf.Exp(-10 * dt)); SetVolumes();
        }
        void Fade(int group, float target, float seconds)
        { from[group] = gains[group]; to[group] = target; fadeTime[group] = 0; fadeDuration[group] = seconds; }
        public void Duck(float value) => duckTarget = Mathf.Clamp01(value);
        public void Music(string name)
        {
            if (!Application.isPlaying || !enabled || song == name) return;
            if (channel >= 0) Fade(channel, 0, string.IsNullOrEmpty(name) ? .4f : .6f);
            song = name; if (string.IsNullOrEmpty(name) || !songs.TryGetValue(name, out var entry)) return;
            channel = (channel + 1) % 2;
            var intro = musicSources[channel * 2]; var loop = musicSources[channel * 2 + 1]; intro.Stop(); loop.Stop();
            intro.clip = entry.intro; loop.clip = entry.loop; intro.loop = false; loop.loop = true;
            double start = AudioSettings.dspTime + .04; intro.PlayScheduled(start);
            double join = start + entry.intro.samples / (double)entry.intro.frequency;
            intro.SetScheduledEndTime(join); loop.PlayScheduled(join);
            gains[channel] = from[channel] = 0; Fade(channel, 1, .5f); SetVolumes();
        }
        public void Play(string name, int value = 0)
        {
            if (!Application.isPlaying || !enabled || mute || string.IsNullOrEmpty(name)) return;
            switch (name)
            {
                case "gift": name = "play"; break; case "blocked": name = "blockHit"; break;
                case "cookies": name = "coin"; break; case "victory": name = "win"; break; case "defeat": name = "lose"; break;
                case "attack": name = "windup"; break; case "cast": name = "buff"; break; case "ready": name = "spirit"; break;
                case "soft": name = "charm"; break; case "pips": name = "pop"; break;
            }
            // Unknown visual events are silent, never a generic replacement beep.
            if (!effects.TryGetValue(name, out var effect) || effect.clips.Length == 0) return;
            double now = AudioSettings.dspTime;
            if (last.TryGetValue(name, out var previous) && now - previous < effect.gateSeconds) return;
            last[name] = now;
            int variant = name == "play" || name == "cardHover" ? Mathf.Clamp(value, 0, effect.clips.Length - 1)
                : name == "hit" ? value <= 3 ? 0 : value <= 9 ? 1 : value <= 18 ? 2 : 3
                : name == "bell" ? value == 79 ? 0 : value == 96 ? 2 : 1 : Random.Range(0, effect.clips.Length);
            var clip = effect.clips[Mathf.Clamp(variant, 0, effect.clips.Length - 1)]; if (!clip) return;
            int chosen = -1;
            for (int i = 0; i < effectVoices.Length; i++) { int n = (voice + i) % effectVoices.Length; if (!effectVoices[n].isPlaying) { chosen = n; break; } }
            if (chosen < 0) chosen = voice;
            voice = (chosen + 1) % effectVoices.Length; var source = effectVoices[chosen];
            source.Stop(); source.loop = false; source.clip = clip; source.Play();
        }
    }
}
