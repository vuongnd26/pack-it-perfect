using NUnit.Framework;
using UnityEngine;

namespace PackItPerfect.Tests
{
    /// <summary>In-memory assist contracts, including regressions that must fail before runtime fixes.</summary>
    [TestFixture, Category("Unit")]
    public sealed class AssistRegressionTests
    {
        /// <summary>A rejected candidate must not let the next candidate exceed the frame's node budget.</summary>
        [Test, Category("Regression")]
        public void RejectedCandidate_StepDoesNotOvershootNodeBudget()
        {
            var level = PartiallyPackedDominoLevel();
            var solver = new AutoPackSolver(level, level.Board, PackingFixtures.SearchLimit);

            var status = solver.Step(1);

            Assert.That(solver.NodesVisited, Is.EqualTo(1), "One Step(1) may try only one candidate.");
            Assert.That(status, Is.EqualTo(SolveStatus.Searching), "The second candidate has not been budgeted yet.");
            Assert.That(solver.Step(1), Is.EqualTo(SolveStatus.Solved), "The saved candidate frame must resume.");
            Assert.That(solver.NodesVisited, Is.EqualTo(2));
        }

        /// <summary>The total limit also applies inside the candidate loop, not only between depths.</summary>
        [Test, Category("Regression")]
        public void RejectedCandidate_TotalLimitStopsBeforeSecondCandidate()
        {
            var level = PartiallyPackedDominoLevel();
            var solver = new AutoPackSolver(level, level.Board, nodeLimit: 1);

            var status = solver.Step(10);

            Assert.That(solver.NodesVisited, Is.EqualTo(1), "A global limit of one must stop before candidate two.");
            Assert.That(status, Is.EqualTo(SolveStatus.GaveUp), "Budget exhaustion is not a solution or proof of impossibility.");
            Assert.That(solver.Step(10), Is.EqualTo(SolveStatus.GaveUp), "The terminal result must stay stable.");
        }

        /// <summary>A non-overlapping heavy/fragile neighbor makes the hint fail atomically in either role.</summary>
        [TestCase(false), TestCase(true), Category("Regression")]
        public void HintTargetBlockedByFragileNeighbor_RejectsWithoutMutationOrUsedFlag(bool targetHeavy)
        {
            var level = PackingFixtures.HintConflictLevel(targetHeavy);
            var target = level.SolutionOrigins[0];
            var neighbor = targetHeavy ? new Vector2Int(1, 2) : new Vector2Int(0, 2);
            Assert.That(level.Board.Place(1, level.SolutionShapes[1], neighbor), Is.True);
            Assert.That(level.Board.Place(4, level.SolutionShapes[4], target), Is.True);
            var trial = level.Board.Clone();
            trial.Remove(4);
            Assert.That(trial.CheckPlacement(0, level.SolutionShapes[0], target), Is.EqualTo(PlacementFailure.Fragile),
                "The conflict survives clearing target occupancy: the blocker shares an edge, not a target cell.");
            string before = PackingFixtures.Snapshot(level.Board);
            var hints = new HintService(level);

            bool applied = hints.TryApply(level.Board, out var result);

            Assert.That(applied, Is.False, "Hint must reject a target still forbidden by the neighbor rule.");
            Assert.That(hints.Used, Is.False, "A rejected hint must not reduce the star allowance.");
            Assert.That(PackingFixtures.Snapshot(level.Board), Is.EqualTo(before), "The target occupant must not be evicted on failure.");
            Assert.That(result, Is.Null, "A failed hint must not expose a successful hint result.");
        }

