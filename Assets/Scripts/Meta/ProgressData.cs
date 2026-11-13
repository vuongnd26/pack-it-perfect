// Saved player progress: best stars per level. Serialized with JsonUtility by JsonSaveStore.
// Implements: design/game-brief.md "Progression & Meta".
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

using System;

namespace PackItPerfect.Meta
{
    /// <summary>
    /// Plain save record. <see cref="bestStars"/>[i] is the best star result on level i + 1: 0 = never won, else 2 or 3.
    /// Version 2 adds the booster inventory; a version 1 save loads with an empty inventory and keeps its stars.
    /// Unlock state is not stored; it is derived from the stars (see <see cref="ProgressService.IsUnlocked"/>), so the
    /// two can never disagree.
    /// </summary>
    [Serializable]
    public class ProgressData
    {
        /// <summary>Save format version written by this build.</summary>
        public const int CURRENT_VERSION = 2;

        /// <summary>Save format version, for future migrations.</summary>
        public int version = CURRENT_VERSION;

        /// <summary>Best stars per level; index = level number - 1.</summary>
        public int[] bestStars = Array.Empty<int>();

        /// <summary>Player preference: silence all game audio.</summary>
        public bool muted;

        /// <summary>Player preference: skip decorative movement.</summary>
        public bool reduceMotion;
        /// <summary>Readable UI size: 100, 125 or 150 percent. Missing/invalid legacy values normalize to 100.</summary>
        public int uiScalePercent = 100;
        /// <summary>Booster charges held, indexed by <see cref="BoosterType"/> (version 2).</summary>
        public int[] boosters = Array.Empty<int>();
        /// <summary>True once the starting boosters of a new or migrated save were handed out (version 2).</summary>
        public bool startingBoostersGranted;
    }
}
