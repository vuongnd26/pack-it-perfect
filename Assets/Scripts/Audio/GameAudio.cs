using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Semantic audio identifiers; UI and gameplay never select clips directly.</summary>
    public enum GameSound
    {
        Pickup, Place, Rotate, Invalid, Zipper, Win, Tick, UI, Booster,
        Return, Undo, Hint, Freeze, Locked, Banner, LevelStart, Star, Reward, Lose,
    }

    /// <summary>Owns a bounded voice bank and original short procedural PCM clips; no downloads or imported audio.</summary>
    public sealed class GameAudio : IDisposable
    {
        const int SOUND_COUNT = (int)GameSound.Lose + 1;
        const float MIN_PITCH = 0.5f;
        const float MAX_PITCH = 2f;

        readonly AudioClip[] _clips = new AudioClip[SOUND_COUNT];
        readonly float[] _lastPlayed = new float[SOUND_COUNT];
        readonly AudioSource[] _voices;
        readonly GameObject _root;
        readonly AudioTuning _tuning;
        readonly System.Random _noise = new System.Random(721);
        readonly System.Random _spread = new System.Random(1187);
        int _nextVoice;
        int _warm;
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
                source.bypassEffects = true;
                source.bypassListenerEffects = true;
                source.bypassReverbZones = true;
                source.volume = Mathf.Clamp01(tuning.volume);
                _voices[i] = source;
            }
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = float.NegativeInfinity;

            // No clip is synthesised here: building all of them at once froze the start-up on slower phones. The owner
            // calls WarmNext once per frame; a sound needed earlier is built on the spot by Play.
        }

        AudioTuning.Tone ToneFor(GameSound sound)
        {
            switch (sound)
            {
                case GameSound.Pickup: return _tuning.pickup;
                case GameSound.Place: return _tuning.place;
                case GameSound.Rotate: return _tuning.rotate;
                case GameSound.Invalid: return _tuning.invalid;
                case GameSound.Zipper: return _tuning.zipper;
                case GameSound.Tick: return _tuning.tick;
                case GameSound.UI: return _tuning.ui;
                case GameSound.Booster: return _tuning.booster;
                case GameSound.Return: return _tuning.pieceReturn;
                case GameSound.Undo: return _tuning.undo;
                case GameSound.Hint: return _tuning.hint;
                case GameSound.Freeze: return _tuning.freeze;
                case GameSound.Locked: return _tuning.locked;
                case GameSound.Banner: return _tuning.banner;
                case GameSound.LevelStart: return _tuning.levelStart;
                case GameSound.Star: return _tuning.star;
                case GameSound.Reward: return _tuning.reward;
                case GameSound.Lose: return _tuning.lose;
                default: return null;
            }
        }

        void Build(int index)
        {
            var sound = (GameSound)index;
            if (sound == GameSound.Win)
            {
                _clips[index] = VictoryClip();
                return;
            }
            var tone = ToneFor(sound);
            if (tone != null) _clips[index] = ToneClip(sound.ToString(), tone, sound == GameSound.Zipper);
        }

        /// <summary>Builds the next clip that does not exist yet. Call once per frame; returns false when everything is built.</summary>
        public bool WarmNext()
        {
            while (_warm < _clips.Length)
            {
                int index = _warm++;
                if (_clips[index] != null) continue;
                Build(index);
                return _warm < _clips.Length;
            }
            return false;
        }

        /// <summary>Mutes all categories and immediately stops any active sound.</summary>
        public bool Muted
        {
            get => _muted;
            set { _muted = value; if (value) Stop(); }
        }

        /// <summary>Plays one semantic event with bounded polyphony.</summary>
        /// <param name="sound">Which event.</param>
        /// <param name="pitch">Playback pitch multiplier (1 = as synthesized).</param>
        /// <param name="volume">Volume multiplier on top of the mix volume, 0..1.</param>
        /// <param name="vary">True adds the small random pitch spread used for repeated gameplay sounds.</param>
        public void Play(GameSound sound, float pitch = 1f, float volume = 1f, bool vary = false)
        {
            int index = (int)sound;
            if (_muted || _root == null || index < 0 || index >= _clips.Length) return;
            if (_clips[index] == null) Build(index);
            if (_clips[index] == null) return;
            float now = Time.unscaledTime;
            if (now - _lastPlayed[index] < _tuning.retriggerGuard) return;
            _lastPlayed[index] = now;

            AudioSource voice = null;
            foreach (var source in _voices)
                if (!source.isPlaying) { voice = source; break; }
            if (voice == null) { voice = _voices[_nextVoice]; _nextVoice = (_nextVoice + 1) % _voices.Length; }
            if (vary && _tuning.pitchSpread > 0f)
                pitch *= 1f + ((float)_spread.NextDouble() * 2f - 1f) * _tuning.pitchSpread;
            voice.Stop();
            voice.clip = _clips[index];
            voice.pitch = Mathf.Clamp(pitch, MIN_PITCH, MAX_PITCH);
            voice.volume = Mathf.Clamp01(_tuning.volume * volume);
            voice.Play();
        }

        /// <summary>Stops in-flight sounds, for example when changing levels.</summary>
        public void Stop()
        {
            foreach (var source in _voices) if (source != null) source.Stop();
        }

        AudioClip ToneClip(string name, AudioTuning.Tone tone, bool rasp)
        {
            int rate = Mathf.Clamp(_tuning.sampleRate, 8000, 48000);
            float duration = Mathf.Clamp(tone.duration, 0.01f, 2f);
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            AddNote(samples, rate, tone, 0f, 1f, rasp);
            if (tone.secondNoteDelay > 0f && tone.secondNoteDelay < duration)
                AddNote(samples, rate, tone, tone.secondNoteDelay, Mathf.Max(0.1f, tone.secondNoteRatio), rasp);
            return MakeClip(name, samples, rate);
        }

        // Adds one voice of the tone into the buffer, starting at startTime, with its frequencies scaled by ratio.
        void AddNote(float[] samples, int rate, AudioTuning.Tone tone, float startTime, float ratio, bool rasp)
        {
            int start = Mathf.Clamp(Mathf.RoundToInt(startTime * rate), 0, samples.Length);
            int count = samples.Length - start;
            if (count <= 0) return;
            float duration = (float)count / rate;
            double phase = 0d, overtonePhase = 0d;
            float noiseState = 0f;
            float lowFrequency = Mathf.Max(30f, tone.frequency * ratio);
            float highFrequency = Mathf.Max(30f, tone.endFrequency * ratio);
            float nyquistGuard = rate * 0.4f;
            for (int i = 0; i < count; i++)
            {
                float time = (float)i / rate, k = time / duration;
                // Exponential glide: equal musical steps per unit time, so sweeps sound natural.
                float frequency = Mathf.Min(nyquistGuard, lowFrequency * Mathf.Pow(highFrequency / lowFrequency, k));
                double step = Math.PI * 2d * frequency / rate;
                phase += step;
                float pure = (float)(Math.Sin(phase) + _tuning.harmonic * Math.Sin(phase * 2d));
                if (tone.overtone > 0f && tone.overtoneGain > 0f && frequency * tone.overtone < nyquistGuard)
                {
                    overtonePhase += step * tone.overtone;
                    // The partial dies faster than the fundamental, like a struck bell.
                    pure += tone.overtoneGain * (float)Math.Sin(overtonePhase) * (1f - k);
                }
                noiseState = noiseState * 0.5f + ((float)_noise.NextDouble() * 2f - 1f) * 0.5f;
                float body = tone.decay > 0f ? Mathf.Exp(-tone.decay * k) * (1f - k) : (1f - k) * (1f - k);
                float envelope = Edge(time, duration) * body;
                float texture = rasp ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin((float)phase)) : 1f;
                samples[start + i] += Mathf.Lerp(pure, noiseState, tone.noise) * tone.gain * envelope * texture;
            }
        }

        // Notes start one after another and ring on, so the phrase ends as a full chord instead of chopped beeps.
        AudioClip VictoryClip()
        {
            int rate = Mathf.Clamp(_tuning.sampleRate, 8000, 48000);
            float step = Mathf.Clamp(_tuning.victoryNoteDuration, 0.04f, 0.4f);
            float ring = Mathf.Clamp(_tuning.victoryRing, step, 2f);
            int count = Mathf.Clamp(_tuning.victoryNotes?.Length ?? 0, 1, 16);
            var samples = new float[Mathf.CeilToInt((step * (count - 1) + ring) * rate)];
            int ringSamples = Mathf.CeilToInt(ring * rate);
            for (int note = 0; note < count; note++)
            {
                int semitone = _tuning.victoryNotes != null && note < _tuning.victoryNotes.Length ? _tuning.victoryNotes[note] : 0;
                float frequency = _tuning.victoryFrequency * Mathf.Pow(2f, semitone / 12f);
                int offset = Mathf.RoundToInt(note * step * rate);
                for (int i = 0; i < ringSamples && offset + i < samples.Length; i++)
                {
                    float time = (float)i / rate, k = time / ring;
                    float phase = time * frequency * Mathf.PI * 2f;
                    float bell = Mathf.Sin(phase) + _tuning.harmonic * Mathf.Sin(phase * 2f) + 0.2f * Mathf.Sin(phase * 2.76f) * (1f - k);
                    samples[offset + i] += bell * Edge(time, ring) * Mathf.Exp(-4f * k) * (1f - k) * _tuning.victoryGain;
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
            // Soft limiter: overlapping notes stay warm instead of clipping harshly.
            for (int i = 0; i < samples.Length; i++)
            {
                float x = samples[i];
                samples[i] = x / (1f + Mathf.Abs(x) * 0.6f);
            }
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
