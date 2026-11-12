// Implements: design/game-brief.md "Quyết định đã chốt" (levels.json: each letter is one piece at its
// reference solution, '#' is blocked; a piece starts turned startTurns × 90° from its solution shape),
// "Progression & Meta" (grid 4x3–7x7, 3–13 items) and "Obstacles" (a level's solution must obey its own rules).
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-002-level-data.md
// Pure C#: no MonoBehaviour dependency. This is the JSON boundary, so it is the only place that validates data.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>A level ready to play: its data, an empty board, its rules and each piece's start and solution.</summary>
    public sealed class LoadedLevel
    {
        /// <summary>The level as parsed from JSON.</summary>
        public LevelData Data { get; }

        /// <summary>Read-only meta for the UI (name, timer, banner, rule chips).</summary>
        public LevelInfo Info { get; }

        /// <summary>Empty board with the level's blocked cells and active rules.</summary>
        public BoardModel Board { get; }

        /// <summary>The level's active obstacle rules.</summary>
        public LevelRules Rules { get; }

        /// <summary>Per piece (same index as Data.pieces): solution shape turned startTurns times.</summary>
        public PieceShape[] StartShapes { get; }

        /// <summary>Per piece: shape in the reference solution (its correct orientation).</summary>
        public PieceShape[] SolutionShapes { get; }

        /// <summary>Per piece: grid cell of the solution shape's top-left corner.</summary>
        public Vector2Int[] SolutionOrigins { get; }

        /// <summary>Per piece (same index as Data.pieces): the item it shows.</summary>
        public ItemData[] Items { get; }

        /// <summary>Creates a loaded level.</summary>
        public LoadedLevel(LevelData data, BoardModel board, LevelRules rules, PieceShape[] startShapes,
            PieceShape[] solutionShapes, Vector2Int[] solutionOrigins, ItemData[] items)
        {
            Data = data;
            Info = new LevelInfo(data);
            Board = board;
            Rules = rules;
            StartShapes = startShapes;
            SolutionShapes = solutionShapes;
            SolutionOrigins = solutionOrigins;
            Items = items;
        }

        /// <summary>Grid cells piece <paramref name="pieceId"/> covers in the reference solution.</summary>
        public Vector2Int[] SolutionCellsOf(int pieceId)
        {
            var shape = SolutionShapes[pieceId];
            var cells = new Vector2Int[shape.Cells.Count];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = SolutionOrigins[pieceId] + shape.Cells[i];
            }
            return cells;
        }

        /// <summary>True if the piece is on the board exactly at its reference solution (same cells).</summary>
        public bool IsAtSolution(BoardModel board, int pieceId) =>
            board.TryGetPlacement(pieceId, out var shape, out var origin) &&
            origin == SolutionOrigins[pieceId] && shape.SameCells(SolutionShapes[pieceId]);

        /// <summary>The zone a piece must sit in, or null if it may go anywhere (or the level has no zones).</summary>
        public ZoneData ZoneOf(int pieceId)
        {
            string key = Data.pieces[pieceId].zone;
            if (string.IsNullOrEmpty(key) || Data.zones == null)
            {
                return null;
            }
            return Array.Find(Data.zones, z => z.key == key);
        }
    }

    /// <summary>Read-only level meta for the UI wave. Strings are verbatim; pass them through <see cref="TmpText.Clean"/> for TMP.</summary>
    public sealed class LevelInfo
    {
        /// <summary>1-based level number.</summary>
        public int Number { get; }

        /// <summary>Level name, e.g. "Weekend suitcase".</summary>
        public string Name { get; }

        /// <summary>Countdown in seconds, 0 = no timer.</summary>
        public int TimerSeconds { get; }

        /// <summary>True if the level is timed.</summary>
        public bool HasTimer => TimerSeconds > 0;

        /// <summary>New-rule banner title, "" = none.</summary>
        public string BannerTitle { get; }

        /// <summary>New-rule banner subtitle, "" = none.</summary>
        public string BannerSubtitle { get; }

        /// <summary>Rule chips shown with the level (may be empty).</summary>
        public IReadOnlyList<string> RuleChips { get; }

        /// <summary>Copies the meta out of <paramref name="data"/>.</summary>
        public LevelInfo(LevelData data)
        {
            Number = data.number;
            Name = data.name ?? "";
            TimerSeconds = data.timerSeconds;
            BannerTitle = data.bannerTitle ?? "";
            BannerSubtitle = data.bannerSubtitle ?? "";
            RuleChips = data.ruleChips != null ? (string[])data.ruleChips.Clone() : new string[0];
        }
    }

    /// <summary>Parses levels.json, validates each level and turns its letter map into a board, rules and piece shapes.</summary>
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

        /// <summary>
        /// Builds every level once and returns one message per problem: a level out of order
        /// (levels[i].number must be i + 1) or a level <see cref="Build"/> rejects.
        /// </summary>
        public static List<string> ValidateAll(LevelsFile file, LevelLimits limits)
        {
            var errors = new List<string>();
            for (int i = 0; i < file.levels.Length; i++)
            {
                var level = file.levels[i];
                if (level.number != i + 1)
                {
                    errors.Add($"levels[{i}] has number {level.number}; levels must be numbered 1..{file.levels.Length} in order.");
                }
                if (Build(file, level, limits, out string error) == null)
                {
                    errors.Add(error);
                }
            }
            return errors;
        }

        /// <summary>
        /// Builds the board, rules and pieces for one level, rejecting it if its data is malformed, outside
        /// <paramref name="limits"/>, or its reference solution breaks its own rules.
        /// </summary>
        /// <param name="file">Parsed levels file (supplies the items).</param>
        /// <param name="level">The level to build.</param>
        /// <param name="limits">Allowed grid size and piece count.</param>
        /// <param name="error">Why the level was rejected, or null.</param>
        /// <returns>The loaded level, or null if rejected.</returns>
        public static LoadedLevel Build(LevelsFile file, LevelData level, LevelLimits limits, out string error)
        {
            string at = $"Level {level.number}";
            if (level.map == null || level.map.Length == 0 || level.pieces == null || level.pieces.Length == 0)
            {
                error = $"{at} has no map or no pieces.";
                return null;
            }
            if (level.theme == null)
            {
                error = $"{at} has no theme.";
                return null;
            }

            int height = level.map.Length;
            int width = level.map[0].Length;
            foreach (var row in level.map)
            {
                if (row.Length != width)
                {
                    error = $"{at}: map rows have different lengths.";
                    return null;
                }
            }
            if (width < limits.minWidth || width > limits.maxWidth || height < limits.minHeight || height > limits.maxHeight)
            {
                error = $"{at}: grid {width}x{height} is outside {limits.minWidth}x{limits.minHeight} to {limits.maxWidth}x{limits.maxHeight}.";
                return null;
            }
            if (level.pieces.Length < limits.minPieces || level.pieces.Length > limits.maxPieces)
            {
                error = $"{at}: {level.pieces.Length} items is outside {limits.minPieces}–{limits.maxPieces}.";
                return null;
            }

            var blocked = new List<Vector2Int>();
            var cellsByLetter = new Dictionary<char, List<Vector2Int>>();
            for (int y = 0; y < height; y++)
            {
                string row = level.map[y];
                for (int x = 0; x < width; x++)
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

            int count = level.pieces.Length;
            var startShapes = new PieceShape[count];
            var solutionShapes = new PieceShape[count];
            var solutionOrigins = new Vector2Int[count];
            var items = new ItemData[count];
            var usedLetters = new HashSet<char>();
            for (int i = 0; i < count; i++)
            {
                var piece = level.pieces[i];
                if (string.IsNullOrEmpty(piece.letter) || piece.letter.Length != 1 || !cellsByLetter.TryGetValue(piece.letter[0], out var cells))
                {
                    error = $"{at}: piece {i} has letter '{piece.letter}', which is not a single letter in the map.";
                    return null;
                }
                if (!usedLetters.Add(piece.letter[0]))
                {
                    error = $"{at}: two pieces use letter '{piece.letter}'.";
                    return null;
                }
                if (!IsConnected(cells))
                {
                    error = $"{at}: piece '{piece.letter}' is not one connected shape.";
                    return null;
                }
                items[i] = Array.Find(file.items, item => item.id == piece.item);
                if (items[i] == null)
                {
                    error = $"{at}: piece '{piece.letter}' uses item '{piece.item}', which is not in items[].";
                    return null;
                }

                var solution = PieceShape.FromCells(cells);
                solutionShapes[i] = solution;
                solutionOrigins[i] = MinCorner(cells);
                startShapes[i] = solution.Rotated(piece.startTurns);
                if (piece.upright && !startShapes[i].SameCells(solution))
                {
                    error = $"{at}: upright piece '{piece.letter}' starts turned (startTurns {piece.startTurns}) and can never reach its solution.";
                    return null;
                }
            }
            foreach (var letter in cellsByLetter.Keys)
            {
                if (!usedLetters.Contains(letter))
                {
                    error = $"{at}: map letter '{letter}' has no piece, so its cells could never be filled.";
                    return null;
                }
            }

            if (!CheckZones(level, width, height, at, out error) || !CheckPairs(level, at, out error))
            {
                return null;
            }

            var rules = LevelRules.FromLevel(level, height);

            // The reference solution must obey the level's own rules (story 002): place every piece at its
            // solution through the same rule checks the player faces, then run the completion checks.
            var check = new BoardModel(width, height, blocked, count, rules.All);
            for (int i = 0; i < count; i++)
            {
                var failure = check.CheckPlacement(i, solutionShapes[i], solutionOrigins[i]);
                if (failure != PlacementFailure.None)
                {
                    error = $"{at}: the reference solution breaks its own rules — piece '{level.pieces[i].letter}' fails {failure}.";
                    return null;
                }
                check.Place(i, solutionShapes[i], solutionOrigins[i]);
            }
            var violation = check.FindCompletionViolation();
            if (violation.IsViolation)
            {
                error = $"{at}: the reference solution breaks its own rules — piece '{level.pieces[violation.pieceId].letter}' fails {violation.rule.GetType().Name}.";
                return null;
            }

            error = null;
            var board = new BoardModel(width, height, blocked, count, rules.All);
            return new LoadedLevel(level, board, rules, startShapes, solutionShapes, solutionOrigins, items);
        }

        // Zones: zoneMap matches the map's size and blocked cells, uses only declared single-character keys,
        // and every zoned piece names a declared zone. Without zones, no piece may name one.
        static bool CheckZones(LevelData level, int width, int height, string at, out string error)
        {
            bool hasZones = level.zones != null && level.zones.Length > 0;
            var keys = new HashSet<char>();
            if (hasZones)
            {
                foreach (var zone in level.zones)
                {
                    if (string.IsNullOrEmpty(zone.key) || zone.key.Length != 1 || zone.key[0] == ZoneRule.BLOCKED_ZONE || !keys.Add(zone.key[0]))
                    {
                        error = $"{at}: zone key '{zone.key}' must be one unique character other than '#'.";
                        return false;
                    }
                }
                if (level.zoneMap == null || level.zoneMap.Length != height)
                {
                    error = $"{at}: zoneMap must have {height} rows like the map.";
                    return false;
                }
                for (int y = 0; y < height; y++)
                {
                    if (level.zoneMap[y].Length != width)
                    {
                        error = $"{at}: zoneMap row {y} must have {width} cells like the map.";
                        return false;
                    }
                    for (int x = 0; x < width; x++)
                    {
                        char z = level.zoneMap[y][x];
                        bool blockedInMap = level.map[y][x] == BLOCKED_CELL;
                        if (blockedInMap != (z == ZoneRule.BLOCKED_ZONE) || (!blockedInMap && !keys.Contains(z)))
                        {
                            error = $"{at}: zoneMap cell ({x}, {y}) is '{z}', which does not match the map or the zones.";
                            return false;
                        }
                    }
                }
            }
            foreach (var piece in level.pieces)
            {
                if (!string.IsNullOrEmpty(piece.zone) && (piece.zone.Length != 1 || !keys.Contains(piece.zone[0])))
                {
                    error = $"{at}: piece '{piece.letter}' has zone '{piece.zone}', which is not in zones[].";
                    return false;
                }
            }
            error = null;
            return true;
        }

        // Pairs: each pair index is shared by exactly two pieces.
        static bool CheckPairs(LevelData level, string at, out string error)
        {
            var counts = new Dictionary<int, int>();
            foreach (var piece in level.pieces)
            {
                if (piece.pair >= 0)
                {
                    counts.TryGetValue(piece.pair, out int n);
                    counts[piece.pair] = n + 1;
                }
            }
            foreach (var entry in counts)
            {
                if (entry.Value != 2)
                {
                    error = $"{at}: pair {entry.Key} has {entry.Value} pieces; a pair needs exactly 2.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        static bool IsConnected(List<Vector2Int> cells)
        {
            var remaining = new HashSet<Vector2Int>(cells);
            var stack = new Stack<Vector2Int>();
            stack.Push(cells[0]);
            remaining.Remove(cells[0]);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                for (int d = 0; d < 4; d++)
                {
                    var n = c + (d == 0 ? Vector2Int.right : d == 1 ? Vector2Int.left : d == 2 ? Vector2Int.up : Vector2Int.down);
                    if (remaining.Remove(n))
                    {
                        stack.Push(n);
                    }
                }
            }
            return remaining.Count == 0;
        }

        static Vector2Int MinCorner(List<Vector2Int> cells)
        {
            var min = cells[0];
            foreach (var c in cells)
            {
                min = Vector2Int.Min(min, c);
            }
            return min;
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
