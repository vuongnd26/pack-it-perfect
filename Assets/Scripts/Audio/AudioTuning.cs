using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Serialized synthesis and mix values for original, locally generated game sounds.</summary>
    [Serializable]
    public sealed class AudioTuning
    {
        /// <summary>One short synthesized sound, configured independently of gameplay rules.</summary>
        [Serializable]
        public sealed class Tone
        {
            /// <summary>Length in seconds.</summary>
            [Min(0.01f)] public float duration;
            /// <summary>Initial fundamental in hertz.</summary>
            public float frequency;
            /// <summary>Final fundamental in hertz.</summary>
            public float endFrequency;
            /// <summary>Sound gain.</summary>
            [Range(0f, 1f)] public float gain;
            /// <summary>Fraction of filtered deterministic noise in the sound.</summary>
            [Range(0f, 1f)] public float noise;
            /// <summary>Creates a synthesis preset.</summary>
            public Tone(float duration, float frequency, float endFrequency, float gain, float noise = 0f)
            {
                this.duration = duration; this.frequency = frequency; this.endFrequency = endFrequency;
                this.gain = gain; this.noise = noise;
            }
        }

        [Header("Mix")]
        [Tooltip("Mono PCM sample rate for generated clips.")]
        public int sampleRate = 22050;
        [Tooltip("Overall mix volume.")]
        [Range(0f, 1f)] public float volume = 0.55f;
        [Tooltip("Bounded number of simultaneous sound voices; oldest is stolen if all are occupied.")]
        [Range(2, 12)] public int voices = 6;
        [Tooltip("Short attack/release ramp prevents clicks at clip boundaries.")]
        [Min(0.001f)] public float edgeFade = 0.008f;
        [Tooltip("Strength of the second harmonic.")]
        [Range(0f, 0.5f)] public float harmonic = 0.18f;

        [Header("Sound presets")]
        [Tooltip("Soft rising pickup.")]
        public Tone pickup = new Tone(0.09f, 360f, 620f, 0.3f);
        [Tooltip("Warm settling click.")]
        public Tone place = new Tone(0.13f, 620f, 290f, 0.4f, 0.08f);
        [Tooltip("Quick turning chirp.")]
        public Tone rotate = new Tone(0.09f, 520f, 780f, 0.25f);
        [Tooltip("Muted low wrong-drop thud.")]
        public Tone invalid = new Tone(0.16f, 180f, 100f, 0.28f, 0.12f);
        [Tooltip("Filtered mechanical zipper rasp.")]
        public Tone zipper = new Tone(0.5f, 100f, 180f, 0.22f, 0.85f);
        [Tooltip("Small warning clock tick.")]
        public Tone tick = new Tone(0.045f, 1050f, 720f, 0.22f, 0.25f);
        [Tooltip("Quiet UI confirmation.")]
        public Tone ui = new Tone(0.065f, 720f, 850f, 0.22f);

        [Header("Victory phrase")]
        [Tooltip("Original short major arpeggio, semitone offsets from the base frequency.")]
        public int[] victoryNotes = { 0, 4, 7, 12, 7, 12 };
        [Tooltip("Base note in hertz.")]
        public float victoryFrequency = 523.25f;
        [Tooltip("Seconds per note.")]
        public float victoryNoteDuration = 0.15f;
        [Tooltip("Victory phrase gain.")]
        [Range(0f, 1f)] public float victoryGain = 0.28f;
    }
}
