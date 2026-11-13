// Shared UI motion: scale pop that respects UITuning.reduceMotion.
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Common animations built on <see cref="UIAnimator"/>.</summary>
    public static class UIMotion
    {
        /// <summary>Fades <paramref name="group"/> in from transparent. With reduced motion it is shown at once.</summary>
        public static void FadeIn(UIAnimator animator, object key, CanvasGroup group, float duration, bool reduceMotion)
        {
            if (group == null) return;
            if (reduceMotion || duration <= 0f)
            {
                animator.Cancel(key);
                group.alpha = 1f;
                return;
            }
            animator.Play(key, 0f, duration, t => group.alpha = t);
        }

        /// <summary>One quick swell and back (a heartbeat). Skipped under reduced motion.</summary>
        public static void Pulse(UIAnimator animator, object key, Transform target, float duration, float amount, bool reduceMotion)
        {
            if (reduceMotion || duration <= 0f)
            {
                animator.Cancel(key);
                target.localScale = Vector3.one;
                return;
            }
            animator.Play(key, 0f, duration, t =>
            {
                float scale = t >= 1f ? 1f : 1f + amount * Mathf.Sin(t * Mathf.PI);
                target.localScale = new Vector3(scale, scale, 1f);
            });
        }

        /// <summary>
        /// Pops <paramref name="target"/> from <paramref name="fromScale"/> to 1 with an overshoot after <paramref name="delay"/>.
        /// With reduced motion the target is shown at full size at once (the delay is kept so sequences stay in order).
        /// </summary>
        public static void Pop(UIAnimator animator, object key, Transform target, float delay, float duration,
            float fromScale, bool reduceMotion, Action done = null)
        {
            if (reduceMotion)
            {
                animator.Play(key, delay, 0f, t => target.localScale = t >= 1f ? Vector3.one : Vector3.zero, done);
                return;
            }
            animator.Play(key, delay, duration, t =>
            {
                float scale = Mathf.LerpUnclamped(fromScale, 1f, UIAnimator.EaseOutBack(t));
                target.localScale = new Vector3(scale, scale, 1f);
            }, done);
        }
    }
}
