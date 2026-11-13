// Implements: design/game-brief.md "Obstacles" and "Điều kiện thắng / thua" (won only when every active rule holds).
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency, so every rule can be unit-tested.

using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Why a piece cannot sit at a position. Flags: one drop can break several checks at once.</summary>
    [Flags]
    public enum PlacementFailure
    {
        /// <summary>The piece may be placed.</summary>
        None = 0,
        /// <summary>A cell is outside the grid.</summary>
        OutOfBounds = 1 << 0,
        /// <summary>A cell is blocked ('#').</summary>
        Blocked = 1 << 1,
        /// <summary>A cell is covered by another piece.</summary>
        Occupied = 1 << 2,
        /// <summary>A cell lies outside the piece's zone (<see cref="ZoneRule"/>).</summary>
        Zone = 1 << 3,
        /// <summary>A heavy piece does not touch the bottom row (<see cref="HeavyRule"/>).</summary>
        Heavy = 1 << 4,
        /// <summary>A fragile piece would share a cell edge with a heavy piece (<see cref="FragileRule"/>).</summary>
        Fragile = 1 << 5,
    }

    /// <summary>
    /// One obstacle rule of a level. A rule is only created when the level's data uses it
    /// (see <see cref="LevelRules.FromLevel"/>), so rule code never checks level numbers.
    /// Rules are evaluated by <see cref="BoardModel"/>.
    /// </summary>
    public interface IPackingRule
    {
        /// <summary>
        /// Drop-time check. Returns the failure flag this rule adds when piece <paramref name="pieceId"/>
        /// may not sit with <paramref name="shape"/> at <paramref name="origin"/>, else <see cref="PlacementFailure.None"/>.
        /// </summary>
        PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin);

        /// <summary>
        /// Completion check, run once every piece is placed. Returns the index of the first piece
        /// (in data order) that breaks the rule, or <see cref="BoardModel.NO_PIECE"/>.
        /// </summary>
        int FindCompletionViolation(BoardModel board);

        /// <summary>False if the piece may not be turned in the tray.</summary>
        bool AllowsRotation(int pieceId);
    }

    /// <summary>
    /// A rule whose drop-time check depends on the pieces already on the board (e.g. <see cref="FragileRule"/>).
    /// <see cref="BoardModel.CheckPlacement"/> calls this in addition to <see cref="IPackingRule.CheckPlacement"/>.
    /// The check must be symmetric: a set of placements is legal or illegal whatever order they were made in.
    /// </summary>
    public interface IBoardAwareRule
    {
        /// <summary>Drop-time check against the current occupants of <paramref name="board"/> (the piece itself is ignored).</summary>
        PlacementFailure CheckPlacement(BoardModel board, int pieceId, PieceShape shape, Vector2Int origin);
    }

    /// <summary>Result of <see cref="BoardModel.FindCompletionViolation"/>.</summary>
    public readonly struct RuleViolation
    {
        /// <summary>No rule is broken.</summary>
        public static readonly RuleViolation NONE = new RuleViolation(null, BoardModel.NO_PIECE);

        /// <summary>The broken rule, or null.</summary>
        public readonly IPackingRule rule;

        /// <summary>First piece breaking it, or <see cref="BoardModel.NO_PIECE"/>.</summary>
        public readonly int pieceId;

        /// <summary>True if a rule is broken.</summary>
        public bool IsViolation => rule != null;

        /// <summary>Creates a result.</summary>
        public RuleViolation(IPackingRule rule, int pieceId)
        {
            this.rule = rule;
            this.pieceId = pieceId;
        }
    }
}
