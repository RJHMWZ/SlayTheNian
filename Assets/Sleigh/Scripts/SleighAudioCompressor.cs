using System;
using UnityEngine;

namespace Sleigh
{
    // Original master bus: -14 dB, 4:1. No allocations or Unity calls in the audio callback.
    public sealed class SleighAudioCompressor : MonoBehaviour
    {
        double envelope, attack, release;
        void Awake() { int rate = AudioSettings.outputSampleRate; attack = Math.Exp(-1.0 / (.003 * rate)); release = Math.Exp(-1.0 / (.25 * rate)); }
        void OnAudioFilterRead(float[] data, int channels)
        {
            const double threshold = .1995262315;
            for (int i = 0; i < data.Length; i += channels)
            {
                double peak = 0; for (int c = 0; c < channels; c++) peak = Math.Max(peak, Math.Abs(data[i + c]));
                double coefficient = peak > envelope ? attack : release; envelope = coefficient * envelope + (1 - coefficient) * peak;
                double gain = envelope > threshold ? Math.Pow(threshold / envelope, .75) : 1;
                for (int c = 0; c < channels; c++) data[i + c] = (float)Math.Max(-.98, Math.Min(.98, data[i + c] * gain));
            }
        }
    }
}
