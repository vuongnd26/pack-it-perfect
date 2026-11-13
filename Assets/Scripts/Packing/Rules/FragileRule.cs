// Implements: design/game-brief.md "Nâng cấp Cozy Travel" (Đồ dễ vỡ: a fragile item may not share a cell edge with a
// heavy item; checked on drop, whichever of the two is placed second; corners do not count).
// Pure C#: no MonoBehaviour dependency.

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Fragile items: a fragile piece and a heavy piece may never share a cell edge.
    /// The check runs on every drop against the pieces already on the board, so the result does not depend on the
    /// order the two pieces were placed in. A completion check repeats it as a safety net.
    /// </summary>
    public sealed class FragileRule : IPackingRule, IBoardAwareRule
    {
        // Instance field rather than static: no static state survives with domain reload off.
        readonly Vector2Int[] edgeNeighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        readonly bool[] fragile;
        readonly bool[] heavy;

        /// <summary>Creates the rule.</summary>
        /// <param name="fragile">Per piece: true if it is fragile.</param>
        /// <param name="heavy">Per piece: true if it is heavy. A piece flagged both is treated as fragile only.</param>
        public FragileRule(bool[] fragile, bool[] heavy)
        {
            this.fragile = fragile;
            this.heavy = heavy;
        }

        /// <summary>True if the piece is fragile.</summary>
        public bool IsFragile(int pieceId) => fragile[pieceId];

        bool IsHeavyOnly(int pieceId) => heavy[pieceId] && !fragile[pieceId];

        /// <summary>Position-only check: nothing to verify without the board.</summary>
        public PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin) => PlacementFailure.None;

        /// <inheritdoc/>
        public PlacementFailure CheckPlacement(BoardModel board, int pieceId, PieceShape shape, Vector2Int origin) =>
            FindConflict(board, pieceId, shape, origin) == BoardModel.NO_PIECE ? PlacementFailure.None : PlacementFailure.Fragile;

        /// <summary>
        /// The piece on the board that would clash with <paramref name="pieceId"/> at this position (a heavy neighbour
        /// of a fragile piece, or a fragile neighbour of a heavy piece), or <see cref="BoardModel.NO_PIECE"/>.
        /// </summary>
        public int FindConflict(BoardModel board, int pieceId, PieceShape shape, Vector2Int origin)
        {
            bool isFragile = fragile[pieceId];
            bool isHeavy = IsHeavyOnly(pieceId);
            if (!isFragile && !isHeavy)
            {
                return BoardModel.NO_PIECE;
            }
            foreach (var c in shape.Cells)
            {
                foreach (var step in edgeNeighbours)
                {
                    int other = board.PieceAt(origin + c + step);
                    if (other == BoardModel.NO_PIECE || other == pieceId)
                    {
                        continue;
                    }
                    if (isFragile ? IsHeavyOnly(other) : fragile[other])
                    {
                        return other;
                    }
                }
            }
            return BoardModel.NO_PIECE;
        }

        /// <summary>First fragile piece (data order) touching a heavy piece, or <see cref="BoardModel.NO_PIECE"/>.</summary>
        public int FindCompletionViolation(BoardModel board)
        {
            for (int i = 0; i < fragile.Length; i++)
            {
                if (fragile[i] && board.TryGetPlacement(i, out var shape, out var origin) &&
                    FindConflict(board, i, shape, origin) != BoardModel.NO_PIECE)
                {
                    return i;
                }
            }
            return BoardModel.NO_PIECE;
        }

        /// <inheritdoc/>
        public bool AllowsRotation(int pieceId) => true;
    }
}
