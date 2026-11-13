// Booster unlocks and milestone rewards, read from Assets/Resources/Data/progression.json.
// Implements: design/game-brief.md "Nâng cấp Cozy Travel" (boosters open and are handed out at level milestones).
// Pure C# apart from the JSON boundary (FromJson), so the rules are unit-testable.
// Wire format: snake_case, schema_version 1. Rationale for every number: design/game-brief.md, same section.

using System;
using UnityEngine;

namespace PackItPerfect.Meta
{
    /// <summary>The boosters a player can hold. Values index <see cref="ProgressData.boosters"/>; never reorder.</summary>
    public enum BoosterType
    {
        /// <summary>Lights where the next item goes and turns it the right way.</summary>
        Hint = 0,
        /// <summary>Packs one item into a spot that keeps the level solvable.</summary>
        AutoPack = 1,
        /// <summary>Holds the countdown of a timed level.</summary>
        Freeze = 2,
    }

    /// <summary>A bundle of boosters (a reward or a starting stock).</summary>
    public readonly struct BoosterBundle
    {
        /// <summary>Number of booster types.</summary>
        public const int TYPE_COUNT = 3;

        /// <summary>Hint charges.</summary>
        public readonly int hint;
        /// <summary>Auto Pack charges.</summary>
        public readonly int autoPack;
        /// <summary>Freeze charges.</summary>
        public readonly int freeze;

        /// <summary>Creates a bundle; negative amounts count as 0.</summary>
        public BoosterBundle(int hint, int autoPack, int freeze)
        {
            this.hint = Math.Max(0, hint);
            this.autoPack = Math.Max(0, autoPack);
            this.freeze = Math.Max(0, freeze);
        }

        /// <summary>True if the bundle holds nothing.</summary>
        public bool IsEmpty => hint == 0 && autoPack == 0 && freeze == 0;

        /// <summary>Charges of one type.</summary>
        public int Of(BoosterType type) => type == BoosterType.Hint ? hint : type == BoosterType.AutoPack ? autoPack : freeze;

        /// <summary>Sum of two bundles.</summary>
        public BoosterBundle Plus(BoosterBundle other) =>
            new BoosterBundle(hint + other.hint, autoPack + other.autoPack, freeze + other.freeze);
    }

    /// <summary>Immutable progression rules: when each booster opens and what each milestone gives.</summary>
    public sealed class ProgressionRules
    {
        /// <summary>Resource path of the config, relative to a Resources folder.</summary>
        public const string RESOURCE = "Data/progression";
        const int SCHEMA_VERSION = 1;

        // Wire DTOs: the only place snake_case names appear.
        [Serializable] sealed class BundleDto { public int hint; public int auto_pack; public int freeze; }
        [Serializable] sealed class UnlockDto { public int hint; public int auto_pack; public int freeze; }
        [Serializable] sealed class MilestoneDto { public int after_level; public int hint; public int auto_pack; public int freeze; }
        [Serializable] sealed class FileDto
        {
            public int schema_version;
            public BundleDto starting_boosters;
            public UnlockDto unlock_after_level;
            public BundleDto three_star_first_clear_bonus;
            public float freeze_seconds;
            public MilestoneDto[] milestones;
        }

        readonly int[] unlockAfterLevel;
        readonly int[] milestoneLevels;
        readonly BoosterBundle[] milestoneRewards;

        /// <summary>Boosters a brand-new save starts with.</summary>
        public BoosterBundle StartingBoosters { get; }
        /// <summary>Extra boosters for the first time a level is cleared with 3 stars.</summary>
        public BoosterBundle ThreeStarBonus { get; }
        /// <summary>How long one Freeze holds the clock, in seconds.</summary>
        public float FreezeSeconds { get; }

        /// <summary>Creates rules from plain values (tests) — <see cref="FromJson"/> is the data path.</summary>
        /// <param name="unlockAfterLevel">Per <see cref="BoosterType"/>: the level whose first clear opens it (0 = open from the start).</param>
        /// <param name="milestoneLevels">Levels whose first clear gives a reward.</param>
        /// <param name="milestoneRewards">Reward per entry of <paramref name="milestoneLevels"/>.</param>
        public ProgressionRules(BoosterBundle startingBoosters, int[] unlockAfterLevel, BoosterBundle threeStarBonus,
            float freezeSeconds, int[] milestoneLevels, BoosterBundle[] milestoneRewards)
        {
            StartingBoosters = startingBoosters;
            this.unlockAfterLevel = (int[])unlockAfterLevel.Clone();
            ThreeStarBonus = threeStarBonus;
            FreezeSeconds = freezeSeconds;
            this.milestoneLevels = (int[])milestoneLevels.Clone();
            this.milestoneRewards = (BoosterBundle[])milestoneRewards.Clone();
        }

