// Implements: design/game-brief.md "Cơ chế chính" (Khuôn gợi ý: a faded shape + icon shows the correct position of
// a few items, only where the level data flags them) and "Quyết định đã chốt" (outlines only advise; never enforced).
// Story: production/epics/pack-it-perfect/story-004-outline-imprints.md

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Faded imprints of the pieces flagged <c>outline</c> in the level data, drawn at their reference-solution cells
    /// under the placed pieces. An imprint fades to <see cref="PackingTuning.outlinePlacedOpacity"/> once its own piece
    /// sits exactly on it. Purely visual: the board never reads it.
    /// </summary>
    public sealed class OutlineView
    {
        readonly LoadedLevel level;
        readonly List<int> pieceIds = new List<int>();
        readonly List<PieceView> views = new List<PieceView>();
        readonly List<bool> dimmed = new List<bool>();
        readonly PackingTuning tuning;

        /// <summary>Builds one imprint per outlined piece under the grid root, so it moves with the container.</summary>
        public OutlineView(SpriteFactory factory, GridView grid, LoadedLevel level, PackingTuning tuning)
        {
            this.level = level;
            this.tuning = tuning;
            var pieces = level.Data.pieces;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!pieces[i].outline)
                {
                    continue;
                }
                var decor = new PieceDecor { icon = factory.GetIcon(pieces[i].item), iconAlpha = tuning.outlineIconAlpha };
                var view = new PieceView(factory, grid.Root, $"Outline {pieces[i].letter}", tuning.outlineColor,
                    SpriteFactory.ORDER_OUTLINE, tuning, decor);
                view.SetShape(level.SolutionShapes[i]);
                view.PlaceAt(grid.CellTopLeft(level.SolutionOrigins[i]), 1f);
                pieceIds.Add(i);
                views.Add(view);
                dimmed.Add(false);
            }
        }

        /// <summary>Dims imprints whose piece now sits exactly at its solution and restores the others. Call after any board change.</summary>
        public void Refresh(BoardModel board)
        {
            for (int k = 0; k < pieceIds.Count; k++)
            {
                bool placed = level.IsAtSolution(board, pieceIds[k]);
                if (placed == dimmed[k])
                {
                    continue;
                }
                dimmed[k] = placed;
                float opacity = placed ? tuning.outlinePlacedOpacity : 1f;
                views[k].SetVisible(opacity > 0f);
                views[k].SetOpacity(opacity);
            }
        }
    }
}