        /// <summary>A normal hint evicts only target occupants and leaves the hinted piece for the player to drag.</summary>
        [Test]
        public void ValidHint_EvictsOnlyTargetOccupantsAndMarksUsed()
        {
            var level = PackingFixtures.DominoLevel();
            Assert.That(level.Board.Place(1, level.SolutionShapes[1], Vector2Int.zero), Is.True);
            var hints = new HintService(level);

            Assert.That(hints.TryApply(level.Board, out var result), Is.True);

            Assert.That(result.PieceId, Is.Zero);
            Assert.That(result.Shape.SameCells(level.SolutionShapes[0]), Is.True);
            Assert.That(result.Evicted, Is.EquivalentTo(new[] { 1 }));
            Assert.That(result.TargetCells, Is.EquivalentTo(level.SolutionCellsOf(0)));
            Assert.That(hints.Used, Is.True);
            Assert.That(level.Board.IsPlaced(0), Is.False);
            Assert.That(level.Board.IsPlaced(1), Is.False);
            Assert.That(level.Board.CanPlace(0, result.Shape, level.SolutionOrigins[0]), Is.True);
            Assert.That(new HintService(level).Used, Is.False, "A new play starts without assistance.");
        }

        /// <summary>No candidate on a complete board means no hint and no star penalty.</summary>
        [Test]
        public void CompleteBoard_HintDoesNothingAndRemainsUnused()
        {
            var level = PackingFixtures.DominoLevel();
            PackingFixtures.ReplayReference(level);
            string before = PackingFixtures.Snapshot(level.Board);
            var hints = new HintService(level);

            Assert.That(hints.TryApply(level.Board, out var result), Is.False);

            Assert.That(result, Is.Null);
            Assert.That(hints.Used, Is.False);
            Assert.That(PackingFixtures.Snapshot(level.Board), Is.EqualTo(before));
        }

        /// <summary>Successful search retains fixed items and exposes placements only for unplaced items.</summary>
        [Test]
        public void SolvablePartialBoard_SolverPreservesSourceAndFixedPlacement()
        {
            var level = PartiallyPackedDominoLevel();
            string before = PackingFixtures.Snapshot(level.Board);
            var solver = new AutoPackSolver(level, level.Board, PackingFixtures.SearchLimit);

            Assert.That(solver.Run(), Is.EqualTo(SolveStatus.Solved));

            Assert.That(PackingFixtures.Snapshot(level.Board), Is.EqualTo(before));
            Assert.That(solver.Solution.Keys, Is.EquivalentTo(new[] { 0 }));
            var placement = solver.Solution[0];
            Assert.That(level.Board.Place(0, placement.shape, placement.origin), Is.True);
            Assert.That(level.SolutionShapes[0].Rotated(placement.turnsFromSolution).SameCells(placement.shape), Is.True);
            Assert.That(level.Board.IsWon(), Is.True);
        }

        /// <summary>A truly exhausted search proves impossible without changing the player's partial board.</summary>
        [Test]
        public void IsolatedGap_SolverReportsImpossibleWithoutMutatingSource()
        {
            var level = PackingFixtures.Build(PackingFixtures.Data(new[] { "AA", "BC" },
                PackingFixtures.Piece("A"), PackingFixtures.Piece("B"), PackingFixtures.Piece("C")));
            Assert.That(level.Board.Place(1, level.SolutionShapes[1], Vector2Int.zero), Is.True);
            Assert.That(level.Board.Place(2, level.SolutionShapes[2], new Vector2Int(1, 1)), Is.True);
            string before = PackingFixtures.Snapshot(level.Board);
            var solver = new AutoPackSolver(level, level.Board, PackingFixtures.SearchLimit);

            Assert.That(solver.Run(), Is.EqualTo(SolveStatus.Impossible));

            Assert.That(PackingFixtures.Snapshot(level.Board), Is.EqualTo(before));
            Assert.That(solver.Step(1), Is.EqualTo(SolveStatus.Impossible));
        }

        static LoadedLevel PartiallyPackedDominoLevel()
        {
            var level = PackingFixtures.DominoLevel();
            Assert.That(level.Board.Place(1, level.SolutionShapes[1].Rotated(), Vector2Int.zero), Is.True);
            return level;
        }
    }
}
