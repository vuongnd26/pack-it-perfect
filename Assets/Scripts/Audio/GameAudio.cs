using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Semantic audio identifiers; UI and gameplay never select clips directly.</summary>
    public enum GameSound { Pickup, Place, Rotate, Invalid, Zipper, Win, Tick, UI, Booster }

    /// <summary>Owns a bounded voice bank and original short procedural PCM clips; no downloads or imported audio.</summary>
    public sealed class GameAudio : IDisposable
    {
        readonly AudioClip[] _clips = new AudioClip[(int)GameSound.Booster + 1];
        readonly AudioSource[] _voices;
        readonly GameObject _root;
        readonly AudioTuning _tuning;
        readonly System.Random _noise = new System.Random(721);
        int _nextVoice;
        bool _muted;

        /// <summary>Creates clips and a fixed bank of two-dimensional audio voices.</summary>
        public GameAudio(Transform parent, AudioTuning tuning)
        {
            _tuning = tuning;
            _root = new GameObject("Game audio");
            _root.transform.SetParent(parent, false);
            _voices = new AudioSource[Mathf.Clamp(tuning.voices, 2, 12)];
            for (int i = 0; i < _voices.Length; i++)
            {
                var source = _root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.volume = Mathf.Clamp01(tuning.volume);
                _voices[i] = source;
            }
            _clips[(int)GameSound.Pickup] = ToneClip("Pickup", tuning.pickup);
            _clips[(int)GameSound.Place] = ToneClip("Place", tuning.place);
            _clips[(int)GameSound.Rotate] = ToneClip("Rotate", tuning.rotate);
            _clips[(int)GameSound.Invalid] = ToneClip("Invalid", tuning.invalid);
            _clips[(int)GameSound.Zipper] = ToneClip("Zipper", tuning.zipper);
            _clips[(int)GameSound.Tick] = ToneClip("Tick", tuning.tick);
            _clips[(int)GameSound.UI] = ToneClip("UI", tuning.ui);
            _clips[(int)GameSound.Booster] = ToneClip("Booster", tuning.booster);
            _clips[(int)GameSound.Win] = VictoryClip();
        }

        /// <summary>Mutes all categories and immediately stops any active sound.</summary>
        public bool Muted
        {
            get => _muted;
            set { _muted = value; if (value) Stop(); }
        }

        /// <summary>Plays one semantic event with bounded polyphony.</summary>
        public void Play(GameSound sound)
        {
            if (_muted || _root == null || (int)sound < 0 || (int)sound >= _clips.Length) return;
            AudioSource voice = null;
            foreach (var source in _voices)
                if (!source.isPlaying) { voice = source; break; }
            if (voice == null) { voice = _voices[_nextVoice]; _nextVoice = (_nextVoice + 1) % _voices.Length; }
            voice.Stop();
            voice.clip = _clips[(int)sound];
            voice.Play();
        }

        /// <summary>Stops in-flight sounds, for example when changing levels.</summary>
        public void Stop()
        {
            foreach (var source in _voices) if (source != null) source.Stop();
        }

        AudioClip ToneClip(string name, AudioTuning.Tone tone)
        {
            int rate = Mathf.Clamp(_tuning.sampleRate, 8000, 48000);
            float duration = Mathf.Clamp(tone.duration, 0.01f, 2f);
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            double phase = 0d;
            float noiseState = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / rate, k = time / duration;
                float frequency = Mathf.Lerp(tone.frequency, tone.endFrequency, k);
                phase += Math.PI * 2d * Mathf.Clamp(frequency, 30f, rate * 0.4f) / rate;
                float pure = (float)(Math.Sin(phase) + _tuning.harmonic * Math.Sin(phase * 2d));
                noiseState = noiseState * 0.5f + ((float)_noise.NextDouble() * 2f - 1f) * 0.5f;
                float envelope = Edge(time, duration) * (1f - k) * (1f - k);
                float rasp = name == "Zipper" ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin((float)phase)) : 1f;
                samples[i] = Mathf.Lerp(pure, noiseState, tone.noise) * tone.gain * envelope * rasp;
            }
            return MakeClip(name, samples, rate);
        }

        AudioClip VictoryClip()
        {
            int rate = Mathf.Clamp(_tuning.sampleRate, 8000, 48000);
            float step = Mathf.Clamp(_tuning.victoryNoteDuration, 0.04f, 0.4f);
            int count = Mathf.Clamp(_tuning.victoryNotes?.Length ?? 0, 1, 16);
            int noteSamples = Mathf.CeilToInt(step * rate);
            var samples = new float[noteSamples * count];
            for (int note = 0; note < count; note++)
            {
                int semitone = _tuning.victoryNotes != null && note < _tuning.victoryNotes.Length ? _tuning.victoryNotes[note] : 0;
                float frequency = _tuning.victoryFrequency * Mathf.Pow(2f, semitone / 12f);
                for (int i = 0; i < noteSamples; i++)
                {
                    float time = (float)i / rate;
                    float phase = time * frequency * Mathf.PI * 2f;
                    samples[note * noteSamples + i] = (Mathf.Sin(phase) + _tuning.harmonic * Mathf.Sin(phase * 2f)) *
                        Edge(time, step) * (1f - time / step) * _tuning.victoryGain;
                }
            }
            return MakeClip("Victory", samples, rate);
        }

        float Edge(float time, float duration)
        {
            float fade = Mathf.Max(0.001f, _tuning.edgeFade);
            return Mathf.Clamp01(time / fade) * Mathf.Clamp01((duration - time) / fade);
        }

        static AudioClip MakeClip(string name, float[] samples, int rate)
        {
            var clip = AudioClip.Create("Pack It " + name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Stops voices and destroys every generated clip and audio child.</summary>
        public void Dispose()
        {
            Stop();
            foreach (var clip in _clips) if (clip != null) UnityEngine.Object.Destroy(clip);
            if (_root != null) UnityEngine.Object.Destroy(_root);
        }
    }
}
