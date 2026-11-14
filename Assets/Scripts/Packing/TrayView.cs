// Implements: design/game-brief.md "Core loop" and the approved mobile readability plan.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Presents stable square home slots in a fixed two-row viewport. Membership and scale are established on load;
    /// placed items leave empty slots and hidden pages never participate in pointer hit testing.
    /// </summary>
    public sealed class TrayView
    {
        readonly TrayPagination pagination;

        /// <summary>World rectangle occupied by the visible viewport, not by all catalog pages.</summary>
        public Rect Bounds => pagination.Viewport;

        /// <summary>Effective resting scale, capped only to make the largest item fit within the frame width.</summary>
        public float Scale => pagination.Scale;

        /// <summary>Zero-based currently displayed page.</summary>
        public int Page { get; private set; }

        /// <summary>Number of stable pages, zero for an empty tray.</summary>
        public int PageCount => pagination.PageCount;

        /// <summary>Lays out permanent slots at the supplied frame width, starting at world y <paramref name="top"/>.</summary>
        /// <param name="scale">Resting scale to lay out at (see <see cref="TrayPagination.FitScale"/>).</param>
        public TrayView(IReadOnlyList<PieceShape> shapes, float top, float width, PackingTuning tuning, float scale = 0f)
        {
            if (scale <= 0f) scale = tuning.trayScale;
            pagination = new TrayPagination(shapes, top, width, scale, tuning.traySlotSpacing, tuning.trayRowsPerPage,
                tuning.traySortBySize);
        }

        /// <summary>Stable zero-based home page for a piece, or -1 when the piece index is invalid.</summary>
        public int PageFor(int pieceIndex) => pagination.PageFor(pieceIndex);

        /// <summary>True when the piece belongs to the currently displayed page.</summary>
        public bool IsOnVisiblePage(int pieceIndex) => PageFor(pieceIndex) == Page && PageCount > 0;

        /// <summary>Changes pages without changing slots; returns false for an invalid page or the current page.</summary>
        public bool SetPage(int page)
        {
            if (page < 0 || page >= PageCount || page == Page) return false;
            Page = page;
            return true;
        }

        /// <summary>World top-left for a rotated shape centered in its permanent square home slot.</summary>
        public Vector3 TopLeftFor(int pieceIndex, PieceShape shape)
        {
            var center = pagination.SlotFor(pieceIndex).center;
            return new Vector3(center.x - shape.Width * Scale / 2f, center.y + shape.Height * Scale / 2f, 0f);
        }

        /// <summary>Nearest visible available slot within a bounded margin; exact hits win and ties keep data order.</summary>
        public int HitTestExpanded(Vector2 world, Func<int, bool> isInTray, float margin)
        {
            int exact = HitTest(world, isInTray);
            if (exact >= 0) return exact;
            if (margin <= 0f) return -1;
            var allowed = new Rect(Bounds.xMin - margin, Bounds.yMin - margin,
                Bounds.width + margin * 2f, Bounds.height + margin * 2f);
            if (!allowed.Contains(world)) return -1;
            int nearest = -1;
            float distance = margin * margin;
            for (int i = 0; i < pagination.Count; i++)
            {
                if (!IsOnVisiblePage(i) || !isInTray(i)) continue;
                Rect slot = pagination.SlotFor(i);
                var closest = new Vector2(Mathf.Clamp(world.x, slot.xMin, slot.xMax),
                    Mathf.Clamp(world.y, slot.yMin, slot.yMax));
                float squared = (closest - world).sqrMagnitude;
                if (squared < distance) { distance = squared; nearest = i; }
            }
            return nearest;
        }

        /// <summary>Index of the visible, unplaced piece whose home slot contains the point, or -1.</summary>
        public int HitTest(Vector2 world, Func<int, bool> isInTray)
        {
            for (int i = 0; i < pagination.Count; i++)
            {
                if (IsOnVisiblePage(i) && isInTray(i) && pagination.SlotFor(i).Contains(world)) return i;
            }
            return -1;
        }
    }
}
