// Implements: design/game-brief.md "Quyết định đã chốt" (levels.json: each letter is one piece at its
// reference solution, '#' is blocked; a piece starts turned startTurns × 90° from its solution shape).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md
// Pure C#: no MonoBehaviour dependency. This is the JSON boundary, so it is the only place that validates data.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>A level ready to play: its data, an empty board and each piece's tray start shape and item.</summary>
    public sealed class LoadedLevel
    {
        /// <summary>The level as parsed from JSON.</summary>
        public LevelData Data { get; }

        /// <summary>Empty board with the level's blocked cells.</summary>
        public BoardModel Board { get; }

        /// <summary>Per piece (same index as Data.pieces): solution shape turned startTurns times.</summary>
        public PieceShape[] StartShapes { get; }

        /// <summary>Per piece (same index as Data.pieces): the item it shows.</summary>
        public ItemData[] Items { get; }

        /// <summary>Creates a loaded level.</summary>
        public LoadedLevel(LevelData data, BoardModel board, PieceShape[] startShapes, ItemData[] items)
        {
            Data = data;
            Board = board;
            StartShapes = startShapes;
            Items = items;
        }
    }

    /// <summary>Parses levels.json and turns a level's letter map into a board and piece shapes.</summary>
    public static class LevelLoader
    {
        const char BLOCKED_CELL = '#';

        /// <summary>Parses the whole file.</summary>
        /// <param name="json">Contents of levels.json.</param>
        /// <param name="error">Why parsing failed, or null.</param>
        /// <returns>The parsed file, or null on failure.</returns>
        public static LevelsFile Parse(string json, out string error)
        {
            LevelsFile file;
            try
            {
                file = JsonUtility.FromJson<LevelsFile>(json);
            }
            catch (ArgumentException e)
            {
                error = $"levels.json is not valid JSON: {e.Message}";
                return null;
            }

            if (file == null || file.levels == null || file.levels.Length == 0)
            {
                error = "levels.json has no levels.";
                return null;
            }
            if (file.items == null)
            {
                error = "levels.json has no items.";
                return null;
            }
            error = null;
            return file;
        }

        /// <summary>Builds the board and the pieces' start shapes for one level.</summary>
        /// <param name="file">Parsed levels file (supplies the items).</param>
        /// <param name="level">The level to build.</param>
        /// <param name="error">Why the level could not be built, or null.</param>
        /// <returns>The loaded level, or null on failure.</returns>
        public static LoadedLevel Build(LevelsFile file, LevelData level, out string error)
        {
            if (level.map == null || level.map.Length == 0 || level.pieces == null || level.pieces.Length == 0)
            {
                error = $"Level {level.number} has no map or no pieces.";
                return null;
            }

            int height = level.map.Length;
            int width = 0;
            foreach (var row in level.map)
            {
                width = Mathf.Max(width, row.Length);
            }

            var blocked = new List<Vector2Int>();
            var cellsByLetter = new Dictionary<char, List<Vector2Int>>();
            for (int y = 0; y < height; y++)
            {
                string row = level.map[y];
                for (int x = 0; x < row.Length; x++)
                {
                    char ch = row[x];
                    if (ch == BLOCKED_CELL)
                    {
                        blocked.Add(new Vector2Int(x, y));
                        continue;
                    }
                    if (!cellsByLetter.TryGetValue(ch, out var cells))
                    {
                        cells = new List<Vector2Int>();
                        cellsByLetter[ch] = cells;
                    }
                    cells.Add(new Vector2Int(x, y));
                }
            }

            var startShapes = new PieceShape[level.pieces.Length];
            var items = new ItemData[level.pieces.Length];
            for (int i = 0; i < level.pieces.Length; i++)
            {
                var piece = level.pieces[i];
                if (string.IsNullOrEmpty(piece.letter) || !cellsByLetter.TryGetValue(piece.letter[0], out var cells))
                {
                    error = $"Level {level.number}: piece {i} has letter '{piece.letter}', which is not in the map.";
                    return null;
                }
                items[i] = Array.Find(file.items, item => item.id == piece.item);
                if (items[i] == null)
                {
                    error = $"Level {level.number}: piece '{piece.letter}' uses item '{piece.item}', which is not in items[].";
                    return null;
                }
                startShapes[i] = PieceShape.FromCells(cells).Rotated(piece.startTurns);
            }

            error = null;
            return new LoadedLevel(level, new BoardModel(width, height, blocked, level.pieces.Length), startShapes, items);
        }

        /// <summary>Parses "#RRGGBB" or "#RRGGBBAA". Logs an error and returns magenta if the string is not a colour.</summary>
        public static Color ParseColor(string html)
        {
            if (ColorUtility.TryParseHtmlString(html, out var color))
            {
                return color;
            }
            Debug.LogError($"levels.json: '{html}' is not a colour (expected #RRGGBB or #RRGGBBAA).");
            return Color.magenta;
        }
    }
}
