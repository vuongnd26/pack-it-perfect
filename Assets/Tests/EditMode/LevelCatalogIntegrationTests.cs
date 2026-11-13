using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Range = NUnit.Framework.RangeAttribute;

namespace PackItPerfect.Tests
{
    /// <summary>Editor integration checks of the catalog and limits actually serialized in the gameplay scene.</summary>
    [TestFixture, Category("Integration")]
    public sealed class LevelCatalogIntegrationTests
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        LevelsFile catalog;
        LevelLimits limits;
        string catalogPath;
        long searchLimit;

        /// <summary>Loads the scene's catalog reference without opening the scene or touching player saves.</summary>
        [OneTimeSetUp]
        public void LoadSceneCatalog()
        {
            // Catalog/scene I/O belongs only to this explicitly named integration fixture.
            string sceneFile = Path.Combine(Application.dataPath, "Scenes", Path.GetFileName(ScenePath));
            string scene = File.ReadAllText(sceneFile);
            var binding = Regex.Match(scene, @"(?m)^\s*levelsJson:\s*\{[^\r\n}]*guid:\s*([a-fA-F0-9]{32})");
            Assert.That(binding.Success, Is.True, $"{ScenePath}: missing serialized levelsJson binding.");
            catalogPath = AssetDatabase.GUIDToAssetPath(binding.Groups[1].Value);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(catalogPath);
            Assert.That(asset, Is.Not.Null, $"{ScenePath}: levelsJson GUID does not resolve to a TextAsset.");
            catalog = LevelLoader.Parse(asset.text, out string error);
            Assert.That(catalog, Is.Not.Null, error);
            limits = new LevelLimits
            {
                minWidth = SceneInt(scene, "minWidth"), minHeight = SceneInt(scene, "minHeight"),
                maxWidth = SceneInt(scene, "maxWidth"), maxHeight = SceneInt(scene, "maxHeight"),
                minPieces = SceneInt(scene, "minPieces"), maxPieces = SceneInt(scene, "maxPieces"),
            };
            var serializedLimit = Regex.Match(scene, @"(?m)^\s*autoPackNodeLimit:\s*(\d+)\s*$");
            searchLimit = serializedLimit.Success ? long.Parse(serializedLimit.Groups[1].Value) : new PackingTuning().autoPackNodeLimit;
            TestContext.WriteLine($"Catalog binding: {ScenePath} -> {catalogPath}; solver node limit: {searchLimit}.");
        }

        /// <summary>All twenty numbered levels must meet shape, coverage, blocked, zone, pair and rule validation.</summary>
        [Test]
        public void SceneCatalog_ContainsTwentyValidSequentialLevels()
        {
            Assert.That(catalog.levels.Length, Is.EqualTo(PackingFixtures.LevelCount));

            var errors = LevelLoader.ValidateAll(catalog, limits);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        /// <summary>Each authored witness must win through real C# board placement and completion rules.</summary>
        [Test]
        public void ReferenceWitness_ReplaysToFullLegalWin([Range(1, PackingFixtures.LevelCount)] int levelNumber)
        {
            var level = Build(levelNumber);

            PackingFixtures.ReplayReference(level);

            Assert.That(level.Board.FindCompletionViolation().IsViolation, Is.False);
            TestContext.WriteLine($"Level {levelNumber}: reference witness won, {level.Board.CoveredCellCount} cells, {level.Board.PieceCount} pieces.");
        }

        /// <summary>The independent C# search must solve the empty level; its placements must also win on the board.</summary>
        [Test]
        public void EmptyBoard_SolverSolutionReplaysWithoutChangingSource([Range(1, PackingFixtures.LevelCount)] int levelNumber)
        {
            var level = Build(levelNumber);
            string before = PackingFixtures.Snapshot(level.Board);
            var solver = new AutoPackSolver(level, level.Board, searchLimit);

            var status = solver.Run();

            Assert.That(status, Is.EqualTo(SolveStatus.Solved),
                $"Level {levelNumber}: {status}, {solver.NodesVisited} nodes. GaveUp is inconclusive, not proof of impossibility.");
            Assert.That(PackingFixtures.Snapshot(level.Board), Is.EqualTo(before));
            Assert.That(solver.Solution.Count, Is.EqualTo(level.Board.PieceCount));
            var replay = level.Board.Clone();
            foreach (var entry in solver.Solution)
            {
                var placement = entry.Value;
                Assert.That(level.SolutionShapes[entry.Key].Rotated(placement.turnsFromSolution).SameCells(placement.shape), Is.True);
                Assert.That(replay.Place(entry.Key, placement.shape, placement.origin), Is.True,
                    $"Level {levelNumber}: solver placement {entry.Key} rejected during replay.");
            }
            Assert.That(replay.CoveredCellCount, Is.EqualTo(replay.FillableCellCount));
            Assert.That(replay.IsWon(), Is.True);
            TestContext.WriteLine($"Level {levelNumber}: empty-board solution won, {solver.NodesVisited} nodes.");
        }

        LoadedLevel Build(int levelNumber)
        {
            Assert.That(catalog.levels.Length, Is.EqualTo(PackingFixtures.LevelCount));
            var data = catalog.levels[levelNumber - 1];
            Assert.That(data.number, Is.EqualTo(levelNumber));
            var level = LevelLoader.Build(catalog, data, limits, out string error);
            Assert.That(level, Is.Not.Null, error);
            return level;
        }

        static int SceneInt(string scene, string field)
        {
            var match = Regex.Match(scene, @"(?m)^\s*" + Regex.Escape(field) + @":\s*(\d+)\s*$");
            Assert.That(match.Success, Is.True, $"{ScenePath}: missing serialized level limit {field}.");
            return int.Parse(match.Groups[1].Value);
        }
    }
}
