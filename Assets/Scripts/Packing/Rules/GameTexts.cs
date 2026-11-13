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

        /// <summary>Search exhaustion is unknown, not proof that the current board is impossible.</summary>
        public const string AUTO_PACK_SEARCH_LIMIT = "No answer yet — try moving an item. No booster used.";
        /// <summary>A geometric Hint target still conflicts with a neighboring rule.</summary>
        public const string HINT_BLOCKED = "Move heavy items away from fragile items near the hint spot. No booster used.";
        /// <summary>Shape-independent, non-colour placement cues.</summary>
        public const string PLACEMENT_VALID = "OK";
        /// <summary>Invalid preview marker remains legible without red/green discrimination.</summary>
        public const string PLACEMENT_INVALID = "X";

        /// <summary>Fragile violation toast (Cozy Travel upgrade: fragile items may not touch heavy ones).</summary>
        public const string FRAGILE_VIOLATION = "Fragile! Keep it away from heavy items";
        /// <summary>Badge text of fragile pieces when the illustrated badge is missing.</summary>
        public const string FRAGILE_ICON = "!";
        /// <summary>Badge icon of heavy pieces.</summary>
        public const string HEAVY_ICON = "⚓";

        /// <summary>Badge icon of upright pieces ("⬆️", U+2B06 U+FE0F).</summary>
        public const string UPRIGHT_ICON = "⬆️";

        /// <summary>Badge icon of paired pieces.</summary>
        public const string PAIR_ICON = "🔗";

        /// <summary>Number of approved placement-praise strings; the vocabulary is closed by the brief.</summary>
        public const int PRAISE_COUNT = 5;

        /// <summary>Returns one approved praise string without exposing a mutable static array.</summary>
        public static string PlacementPraise(int index) => index switch
        {
            0 => "Snug!",
            1 => "Nice fit!",
            2 => "Click!",
            3 => "Perfect!",
            _ => "Neat!",
        };

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
