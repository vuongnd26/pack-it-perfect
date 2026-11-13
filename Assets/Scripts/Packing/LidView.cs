// Implements: design/game-brief.md "Core loop" step 3 (Đóng nắp: the lid closes and zips when the container is full).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// A lid in the theme frame colour that slides down over the frame, then a zipper pull crosses it
    /// left to right, closing the teeth behind it. Run <see cref="Close"/> as a coroutine.
    /// </summary>
    public sealed class LidView
    {
        readonly PackingTuning tuning;
        readonly Transform root;
        readonly Vector3 openPosition;
        readonly Vector3 closedPosition;
        readonly Transform pull;
        readonly List<SpriteRenderer> teeth = new List<SpriteRenderer>();
        readonly float stripeLeft;
        readonly float stripeWidth;

        /// <summary>Builds the lid, hidden above <paramref name="frameRect"/>.</summary>
        /// <param name="factory">Quad factory.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="frameRect">World rectangle the lid covers when closed.</param>
        /// <param name="theme">Level theme: frame = lid, rim = zipper stripe, lining = teeth and pull.</param>
        /// <param name="tuning">Timings and zipper sizes.</param>
        public LidView(SpriteFactory factory, Transform parent, Rect frameRect, ThemeData theme, PackingTuning tuning)
        {
            this.tuning = tuning;
            root = new GameObject("Lid").transform;
            root.SetParent(parent, false);
            closedPosition = frameRect.center;
            openPosition = closedPosition + new Vector3(0f, frameRect.height, 0f);
            root.position = openPosition;

            Color lidColor = LevelLoader.ParseColor(theme.frame);
            Color stripeColor = LevelLoader.ParseColor(theme.rim);
            Color toothColor = LevelLoader.ParseColor(theme.lining);

            factory.CreateQuad(root, "LidBody", Vector2.zero, frameRect.size, lidColor, SpriteFactory.ORDER_LID);
            stripeWidth = frameRect.width - 2f * tuning.framePadding;
            stripeLeft = -stripeWidth / 2f;
            factory.CreateQuad(root, "ZipperStripe", Vector2.zero, new Vector2(stripeWidth, tuning.zipperHeight), stripeColor, SpriteFactory.ORDER_LID + 1);

            int toothCount = Mathf.FloorToInt(stripeWidth / tuning.zipperToothSpacing);
            var toothSize = new Vector2(tuning.zipperToothSize, tuning.zipperToothSize);
            for (int i = 0; i < toothCount; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                var center = new Vector2(stripeLeft + (i + 0.5f) * tuning.zipperToothSpacing, side * tuning.zipperToothSize / 2f);
                var tooth = factory.CreateQuad(root, "Tooth", center, toothSize, toothColor, SpriteFactory.ORDER_LID + 2);
                tooth.enabled = false;
                teeth.Add(tooth);
            }

            pull = factory.CreateQuad(root, "ZipperPull", new Vector2(stripeLeft, 0f),
                new Vector2(tuning.zipperPullSize, tuning.zipperPullSize), toothColor, SpriteFactory.ORDER_LID + 3).transform;
            root.gameObject.SetActive(false);
        }

        /// <summary>Skips the slide and zipper travel when accessibility motion reduction is enabled.</summary>
        public bool ReduceMotion { get; set; }

        /// <summary>Slides the lid down over the grid, then zips it shut. Ends when both are done.</summary>
        /// <param name="zipperStarted">Optional semantic callback when the lid has landed and zipper travel begins.</param>
        public IEnumerator Close(System.Action zipperStarted = null)
        {
            root.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < tuning.lidSlideDuration)
            {
                if (ReduceMotion) break;
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / tuning.lidSlideDuration);
                float eased = 1f - (1f - k) * (1f - k) * (1f - k); // ease-out cubic
                root.position = Vector3.LerpUnclamped(openPosition, closedPosition, eased);
                yield return null;
            }
            root.position = closedPosition;
            zipperStarted?.Invoke();

            elapsed = 0f;
            while (elapsed < tuning.zipperDuration)
            {
                if (ReduceMotion) break;
                elapsed += Time.deltaTime;
                SetZip(Mathf.Clamp01(elapsed / tuning.zipperDuration));
                yield return null;
            }
            SetZip(1f);
        }

        void SetZip(float progress)
        {
            float pullX = stripeLeft + progress * stripeWidth;
            pull.localPosition = new Vector3(pullX, 0f, 0f);
            foreach (var tooth in teeth)
            {
                tooth.enabled = tooth.transform.localPosition.x <= pullX;
            }
        }
    }
}
