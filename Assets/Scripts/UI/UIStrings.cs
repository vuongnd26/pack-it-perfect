// Every player-facing UI string in one table (the project has no localization system yet; swap this table for one).
// GDD texts are VERBATIM from design/game-brief.md — do not paraphrase them:
//   "Điều kiện thắng / thua": "Out of time! The taxi is leaving and the trunk won't close.", "Try again", "Install to keep packing"
//   "Cơ chế chính" (Hẹn giờ): "Hard level! Pack before the taxi leaves"; "Booster": "💡 Hint"
// The rest are UI-wave wording (not in the GDD). Pass every string through TmpText.Clean before TMP.
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using PackItPerfect.Meta;
using UnityEngine;

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

        /// <summary>HUD level pill: {0} level number.</summary>
        public const string LEVEL_PILL_FORMAT = "Level {0}";
        /// <summary>HUD fill percentage: {0} = 0..100.</summary>
        public const string FILL_FORMAT = "{0}%";
        public const string RULES_BUTTON = "Rules";

        /// <summary>First displayed symbol of a rule chip, preserving surrogate pairs.</summary>
        public static string RuleIcon(string text) => string.IsNullOrEmpty(text) ? string.Empty
            : System.Globalization.StringInfo.GetNextTextElement(text);
        /// <summary>HUD timer: {0} = seconds left.</summary>
        public const string TIMER_FORMAT = "⏱ {0}s";
        /// <summary>Hint button (GDD verbatim, "Booster").</summary>
        public const string HINT = GameTexts.HINT_BUTTON;
        /// <summary>Hint label when the lightbulb is rendered as illustrated art instead of an emoji.</summary>
        public const string HINT_LABEL = "Hint";
        /// <summary>Seconds label beside the illustrated clock.</summary>
        public const string SECONDS_FORMAT = "{0}s";
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
        /// <summary>Win screen note when a booster capped the stars at 2.</summary>
        public const string WIN_ASSIST_NOTE = "Finish without boosters for 3 stars";
        /// <summary>Win screen tag over each booster that just opened.</summary>
        public const string WIN_NEW_BOOSTER = "New booster!";
        /// <summary>Win screen reward amount: {0} = charges.</summary>
        public const string WIN_REWARD_FORMAT = "+{0}";
        /// <summary>Win screen teaser: {0} = level that pays the next reward.</summary>
        public const string WIN_NEXT_REWARD_FORMAT = "Next reward after level {0}";
        /// <summary>Win screen: next level.</summary>
        public const string NEXT_LEVEL = "Next level";
        /// <summary>Win screen: replay this level.</summary>
        public const string REPLAY = "Replay";
        /// <summary>End card title after the last level.</summary>
        public const string END_TITLE = "All packed!";
        /// <summary>End card body after the last level.</summary>
        public const string END_BODY = "Every space packed, zero gaps. You’re a packing pro.";

        /// <summary>One star glyph.</summary>
        public const string STAR = "⭐";
        /// <summary>Locked level glyph.</summary>
        public const string LOCK = "🔒";

        /// <summary>Audio preference labels.</summary>
        public const string SOUND_ON = "Sound: On";
        /// <summary>Readable interface size preference.</summary>
        public const string UI_SIZE_FORMAT = "Text & controls: {0}%";
        /// <summary>Audio preference label when muted.</summary>
        public const string SOUND_OFF = "Sound: Off";
        /// <summary>Motion preference label.</summary>
        public const string MOTION_ON = "Motion: On";
        /// <summary>Motion preference label when decorative movement is reduced.</summary>
        public const string MOTION_REDUCED = "Motion: Reduced";

        // ---- Cozy Travel: pause, boosters, toasts, banner tag

        /// <summary>Pause overlay title.</summary>
        public const string PAUSED_TITLE = "Paused";
        /// <summary>Pause overlay: close it and keep playing.</summary>
        public const string RESUME = "Resume";
        /// <summary>Plain Home label (no emoji).</summary>
        public const string HOME_LABEL = "Home";
        /// <summary>Text fallback of the pause icon when the art is missing.</summary>
        public const string PAUSE_ICON = "II";

        /// <summary>Undo dock slot name.</summary>
        public const string UNDO = "Undo";
        /// <summary>Hint booster name.</summary>
        public const string BOOSTER_HINT = "Hint";
        /// <summary>Auto Pack booster name.</summary>
        public const string BOOSTER_AUTOPACK = "Auto Pack";
        /// <summary>Freeze booster name.</summary>
        public const string BOOSTER_FREEZE = "Freeze";
        /// <summary>Hint plural, for "No Hints left".</summary>
        public const string BOOSTER_HINT_PLURAL = "Hints";
        /// <summary>Auto Pack plural.</summary>
        public const string BOOSTER_AUTOPACK_PLURAL = "Auto Packs";
        /// <summary>Freeze plural.</summary>
        public const string BOOSTER_FREEZE_PLURAL = "Freezes";
        /// <summary>Hint description.</summary>
        public const string BOOSTER_HINT_DESC = "Shows where the next item goes";
        /// <summary>Auto Pack description.</summary>
        public const string BOOSTER_AUTOPACK_DESC = "Packs one item for you";
        /// <summary>Freeze description: {0} = seconds.</summary>
        public const string BOOSTER_FREEZE_DESC_FORMAT = "Stops the clock for {0} seconds";

        /// <summary>Toast when a locked booster is tapped: {0} = level.</summary>
        public const string BOOSTER_LOCKED_FORMAT = "Unlocks after level {0}";
        /// <summary>Toast when a booster has no charges: {0} = plural booster name.</summary>
        public const string BOOSTER_EMPTY_FORMAT = "No {0} left – get more in the Shop";
        /// <summary>Toast when Freeze cannot start (already frozen, clock not running).</summary>
        public const string FREEZE_UNAVAILABLE = "The clock can't be frozen right now";
        /// <summary>Lock overlay label on a dock slot: {0} = level.</summary>
        public const string LOCK_LEVEL_FORMAT = "Lv {0}";

        /// <summary>Shop button label (main, win and pause screens).</summary>
        public const string SHOP = "Shop";
        /// <summary>Shop screen title.</summary>
        public const string SHOP_TITLE = "Shop";
        /// <summary>Star balance pill on the shop screen.</summary>
        public const string SHOP_BALANCE_FORMAT = "⭐ {0}";
        /// <summary>Buy button label: the price in stars.</summary>
        public const string SHOP_PRICE_FORMAT = "⭐ {0}";
        /// <summary>How many of a booster the player holds.</summary>
        public const string SHOP_OWNED_FORMAT = "You have {0}";
        /// <summary>Line under the title explaining how to earn stars.</summary>
        public const string SHOP_HINT = "Earn stars by clearing levels";
        /// <summary>Toast after a purchase; {0} is the booster name.</summary>
        public const string SHOP_BOUGHT_FORMAT = "{0} added!";
        /// <summary>Toast when the balance is too low; {0} is the number of missing stars.</summary>
        public const string SHOP_NEED_FORMAT = "Need {0} more ⭐";
        /// <summary>Shown instead of the price while a booster is locked; {0} is the level.</summary>
        public const string SHOP_LOCKED_FORMAT = "Opens after level {0}";
        /// <summary>Close button of the shop.</summary>
        public const string SHOP_CLOSE = "Back";
        /// <summary>Timer pill while frozen and no snowflake art: {0} = freeze seconds left.</summary>
        public const string FROZEN_FORMAT = "Frozen {0}s";
        /// <summary>Tag on the level-start banner when the level adds a rule.</summary>
        public const string NEW_TAG = "New!";
        /// <summary>Badge text above 99 charges.</summary>
        public const string BADGE_OVERFLOW = "99+";
        /// <summary>Marker text when the gift art is missing.</summary>
        public const string GIFT_FALLBACK = "+";

        /// <summary>Tray pager label: {0} page, {1} page count, {2} items still to pack.</summary>
        public const string TRAY_PAGE_FORMAT = "Tray {0}/{1}  ·  {2} left";
        /// <summary>Previous tray page button.</summary>
        public const string TRAY_PREVIOUS = "<";
        /// <summary>Next tray page button.</summary>
        public const string TRAY_NEXT = ">";
        /// <summary>Held item line: {0} item name, {1} its rules.</summary>
        public const string ITEM_INFO_FORMAT = "{0}  ·  {1}";
        /// <summary>Separator between an item's rules.</summary>
        public const string ITEM_TRAIT_SEPARATOR = "  ";
        /// <summary>Held item rule: heavy.</summary>
        public const string TRAIT_HEAVY = "⚓ Heavy";
        /// <summary>Held item rule: fragile.</summary>
        public const string TRAIT_FRAGILE = "✨ Fragile";
        /// <summary>Held item rule: cannot be turned.</summary>
        public const string TRAIT_UPRIGHT = "⬆️ Upright";
        /// <summary>Held item rule: must touch its partner.</summary>
        public const string TRAIT_PAIR = "🔗 Pair";
        /// <summary>Held item rule: zone, {0} icon, {1} zone name.</summary>
        public const string TRAIT_ZONE_FORMAT = "{0} {1}";

        /// <summary>Display name of a booster.</summary>
        public static string BoosterName(BoosterType type) => type == BoosterType.Hint ? BOOSTER_HINT
            : type == BoosterType.AutoPack ? BOOSTER_AUTOPACK : BOOSTER_FREEZE;

        /// <summary>Plural display name of a booster.</summary>
        public static string BoosterPlural(BoosterType type) => type == BoosterType.Hint ? BOOSTER_HINT_PLURAL
            : type == BoosterType.AutoPack ? BOOSTER_AUTOPACK_PLURAL : BOOSTER_FREEZE_PLURAL;

        /// <summary>One-line description of a booster; <paramref name="freezeSeconds"/> fills the Freeze text.</summary>
        public static string BoosterDescription(BoosterType type, float freezeSeconds) => type == BoosterType.Hint
            ? BOOSTER_HINT_DESC : type == BoosterType.AutoPack ? BOOSTER_AUTOPACK_DESC
            : string.Format(BOOSTER_FREEZE_DESC_FORMAT, Mathf.RoundToInt(freezeSeconds));

        /// <summary>Resources/Art name of a booster's icon.</summary>
        public static string BoosterArt(BoosterType type) => type == BoosterType.Hint ? "ui_hint"
            : type == BoosterType.AutoPack ? "ui_autopack" : "ui_freeze";
    }
}
