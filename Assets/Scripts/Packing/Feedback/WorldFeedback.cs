// Built-in world-space feedback: a dark pill toast near the bottom of the view and floating word pills.
// Placeholder until the UI wave supplies its own IFeedback (story 006/007/008).
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// <see cref="IFeedback"/> drawn in world space. The toast follows the camera and scales with the view height so it
    /// reads the same on every level; words live in grid units at the position given. Call <see cref="Tick"/> every frame
    /// after the camera has moved.
    /// </summary>
    public sealed class WorldFeedback : IFeedback
    {
        sealed class FloatingWord
        {
            public PillLabel pill;
            public Vector3 start;
            public float age;
        }

        readonly SpriteFactory factory;
        readonly Transform parent;
        readonly Camera cam;
        readonly PackingTuning tuning;
        readonly List<FloatingWord> words = new List<FloatingWord>();
        PillLabel toast;
        float toastAge;
        bool toastShowing;

        /// <summary>Creates the feedback; nothing is built until the first message.</summary>
        /// <param name="factory">Quad and disc factory.</param>
        /// <param name="parent">Parent that survives level loads.</param>
        /// <param name="cam">Camera the toast sticks to.</param>
        /// <param name="tuning">Sizes, colours and timings.</param>
        public WorldFeedback(SpriteFactory factory, Transform parent, Camera cam, PackingTuning tuning)
        {
            this.factory = factory;
            this.parent = parent;
            this.cam = cam;
            this.tuning = tuning;
        }

        /// <inheritdoc/>
        public void Toast(string message)
        {
            if (toast == null)
            {
                toast = new PillLabel(factory, parent, "Toast", tuning.toastTextSize, tuning.toastHeight, tuning.toastPaddingX,
                    tuning.toastBackColor, tuning.toastTextColor, SpriteFactory.ORDER_TOAST);
            }
            toast.SetText(message);
            toast.SetAlpha(1f);
            toast.SetVisible(true);
            toastAge = 0f;
            toastShowing = true;
            PlaceToast();
        }

        /// <inheritdoc/>
        public void Word(Vector3 worldPosition, string word)
        {
            var pill = new PillLabel(factory, parent, "Word", tuning.wordTextSize, tuning.wordHeight, tuning.wordPaddingX,
                tuning.wordBackColor, tuning.wordTextColor, SpriteFactory.ORDER_WORD);
            pill.SetText(word);
            pill.Root.position = worldPosition;
            words.Add(new FloatingWord { pill = pill, start = worldPosition });
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var w in words)
            {
                w.pill.Destroy();
            }
            words.Clear();
            toastShowing = false;
            toast?.SetVisible(false);
        }

        /// <summary>Ages the toast (hold, then fade) and the words (rise and fade).</summary>
        public void Tick(float deltaTime)
        {
            if (toastShowing)
            {
                toastAge += deltaTime;
                float fade = tuning.toastFadeDuration > 0f ? (toastAge - tuning.toastDuration) / tuning.toastFadeDuration : 1f;
                if (toastAge >= tuning.toastDuration + tuning.toastFadeDuration)
                {
                    toastShowing = false;
                    toast.SetVisible(false);
                }
                else
                {
                    toast.SetAlpha(1f - Mathf.Clamp01(fade));
                    PlaceToast();
                }
            }

            for (int i = words.Count - 1; i >= 0; i--)
            {
                var w = words[i];
                w.age += deltaTime;
                float k = tuning.wordDuration > 0f ? w.age / tuning.wordDuration : 1f;
                if (k >= 1f)
                {
                    w.pill.Destroy();
                    words.RemoveAt(i);
                    continue;
                }
                float eased = 1f - (1f - k) * (1f - k);
                w.pill.Root.position = w.start + new Vector3(0f, tuning.wordRise * eased, 0f);
                // Full opacity for the first half, then fade out.
                w.pill.SetAlpha(Mathf.Clamp01(2f - 2f * k));
            }
        }

        void PlaceToast()
        {
            if (cam == null || toast == null)
            {
                return;
            }
            float viewHeight = cam.orthographicSize * 2f;
            float scale = tuning.toastReferenceViewHeight > 0f ? viewHeight / tuning.toastReferenceViewHeight : 1f;
            var c = cam.transform.position;
            float y = c.y - cam.orthographicSize + (tuning.toastBottomMargin + tuning.toastHeight / 2f) * scale;
            toast.Root.position = new Vector3(c.x, y, 0f);
            toast.Root.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
