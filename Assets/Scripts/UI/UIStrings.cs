// Every player-facing UI string in one table (the project has no localization system yet; swap this table for one).
// GDD texts are VERBATIM from design/game-brief.md — do not paraphrase them:
//   "Điều kiện thắng / thua": "Out of time! The taxi is leaving and the trunk won't close.", "Try again", "Install to keep packing"
//   "Cơ chế chính" (Hẹn giờ): "Hard level! Pack before the taxi leaves"; "Booster": "💡 Hint"
// The rest are UI-wave wording (not in the GDD). Pass every string through TmpText.Clean before TMP.
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

namespace PackItPerfect.UI
{
    /// <summary>UI texts and formats.</summary>
    public static class UIStrings
    {
        /// <summary>Game title on the main screen.</summary>
        public const string TITLE = "Pack It Perfect";
        /// <summary>Main screen tagline (from the brief's pitch, not verbatim).</summary>
        public const string TAGLINE = "Turn and fit every item until the lid snaps shut.";
        /// <summary>Main screen stats: {0} total stars, {1} levels passed, {2} level count.</summary>
        public const string STATS_FORMAT = "⭐ {0}  ·  {1} / {2} levels passed";
        /// <summary>Main screen Play button.</summary>
        public const string PLAY = "▶ Play";
        /// <summary>Opens level select.</summary>
        public const string LEVELS = "Levels";
        /// <summary>Back to the main screen.</summary>
        public const string HOME = "🏠 Home";
        /// <summary>Icon-only home button in the HUD.</summary>
        public const string HOME_ICON = "🏠";

        /// <summary>HUD level pill: {0} level, {1} level count.</summary>
        public const string LEVEL_PILL_FORMAT = "Level {0} / {1}";
        /// <summary>HUD fill percentage: {0} = 0..100.</summary>
        public const string FILL_FORMAT = "{0}%";
        /// <summary>HUD timer: {0} = seconds left.</summary>
        public const string TIMER_FORMAT = "⏱ {0}s";
        /// <summary>Hint button (GDD verbatim, "Booster").</summary>
        public const string HINT = GameTexts.HINT_BUTTON;
        /// <summary>Restart button.</summary>
        public const string RESTART = "Restart";

        /// <summary>Timed-level banner, first half of the GDD line "Hard level! Pack before the taxi leaves".</summary>
        public const string HARD_TITLE = "Hard level!";
        /// <summary>Timed-level banner, second half of the GDD line.</summary>
        public const string HARD_SUBTITLE = "Pack before the taxi leaves";

        /// <summary>Lose screen, first sentence of the GDD line.</summary>
        public const string LOSE_TITLE = "Out of time!";
        /// <summary>Lose screen, rest of the GDD line.</summary>
        public const string LOSE_BODY = "The taxi is leaving and the trunk won't close.";
        /// <summary>Lose screen button (GDD verbatim).</summary>
        public const string TRY_AGAIN = "Try again";
        /// <summary>Lose screen button (GDD verbatim). Opens no link in this build.</summary>
        public const string INSTALL = "Install to keep packing";

        /// <summary>Win screen title.</summary>
        public const string WIN_TITLE = "Packed!";
        /// <summary>Win screen note when Hint capped the stars: {0} = stars.</summary>
        public const string WIN_HINT_NOTE_FORMAT = "Used a hint – max {0} stars";
        /// <summary>Win screen: next level.</summary>
        public const string NEXT_LEVEL = "Next level";
        /// <summary>Win screen: replay this level.</summary>
        public const string REPLAY = "Replay";
        /// <summary>End card title after the last level.</summary>
        public const string END_TITLE = "All packed!";
        /// <summary>End card body after the last level.</summary>
        public const string END_BODY = "Fifteen spaces, zero gaps. You’re a packing pro.";

        /// <summary>One star glyph.</summary>
        public const string STAR = "⭐";
        /// <summary>Locked level glyph.</summary>
        public const string LOCK = "🔒";

        /// <summary>Audio preference labels.</summary>
        public const string SOUND_ON = "Sound: On";
        /// <summary>Audio preference label when muted.</summary>
        public const string SOUND_OFF = "Sound: Off";
        /// <summary>Motion preference label.</summary>
        public const string MOTION_ON = "Motion: On";
        /// <summary>Motion preference label when decorative movement is reduced.</summary>
        public const string MOTION_REDUCED = "Motion: Reduced";
    }
}
