// Implements: design/game-brief.md "Obstacles" (Đứng thẳng: an item with the ⬆️ icon cannot be rotated)
// and "Cơ chế chính" (Xoay đồ: món có luật Đứng thẳng thì không xoay được).
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency.

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Upright items: cannot be turned. They start in their solution orientation (checked by the loader).</summary>
    public sealed class UprightRule : IPackingRule
    {
        readonly bool[] upright;

        /// <summary>Creates the rule.</summary>
        /// <param name="upright">Per piece: true if it must stay upright.</param>
        public UprightRule(bool[] upright)
        {
            this.upright = upright;
        }

        /// <summary>True if the piece must stay upright.</summary>
        public bool IsUpright(int pieceId) => upright[pieceId];

        /// <inheritdoc/>
        public PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin) => PlacementFailure.None;

        /// <summary>Upright pieces cannot turn, so a full board never breaks the rule.</summary>
        public int FindCompletionViolation(BoardModel board) => BoardModel.NO_PIECE;

        /// <inheritdoc/>
        public bool AllowsRotation(int pieceId) => !upright[pieceId];
    }
}
