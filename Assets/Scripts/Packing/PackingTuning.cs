// Tuning values for story 001 (production/epics/pack-it-perfect/story-001-core-packing-loop.md).
// Values marked "prototype" come from the behaviour reference (claude.ai artifact PskwEekJTZ85nAVkZywy4j);
// the rest are first-pass layout and feel values. All lengths are world units, 1 unit = 1 grid cell.

using System;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Every tunable value of the packing screen, edited on the PackingGame component.</summary>
    [Serializable]
    public class PackingTuning
    {
        [Header("Input")]
        [Tooltip("Pointer travel in screen pixels before a press becomes a drag; less is a tap. Prototype: 7.")]
        public float dragThresholdPixels = 7f;

        [Tooltip("The ghost hides when the pointer is further than this (cells) outside the grid.")]
        public float ghostHideDistance = 1f;

        [Header("Pieces")]
        [Tooltip("Scale of pieces resting in the tray. Prototype: 0.6.")]
        public float trayScale = 0.6f;

        [Tooltip("Gap kept on each side of a piece cell, in cells, so cells read as a grid inside the piece.")]
        public float pieceCellInset = 0.06f;

        [Tooltip("Ghost colour where the piece can be placed. Prototype: #3FB68B at 50%.")]
        public Color ghostValidColor = new Color(0x3F / 255f, 0xB6 / 255f, 0x8B / 255f, 0.5f);

        [Tooltip("Ghost colour where the piece cannot be placed. Prototype: #E5484D at 50%.")]
        public Color ghostInvalidColor = new Color(0xE5 / 255f, 0x48 / 255f, 0x4D / 255f, 0.5f);

        [Header("Invalid drop shake")]
        [Tooltip("Seconds the returned piece shakes.")]
        public float shakeDuration = 0.35f;

        [Tooltip("Peak horizontal offset of the shake, in cells.")]
        public float shakeAmplitude = 0.15f;

        [Tooltip("Full left-right oscillations during the shake.")]
        public float shakeCycles = 3f;

        [Header("Grid")]
        [Tooltip("Width of the theme-coloured frame around the grid, in cells.")]
        public float framePadding = 0.35f;

        [Tooltip("Thickness of the grid lines drawn in the theme line colour, in cells.")]
        public float gridLineWidth = 0.04f;

        [Tooltip("Inset of each blocked cell inside its cell, in cells.")]
        public float blockedCellInset = 0.04f;

        [Header("Fill bar")]
        [Tooltip("Height of the fill bar above the frame, in cells.")]
        public float fillBarHeight = 0.3f;

        [Tooltip("Space between the fill bar and the frame, in cells.")]
        public float fillBarGap = 0.45f;

        [Tooltip("Colour of the empty part of the fill bar.")]
        public Color fillBarBackColor = new Color(0f, 0f, 0f, 0.15f);

        [Tooltip("Colour of the filled part of the fill bar.")]
        public Color fillBarColor = new Color(0x3F / 255f, 0xB6 / 255f, 0x8B / 255f, 1f);

        [Header("Tray")]
        [Tooltip("Space between the frame and the tray, in cells.")]
        public float trayGap = 0.7f;

        [Tooltip("Space between tray slots and between tray rows, in cells.")]
        public float traySlotSpacing = 0.35f;

        [Tooltip("Minimum tray width, in cells; the tray is at least as wide as the frame.")]
        public float minTrayWidth = 6f;

        [Header("Camera")]
        [Tooltip("Space kept between the content and the screen edge, in cells.")]
        public float screenMargin = 0.5f;

        [Tooltip("Camera background colour.")]
        public Color backgroundColor = new Color(0xFF / 255f, 0xF8 / 255f, 0xEE / 255f, 1f);

        [Header("Lid")]
        [Tooltip("Seconds the lid takes to slide down over the grid.")]
        public float lidSlideDuration = 0.55f;

        [Tooltip("Seconds the zipper pull takes to cross the lid after it lands.")]
        public float zipperDuration = 0.5f;

        [Tooltip("Height of the zipper stripe, in cells.")]
        public float zipperHeight = 0.22f;

        [Tooltip("Size of one zipper tooth, in cells.")]
        public float zipperToothSize = 0.09f;

        [Tooltip("Distance between zipper teeth, in cells.")]
        public float zipperToothSpacing = 0.18f;

        [Tooltip("Size of the zipper pull, in cells.")]
        public float zipperPullSize = 0.32f;
    }
}
