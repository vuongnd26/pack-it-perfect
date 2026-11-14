// Implements: design/game-brief.md, approved mobile readability plan (fixed two-row tray pages).
// Pure geometry: no scene, camera, placement state, history or save dependency.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Immutable, data-order square home slots in a shared two-row viewport. Rotating or placing an item never changes
    /// its home slot or page. Additional items create pages, not a taller viewport or a smaller catalog-wide scale.
    /// </summary>
    public sealed class TrayPagination
    {
        /// <summary>Number of fixed row tracks visible on every page.</summary>
        public const int ROWS_PER_PAGE = 2;

        readonly Rect[] slots;
        readonly int[] pages;
        readonly int[] rows;

        /// <summary>Number of permanent home slots.</summary>
        public int Count => slots.Length;

        /// <summary>Number of pages; zero for an empty catalog.</summary>
        public int PageCount { get; }

        /// <summary>Requested tray scale, reduced only when necessary to fit the single largest square within the width.</summary>
        public float Scale { get; }

        /// <summary>World rectangle shared by every page, always exactly the supplied width.</summary>
        public Rect Viewport { get; }

        /// <summary>
        /// Builds slots from immutable maximum shape dimensions. Each row is as tall as its tallest home square and the
        /// viewport as tall as the tallest page, so page changes cannot change the viewport height or the camera fit.
        /// </summary>
        /// <param name="rowsPerPage">Rows on one page; zero or less puts every row on a single page.</param>
        /// <param name="sortBySize">True lays the biggest home squares out first (rows of similar height waste less
        /// room); false keeps data order. Either way a piece keeps its slot for the whole level.</param>
        public TrayPagination(IReadOnlyList<PieceShape> shapes, float top, float width, float requestedScale, float spacing,
            int rowsPerPage = ROWS_PER_PAGE, bool sortBySize = false)
        {
            if (shapes == null) throw new ArgumentNullException(nameof(shapes));
            if (!IsFinite(top)) throw new ArgumentOutOfRangeException(nameof(top));
            if (!IsFinite(width) || width <= 0f) throw new ArgumentOutOfRangeException(nameof(width));
            if (!IsFinite(requestedScale) || requestedScale <= 0f) throw new ArgumentOutOfRangeException(nameof(requestedScale));
            if (!IsFinite(spacing) || spacing < 0f) throw new ArgumentOutOfRangeException(nameof(spacing));

            slots = new Rect[shapes.Count];
            pages = new int[shapes.Count];
            rows = new int[shapes.Count];
            int largestDimension = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i] == null || shapes[i].MaxDimension <= 0)
                    throw new ArgumentException("Tray shapes must have a nonempty footprint.", nameof(shapes));
                largestDimension = Mathf.Max(largestDimension, shapes[i].MaxDimension);
            }
            int perPage = rowsPerPage > 0 ? rowsPerPage : int.MaxValue;
            // Display order: data order, or biggest first (stable, so equal sizes keep data order).
            var order = new int[shapes.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            if (sortBySize)
            {
                for (int i = 1; i < order.Length; i++)
                {
                    int moved = order[i];
                    int j = i - 1;
                    while (j >= 0 && shapes[order[j]].MaxDimension < shapes[moved].MaxDimension)
                    {
                        order[j + 1] = order[j];
                        j--;
                    }
                    order[j + 1] = moved;
                }
            }
            Scale = largestDimension > 0 ? Mathf.Min(requestedScale, width / largestDimension) : requestedScale;
            // Pass 1: fill rows in data order and remember each row's own height (its tallest home square).
            var rowStart = new List<int>();
            var rowHeights = new List<float>();
            int next = 0;
            while (next < shapes.Count)
            {
                rowStart.Add(next);
                float rowWidth = shapes[order[next]].MaxDimension * Scale;
                float tallest = rowWidth;
                next++;
                while (next < shapes.Count)
                {
                    float side = shapes[order[next]].MaxDimension * Scale;
                    if (rowWidth + spacing + side > width) break;
                    rowWidth += spacing + side;
                    tallest = Mathf.Max(tallest, side);
                    next++;
                }
                rowHeights.Add(tallest);
            }
            int rowIndex = rowHeights.Count;

            // The shared viewport is as tall as the tallest page, so no page ever needs more room than another.
            float height = 0f;
            for (int row = 0; row < rowIndex; row += Mathf.Min(perPage, rowIndex))
            {
                float pageHeight = rowHeights[row];
                for (int track = 1; track < perPage && row + track < rowIndex; track++)
                    pageHeight += spacing + rowHeights[row + track];
                height = Mathf.Max(height, pageHeight);
            }
            Viewport = new Rect(-width / 2f, top - height, width, height);

            // Pass 2: place every slot; a page's rows stack from the viewport top.
            float y = top;
            for (int row = 0; row < rowIndex; row++)
            {
                int track = row % perPage;
                if (track == 0) y = top;
                int first = rowStart[row];
                int end = row + 1 < rowIndex ? rowStart[row + 1] : shapes.Count;
                float rowWidth = -spacing;
                for (int i = first; i < end; i++) rowWidth += spacing + shapes[order[i]].MaxDimension * Scale;
                float x = -rowWidth / 2f;
                float centerY = y - rowHeights[row] / 2f;
                for (int at = first; at < end; at++)
                {
                    int i = order[at];
                    float side = shapes[i].MaxDimension * Scale;
                    slots[i] = new Rect(x, centerY - side / 2f, side, side);
                    pages[i] = row / perPage;
                    rows[i] = track;
                    x += side + spacing;
                }
                y -= rowHeights[row] + spacing;
            }
            PageCount = rowIndex == 0 ? 0 : (rowIndex - 1) / perPage + 1;
        }

        /// <summary>
        /// Largest scale from <paramref name="maxScale"/> down to <paramref name="minScale"/> at which every slot fits on
        /// one page no taller than <paramref name="maxHeight"/>; <paramref name="minScale"/> when none does.
        /// </summary>
        public static float FitScale(IReadOnlyList<PieceShape> shapes, float width, float minScale, float maxScale,
            float spacing, float maxHeight, bool sortBySize)
        {
            const float STEP = 0.02f;
            if (shapes == null || shapes.Count == 0 || maxScale <= minScale) return Mathf.Max(minScale, 0.01f);
            for (float scale = maxScale; scale > minScale; scale -= STEP)
            {
                var trial = new TrayPagination(shapes, 0f, width, scale, spacing, 0, sortBySize);
                if (trial.Viewport.height <= maxHeight) return trial.Scale;
            }
            return minScale;
        }

        /// <summary>Returns a piece's square world-space home slot; throws for an invalid piece index.</summary>
        public Rect SlotFor(int pieceIndex) => slots[pieceIndex];

        /// <summary>Returns a stable zero-based home page, or -1 for an invalid piece index.</summary>
        public int PageFor(int pieceIndex) => pieceIndex >= 0 && pieceIndex < pages.Length ? pages[pieceIndex] : -1;

        /// <summary>Returns the zero-based row track within a page, or -1 for an invalid piece index.</summary>
        public int RowFor(int pieceIndex) => pieceIndex >= 0 && pieceIndex < rows.Length ? rows[pieceIndex] : -1;

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
