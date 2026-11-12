// Implements: design/game-brief.md "Cơ chế chính" (Xoay đồ: 90° per tap).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md
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
    }
}
