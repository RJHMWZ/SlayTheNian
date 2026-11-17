using System;
using UnityEngine;

namespace Sleigh
{
    [CreateAssetMenu(menuName = "Sleigh/Audio Bank")]
    public sealed class SleighAudioBank : ScriptableObject
    {
        [Serializable] public class Effect { public string id; public AudioClip[] clips; public float gateSeconds; }
        [Serializable] public class Song { public string id; public AudioClip intro, loop; public int originalLoopSamples44100; }
        public Effect[] effects;
        public Song[] music;
        public int version;
    }
}
