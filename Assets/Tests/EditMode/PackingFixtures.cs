using System;
using System.Text;
using NUnit.Framework;
using PackItPerfect.Meta;
using UnityEngine;

namespace PackItPerfect.Tests
{
    // New objects on every call: no catalog I/O, random choices, or shared mutable game state.
    internal static class PackingFixtures
    {
        internal const long SearchLimit = 1000;
        internal const int LevelCount = 100;
        internal const int WarningSeconds = 5;
        internal const int CountdownSeconds = 30;
        internal const float StartDelay = 2f;
        internal const float FreezeSeconds = 10f;
        internal const float Tolerance = 0.0001f;

        internal static PieceShape Single() => PieceShape.FromCells(new[] { Vector2Int.zero });

        internal static PieceShape Domino() =>
            PieceShape.FromCells(new[] { Vector2Int.zero, Vector2Int.right });

        internal static PieceData Piece(string letter, bool heavy = false, bool fragile = false) =>
            new PieceData { letter = letter, item = "fixture", pair = -1, zone = "", heavy = heavy, fragile = fragile };

        internal static LevelsFile File(LevelData data) => new LevelsFile
        {
            items = new[] { new ItemData { id = "fixture", displayName = "Fixture", color = "#FFFFFF", pattern = "x" } },
            levels = new[] { data },
        };

        internal static LevelData Data(string[] map, params PieceData[] pieces) => new LevelData
        {
            number = 1, name = "In-memory fixture", theme = new ThemeData(), map = map, pieces = pieces,
        };

        internal static LevelLimits SmallLimits() => new LevelLimits
        {
            minWidth = 1, minHeight = 1, maxWidth = 7, maxHeight = 7, minPieces = 1, maxPieces = 13,
        };

        internal static LoadedLevel Build(LevelData data)
        {
            var level = LevelLoader.Build(File(data), data, SmallLimits(), out string error);
            Assert.That(level, Is.Not.Null, error);
            return level;
        }

        internal static LoadedLevel DominoLevel() =>
            Build(Data(new[] { "AA", "BB" }, Piece("A"), Piece("B")));

        internal static LoadedLevel HintConflictLevel(bool targetHeavy)
        {
            var map = targetHeavy ? new[] { "CCB", "CCE", "ADD" } : new[] { "CCE", "ACC", "DDB" };
            return Build(Data(map, Piece("A", heavy: targetHeavy, fragile: !targetHeavy),
                Piece("B", heavy: !targetHeavy, fragile: targetHeavy), Piece("C"), Piece("D"), Piece("E")));
        }

        internal static BoardModel Board(int width, int height, int pieceCount, params IPackingRule[] rules) =>
            new BoardModel(width, height, Array.Empty<Vector2Int>(), pieceCount, rules);

        internal static string Snapshot(BoardModel board)
        {
            var state = new StringBuilder();
            state.Append(board.Width).Append('/').Append(board.Height).Append('/').Append(board.CoveredCellCount);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    state.Append('|').Append(board.IsBlocked(cell)).Append(':').Append(board.PieceAt(cell));
                }
            }
            for (int id = 0; id < board.PieceCount; id++)
            {
                if (!board.TryGetPlacement(id, out var shape, out var origin)) continue;
                state.Append(';').Append(id).Append('@').Append(origin.x).Append(',').Append(origin.y);
                foreach (var cell in shape.Cells) state.Append(':').Append(cell.x).Append(',').Append(cell.y);
            }
            return state.ToString();
        }

        internal static void ReplayReference(LoadedLevel level)
        {
            for (int id = 0; id < level.Board.PieceCount; id++)
            {
                Assert.That(level.Board.Place(id, level.SolutionShapes[id], level.SolutionOrigins[id]), Is.True,
                    $"Level {level.Data.number}, piece {id}: reference placement rejected.");
            }
            Assert.That(level.Board.CoveredCellCount, Is.EqualTo(level.Board.FillableCellCount));
            Assert.That(level.Board.IsWon(), Is.True, $"Level {level.Data.number}: reference did not win.");
        }

        internal static ProgressionRules Progression() => new ProgressionRules(
            new BoosterBundle(0, 0, 0), new[] { 1, 6, 4 }, new BoosterBundle(1, 0, 0), FreezeSeconds,
            new[] { 1, 4, 6 },
            new[] { new BoosterBundle(3, 0, 0), new BoosterBundle(0, 0, 2), new BoosterBundle(0, 2, 0) });
    }

    internal sealed class MemorySaveStore : ISaveStore
    {
        internal ProgressData Saved { get; private set; }
        internal int SaveCount { get; private set; }

        internal MemorySaveStore(ProgressData initial = null)
        {
            Saved = Copy(initial);
        }

        /// <inheritdoc />
        public ProgressData Load() => Copy(Saved);

        /// <inheritdoc />
        public void Save(ProgressData data)
        {
            Saved = Copy(data);
            SaveCount++;
        }

        static ProgressData Copy(ProgressData data) => data == null ? null : new ProgressData
        {
            version = data.version,
            bestStars = data.bestStars == null ? null : (int[])data.bestStars.Clone(),
            boosters = data.boosters == null ? null : (int[])data.boosters.Clone(),
            startingBoostersGranted = data.startingBoostersGranted,
            muted = data.muted,
            reduceMotion = data.reduceMotion,
            uiScalePercent = data.uiScalePercent,
            starsSpent = data.starsSpent,
        };
    }
}
