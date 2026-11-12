// Implements: design/game-brief.md "Điều kiện thắng / thua" and "Booster" (3 stars without Hint, 2 stars if Hint was
// used on that level).
// Story: production/epics/pack-it-perfect/story-005-hint-and-stars.md
// Pure C#: no MonoBehaviour dependency.

namespace PackItPerfect
{
    /// <summary>The star rule. The values are the brief's rule, not tuning, so they are constants.</summary>
    public static class StarCalculator
    {
        /// <summary>Stars for a win without Hint.</summary>
        public const int STARS_WITHOUT_HINT = 3;

        /// <summary>Stars for a win after using Hint at least once.</summary>
        public const int STARS_WITH_HINT = 2;

        /// <summary>Stars earned by a win.</summary>
        /// <param name="hintUsed">True if Hint was used during this play of the level.</param>
        public static int StarsFor(bool hintUsed) => hintUsed ? STARS_WITH_HINT : STARS_WITHOUT_HINT;
    }
}
