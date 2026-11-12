// Player-facing texts, VERBATIM from design/game-brief.md ("Obstacles", "Booster"); icons included.
// Do not paraphrase or shorten: the brief is checked against the build (see "Ghi chú cho Claude Code").
// Strings keep U+FE0F where the brief has it; pass them through TmpText.Clean before giving them to TextMesh Pro.
// Stories: production/epics/pack-it-perfect/story-003-obstacle-rules.md, story-005-hint-and-stars.md

namespace PackItPerfect
{
    /// <summary>Verbatim rule texts and badge icons.</summary>
    public static class GameTexts
    {
        /// <summary>Heavy violation toast (brief: Obstacles, Đồ nặng).</summary>
        public const string HEAVY_VIOLATION = "⚓ Heavy items must touch the bottom";

        /// <summary>Upright tap toast (brief: Obstacles, Đứng thẳng). "⬆️" is U+2B06 U+FE0F.</summary>
        public const string UPRIGHT_VIOLATION = "⬆️ Bottles stay upright – no turning";

        /// <summary>Word shown when a dropped piece touches its pair partner (brief: Obstacles, Cặp đồ).</summary>
        public const string PAIR_PASS = "Pair! 💞";

        /// <summary>Label of the Hint button, for the UI wave (brief: Booster).</summary>
        public const string HINT_BUTTON = "💡 Hint";

        /// <summary>Badge icon of heavy pieces.</summary>
        public const string HEAVY_ICON = "⚓";

        /// <summary>Badge icon of upright pieces ("⬆️", U+2B06 U+FE0F).</summary>
        public const string UPRIGHT_ICON = "⬆️";

        /// <summary>Badge icon of paired pieces.</summary>
        public const string PAIR_ICON = "🔗";

        const string ZONE_VIOLATION_FORMAT = "{0} goes on the {1} side";
        const string PAIR_VIOLATION_FORMAT = "Pairs must touch – move the {0}s together";

        /// <summary>Zone violation toast: "[Item] goes on the [Zone] side".</summary>
        /// <param name="itemName">The item's display name, e.g. "Onigiri".</param>
        /// <param name="zoneName">The zone's name, e.g. "Food".</param>
        public static string ZoneViolation(string itemName, string zoneName) =>
            string.Format(ZONE_VIOLATION_FORMAT, itemName, zoneName);

        /// <summary>Pair violation toast: "Pairs must touch – move the [item]s together", item in lower case.</summary>
        /// <param name="itemName">The item's display name, e.g. "Sock" (becomes "socks").</param>
        public static string PairViolation(string itemName) =>
            string.Format(PAIR_VIOLATION_FORMAT, itemName.ToLowerInvariant());
    }
}
