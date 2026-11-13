// Implements: design/game-brief.md "Nâng cấp Cozy Travel" (Auto Pack: place one item only where the rest of the
// level can still be completed, keeping every item the player already placed).
// Pure C#: no MonoBehaviour dependency. The search runs in bounded steps so the caller never blocks a frame.

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Outcome of an <see cref="AutoPackSolver"/> search.</summary>
    public enum SolveStatus
    {
        /// <summary>The node budget of this step ran out; call <see cref="AutoPackSolver.Step"/> again.</summary>
        Searching,
        /// <summary>A complete, rule-abiding packing was found that keeps every current placement.</summary>
        Solved,
        /// <summary>Proven: the current placements cannot be completed.</summary>
        Impossible,
        /// <summary>The total node limit was reached without an answer (treated as "no answer", never as impossible).</summary>
        GaveUp,
    }

    /// <summary>One piece position in a solution.</summary>
    public readonly struct SolvedPlacement
    {
        /// <summary>Orientation of the piece.</summary>
        public readonly PieceShape shape;
        /// <summary>Grid cell of the shape's top-left corner.</summary>
        public readonly Vector2Int origin;
        /// <summary>Clockwise quarter turns from the level's reference orientation to <see cref="shape"/>.</summary>
        public readonly int turnsFromSolution;

        /// <summary>Creates a placement.</summary>
        public SolvedPlacement(PieceShape shape, Vector2Int origin, int turnsFromSolution)
        {
            this.shape = shape;
            this.origin = origin;
            this.turnsFromSolution = turnsFromSolution;
        }
    }

    /// <summary>
    /// Depth-first exact-cover search over the unplaced pieces of a level. Every candidate goes through
    /// <see cref="BoardModel.CheckPlacement"/> on a private copy of the board, so geometry, blocked cells, zones, heavy,
    /// fragile and upright are exactly the rules the player faces; completion rules (pairs) are checked at each full board.
    /// The level's reference placement of a piece is tried first, so an untouched or on-track board is solved at once.
    /// </summary>
    public sealed class AutoPackSolver
    {
        struct Candidate
        {
            public PieceShape shape;
            public int turns;
            public Vector2Int anchor; // the shape's first cell in reading order
        }

        struct Frame
        {
            public Vector2Int cell;   // empty cell this depth must cover
            public int preferred;     // index into unplaced of the piece whose reference solution covers the cell, or -1
            public int piece;         // position in this frame's trial order (preferred piece first)
            public int candidate;     // next candidate of that piece
            public int placedPiece;   // piece currently placed by this frame, or NO_PIECE
        }

        readonly BoardModel board;
        readonly LoadedLevel level;
        readonly List<int> unplaced = new List<int>();
        readonly Dictionary<int, List<Candidate>> candidates = new Dictionary<int, List<Candidate>>();
        readonly Dictionary<int, SolvedPlacement> solution = new Dictionary<int, SolvedPlacement>();
        readonly Stack<Frame> stack = new Stack<Frame>();
        readonly long nodeLimit;
        long nodes;
        bool started;

        /// <summary>Current state of the search.</summary>
        public SolveStatus Status { get; private set; } = SolveStatus.Searching;

        /// <summary>Placements tried so far (for tests and tuning).</summary>
        public long NodesVisited => nodes;

        /// <summary>After <see cref="SolveStatus.Solved"/>: where each previously unplaced piece goes.</summary>
        public IReadOnlyDictionary<int, SolvedPlacement> Solution => solution;

        /// <summary>Prepares a search from the current state of <paramref name="source"/> (which is copied, never changed).</summary>
        /// <param name="level">The level, for piece shapes and the reference solution.</param>
        /// <param name="source">The board as the player has it now.</param>
        /// <param name="nodeLimit">Total placements to try before giving up.</param>
        public AutoPackSolver(LoadedLevel level, BoardModel source, long nodeLimit)
        {
            this.level = level;
            this.nodeLimit = nodeLimit;
            board = source.Clone();
            for (int i = 0; i < board.PieceCount; i++)
            {
                if (!board.IsPlaced(i))
                {
                    unplaced.Add(i);
                    candidates[i] = BuildCandidates(i);
                }
            }
        }

        // Unique orientations of a piece, reference orientation first. Pieces a rule pins (upright) keep only that one.
        List<Candidate> BuildCandidates(int piece)
        {
            var list = new List<Candidate>();
            var reference = level.SolutionShapes[piece];
            int turnCount = board.CanRotate(piece) ? 4 : 1;
            for (int turns = 0; turns < turnCount; turns++)
            {
                var shape = reference.Rotated(turns);
                bool duplicate = false;
                foreach (var existing in list)
                {
                    duplicate |= existing.shape.SameCells(shape);
                }
                if (!duplicate)
                {
                    list.Add(new Candidate { shape = shape, turns = turns, anchor = shape.FirstCell });
                }
            }
            return list;
        }

        /// <summary>Runs up to <paramref name="nodeBudget"/> more placements and returns the status.</summary>
        public SolveStatus Step(int nodeBudget)
        {
            if (Status != SolveStatus.Searching)
            {
                return Status;
            }
            if (!started)
            {
                started = true;
                if (!PushFrame())
                {
                    return Status;
                }
            }

            long stopAt = nodes + nodeBudget;
            while (stack.Count > 0)
            {
                if (nodes >= nodeLimit)
                {
                    return Status = SolveStatus.GaveUp;
                }
                if (nodes >= stopAt)
                {
                    return SolveStatus.Searching;
                }

                var frame = stack.Pop();
                if (frame.placedPiece != BoardModel.NO_PIECE)
                {
                    board.Remove(frame.placedPiece);
                    solution.Remove(frame.placedPiece);
                    frame.placedPiece = BoardModel.NO_PIECE;
                }

                bool placed = false;
                while (frame.piece < unplaced.Count && !placed)
                {
                    int id = unplaced[OrderedIndex(frame.preferred, frame.piece)];
                    var options = candidates[id];
                    if (board.IsPlaced(id) || frame.candidate >= options.Count)
                    {
                        frame.piece++;
                        frame.candidate = 0;
                        continue;
                    }
                    // Failed candidates also consume work. Preserve the next candidate before yielding,
                    // otherwise a single frame can overshoot both budgets while rejecting placements.
                    if (nodes >= nodeLimit)
                    {
                        stack.Push(frame);
                        return Status = SolveStatus.GaveUp;
                    }
                    if (nodes >= stopAt)
                    {
                        stack.Push(frame);
                        return SolveStatus.Searching;
                    }
                    var option = options[frame.candidate++];
                    // The first empty cell in reading order can only be covered by a shape's first cell in reading order.
                    var origin = frame.cell - option.anchor;
                    nodes++;
                    if (board.Place(id, option.shape, origin))
                    {
                        if (SplitsPair(id))
                        {
                            board.Remove(id); // both halves of a pair are down and apart: no completion can fix that
                            continue;
                        }
                        solution[id] = new SolvedPlacement(option.shape, origin, option.turns);
                        frame.placedPiece = id;
                        placed = true;
                    }
                }

                if (!placed)
                {
                    continue; // this depth is exhausted: backtrack
                }
                stack.Push(frame);
                if (!PushFrame())
                {
                    if (Status == SolveStatus.Solved)
                    {
                        return Status;
                    }
                    Status = SolveStatus.Searching; // dead end below: keep trying siblings of the frame just pushed
                }
            }
            return Status = SolveStatus.Impossible;
        }

        // Opens the next depth. Returns false when there is nothing to open: either the board is complete
        // (Status = Solved when the completion rules hold) or the position is a dead end.
        bool PushFrame()
        {
            if (!TryFirstEmptyCell(out var cell))
            {
                if (board.AllPlaced && !board.FindCompletionViolation().IsViolation)
                {
                    Status = SolveStatus.Solved;
                }
                else if (stack.Count == 0)
                {
                    Status = SolveStatus.Impossible;
                }
                return false;
            }
            if (board.AllPlaced)
            {
                // Empty cells left but no piece to fill them (cannot happen with validated data).
                if (stack.Count == 0) Status = SolveStatus.Impossible;
                return false;
            }
            int preferred = -1;
            for (int i = 0; i < unplaced.Count && preferred < 0; i++)
            {
                int id = unplaced[i];
                if (!board.IsPlaced(id) && level.SolutionShapes[id].Contains(cell - level.SolutionOrigins[id]))
                {
                    preferred = i;
                }
            }
            stack.Push(new Frame { cell = cell, preferred = preferred, piece = 0, candidate = 0, placedPiece = BoardModel.NO_PIECE });
            return true;
        }

        // Trial order of a frame: the preferred piece first, then the others in data order.
        static int OrderedIndex(int preferred, int position)
        {
            if (preferred < 0) return position;
            if (position == 0) return preferred;
            return position <= preferred ? position - 1 : position;
        }

        bool SplitsPair(int pieceId)
        {
            var pairs = level.Rules.Pairs;
            if (pairs == null) return false;
            int partner = pairs.PartnerOf(pieceId);
            return partner != BoardModel.NO_PIECE && board.IsPlaced(partner) && !pairs.TouchesPartner(board, pieceId);
        }

        bool TryFirstEmptyCell(out Vector2Int cell)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    cell = new Vector2Int(x, y);
                    if (!board.IsBlocked(cell) && board.PieceAt(cell) == BoardModel.NO_PIECE)
                    {
                        return true;
                    }
                }
            }
            cell = default;
            return false;
        }

        /// <summary>Runs the whole search at once (tests and tools; the game steps it across frames).</summary>
        public SolveStatus Run()
        {
            while (Step(int.MaxValue) == SolveStatus.Searching)
            {
            }
            return Status;
        }
    }
}
