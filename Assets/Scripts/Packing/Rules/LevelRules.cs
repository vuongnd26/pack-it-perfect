// Implements: design/game-brief.md "Obstacles" — which rules a level uses comes from its data flags, never its number.
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency.

using System.Collections.Generic;

namespace PackItPerfect
{
    /// <summary>The obstacle rules active in one level. Each typed rule is null when the level does not use it.</summary>
    public sealed class LevelRules
    {
        /// <summary>Zones, or null when the level has no zones.</summary>
        public ZoneRule Zones { get; }

        /// <summary>Pairs, or null when no piece is in a pair.</summary>
        public PairRule Pairs { get; }

        /// <summary>Heavy items, or null when no piece is heavy.</summary>
        public HeavyRule Heavy { get; }

        /// <summary>Upright items, or null when no piece is upright.</summary>
        public UprightRule Upright { get; }

        /// <summary>Every active rule, in the order the board evaluates them.</summary>
        public IReadOnlyList<IPackingRule> All { get; }

        LevelRules(ZoneRule zones, PairRule pairs, HeavyRule heavy, UprightRule upright)
        {
            Zones = zones;
            Pairs = pairs;
            Heavy = heavy;
            Upright = upright;
            var all = new List<IPackingRule>();
            if (zones != null) all.Add(zones);
            if (heavy != null) all.Add(heavy);
            if (pairs != null) all.Add(pairs);
            if (upright != null) all.Add(upright);
            All = all;
        }

        /// <summary>Creates the rules a level's data asks for.</summary>
        /// <param name="level">Level data (zones, zoneMap and per-piece flags).</param>
        /// <param name="gridHeight">Rows in the level's grid.</param>
        public static LevelRules FromLevel(LevelData level, int gridHeight)
        {
            var pieces = level.pieces;
            int count = pieces.Length;
            var heavy = new bool[count];
            var upright = new bool[count];
            var pairIndex = new int[count];
            bool anyHeavy = false, anyUpright = false, anyPair = false;
            for (int i = 0; i < count; i++)
            {
                heavy[i] = pieces[i].heavy;
                upright[i] = pieces[i].upright;
                pairIndex[i] = pieces[i].pair < 0 ? PairRule.NO_PAIR : pieces[i].pair;
                anyHeavy |= heavy[i];
                anyUpright |= upright[i];
                anyPair |= pairIndex[i] != PairRule.NO_PAIR;
            }

            bool hasZones = level.zones != null && level.zones.Length > 0;
            return new LevelRules(
                hasZones ? new ZoneRule(level.zoneMap, pieces) : null,
                anyPair ? new PairRule(pairIndex) : null,
                anyHeavy ? new HeavyRule(heavy, gridHeight) : null,
                anyUpright ? new UprightRule(upright) : null);
        }
    }
}