        /// <summary>Rules with no boosters at all (used when the config is missing, so the game still runs).</summary>
        public static ProgressionRules Empty() => new ProgressionRules(default, new int[BoosterBundle.TYPE_COUNT], default, 0f,
            new int[0], new BoosterBundle[0]);

        /// <summary>Parses progression.json. Returns null and an error when the text is not a supported config.</summary>
        public static ProgressionRules FromJson(string json, out string error)
        {
            FileDto dto;
            try
            {
                dto = JsonUtility.FromJson<FileDto>(json);
            }
            catch (ArgumentException e)
            {
                error = $"progression.json is not valid JSON: {e.Message}";
                return null;
            }
            if (dto == null || dto.schema_version != SCHEMA_VERSION)
            {
                error = $"progression.json: schema_version must be {SCHEMA_VERSION}.";
                return null;
            }
            if (dto.freeze_seconds <= 0f)
            {
                error = "progression.json: freeze_seconds must be above 0.";
                return null;
            }
            var unlock = dto.unlock_after_level ?? new UnlockDto();
            var milestones = dto.milestones ?? new MilestoneDto[0];
            var levels = new int[milestones.Length];
            var rewards = new BoosterBundle[milestones.Length];
            for (int i = 0; i < milestones.Length; i++)
            {
                if (milestones[i].after_level < 1 || Array.IndexOf(levels, milestones[i].after_level) >= 0)
                {
                    error = $"progression.json: milestone {i} needs a unique after_level of 1 or more.";
                    return null;
                }
                levels[i] = milestones[i].after_level;
                rewards[i] = new BoosterBundle(milestones[i].hint, milestones[i].auto_pack, milestones[i].freeze);
            }
            error = null;
            return new ProgressionRules(Bundle(dto.starting_boosters),
                new[] { Math.Max(0, unlock.hint), Math.Max(0, unlock.auto_pack), Math.Max(0, unlock.freeze) },
                Bundle(dto.three_star_first_clear_bonus), dto.freeze_seconds, levels, rewards);
        }

        static BoosterBundle Bundle(BundleDto dto) =>
            dto == null ? default : new BoosterBundle(dto.hint, dto.auto_pack, dto.freeze);

        /// <summary>The level whose first clear opens <paramref name="type"/> (0 = open from the start).</summary>
        public int UnlockAfterLevel(BoosterType type) => unlockAfterLevel[(int)type];

        /// <summary>True if <paramref name="type"/> is open for a player whose highest cleared level is <paramref name="highestLevelPassed"/>.</summary>
        public bool IsUnlocked(BoosterType type, int highestLevelPassed) => highestLevelPassed >= unlockAfterLevel[(int)type];

        /// <summary>Milestone reward for the first clear of <paramref name="level"/> (empty when it is not a milestone).</summary>
        public BoosterBundle MilestoneReward(int level)
        {
            int index = Array.IndexOf(milestoneLevels, level);
            return index >= 0 ? milestoneRewards[index] : default;
        }

        /// <summary>The next milestone level above <paramref name="highestLevelPassed"/>, or 0 when there is none.</summary>
        public int NextMilestoneAfter(int highestLevelPassed)
        {
            int next = 0;
            foreach (int level in milestoneLevels)
            {
                if (level > highestLevelPassed && (next == 0 || level < next))
                {
                    next = level;
                }
            }
            return next;
        }
    }

    /// <summary>What clearing a level gave the player. Returned by <see cref="ProgressService.CompleteLevel"/>.</summary>
    public sealed class LevelReward
    {
        /// <summary>True if this was the first win on the level.</summary>
        public bool FirstClear { get; }
        /// <summary>True if the level's best stars improved.</summary>
        public bool Improved { get; }
        /// <summary>Boosters added to the inventory by this win (milestone + 3-star bonus); may be empty.</summary>
        public BoosterBundle Boosters { get; }
        /// <summary>Boosters that opened with this win, in <see cref="BoosterType"/> order (may be empty).</summary>
        public BoosterType[] Unlocked { get; }

        /// <summary>Creates a result.</summary>
        public LevelReward(bool firstClear, bool improved, BoosterBundle boosters, BoosterType[] unlocked)
        {
            FirstClear = firstClear;
            Improved = improved;
            Boosters = boosters;
            Unlocked = unlocked;
        }
    }
}
