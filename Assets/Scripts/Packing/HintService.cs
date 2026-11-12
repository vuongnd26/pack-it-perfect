// Implements: design/game-brief.md "Booster" (Gợi ý: turn one unplaced item to its correct orientation and send back
// to the tray the items occupying its place; the player still drags it in; unlimited uses; using it caps the level at
// 2 stars) and "Quyết định đã chốt" (Hint picks the first unplaced piece in data order; an upright piece already
// starts upright, so Hint only clears and lights its target cells).
// Story: production/epics/pack-it-perfect/story-005-hint-and-stars.md
// Pure C#: no MonoBehaviour dependency.

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>What a hint did, for the view to show.</summary>
    public sealed class HintResult
    {
        /// <summary>The hinted piece (first unplaced in data order).</summary>
        public int PieceId { get; }

        /// <summary>Its solution shape: the orientation it must now have in the tray.</summary>
        public PieceShape Shape { get; }

        /// <summary>The grid cells it covers in the reference solution.</summary>
        public IReadOnlyList<Vector2Int> TargetCells { get; }

        /// <summary>Pieces taken off the board because they covered a target cell (now in the tray).</summary>
        public IReadOnlyList<int> Evicted { get; }

        /// <summary>Creates a result.</summary>
        public HintResult(int pieceId, PieceShape shape, IReadOnlyList<Vector2Int> targetCells, IReadOnlyList<int> evicted)
        {
            PieceId = pieceId;
            Shape = shape;
            TargetCells = targetCells;
            Evicted = evicted;
        }
    }

    /// <summary>
    /// Applies hints to a board and remembers whether this play of the level used one.
    /// Create one per level load: a new instance starts unflagged, so replaying a level starts at 3 stars.
    /// </summary>
    public sealed class HintService
    {
        readonly LoadedLevel level;

        /// <summary>True once a hint was applied during this play of the level.</summary>
        public bool Used { get; private set; }

        /// <summary>Creates the service for one play of <paramref name="level"/>.</summary>
        public HintService(LoadedLevel level)
        {
            this.level = level;
        }

        /// <summary>
        /// Picks the first unplaced piece, removes from <paramref name="board"/> every piece covering one of its
        /// solution cells, and marks the hint as used. Does nothing if every piece is placed.
        /// The caller turns the piece to <see cref="HintResult.Shape"/> and returns the evicted pieces to the tray.
        /// </summary>
        /// <returns>False if there is no unplaced piece.</returns>
        public bool TryApply(BoardModel board, out HintResult result)
        {
            int piece = BoardModel.NO_PIECE;
            for (int i = 0; i < board.PieceCount; i++)
            {
                if (!board.IsPlaced(i))
                {
                    piece = i;
                    break;
                }
            }
            if (piece == BoardModel.NO_PIECE)
            {
                result = null;
                return false;
            }

            var targets = level.SolutionCellsOf(piece);
            var evicted = new List<int>();
            foreach (var cell in targets)
            {
                int occupant = board.PieceAt(cell);
                if (occupant != BoardModel.NO_PIECE && !evicted.Contains(occupant))
                {
                    evicted.Add(occupant);
                }
            }
            foreach (int id in evicted)
            {
                board.Remove(id);
            }

            Used = true;
            result = new HintResult(piece, level.SolutionShapes[piece], targets, evicted);
            return true;
        }
    }
}
