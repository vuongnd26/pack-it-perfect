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
        [Tooltip("Height of the top HUD band (pause, level pill, timer, rule chips); the board is fitted below it.")]
        public float hudHeight = 200f;
        [Tooltip("Gap above the first HUD row.")]
        public float hudTopMargin = 10f;
        [Tooltip("Diameter of the round pause button.")]
        public float hudButtonSize = 120f;
        public float hudPillHeight = 76f;
        public float hudNameHeight = 42f;
        public float hudNameFontSize = 34f;
        public float levelPillWidth = 300f;
        public float timerPillWidth = 250f;
        [Tooltip("Distance from the top of the band to the rule chip row.")]
        public float chipRowTop = 140f;
        public float chipHeight = 50f;
        public float chipSpacing = 10f;
        public float chipPaddingX = 18f;
        [Tooltip("Height of the booster dock; equals the slot diameter. The board is fitted above it.")]
        public float bottomBarHeight = 132f;
        public float bottomBarMargin = 18f;

        [Header("Booster dock")]
        [Tooltip("Diameter of a dock slot (at least 120 reference px for touch).")]
        public float dockSlotSize = 132f;
        public float dockSpacing = 36f;
        public float badgeSize = 52f;
        public float badgeFontSize = 30f;
        [Tooltip("How far the count badge sits outside the slot's top-right corner.")]
        public Vector2 badgeOffset = new Vector2(6f, 6f);
        public float dockLabelFontSize = 26f;
        public float lockIconSize = 44f;
        public float lockLabelFontSize = 28f;
        [Tooltip("Tint multiplied onto a slot that cannot be used right now.")]
        public Color dockDisabledTint = new Color(0.72f, 0.72f, 0.72f, 0.9f);
        [Tooltip("Dark veil over a locked slot.")]
        public Color dockLockVeil = new Color(0.1f, 0.14f, 0.2f, 0.62f);
        public Color badgeColor = new Color32(0xE5, 0x48, 0x4D, 0xFF);
        public Color badgeEmptyColor = new Color32(0x8A, 0x95, 0xA5, 0xFF);

        [Header("Toast")]
        public float toastWidth = 940f;
        public float toastHeight = 96f;
        public float toastFontSize = 34f;
        public float toastHold = 1.8f;
        public float toastFade = 0.3f;
        public float toastGap = 22f;

        [Header("Freeze")]
        [Tooltip("Timer pill colour while a Freeze holds the clock.")]
        public Color freezeTint = new Color32(0xBF, 0xE4, 0xFF, 0xFF);
        public Color freezeText = new Color32(0x16, 0x4A, 0x7A, 0xFF);

        [Header("Pause")]
        public float pauseCardWidth = 860f;
        public float pauseButtonHeight = 130f;
        public float pauseToggleHeight = 110f;

        [Header("Responsive menus")]
        [Tooltip("Maximum share of safe-area height covered by the bottom result card. Overflow scrolls.")]
        [Range(0.3f, 0.8f)] public float resultCardHeightShare = 0.55f;

        [Header("Win screen")]
        [Tooltip("Dim behind the win card; light so the packed container stays visible.")]
        public Color winDim = new Color(10f / 255f, 16f / 255f, 24f / 255f, 0.35f);
        public float winCardWidth = 900f;
        public float winCardBottomMargin = 40f;
        public float winStarSize = 140f;
        public float winRewardItemWidth = 210f;
        public float winRewardItemHeight = 96f;
        public float winBoosterBlockHeight = 150f;
        [Tooltip("Fill of the reward chips and the \"New booster!\" blocks on the win card.")]
        public Color winBlockColor = new Color32(0xFF, 0xEC, 0xB8, 0xFF);

        [Header("Banner")]
        public float bannerWidth = 780f;
        public float bannerHeight = 150f;
        public float bannerHeightSingle = 100f;

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
        [Tooltip("Seconds a full screen or overlay takes to fade in.")]
        public float screenFade = 0.16f;
        [Tooltip("Seconds the toast line takes to pop in.")]
        public float toastPop = 0.18f;
        [Tooltip("Seconds of the clock's heartbeat pulse on each second in the danger zone.")]
        public float timerPulse = 0.26f;
        [Tooltip("Extra scale at the peak of the clock's pulse and the fill percentage's bump.")]
        [Range(0f, 0.4f)] public float pulseAmount = 0.14f;
        public float bannerPopIn = 0.25f;
        public float bannerHold = 1.0f;
        public float bannerFadeOut = 0.25f;
        public float cardPopDuration = 0.25f;
        public float starPopDuration = 0.35f;
        public float starPopStagger = 0.15f;
        [Tooltip("Delay between reward items popping in on the win card.")]
        public float rewardPopStagger = 0.12f;
        public float badgePopDuration = 0.3f;
        [Range(0.2f, 1f)] public float badgePopStart = 0.5f;
        public float dockBounceDuration = 0.28f;
        [Tooltip("Extra scale at the peak of a dock slot bounce.")]
        public float dockBounceAmount = 0.16f;
        public float shakeDuration = 0.3f;
        public float shakeDistance = 14f;
        [Range(0f, 1f)] public float emptyStarAlpha = 0.2f;

        [Header("Level select")]
        public int levelSelectColumns = 4;
        public Vector2 levelCellSize = new Vector2(216f, 216f);
        public Vector2 levelCellSpacing = new Vector2(24f, 24f);
        public float giftMarkerSize = 56f;

        [Header("Main screen")]
        public Vector2 mainBoosterChipSize = new Vector2(190f, 90f);
    }
}
