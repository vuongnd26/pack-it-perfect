// Tunable values of the screen-space UI (sizes in reference pixels at 1080x1920, durations in seconds).
// Colours come from the prototype (design/game-brief.md "Quyết định đã chốt": prototype). Thresholds come from
// design/game-brief.md "Cơ chế chính" (Hẹn giờ: red at 10 s, tick at 5 s).
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>UI tuning, serialized on <see cref="GameUI"/>.</summary>
    [Serializable]
    public class UITuning
    {
        [Header("Canvas")]
        [Tooltip("CanvasScaler reference resolution (portrait).")]
        public Vector2 referenceResolution = new Vector2(1080f, 1920f);

        [Tooltip("CanvasScaler match: 0 = width, 1 = height.")]
        [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

        [Tooltip("Overlay canvas sorting order.")]
        public int canvasSortingOrder = 10;

        [Header("Colours")]
        public Color ink = new Color32(0x1E, 0x2A, 0x3A, 0xFF);
        public Color panel = Color.white;
        public Color primary = new Color32(0x3F, 0xB6, 0x8B, 0xFF);
        public Color primaryShadow = new Color32(0x2C, 0x8A, 0x68, 0xFF);
        public Color danger = new Color32(0xE5, 0x48, 0x4D, 0xFF);
        public Color dangerShadow = new Color32(0xA8, 0x32, 0x2F, 0xFF);
        public Color accent = new Color32(0xF2, 0xC1, 0x4E, 0xFF);
        [Tooltip("Not in the prototype palette: a darker yellow for the accent button's shadow.")]
        public Color accentShadow = new Color32(0xC9, 0x9A, 0x2E, 0xFF);
        [Tooltip("Not in the prototype palette: shadow under white buttons.")]
        public Color secondaryShadow = new Color32(0xD3, 0xDA, 0xE3, 0xFF);
        public Color muted = new Color32(0x55, 0x65, 0x7A, 0xFF);
        public Color overlay = new Color(10f / 255f, 16f / 255f, 24f / 255f, 0.8f);
        [Tooltip("Opaque background of the full-screen menus (same cream as the board background).")]
        public Color menuBackground = new Color32(0xFF, 0xF8, 0xEE, 0xFF);
        [Tooltip("Banner card colour.")]
        public Color bannerBackground = new Color(0x1E / 255f, 0x2A / 255f, 0x3A / 255f, 0.92f);

        [Header("Shapes")]
        public float cornerRadius = 36f;
        public float cardRadius = 48f;
        [Tooltip("Depth of the darker band under buttons.")]
        public float buttonShadowDepth = 14f;
        [Tooltip("Opacity of the light band along the top edge of a button face.")]
        [Range(0f, 1f)] public float buttonGlossAlpha = 0.34f;
        [Tooltip("Opacity of the dark band along the bottom edge of a button face.")]
        [Range(0f, 1f)] public float buttonShadeAlpha = 0.2f;
        [Tooltip("Scale of a button while it is held down.")]
        [Range(0.8f, 1f)] public float buttonPressScale = 0.94f;
        [Tooltip("Seconds a button takes to press in or spring back.")]
        public float buttonPressDuration = 0.07f;
        [Tooltip("Share of the shadow depth the face sinks while pressed.")]
        [Range(0f, 1f)] public float buttonPressSink = 0.7f;
        public float sideMargin = 32f;

        [Header("Font sizes")]
        public float titleFontSize = 120f;
        public float headingFontSize = 88f;
        public float bodyFontSize = 46f;
        public float buttonFontSize = 60f;
        public float smallFontSize = 40f;
        public float chipFontSize = 34f;

        [Header("HUD")]
        [Tooltip("Height of the top HUD band; the board is fitted below it.")]
        public float hudHeight = 320f;
        public float hudRowHeight = 120f;
        public float hudButtonSize = 120f;
        public float levelPillWidth = 420f;
        public float timerPillWidth = 230f;
        public float fillPillWidth = 170f;
        public float chipHeight = 64f;
        [Tooltip("Height of the bottom bar (Hint, Restart); the board is fitted above it.")]
        public float bottomBarHeight = 140f;
        public float bottomBarMargin = 32f;

        [Header("Timer")]
        [Tooltip("Seconds after a timed level loads before the clock starts (lets the banner play).")]
        public float timerStartDelay = 1.3f;
        [Tooltip("The clock turns red at this many seconds left (brief: 10).")]
        public int timerDangerSeconds = 10;
        [Tooltip("The tick sound plays each second from this many seconds left (brief: 5).")]
        public int timerWarningSeconds = 5;

        [Header("Animation")]
        [Tooltip("Skip scale pops (motion preference).")]
        public bool reduceMotion = false;
        public float popStartScale = 0.6f;
        public float bannerPopIn = 0.25f;
        public float bannerHold = 1.0f;
        public float bannerFadeOut = 0.25f;
        public float cardPopDuration = 0.25f;
        public float starPopDuration = 0.35f;
        public float starPopStagger = 0.15f;
        [Range(0f, 1f)] public float emptyStarAlpha = 0.2f;

        [Header("Level select")]
        public int levelSelectColumns = 3;
        public Vector2 levelCellSize = new Vector2(280f, 250f);
        public Vector2 levelCellSpacing = new Vector2(40f, 40f);
    }
}
