// Tiny tween runner for UI pops and fades, ticked by GameUI with unscaled time (keeps running if timeScale changes).
// Easing and durations are UI-wave picks pending a UX spec (see UITuning "Animation").
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using System.Collections.Generic;

namespace PackItPerfect.UI
{
    /// <summary>
    /// Runs keyed tweens: Play(key, …) replaces any tween with the same key. Each tween calls apply(0) at once,
    /// apply(t) every tick after its delay (t in 0..1), then apply(1) and done.
    /// </summary>
    public sealed class UIAnimator
    {
        sealed class Tween
        {
            public object Key;
            public float Delay;
            public float Duration;
            public float Elapsed;
            public Action<float> Apply;
            public Action Done;
        }

        readonly List<Tween> active = new List<Tween>();
        readonly List<Tween> snapshot = new List<Tween>();

        /// <summary>Starts a tween, cancelling any running tween with the same <paramref name="key"/>.</summary>
        public void Play(object key, float delay, float duration, Action<float> apply, Action done = null)
        {
            Cancel(key);
            active.Add(new Tween { Key = key, Delay = delay, Duration = duration, Apply = apply, Done = done });
            apply?.Invoke(0f);
        }

        /// <summary>Stops the tween with <paramref name="key"/> where it is, without calling done.</summary>
        public void Cancel(object key)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (Equals(active[i].Key, key))
                {
                    active.RemoveAt(i);
                }
            }
        }

        /// <summary>Jumps every running tween to its end (apply(1), then done). Use to skip animations.</summary>
        public void FinishAll()
        {
            snapshot.Clear();
            snapshot.AddRange(active);
            active.Clear();
            foreach (var tween in snapshot)
            {
                tween.Apply?.Invoke(1f);
                tween.Done?.Invoke();
            }
        }

        /// <summary>Advances all tweens by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (active.Count == 0)
            {
                return;
            }
            snapshot.Clear();
            snapshot.AddRange(active);
            foreach (var tween in snapshot)
            {
                if (!active.Contains(tween))
                {
                    continue; // cancelled by an earlier callback this tick
                }
                tween.Elapsed += deltaTime;
                float time = tween.Elapsed - tween.Delay;
                if (time < 0f)
                {
                    continue;
                }
                float t = tween.Duration <= 0f ? 1f : Math.Min(1f, time / tween.Duration);
                tween.Apply?.Invoke(t);
                if (t >= 1f)
                {
                    active.Remove(tween);
                    tween.Done?.Invoke();
                }
            }
        }

        /// <summary>Ease-out with a small overshoot (pop). 0 → 0, 1 → 1.</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
