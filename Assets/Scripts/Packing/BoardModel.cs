// Implements: design/game-brief.md "Core loop", "Điều kiện thắng / thua" (placement and win),
// "Cơ chế chính" (Kéo thả đặt đồ: valid = in bounds, no overlap, not blocked; fill %).
// "Obstacles" + "Điều kiện thắng / thua" (won only when every active rule holds).
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency, so it can be unit-tested.

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// The container grid: blocked cells and which piece occupies each cell.
    /// Cells are (column, row) with row 0 at the top. Pieces are identified by their index in the level's pieces array.
    /// </summary>
    public sealed class BoardModel
    {
        /// <summary>Value returned by <see cref="PieceAt"/> for a cell no piece covers.</summary>
        public const int NO_PIECE = -1;

        readonly struct Placement
        {
            public readonly PieceShape shape;
            public readonly Vector2Int origin;

            public Placement(PieceShape shape, Vector2Int origin)
            {
                this.shape = shape;
                this.origin = origin;
            }
        }

        readonly bool[,] blocked;
        readonly int[,] occupant;
        readonly Dictionary<int, Placement> placements = new Dictionary<int, Placement>();
        readonly IReadOnlyList<IPackingRule> rules;

        /// <summary>Columns in the grid.</summary>
        public int Width { get; }

        /// <summary>Rows in the grid.</summary>
        public int Height { get; }

        /// <summary>Number of pieces the level has; the board is full of pieces when all are placed.</summary>
        public int PieceCount { get; }

        /// <summary>Cells that are not blocked: w·h − blocked.</summary>
        public int FillableCellCount { get; }

        /// <summary>Cells currently covered by a placed piece.</summary>
        public int CoveredCellCount { get; private set; }

        /// <summary>CoveredCellCount / FillableCellCount, in [0, 1].</summary>
        public float FillRatio => (float)CoveredCellCount / FillableCellCount;

        /// <summary>True when every piece is on the board.</summary>
        public bool AllPlaced => placements.Count == PieceCount;

        /// <summary>The level's active obstacle rules (empty when it has none).</summary>
        public IReadOnlyList<IPackingRule> Rules => rules;

        /// <summary>Creates an empty board.</summary>
        /// <param name="width">Columns.</param>
        /// <param name="height">Rows.</param>
        /// <param name="blockedCells">Cells nothing can be placed on.</param>
        /// <param name="pieceCount">Number of pieces in the level.</param>
        /// <param name="rules">Active obstacle rules, evaluated on every placement and by <see cref="IsWon"/>; null = none.</param>
        public BoardModel(int width, int height, IEnumerable<Vector2Int> blockedCells, int pieceCount,
            IReadOnlyList<IPackingRule> rules = null)
        {
            this.rules = rules ?? new IPackingRule[0];
            Width = width;
            Height = height;
            PieceCount = pieceCount;
            blocked = new bool[width, height];
            occupant = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    occupant[x, y] = NO_PIECE;
                }
            }

            int blockedCount = 0;
            foreach (var c in blockedCells)
            {
                if (!blocked[c.x, c.y])
                {
                    blocked[c.x, c.y] = true;
                    blockedCount++;
                }
            }
            FillableCellCount = width * height - blockedCount;
        }

        /// <summary>
        /// Independent copy with the same blocked cells, rules and current placements. Used by
        /// <see cref="AutoPackSolver"/> to search without touching the board the player sees.
        /// </summary>
        public BoardModel Clone()
        {
            var blockedCells = new List<Vector2Int>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (blocked[x, y]) blockedCells.Add(new Vector2Int(x, y));
                }
            }
            var copy = new BoardModel(Width, Height, blockedCells, PieceCount, rules);
            foreach (var entry in placements)
            {
                foreach (var c in entry.Value.shape.Cells)
                {
                    var cell = entry.Value.origin + c;
                    copy.occupant[cell.x, cell.y] = entry.Key;
                }
                copy.placements[entry.Key] = entry.Value;
                copy.CoveredCellCount += entry.Value.shape.Cells.Count;
            }
            return copy;
        }

        /// <summary>True if the cell lies inside the grid.</summary>
        public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        /// <summary>True if the cell is inside the grid and blocked.</summary>
        public bool IsBlocked(Vector2Int cell) => InBounds(cell) && blocked[cell.x, cell.y];

        /// <summary>Index of the piece covering the cell, or <see cref="NO_PIECE"/>.</summary>
        public int PieceAt(Vector2Int cell) => InBounds(cell) ? occupant[cell.x, cell.y] : NO_PIECE;

        /// <summary>True if the piece is on the board.</summary>
        public bool IsPlaced(int pieceId) => placements.ContainsKey(pieceId);

        /// <summary>
        /// Every reason piece <paramref name="pieceId"/> cannot sit with <paramref name="shape"/> at <paramref name="origin"/>:
        /// out of bounds, blocked, covered by another piece, and each active rule's drop-time check.
        /// </summary>
        /// <returns><see cref="PlacementFailure.None"/> if the piece may be placed there.</returns>
        public PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin)
        {
            var failure = PlacementFailure.None;
            foreach (var c in shape.Cells)
            {
                var cell = origin + c;
                if (!InBounds(cell))
                {
                    failure |= PlacementFailure.OutOfBounds;
                }
                else if (blocked[cell.x, cell.y])
                {
                    failure |= PlacementFailure.Blocked;
                }
                else if (occupant[cell.x, cell.y] != NO_PIECE && occupant[cell.x, cell.y] != pieceId)
                {
                    failure |= PlacementFailure.Occupied;
                }
            }
            foreach (var rule in rules)
            {
                failure |= rule.CheckPlacement(pieceId, shape, origin);
                if (rule is IBoardAwareRule aware)
                {
                    failure |= aware.CheckPlacement(this, pieceId, shape, origin);
                }
            }
            return failure;
        }

        /// <summary>True if <see cref="CheckPlacement"/> finds nothing wrong.</summary>
        public bool CanPlace(int pieceId, PieceShape shape, Vector2Int origin) =>
            CheckPlacement(pieceId, shape, origin) == PlacementFailure.None;

        /// <summary>True unless an active rule (upright) forbids turning the piece.</summary>
        public bool CanRotate(int pieceId)
        {
            foreach (var rule in rules)
            {
                if (!rule.AllowsRotation(pieceId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Shape and origin of a placed piece.</summary>
        /// <returns>False if the piece is not on the board.</returns>
        public bool TryGetPlacement(int pieceId, out PieceShape shape, out Vector2Int origin)
        {
            if (placements.TryGetValue(pieceId, out var placement))
            {
                shape = placement.shape;
                origin = placement.origin;
                return true;
            }
            shape = null;
            origin = default;
            return false;
        }

        /// <summary>Places the piece if <see cref="CanPlace"/> allows it. The piece must not already be placed.</summary>
        /// <returns>True if the piece was placed.</returns>
        public bool Place(int pieceId, PieceShape shape, Vector2Int origin)
        {
            if (placements.ContainsKey(pieceId) || !CanPlace(pieceId, shape, origin))
            {
                return false;
            }
            foreach (var c in shape.Cells)
            {
                var cell = origin + c;
                occupant[cell.x, cell.y] = pieceId;
            }
            placements[pieceId] = new Placement(shape, origin);
            CoveredCellCount += shape.Cells.Count;
            return true;
        }

        /// <summary>Takes the piece off the board. Does nothing if it is not placed.</summary>
        public void Remove(int pieceId)
        {
            if (!placements.TryGetValue(pieceId, out var placement))
            {
                return;
            }
            foreach (var c in placement.shape.Cells)
            {
                var cell = placement.origin + c;
                occupant[cell.x, cell.y] = NO_PIECE;
            }
            placements.Remove(pieceId);
            CoveredCellCount -= placement.shape.Cells.Count;
        }

        /// <summary>
        /// The first active rule a full board breaks (pairs not touching), or <see cref="RuleViolation.NONE"/>.
        /// Only meaningful once <see cref="AllPlaced"/> is true.
        /// </summary>
        public RuleViolation FindCompletionViolation()
        {
            foreach (var rule in rules)
            {
                int piece = rule.FindCompletionViolation(this);
                if (piece != NO_PIECE)
                {
                    return new RuleViolation(rule, piece);
                }
            }
            return RuleViolation.NONE;
        }

        /// <summary>
        /// True when the level is won: every piece is placed (so every fillable cell is covered — the pieces exactly
        /// tile the fillable cells) and every active rule holds.
        /// </summary>
        public bool IsWon() => AllPlaced && !FindCompletionViolation().IsViolation;
    }
}
