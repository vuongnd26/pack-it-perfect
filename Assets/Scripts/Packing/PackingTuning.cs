// Tuning values for stories 001–005 (production/epics/pack-it-perfect/story-00[1-5]-*.md).
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
        [Tooltip("Touch travel in millimetres before dragging; mouse keeps the pixel threshold.")]
        public float touchDragMillimetres = 1.5f;
        [Tooltip("Touch threshold bounds in screen pixels; unreliable DPI uses a short-edge-derived estimate.")]
        public Vector2 touchDragPixelBounds = new Vector2(8f, 28f);
        [Tooltip("Extra touch reach around tray slots in screen pixels, capped in world cells.")]
        public float touchHitPaddingPixels = 10f;
        public float touchHitMaxCells = 0.25f;

        [Tooltip("The ghost hides when the pointer is further than this (cells) outside the grid.")]
        public float ghostHideDistance = 1f;

        [Header("Drag lift (Cozy Travel)")]
        [Tooltip("Cells a touch-dragged piece floats above the finger so the finger never hides it. Rationale: about one finger pad at phone scale.")]
        public float touchLiftCells = 1.1f;
        [Tooltip("Cells a mouse-dragged piece floats above the cursor (0: the cursor hides nothing).")]
        public float mouseLiftCells = 0f;
        [Tooltip("Seconds the lift takes to ease in after a drag starts.")]
        public float liftEaseSeconds = 0.08f;

        [Header("Undo and Auto Pack (Cozy Travel)")]
        [Tooltip("Most player actions Undo can step back through; older ones are forgotten.")]
        public int undoDepth = 40;
        [Tooltip("Placements the Auto Pack search tries per frame (keeps a frame under a few milliseconds).")]
        public int autoPackNodesPerFrame = 2500;
        [Tooltip("Maximum search candidates. Exhaustion reports unknown without spending a booster; only proven impossibility allows reference fallback.")]
        public int autoPackNodeLimit = 300000;
        [Tooltip("Seconds an auto-packed piece takes to fly from the tray into place.")]
        public float autoPackFlySeconds = 0.38f;
        [Tooltip("Badge colour of fragile pieces.")]
        public Color fragileBadgeColor = new Color(0x6C / 255f, 0xC5 / 255f, 0xE8 / 255f, 1f);

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
        public float trayGap = 0.5f;

        [Tooltip("Space between tray slots and between tray rows, in cells.")]
        public float traySlotSpacing = 0.28f;

        [Tooltip("Minimum tray width, in cells; the tray is at least as wide as the frame.")]
        public float minTrayWidth = 9f;

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

        [Header("Win")]
        [Tooltip("After the lid closes, a tap loads the next level. The UI wave turns this off and shows a win screen instead.")]
        public bool autoAdvanceOnWin = true;

        [Header("Level limits (loader check)")]
        [Tooltip("A level outside these limits is rejected by the loader. Brief: grid 4x3 to 7x7, 3–13 items.")]
        public LevelLimits levelLimits = new LevelLimits();

        [Header("Item icons")]
        [Tooltip("Icon width when it sits on a 2x2 block of the piece, in cells.")]
        public float iconSizeOnBlock = 1.2f;

        [Tooltip("Icon width when the piece has no 2x2 block (sits on one cell), in cells.")]
        public float iconSizeOnCell = 0.82f;

        [Header("Rule badges")]
        [Tooltip("Diameter of a rule badge (zone, heavy, upright, pair) on a piece, in cells.")]
        public float badgeSize = 0.36f;

        [Tooltip("Gap between a badge and the edge of its cell, in cells.")]
        public float badgeMargin = 0.03f;

        [Tooltip("Emoji size inside a badge, as a fraction of the badge diameter.")]
        public float badgeEmojiScale = 0.62f;

        [Tooltip("Circle behind the zone icon badge.")]
        public Color zoneBadgeColor = new Color(1f, 1f, 1f, 0.92f);

        [Tooltip("Circle behind the ⚓ heavy badge (dark).")]
        public Color heavyBadgeColor = new Color(0x2E / 255f, 0x2E / 255f, 0x3A / 255f, 1f);

        [Tooltip("Circle behind the ⬆️ upright badge (yellow).")]
        public Color uprightBadgeColor = new Color(0xF2 / 255f, 0xC1 / 255f, 0x4E / 255f, 1f);

        [Tooltip("Circle behind the 🔗 pair badge (pink).")]
        public Color pairBadgeColor = new Color(0xF0 / 255f, 0x8D / 255f, 0xB5 / 255f, 1f);

        [Header("Zones")]
        [Tooltip("Line drawn where neighbouring cells change zone.")]
        public Color zoneDividerColor = new Color(0x50 / 255f, 0x3C / 255f, 0x28 / 255f, 0.55f);

        [Tooltip("Thickness of the zone divider, in cells.")]
        public float zoneDividerWidth = 0.06f;

        [Tooltip("Dashes per cell edge of the zone divider; 0 = solid line.")]
        public int zoneDividerDashesPerCell = 3;

        [Tooltip("Drawn part of each dash period, 0..1.")]
        [Range(0.05f, 1f)] public float zoneDividerDashFill = 0.6f;

        [Tooltip("Height of the zone label text (\"🍴 Food\"), in cells.")]
        public float zoneLabelSize = 0.26f;

        [Tooltip("Zone label text colour.")]
        public Color zoneLabelColor = new Color(0x3C / 255f, 0x28 / 255f, 0x14 / 255f, 0.7f);

        [Header("Outlines (story 004)")]
        [Tooltip("Fill of an outline imprint. Prototype: rgba(60,40,20,.13).")]
        public Color outlineColor = new Color(60f / 255f, 40f / 255f, 20f / 255f, 0.13f);

        [Tooltip("Opacity of the item icon on an outline imprint. Prototype: ~0.32.")]
        [Range(0f, 1f)] public float outlineIconAlpha = 0.32f;

        [Tooltip("Opacity multiplier of an outline once its item is placed exactly on it; 0 = disappears.")]
        [Range(0f, 1f)] public float outlinePlacedOpacity = 0f;

        [Header("Wobble (upright tap)")]
        [Tooltip("Peak rotation of a wobble, in degrees.")]
        public float wobbleAngle = 10f;

        [Tooltip("Seconds an upright piece wobbles when tapped.")]
        public float wobbleDuration = 0.45f;

        [Tooltip("Full left-right swings during that wobble.")]
        public float wobbleCycles = 3f;

        [Header("Hint (story 005)")]
        [Tooltip("Seconds the target cells glow. Brief: ~2 s.")]
        public float hintGlowDuration = 2f;

        [Tooltip("Glow pulses per second.")]
        public float hintGlowPulsesPerSecond = 2f;

        [Tooltip("Glow colour (alpha is driven by the pulse).")]
        public Color hintGlowColor = Color.white;

        [Tooltip("Glow opacity at the low point of a pulse.")]
        [Range(0f, 1f)] public float hintGlowMinAlpha = 0.25f;

        [Tooltip("Glow opacity at the peak of a pulse.")]
        [Range(0f, 1f)] public float hintGlowMaxAlpha = 0.9f;

        [Tooltip("Opacity of the glow's cell fill relative to its dashed border.")]
        [Range(0f, 1f)] public float hintGlowFillAlpha = 0.45f;

        [Tooltip("Thickness of the glow's dashed border, in cells.")]
        public float hintGlowBorderWidth = 0.07f;

        [Tooltip("Inset of the glow square inside its cell, in cells.")]
        public float hintGlowInset = 0.08f;

        [Tooltip("Dashes per side of a glowing cell's border; 0 = solid.")]
        public int hintGlowDashesPerCell = 3;

        [Tooltip("Drawn part of each border dash period, 0..1.")]
        [Range(0.05f, 1f)] public float hintGlowDashFill = 0.6f;

        [Tooltip("Seconds the hinted piece wobbles in the tray.")]
        public float hintWobbleDuration = 1.2f;

        [Tooltip("Full left-right swings during the hint wobble.")]
        public float hintWobbleCycles = 4f;

        [Header("Toast")]
        [Tooltip("Seconds a toast stays fully visible before fading. Prototype: ~1.7 s.")]
        public float toastDuration = 1.7f;

        [Tooltip("Seconds the toast takes to fade out.")]
        public float toastFadeDuration = 0.3f;

        [Tooltip("Toast sizes below are in cells for a view this many cells tall; the toast scales with the view so it reads the same on every level.")]
        public float toastReferenceViewHeight = 12f;

        [Tooltip("Toast text height (reference cells).")]
        public float toastTextSize = 0.42f;

        [Tooltip("Toast pill height (reference cells).")]
        public float toastHeight = 0.8f;

        [Tooltip("Space between the text and each end of the pill (reference cells).")]
        public float toastPaddingX = 0.35f;

        [Tooltip("Space between the bottom of the view and the toast (reference cells).")]
        public float toastBottomMargin = 0.5f;

        [Tooltip("Toast pill colour (dark).")]
        public Color toastBackColor = new Color(0x22 / 255f, 0x22 / 255f, 0x2B / 255f, 0.92f);

        [Tooltip("Toast text colour.")]
        public Color toastTextColor = Color.white;

        [Header("Words (Pair!)")]
        [Tooltip("Seconds a word floats before it is gone.")]
        public float wordDuration = 1f;

        [Tooltip("How far a word rises over its life, in cells.")]
        public float wordRise = 0.6f;

        [Tooltip("Word text height, in cells.")]
        public float wordTextSize = 0.36f;

        [Tooltip("Word pill height, in cells.")]
        public float wordHeight = 0.6f;

        [Tooltip("Space between the word and each end of its pill, in cells.")]
        public float wordPaddingX = 0.22f;

        [Tooltip("Word pill colour.")]
        public Color wordBackColor = new Color(0xE0 / 255f, 0x4F / 255f, 0x8C / 255f, 0.95f);

        [Tooltip("Word text colour.")]
        public Color wordTextColor = Color.white;

        [Header("Piece shadow")]
        [Tooltip("Soft oval under tray and dragged pieces (alpha = strength).")]
        public Color pieceShadowColor = new Color(0.1f, 0.08f, 0.14f, 0.3f);

        [Tooltip("Shadow width as a share of the piece width.")]
        public float pieceShadowWidthScale = 0.95f;

        [Tooltip("Shadow height in cells.")]
        public float pieceShadowHeight = 0.7f;

        [Tooltip("Cells the shadow centre sits below the piece's bottom edge at rest in the tray.")]
        public float pieceShadowDrop = 0.04f;

        [Tooltip("Cells the shadow centre sits below a dragged (lifted) piece.")]
        public float pieceShadowLiftedDrop = 0.32f;

        [Tooltip("Shadow size multiplier while the piece is lifted.")]
        public float pieceShadowLiftedScale = 0.88f;
    }

    /// <summary>Grid size and piece count a level must stay within (brief "Progression & Meta": 4x3 to 7x7, 3–13 items).</summary>
    [Serializable]
    public class LevelLimits
    {
        /// <summary>Fewest columns.</summary>
        public int minWidth = 4;
        /// <summary>Fewest rows.</summary>
        public int minHeight = 3;
        /// <summary>Most columns.</summary>
        public int maxWidth = 7;
        /// <summary>Most rows.</summary>
        public int maxHeight = 7;
        /// <summary>Fewest items.</summary>
        public int minPieces = 3;
        /// <summary>Most items.</summary>
        public int maxPieces = 13;
    }
}
