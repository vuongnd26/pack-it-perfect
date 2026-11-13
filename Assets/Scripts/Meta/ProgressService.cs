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
        readonly ProgressionRules rules;

        /// <summary>Raised when a win improves a level's best stars (a first win included).</summary>
        public event Action Changed;

        /// <summary>Loads progress from <paramref name="saveStore"/> (empty progress if none) for <paramref name="levelCount"/> levels.</summary>
        public ProgressService(ISaveStore saveStore, int levelCount) : this(saveStore, levelCount, null)
        {
        }
        /// <summary>Loads progress and applies <paramref name="progression"/> (booster unlocks and rewards; null = none).</summary>
        public ProgressService(ISaveStore saveStore, int levelCount, ProgressionRules progression)
        {
            rules = progression ?? ProgressionRules.Empty();
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
                StarsChanged?.Invoke();
            }
            return improved;
        }

        /// <summary>Raised when a booster count changes (spent or rewarded).</summary>
        /// <summary>Why a shop purchase did or did not happen.</summary>
        public enum PurchaseResult
        {
            /// <summary>One booster was added and its price taken.</summary>
            Bought,
            /// <summary>The booster has not unlocked yet.</summary>
            Locked,
            /// <summary>The booster has no price.</summary>
            NotForSale,
            /// <summary>Not enough stars in the balance.</summary>
            NotEnoughStars,
        }

        /// <summary>Raised when the star balance changes (a shop purchase or a new best).</summary>
        public event Action StarsChanged;

        /// <summary>Stars earned so far that the shop has already taken.</summary>
        public int StarsSpent => data.starsSpent;

        /// <summary>Stars the player can spend: best stars over all levels minus what the shop took. Never negative.</summary>
        public int StarBalance => Math.Max(0, TotalStars - data.starsSpent);

        /// <summary>Price in stars of one booster; 0 or less means it is not sold.</summary>
        public int ShopPrice(BoosterType type) => rules.ShopPrice(type);

        /// <summary>Buys one booster with stars. Only unlocked boosters with a price can be bought.</summary>
        public PurchaseResult TryBuy(BoosterType type)
        {
            int price = rules.ShopPrice(type);
            if (price <= 0)
            {
                return PurchaseResult.NotForSale;
            }
            if (!IsBoosterUnlocked(type))
            {
                return PurchaseResult.Locked;
            }
            if (StarBalance < price)
            {
                return PurchaseResult.NotEnoughStars;
            }
            data.starsSpent += price;
            data.boosters[(int)type]++;
            store.Save(data);
            StarsChanged?.Invoke();
            BoostersChanged?.Invoke();
            return PurchaseResult.Bought;
        }

        public event Action BoostersChanged;
        /// <summary>The progression rules in force.</summary>
        public ProgressionRules Rules => rules;
        /// <summary>Charges of <paramref name="type"/> the player holds.</summary>
        public int BoosterCount(BoosterType type) => data.boosters[(int)type];
        /// <summary>True once the level that opens <paramref name="type"/> was won (see <see cref="ProgressionRules.UnlockAfterLevel"/>).</summary>
        public bool IsBoosterUnlocked(BoosterType type)
        {
            int after = rules.UnlockAfterLevel(type);
            return after <= 0 || IsPassed(after);
        }
        /// <summary>Spends one charge of <paramref name="type"/> and saves. Returns false (and changes nothing) when none is left.</summary>
        public bool TryConsume(BoosterType type)
        {
            if (data.boosters[(int)type] <= 0)
            {
                return false;
            }
            data.boosters[(int)type]--;
            store.Save(data);
            BoostersChanged?.Invoke();
            return true;
        }
        /// <summary>
        /// Records a win like <see cref="RecordWin"/> and hands out what the win earns: the level's milestone reward on its
        /// first clear, and the 3-star bonus the first time the level reaches 3 stars. Replays never pay twice.
        /// </summary>
        public LevelReward CompleteLevel(int level, int stars)
        {
            if (!InRange(level) || stars <= 0)
            {
                return new LevelReward(false, false, default, new BoosterType[0]);
            }
            int before = data.bestStars[level - 1];
            bool firstClear = before == 0;
            var unlockedBefore = new bool[BoosterBundle.TYPE_COUNT];
            for (int i = 0; i < unlockedBefore.Length; i++)
            {
                unlockedBefore[i] = IsBoosterUnlocked((BoosterType)i);
            }
            var reward = firstClear ? rules.MilestoneReward(level) : default;
            if (before < MAX_STARS && Math.Min(stars, MAX_STARS) == MAX_STARS)
            {
                reward = reward.Plus(rules.ThreeStarBonus);
            }
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                data.boosters[i] += reward.Of((BoosterType)i);
            }
            bool improved = RecordWin(level, stars); // saves, boosters included
            int opened = 0;
            var unlocked = new BoosterType[BoosterBundle.TYPE_COUNT];
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                if (!unlockedBefore[i] && IsBoosterUnlocked((BoosterType)i))
                {
                    unlocked[opened++] = (BoosterType)i;
                }
            }
            Array.Resize(ref unlocked, opened);
            if (!reward.IsEmpty)
            {
                BoostersChanged?.Invoke();
            }
            return new LevelReward(firstClear, improved, reward, unlocked);
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

        /// <summary>Saved readable UI size, in percent.</summary>
        public int UIScalePercent => data.uiScalePercent;

        /// <summary>Saves a supported UI size without changing gameplay progress or inventory.</summary>
        public void SetUIScalePercent(int percent)
        {
            data.uiScalePercent = percent == 125 || percent == 150 ? percent : 100;
            store.Save(data);
        }

        bool InRange(int level) => level >= 1 && level <= LevelCount;

        // Fits the loaded array to the level count (levels.json may have grown or shrunk) and clamps bad values.
        void Normalize()
        {
            if (data.uiScalePercent != 125 && data.uiScalePercent != 150) data.uiScalePercent = 100;
            var old = data.bestStars ?? Array.Empty<int>();
            var fitted = new int[LevelCount];
            for (int i = 0; i < fitted.Length && i < old.Length; i++)
            {
                fitted[i] = Math.Max(0, Math.Min(old[i], MAX_STARS));
            }
            data.bestStars = fitted;
            var held = data.boosters ?? Array.Empty<int>();
            var boosters = new int[BoosterBundle.TYPE_COUNT];
            for (int i = 0; i < boosters.Length && i < held.Length; i++)
            {
                boosters[i] = Math.Max(0, held[i]);
            }
            if (!data.startingBoostersGranted)
            {
                // New save, or a version 1 save from before boosters existed: hand out the starting stock once,
                // plus every milestone the player has already passed so an old save is not short-changed.
                var grant = rules.StartingBoosters;
                for (int level = 1; level <= LevelCount; level++)
                {
                    if (fitted[level - 1] > 0)
                    {
                        grant = grant.Plus(rules.MilestoneReward(level));
                    }
                }
                for (int i = 0; i < boosters.Length; i++)
                {
                    boosters[i] += grant.Of((BoosterType)i);
                }
                data.startingBoostersGranted = true;
            }
            data.boosters = boosters;
            // A hand-edited or damaged save can never owe more stars than the player has earned.
            data.starsSpent = Math.Max(0, Math.Min(data.starsSpent, TotalStars));
            data.version = ProgressData.CURRENT_VERSION;
        }
    }
}
