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
            [Min(0.01f)] public float duration = 0.1f;
            /// <summary>Initial fundamental in hertz.</summary>
            public float frequency = 440f;
            /// <summary>Final fundamental in hertz.</summary>
            public float endFrequency = 440f;
            /// <summary>Sound gain.</summary>
            [Range(0f, 1f)] public float gain = 0.25f;
            /// <summary>Fraction of filtered deterministic noise in the sound.</summary>
            [Range(0f, 1f)] public float noise;
            /// <summary>How fast the sound dies away; 0 keeps the legacy soft curve, higher is more percussive.</summary>
            [Min(0f)] public float decay;
            /// <summary>Frequency ratio of an extra partial (2.76 gives a bell, 2 an octave); 0 = none.</summary>
            [Min(0f)] public float overtone;
            /// <summary>Level of the extra partial relative to the fundamental.</summary>
            [Range(0f, 1f)] public float overtoneGain;
            /// <summary>Seconds after the start at which a second note joins; 0 = single note.</summary>
            [Min(0f)] public float secondNoteDelay;
            /// <summary>Frequency ratio of the second note (1.5 = a fifth up).</summary>
            [Min(0f)] public float secondNoteRatio = 1.5f;

            /// <summary>Creates a default preset (needed for serialization).</summary>
            public Tone() { }

            /// <summary>Creates a synthesis preset.</summary>
            public Tone(float duration, float frequency, float endFrequency, float gain, float noise = 0f, float decay = 0f,
                float overtone = 0f, float overtoneGain = 0f, float secondNoteDelay = 0f, float secondNoteRatio = 1.5f)
            {
                this.duration = duration; this.frequency = frequency; this.endFrequency = endFrequency;
                this.gain = gain; this.noise = noise; this.decay = decay;
                this.overtone = overtone; this.overtoneGain = overtoneGain;
                this.secondNoteDelay = secondNoteDelay; this.secondNoteRatio = secondNoteRatio;
            }
        }

        [Header("Mix")]
        [Tooltip("Mono PCM sample rate for generated clips.")]
        public int sampleRate = 32000;
        [Tooltip("Overall mix volume.")]
        [Range(0f, 1f)] public float volume = 0.6f;
        [Tooltip("Bounded number of simultaneous sound voices; oldest is stolen if all are occupied.")]
        [Range(2, 12)] public int voices = 8;
        [Tooltip("Short attack/release ramp prevents clicks at clip boundaries.")]
        [Min(0.001f)] public float edgeFade = 0.006f;
        [Tooltip("Strength of the second harmonic.")]
        [Range(0f, 0.5f)] public float harmonic = 0.16f;
        [Tooltip("Random pitch spread applied to repeated gameplay sounds so they never sound mechanical (0.03 = ±3%).")]
        [Range(0f, 0.1f)] public float pitchSpread = 0.03f;
        [Tooltip("How much the placement sound rises in pitch as the container fills (0.25 = a quarter higher when full).")]
        [Range(0f, 0.6f)] public float placePitchRise = 0.26f;
        [Tooltip("The same sound is not started twice within this many seconds.")]
        [Min(0f)] public float retriggerGuard = 0.03f;

        [Header("Gameplay sounds")]
        [Tooltip("Soft rising pop when a piece is lifted.")]
        public Tone pickup = new Tone(0.09f, 400f, 660f, 0.26f, 0.04f, 3.5f);
        [Tooltip("Warm settling knock with a small bell on top.")]
        public Tone place = new Tone(0.2f, 560f, 330f, 0.4f, 0.08f, 6f, 2.76f, 0.22f);
        [Tooltip("Quick turning chirp.")]
        public Tone rotate = new Tone(0.09f, 560f, 840f, 0.22f, 0f, 3f);
        [Tooltip("Muted low wrong-drop thud.")]
        public Tone invalid = new Tone(0.2f, 175f, 95f, 0.3f, 0.14f, 4f);
        [Tooltip("Soft falling whoosh when a piece goes back to the tray.")]
        public Tone pieceReturn = new Tone(0.15f, 540f, 250f, 0.18f, 0.4f, 3f);
        [Tooltip("Filtered mechanical zipper rasp.")]
        public Tone zipper = new Tone(0.5f, 100f, 180f, 0.22f, 0.85f);
        [Tooltip("Small warning clock tick.")]
        public Tone tick = new Tone(0.045f, 1050f, 720f, 0.22f, 0.25f);
        [Tooltip("Bright upward sweep when a booster is used.")]
        public Tone booster = new Tone(0.3f, 500f, 1100f, 0.26f, 0f, 2.5f, 2f, 0.2f);
        [Tooltip("Two sparkling notes when a hint lights its cells.")]
        public Tone hint = new Tone(0.34f, 880f, 880f, 0.22f, 0f, 5f, 2.76f, 0.2f, 0.09f, 1.5f);
        [Tooltip("Glassy shimmer when the clock freezes.")]
        public Tone freeze = new Tone(0.5f, 1400f, 2100f, 0.18f, 0.1f, 3f, 1.5f, 0.3f);
        [Tooltip("Short step back for Undo.")]
        public Tone undo = new Tone(0.12f, 720f, 430f, 0.2f, 0f, 3.5f);

        [Header("Interface sounds")]
        [Tooltip("Quiet UI confirmation.")]
        public Tone ui = new Tone(0.07f, 760f, 900f, 0.2f, 0f, 4.5f);
        [Tooltip("Dull knock for a locked or empty booster.")]
        public Tone locked = new Tone(0.13f, 230f, 190f, 0.24f, 0.2f, 5f);
        [Tooltip("Airy swoosh when a banner or card slides in.")]
        public Tone banner = new Tone(0.24f, 320f, 760f, 0.14f, 0.8f, 2f);
        [Tooltip("Two gentle rising notes when a level is dealt.")]
        public Tone levelStart = new Tone(0.34f, 523.25f, 523.25f, 0.22f, 0f, 4.5f, 2f, 0.15f, 0.1f, 1.26f);
        [Tooltip("Bell for each star on the win card; pitched up star by star.")]
        public Tone star = new Tone(0.42f, 1046.5f, 1046.5f, 0.3f, 0f, 5f, 2.76f, 0.28f);
        [Tooltip("Pitch multiplier added per star (1.122 = a whole tone).")]
        public float starPitchStep = 1.122f;
        [Tooltip("Two bright notes when a reward or a new booster appears.")]
        public Tone reward = new Tone(0.45f, 783.99f, 783.99f, 0.26f, 0f, 4f, 2f, 0.22f, 0.1f, 1.5f);
        [Tooltip("Slow falling tone when the timer runs out.")]
        public Tone lose = new Tone(0.7f, 392f, 196f, 0.28f, 0.05f, 2.2f, 1.5f, 0.25f);

        [Header("Victory phrase")]
        [Tooltip("Original short major arpeggio, semitone offsets from the base frequency.")]
        public int[] victoryNotes = { 0, 4, 7, 12, 16, 19, 24 };
        [Tooltip("Base note in hertz.")]
        public float victoryFrequency = 523.25f;
        [Tooltip("Seconds between note starts.")]
        public float victoryNoteDuration = 0.1f;
        [Tooltip("Seconds each note rings on after it starts (notes overlap into a chord).")]
        public float victoryRing = 0.55f;
        [Tooltip("Victory phrase gain.")]
        [Range(0f, 1f)] public float victoryGain = 0.2f;
    }
}
