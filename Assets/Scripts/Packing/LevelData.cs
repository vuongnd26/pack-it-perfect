// Implements: design/game-brief.md "Quyết định đã chốt" (level data in Assets/Data/Levels/levels.json).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md
// These classes mirror the full levels.json schema so JsonUtility parses every field;
// story 001 reads only map, pieces[].letter/item/startTurns, items[] and theme.
// Field names must match the JSON keys exactly, so they are lower camelCase public fields.

using System;

namespace PackItPerfect
{
    /// <summary>Root object of levels.json.</summary>
    [Serializable]
    public class LevelsFile
    {
        /// <summary>Where the data was extracted from.</summary>
        public string source;
        /// <summary>Every item type a piece can reference by id.</summary>
        public ItemData[] items;
        /// <summary>All levels, in play order.</summary>
        public LevelData[] levels;
    }

    /// <summary>One item type (shirt, sock, ...).</summary>
    [Serializable]
    public class ItemData
    {
        /// <summary>Key referenced by <see cref="PieceData.item"/>.</summary>
        public string id;
        /// <summary>Name shown to the player.</summary>
        public string displayName;
        /// <summary>Piece colour, "#RRGGBB".</summary>
        public string color;
        /// <summary>Surface pattern name (story 009).</summary>
        public string pattern;
    }

    /// <summary>One level.</summary>
    [Serializable]
    public class LevelData
    {
        /// <summary>1-based level number.</summary>
        public int number;
        /// <summary>Level name, e.g. "Weekend suitcase".</summary>
        public string name;
        /// <summary>Rows of the grid, row 0 is the top. Each letter is one piece at its solution cells, '#' is blocked.</summary>
        public string[] map;
        /// <summary>Countdown in seconds, 0 = no timer (story 004).</summary>
        public int timerSeconds;
        /// <summary>New-rule banner title (story 006).</summary>
        public string bannerTitle;
        /// <summary>New-rule banner subtitle (story 006).</summary>
        public string bannerSubtitle;
        /// <summary>Rule chips shown with the level (story 006).</summary>
        public string[] ruleChips;
        /// <summary>Rows of zone keys, same size as <see cref="map"/> (story 003).</summary>
        public string[] zoneMap;
        /// <summary>Zone definitions (story 003).</summary>
        public ZoneData[] zones;
        /// <summary>Pieces of this level, one per map letter.</summary>
        public PieceData[] pieces;
        /// <summary>Container colours.</summary>
        public ThemeData theme;
    }

    /// <summary>One zone of a level (story 003).</summary>
    [Serializable]
    public class ZoneData
    {
        /// <summary>Key used in <see cref="LevelData.zoneMap"/>.</summary>
        public string key;
        /// <summary>Zone icon.</summary>
        public string icon;
        /// <summary>Zone name.</summary>
        public string name;
        /// <summary>Zone tint, "#RRGGBBAA".</summary>
        public string color;
    }

    /// <summary>One piece of a level.</summary>
    [Serializable]
    public class PieceData
    {
        /// <summary>Single map letter marking this piece's solution cells.</summary>
        public string letter;
        /// <summary>Id of the <see cref="ItemData"/> this piece shows.</summary>
        public string item;
        /// <summary>Quarter turns applied to the solution shape for the piece's tray start.</summary>
        public int startTurns;
        /// <summary>Zone key the piece must sit in, "" = any (story 003).</summary>
        public string zone;
        /// <summary>Pair group index, -1 = none (story 003).</summary>
        public int pair;
        /// <summary>Must touch the bottom row (story 003).</summary>
        public bool heavy;
        /// <summary>Cannot be rotated (story 003).</summary>
        public bool upright;
        /// <summary>Shows a faded outline at its solution (story 003).</summary>
        public bool outline;
    }

    /// <summary>Container colours of a level.</summary>
    [Serializable]
    public class ThemeData
    {
        /// <summary>Frame colour around the grid, "#RRGGBB".</summary>
        public string frame;
        /// <summary>Rim colour, "#RRGGBB".</summary>
        public string rim;
        /// <summary>Lining (empty cell) colour, "#RRGGBB".</summary>
        public string lining;
        /// <summary>Grid line colour, "#RRGGBBAA".</summary>
        public string line;
        /// <summary>Blocked cell colour, "#RRGGBBAA".</summary>
        public string block;
        /// <summary>Container has a handle (story 009).</summary>
        public bool handle;
        /// <summary>Container has wheels (story 009).</summary>
        public bool wheels;
    }
}
