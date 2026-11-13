// Progress rules: best stars per level, sequential unlock, totals for the main screen.
// Implements: design/game-brief.md "Progression & Meta", "Quyết định đã chốt" (Mở khóa tuần tự).
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

using System;

namespace PackItPerfect.Meta
{
    /// <summary>
    /// Pure C# progress model over an <see cref="ISaveStore"/>. Level numbers are 1-based.
    /// Level 1 is always unlocked; level N + 1 unlocks once level N is won. A replay never lowers a level's best stars.
    /// </summary>
    public sealed class ProgressService
    {
        /// <summary>Most stars one level can give.</summary>
        public const int MAX_STARS = 3;

        readonly ISaveStore store;
        readonly ProgressData data;

        /// <summary>Raised when a win improves a level's best stars (a first win included).</summary>
        public event Action Changed;

        /// <summary>Loads progress from <paramref name="saveStore"/> (empty progress if none) for <paramref name="levelCount"/> levels.</summary>
        public ProgressService(ISaveStore saveStore, int levelCount)
        {
            store = saveStore ?? throw new ArgumentNullException(nameof(saveStore));
            LevelCount = Math.Max(0, levelCount);
            data = store.Load() ?? new ProgressData();
            Normalize();
        }

        /// <summary>Number of levels tracked.</summary>
        public int LevelCount { get; }

        /// <summary>Best stars on <paramref name="level"/>: 0 if never won (or out of range), else 2 or 3.</summary>
        public int BestStars(int level) => InRange(level) ? data.bestStars[level - 1] : 0;

        /// <summary>True if <paramref name="level"/> was won at least once.</summary>
        public bool IsPassed(int level) => BestStars(level) > 0;

        /// <summary>True if <paramref name="level"/> can be played: level 1, or the previous level was won.</summary>
        public bool IsUnlocked(int level) => InRange(level) && (level == 1 || IsPassed(level - 1));

        /// <summary>Sum of the best stars over all levels.</summary>
        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (int stars in data.bestStars)
                {
                    total += stars;
                }
                return total;
            }
        }

        /// <summary>Number of levels won at least once.</summary>
        public int LevelsPassed
        {
            get
            {
                int passed = 0;
                foreach (int stars in data.bestStars)
                {
                    if (stars > 0)
                    {
                        passed++;
                    }
                }
                return passed;
            }
        }

        /// <summary>Highest level reachable by sequential unlock (0 when there are no levels).</summary>
        public int HighestUnlocked
        {
            get
            {
                if (LevelCount == 0)
                {
                    return 0;
                }
                int level = 1;
                while (level < LevelCount && IsPassed(level))
                {
                    level++;
                }
                return level;
            }
        }

        /// <summary>Level the Play button continues at: the first unlocked level not yet won, else the last level (0 if none).</summary>
        public int NextLevelToPlay
        {
            get
            {
                for (int level = 1; level <= LevelCount; level++)
                {
                    if (IsUnlocked(level) && !IsPassed(level))
                    {
                        return level;
                    }
                }
                return LevelCount;
            }
        }

        /// <summary>
        /// Records a win and saves (every win is written). Keeps the better of the stored and new stars.
        /// Returns true if the best stars improved. Out-of-range levels or stars &lt;= 0 are ignored.
        /// </summary>
        public bool RecordWin(int level, int stars)
        {
            if (!InRange(level) || stars <= 0)
            {
                return false;
            }
            stars = Math.Min(stars, MAX_STARS);
            bool improved = stars > data.bestStars[level - 1];
            if (improved)
            {
                data.bestStars[level - 1] = stars;
            }
            store.Save(data);
            if (improved)
            {
                Changed?.Invoke();
            }
            return improved;
        }

        /// <summary>Saved audio preference.</summary>
        public bool Muted => data.muted;

        /// <summary>Saved motion preference.</summary>
        public bool ReduceMotion => data.reduceMotion;

        /// <summary>Saves player presentation preferences without changing stars or unlocks.</summary>
        public void SetPreferences(bool muted, bool reduceMotion)
        {
            data.muted = muted;
            data.reduceMotion = reduceMotion;
            store.Save(data);
        }

        bool InRange(int level) => level >= 1 && level <= LevelCount;

        // Fits the loaded array to the level count (levels.json may have grown or shrunk) and clamps bad values.
        void Normalize()
        {
            var old = data.bestStars ?? Array.Empty<int>();
            var fitted = new int[LevelCount];
            for (int i = 0; i < fitted.Length && i < old.Length; i++)
            {
                fitted[i] = Math.Max(0, Math.Min(old[i], MAX_STARS));
            }
            data.bestStars = fitted;
            data.version = ProgressData.CURRENT_VERSION;
        }
    }
}
