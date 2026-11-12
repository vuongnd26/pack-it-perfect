// Implements: design/game-brief.md "Booster" (Gợi ý: the target cells light up for ~2 seconds).
// Story: production/epics/pack-it-perfect/story-005-hint-and-stars.md

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Pulsing bright squares with dashed borders on a hint's target cells, for <see cref="PackingTuning.hintGlowDuration"/>.</summary>
    public sealed class HintGlowView
    {
        readonly SpriteFactory factory;
        readonly GridView grid;
        readonly PackingTuning tuning;
        readonly List<SpriteRenderer> fills = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> borders = new List<SpriteRenderer>();
        Transform root;
        float elapsed;

        /// <summary>Creates the (empty) glow; it draws under the grid root so it moves with the container.</summary>
        public HintGlowView(SpriteFactory factory, GridView grid, PackingTuning tuning)
        {
            this.factory = factory;
            this.grid = grid;
            this.tuning = tuning;
        }

        /// <summary>True while a glow is showing.</summary>
        public bool Active => root != null;

        /// <summary>Lights <paramref name="cells"/>, replacing any glow already showing.</summary>
        public void Show(IReadOnlyList<Vector2Int> cells)
        {
            Clear();
            root = new GameObject("HintGlow").transform;
            root.SetParent(grid.Root, false);
            elapsed = 0f;

            float size = 1f - 2f * tuning.hintGlowInset;
            float half = size / 2f;
            foreach (var cell in cells)
            {
                Vector2 c = grid.CellCenter(cell);
                fills.Add(factory.CreateQuad(root, "GlowFill", c, new Vector2(size, size), tuning.hintGlowColor, SpriteFactory.ORDER_HINT));
                var tl = c + new Vector2(-half, half);
                var tr = c + new Vector2(half, half);
                var bl = c + new Vector2(-half, -half);
                var br = c + new Vector2(half, -half);
                AddBorder(tl, tr);
                AddBorder(bl, br);
                AddBorder(tl, bl);
                AddBorder(tr, br);
            }
            Apply(0f);
        }

        void AddBorder(Vector2 from, Vector2 to)
        {
            factory.CreateDashedLine(root, "GlowBorder", from, to, tuning.hintGlowBorderWidth, tuning.hintGlowDashesPerCell,
                tuning.hintGlowDashFill, tuning.hintGlowColor, SpriteFactory.ORDER_HINT + 1, borders);
        }

        /// <summary>Advances the pulse and removes the glow when its time is up; call every frame.</summary>
        public void Tick(float deltaTime)
        {
            if (root == null)
            {
                return;
            }
            elapsed += deltaTime;
            if (elapsed >= tuning.hintGlowDuration)
            {
                Clear();
                return;
            }
            Apply(elapsed);
        }

        void Apply(float time)
        {
            // Starts bright, pulses between min and max alpha.
            float pulse = 0.5f + 0.5f * Mathf.Cos(time * tuning.hintGlowPulsesPerSecond * 2f * Mathf.PI);
            float alpha = Mathf.Lerp(tuning.hintGlowMinAlpha, tuning.hintGlowMaxAlpha, pulse);
            var border = tuning.hintGlowColor;
            border.a *= alpha;
            var fill = tuning.hintGlowColor;
            fill.a *= alpha * tuning.hintGlowFillAlpha;
            foreach (var r in fills)
            {
                r.color = fill;
            }
            foreach (var r in borders)
            {
                r.color = border;
            }
        }

        /// <summary>Removes the glow now.</summary>
        public void Clear()
        {
            fills.Clear();
            borders.Clear();
            if (root != null)
            {
                Object.Destroy(root.gameObject);
                root = null;
            }
        }
    }
}
