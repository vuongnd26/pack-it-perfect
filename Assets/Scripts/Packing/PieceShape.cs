// Implements: design/game-brief.md "Cơ chế chính" (Xoay đồ: 90° per tap).
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md,
// story-004-outline-imprints.md / story-005-hint-and-stars.md (SameCells: "placed correctly", solution orientation).
// Pure C#: no MonoBehaviour dependency.

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Immutable set of cells forming a piece, normalised so the minimum x and y are 0.
    /// Coordinates are (column, row) with row 0 at the top, matching the level map.
    /// </summary>
    public sealed class PieceShape
    {
        readonly Vector2Int[] cells;
        readonly HashSet<Vector2Int> cellSet;

        /// <summary>The cells, each in [0, Width) x [0, Height).</summary>
        public IReadOnlyList<Vector2Int> Cells => cells;

        /// <summary>Number of columns the shape spans.</summary>
        public int Width { get; }

        /// <summary>Number of rows the shape spans.</summary>
        public int Height { get; }

        /// <summary>The larger of Width and Height; unchanged by rotation.</summary>
        public int MaxDimension => Mathf.Max(Width, Height);

        PieceShape(Vector2Int[] normalisedCells, int width, int height)
        {
            cells = normalisedCells;
            cellSet = new HashSet<Vector2Int>(normalisedCells);
            Width = width;
            Height = height;
        }

        /// <summary>Builds a shape from cells anywhere on the plane, shifting them so min x and min y are 0.</summary>
        public static PieceShape FromCells(IEnumerable<Vector2Int> sourceCells)
        {
            var list = new List<Vector2Int>(sourceCells);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in list)
            {
                minX = Mathf.Min(minX, c.x);
                minY = Mathf.Min(minY, c.y);
                maxX = Mathf.Max(maxX, c.x);
                maxY = Mathf.Max(maxY, c.y);
            }

            var normalised = new Vector2Int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                normalised[i] = new Vector2Int(list[i].x - minX, list[i].y - minY);
            }
            return new PieceShape(normalised, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Returns this shape turned 90° clockwise on screen <paramref name="quarterTurns"/> times:
        /// each turn maps (x, y) to (-y, x), then normalises.
        /// </summary>
        public PieceShape Rotated(int quarterTurns = 1)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            var result = this;
            for (int t = 0; t < turns; t++)
            {
                var turned = new Vector2Int[result.cells.Length];
                for (int i = 0; i < turned.Length; i++)
                {
                    turned[i] = new Vector2Int(-result.cells[i].y, result.cells[i].x);
                }
                result = FromCells(turned);
            }
            return result;
        }

        /// <summary>True if the shape has a cell at <paramref name="cell"/>.</summary>
        public bool Contains(Vector2Int cell) => cellSet.Contains(cell);

        /// <summary>True if <paramref name="other"/> covers exactly the same normalised cells (same orientation).</summary>
        public bool SameCells(PieceShape other)
        {
            if (other == null || other.Width != Width || other.Height != Height || other.cells.Length != cells.Length)
            {
                return false;
            }
            foreach (var c in other.cells)
            {
                if (!cellSet.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// First cell in reading order (top row first, then leftmost). Rule badges sit on this cell.
        /// </summary>
        public Vector2Int FirstCell
        {
            get
            {
                var first = cells[0];
                foreach (var c in cells)
                {
                    if (c.y < first.y || (c.y == first.y && c.x < first.x))
                    {
                        first = c;
                    }
                }
                return first;
            }
        }

        /// <summary>
        /// Where the item icon goes, in shape units (x right, y down from the top-left corner):
        /// the centre of a 2x2 block of cells (or between two adjacent 2x2 blocks) nearest the shape's centroid
        /// if the shape has one, else the centre of the cell with the most neighbours (ties: nearest the centroid).
        /// Recomputed per orientation, so the icon stays upright while the cell layout turns.
        /// </summary>
        /// <param name="onBlock">True if the point is on a 2x2 block (room for a larger icon).</param>
        public Vector2 IconAnchor(out bool onBlock)
        {
            var centroid = Vector2.zero;
            foreach (var c in cells)
            {
                centroid += new Vector2(c.x + 0.5f, c.y + 0.5f);
            }
            centroid /= cells.Length;

            var right = new Vector2Int(1, 0);
            var down = new Vector2Int(0, 1);
            var blocks = new List<Vector2Int>();
            foreach (var c in cells)
            {
                if (cellSet.Contains(c + right) && cellSet.Contains(c + down) && cellSet.Contains(c + right + down))
                {
                    blocks.Add(c);
                }
            }

            onBlock = blocks.Count > 0;
            var best = Vector2.zero;
            float bestDistance = float.MaxValue;
            if (onBlock)
            {
                var blockSet = new HashSet<Vector2Int>(blocks);
                foreach (var b in blocks)
                {
                    // The block's own centre, plus the midpoint to a block one cell right or down (a 3x2 / 2x3 area).
                    Consider(new Vector2(b.x + 1f, b.y + 1f), centroid, ref best, ref bestDistance);
                    if (blockSet.Contains(b + right))
                    {
                        Consider(new Vector2(b.x + 1.5f, b.y + 1f), centroid, ref best, ref bestDistance);
                    }
                    if (blockSet.Contains(b + down))
                    {
                        Consider(new Vector2(b.x + 1f, b.y + 1.5f), centroid, ref best, ref bestDistance);
                    }
                }
                return best;
            }

            int mostNeighbours = -1;
            foreach (var c in cells)
            {
                int neighbours = 0;
                if (cellSet.Contains(c + right)) neighbours++;
                if (cellSet.Contains(c - right)) neighbours++;
                if (cellSet.Contains(c + down)) neighbours++;
                if (cellSet.Contains(c - down)) neighbours++;
                var center = new Vector2(c.x + 0.5f, c.y + 0.5f);
                float distance = (center - centroid).sqrMagnitude;
                if (neighbours > mostNeighbours || (neighbours == mostNeighbours && distance < bestDistance))
                {
                    mostNeighbours = neighbours;
                    bestDistance = distance;
                    best = center;
                }
            }
            return best;
        }

        static void Consider(Vector2 point, Vector2 centroid, ref Vector2 best, ref float bestDistance)
        {
            float distance = (point - centroid).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = point;
            }
        }
    }
}
