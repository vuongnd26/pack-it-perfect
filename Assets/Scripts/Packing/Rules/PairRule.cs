// Implements: design/game-brief.md "Obstacles" (Cặp đồ: the two items of a pair must sit next to each other,
// checked once every item is placed) and "Quyết định đã chốt" (touching = sharing a cell edge; corners do not count).
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency.

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Pairs: two pieces with the same pair index must share at least one cell edge once all pieces are placed.</summary>
    public sealed class PairRule : IPackingRule
    {
        /// <summary>Pair index meaning "not in a pair".</summary>
        public const int NO_PAIR = -1;

        // Instance field rather than static: no static state survives with domain reload off.
        readonly Vector2Int[] edgeNeighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        readonly int[] partner;

        /// <summary>Creates the rule.</summary>
        /// <param name="pairIndex">Per piece: its pair group, or <see cref="NO_PAIR"/>. The loader guarantees exactly two pieces per group.</param>
        public PairRule(int[] pairIndex)
        {
            partner = new int[pairIndex.Length];
            for (int i = 0; i < pairIndex.Length; i++)
            {
                partner[i] = BoardModel.NO_PIECE;
                if (pairIndex[i] == NO_PAIR)
                {
                    continue;
                }
                for (int j = 0; j < pairIndex.Length; j++)
                {
                    if (j != i && pairIndex[j] == pairIndex[i])
                    {
                        partner[i] = j;
                        break;
                    }
                }
            }
        }

        /// <summary>The other piece of this piece's pair, or <see cref="BoardModel.NO_PIECE"/>.</summary>
        public int PartnerOf(int pieceId) => partner[pieceId];

        /// <summary>True if the piece and its partner are both placed and share at least one cell edge.</summary>
        public bool TouchesPartner(BoardModel board, int pieceId)
        {
            int other = partner[pieceId];
            if (other == BoardModel.NO_PIECE || !board.TryGetPlacement(pieceId, out var shape, out var origin) || !board.IsPlaced(other))
            {
                return false;
            }
            foreach (var c in shape.Cells)
            {
                foreach (var step in edgeNeighbours)
                {
                    if (board.PieceAt(origin + c + step) == other)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin) => PlacementFailure.None;

        /// <summary>First paired piece (data order) not touching its partner, or <see cref="BoardModel.NO_PIECE"/>.</summary>
        public int FindCompletionViolation(BoardModel board)
        {
            for (int i = 0; i < partner.Length; i++)
            {
                if (partner[i] != BoardModel.NO_PIECE && !TouchesPartner(board, i))
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
