// Shared UI motion: scale pop that respects UITuning.reduceMotion.
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Common animations built on <see cref="UIAnimator"/>.</summary>
    public static class UIMotion
    {
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
