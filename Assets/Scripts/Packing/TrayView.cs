// Implements: design/game-brief.md "Core loop" (Chọn và xoay: pick an item from the tray).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Lays out one square home slot per piece below the grid, in centred rows that wrap at the tray width.
    /// A slot's side is the piece's larger dimension × tray scale, so rotating a piece never changes the layout.
    /// A piece keeps its slot while placed; the slot is empty until it returns.
    /// </summary>
    public sealed class TrayView
    {
        readonly Rect[] slots;
        readonly float scale;

        /// <summary>World rectangle the tray occupies.</summary>
        public Rect Bounds { get; }

        /// <summary>Lays out slots for <paramref name="shapes"/> starting at world y <paramref name="top"/>.</summary>
        public TrayView(IReadOnlyList<PieceShape> shapes, float top, float width, PackingTuning tuning)
        {
            scale = tuning.trayScale;
            float spacing = tuning.traySlotSpacing;
            slots = new Rect[shapes.Count];

            var rows = new List<List<int>>();
            var row = new List<int>();
            float rowWidth = 0f;
            for (int i = 0; i < shapes.Count; i++)
            {
                float size = shapes[i].MaxDimension * scale;
                if (row.Count > 0 && rowWidth + spacing + size > width)
                {
                    rows.Add(row);
                    row = new List<int>();
                    rowWidth = 0f;
                }
                rowWidth += (row.Count > 0 ? spacing : 0f) + size;
                row.Add(i);
            }
            rows.Add(row);

            float y = top;
            foreach (var r in rows)
            {
                float rowHeight = 0f;
                float total = spacing * (r.Count - 1);
                foreach (int i in r)
                {
                    float size = shapes[i].MaxDimension * scale;
                    rowHeight = Mathf.Max(rowHeight, size);
                    total += size;
                }
                float x = -total / 2f;
                foreach (int i in r)
                {
                    float size = shapes[i].MaxDimension * scale;
                    slots[i] = new Rect(x, y - rowHeight / 2f - size / 2f, size, size);
                    x += size + spacing;
                }
                y -= rowHeight + spacing;
            }
            float bottom = y + spacing;
            Bounds = new Rect(-width / 2f, bottom, width, top - bottom);
        }

        /// <summary>World top-left corner for <paramref name="shape"/> centred in slot <paramref name="pieceIndex"/> at tray scale.</summary>
        public Vector3 TopLeftFor(int pieceIndex, PieceShape shape)
        {
            var center = slots[pieceIndex].center;
            return new Vector3(center.x - shape.Width * scale / 2f, center.y + shape.Height * scale / 2f, 0f);
        }

        /// <summary>Index of the slot under the world point whose piece is in the tray, or -1.</summary>
        public int HitTest(Vector2 world, Func<int, bool> isInTray)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (isInTray(i) && slots[i].Contains(world))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
