// Implements: design/game-brief.md "Obstacles" (Đồ nặng: an item with the ⚓ icon must touch the bottom row)
// and "Quyết định đã chốt" (heavy is checked on drop).
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency.

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Heavy items: at least one cell of a heavy piece must be on the grid's bottom row.</summary>
    public sealed class HeavyRule : IPackingRule
    {
        readonly bool[] heavy;
        readonly int bottomRow;

        /// <summary>Creates the rule.</summary>
        /// <param name="heavy">Per piece: true if it is heavy.</param>
        /// <param name="gridHeight">Rows in the grid; the bottom row is <paramref name="gridHeight"/> − 1.</param>
        public HeavyRule(bool[] heavy, int gridHeight)
        {
            this.heavy = heavy;
            bottomRow = gridHeight - 1;
        }

        /// <summary>True if the piece is heavy.</summary>
        public bool IsHeavy(int pieceId) => heavy[pieceId];

        /// <inheritdoc/>
        public PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin)
        {
            if (!heavy[pieceId])
            {
                return PlacementFailure.None;
            }
            foreach (var c in shape.Cells)
            {
                if (origin.y + c.y == bottomRow)
                {
                    return PlacementFailure.None;
                }
            }
            return PlacementFailure.Heavy;
        }

        /// <summary>Heavy is enforced on drop, so a full board never breaks it.</summary>
        public int FindCompletionViolation(BoardModel board) => BoardModel.NO_PIECE;

        /// <inheritdoc/>
        public bool AllowsRotation(int pieceId) => true;
    }
}
