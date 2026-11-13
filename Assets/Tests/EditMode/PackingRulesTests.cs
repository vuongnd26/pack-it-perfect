using NUnit.Framework;
using UnityEngine;

namespace PackItPerfect.Tests
{
    /// <summary>Deterministic board geometry and rule contracts using only in-memory fixtures.</summary>
    [TestFixture, Category("Unit")]
    public sealed class PackingRulesTests
    {
        /// <summary>One attempted drop can report several geometric failures without mutating occupancy.</summary>
        [Test]
        public void InvalidDrop_ReturnsCombinedFlagsAndLeavesBoardUnchanged()
        {
            var board = new BoardModel(3, 1, new[] { new Vector2Int(1, 0) }, 2);
            Assert.That(board.Place(0, PackingFixtures.Single(), Vector2Int.zero), Is.True);
            var spanning = PieceShape.FromCells(new[]
            {
                new Vector2Int(-1, 0), Vector2Int.zero, Vector2Int.right, new Vector2Int(2, 0),
            });
            string before = PackingFixtures.Snapshot(board);

            var failure = board.CheckPlacement(1, spanning, new Vector2Int(-1, 0));

            Assert.That(failure, Is.EqualTo(PlacementFailure.OutOfBounds | PlacementFailure.Blocked | PlacementFailure.Occupied));
            Assert.That(board.Place(1, spanning, new Vector2Int(-1, 0)), Is.False);
            Assert.That(PackingFixtures.Snapshot(board), Is.EqualTo(before));
        }

        /// <summary>Rotation normalizes cells, preserves area and returns to the original orientation.</summary>
        [Test]
        public void OffsetShape_RotationNormalizesAndPreservesReadingOrderAnchor()
        {
            var shape = PieceShape.FromCells(new[]
            {
                new Vector2Int(4, 7), new Vector2Int(3, 8), new Vector2Int(4, 8),
            });

            var turned = shape.Rotated();

            Assert.That(shape.FirstCell, Is.EqualTo(Vector2Int.right), "The first occupied cell is not always the bounding corner.");
            Assert.That(shape.Contains(Vector2Int.zero), Is.False);
            Assert.That(turned.FirstCell, Is.EqualTo(Vector2Int.zero));
            Assert.That(turned.Cells.Count, Is.EqualTo(shape.Cells.Count));
            Assert.That(shape.Rotated(4).SameCells(shape), Is.True);
            Assert.That(shape.Rotated(-1).SameCells(shape.Rotated(3)), Is.True);
            var board = PackingFixtures.Board(2, 2, 1);
            var targetCell = Vector2Int.right;
            var origin = targetCell - shape.FirstCell;
            Assert.That(board.Place(0, shape, origin), Is.True);
            Assert.That(board.PieceAt(targetCell), Is.Zero);
        }

        /// <summary>Board clones keep rules and placement orientation but isolate subsequent changes.</summary>
        [Test]
        public void CloneAndRemove_PreserveSourceAndCoveredCellCounts()
        {
            var board = new BoardModel(3, 2, new[] { new Vector2Int(2, 0) }, 2,
                new IPackingRule[] { new HeavyRule(new[] { true, false }, 2) });
            var shape = PackingFixtures.Domino().Rotated();
            Assert.That(board.Place(0, shape, Vector2Int.zero), Is.True);
            string before = PackingFixtures.Snapshot(board);

            var clone = board.Clone();
            clone.Remove(0);
            clone.Remove(0);

            Assert.That(PackingFixtures.Snapshot(board), Is.EqualTo(before));
            Assert.That(clone.CoveredCellCount, Is.Zero);
            Assert.That(clone.FillRatio, Is.Zero);
            Assert.That(clone.FillableCellCount, Is.EqualTo(5));
            Assert.That(clone.IsBlocked(new Vector2Int(2, 0)), Is.True);
            Assert.That(clone.CheckPlacement(0, PackingFixtures.Single(), Vector2Int.zero), Is.EqualTo(PlacementFailure.Heavy));
            Assert.That(board.TryGetPlacement(0, out var keptShape, out var keptOrigin), Is.True);
            Assert.That(keptShape.SameCells(shape), Is.True);
            Assert.That(keptOrigin, Is.EqualTo(Vector2Int.zero));
        }

        /// <summary>Only occupied bottom-row cells satisfy heavy, while ordinary pieces have no such restriction.</summary>
        [Test]
        public void HeavyPiece_RequiresBottomRowContact()
        {
            var board = PackingFixtures.Board(3, 3, 2, new HeavyRule(new[] { true, false }, 3));
            var shape = PackingFixtures.Domino();

            Assert.That(board.CheckPlacement(0, shape, Vector2Int.zero), Is.EqualTo(PlacementFailure.Heavy));
            Assert.That(board.CanPlace(1, shape, Vector2Int.zero), Is.True);
            Assert.That(board.Place(0, shape, new Vector2Int(0, 2)), Is.True);
            Assert.That(board.CanRotate(0), Is.True);
        }

        /// <summary>Upright is a rotation permission; its valid reference orientation remains placeable.</summary>
        [Test]
        public void UprightPiece_DisallowsRotationAndLoaderRejectsTurnedStart()
        {
            var upright = PackingFixtures.Piece("A");
            upright.upright = true;
            var data = PackingFixtures.Data(new[] { "AA", "BB" }, upright, PackingFixtures.Piece("B"));
            var level = PackingFixtures.Build(data);

            Assert.That(level.Board.CanRotate(0), Is.False);
            Assert.That(level.Board.CanRotate(1), Is.True);
            Assert.That(level.Board.CanPlace(0, level.SolutionShapes[0], level.SolutionOrigins[0]), Is.True);
            upright.startTurns = 1;

            Assert.That(LevelLoader.Build(PackingFixtures.File(data), data, PackingFixtures.SmallLimits(), out string error), Is.Null);
            Assert.That(error, Does.Contain("upright"));
        }

        /// <summary>Every cell of a zoned shape must match; an unzoned piece is unrestricted.</summary>
        [Test]
        public void ZonedPiece_RequiresEveryOccupiedCellInsideItsZone()
        {
            var zoned = PackingFixtures.Piece("A");
            zoned.zone = "a";
            var ordinary = PackingFixtures.Piece("B");
            var rule = new ZoneRule(new[] { "aab", "aab" }, new[] { zoned, ordinary });
            var board = PackingFixtures.Board(3, 2, 2, rule);

            Assert.That(board.CanPlace(0, PackingFixtures.Domino(), Vector2Int.zero), Is.True);
            Assert.That(board.CheckPlacement(0, PackingFixtures.Domino(), Vector2Int.right), Is.EqualTo(PlacementFailure.Zone));
            Assert.That(board.CanPlace(1, PackingFixtures.Domino(), Vector2Int.right), Is.True);
        }

        /// <summary>Pairs may be dropped apart temporarily, but corner contact cannot win a full board.</summary>
        [TestCase(true), TestCase(false)]
        public void FullBoard_PairsRequireEdgeNotCornerContact(bool edgeContact)
        {
            var rule = new PairRule(new[] { 0, 0, -1, -1 });
            var board = PackingFixtures.Board(2, 2, 4, rule);
            var partner = edgeContact ? Vector2Int.right : new Vector2Int(1, 1);
            var filler = edgeContact ? new Vector2Int(1, 1) : Vector2Int.right;
            var shape = PackingFixtures.Single();

            Assert.That(board.Place(0, shape, Vector2Int.zero), Is.True);
            Assert.That(board.CanPlace(1, shape, partner), Is.True, "Pairs do not reject a temporary drop.");
            Assert.That(board.Place(1, shape, partner), Is.True);
            Assert.That(board.AllPlaced, Is.False);
            Assert.That(board.Place(2, shape, new Vector2Int(0, 1)), Is.True);
            Assert.That(board.Place(3, shape, filler), Is.True);

            Assert.That(board.FillRatio, Is.EqualTo(1f));
            Assert.That(board.AllPlaced, Is.True);
            Assert.That(rule.TouchesPartner(board, 0), Is.EqualTo(edgeContact));
            Assert.That(board.FindCompletionViolation().IsViolation, Is.EqualTo(!edgeContact));
            Assert.That(board.IsWon(), Is.EqualTo(edgeContact));
        }

        /// <summary>Fragile/ordinary-heavy edge conflicts are symmetric in placement order.</summary>
        [TestCase(true), TestCase(false)]
        public void HeavyFragileEdgeContact_IsRejectedInEitherPlacementOrder(bool heavyFirst)
        {
            var fragile = new FragileRule(new[] { false, true }, new[] { true, false });
            var board = PackingFixtures.Board(3, 2, 2, new HeavyRule(new[] { true, false }, 2), fragile);
            var shape = PackingFixtures.Single();
            int first = heavyFirst ? 0 : 1;
            int second = heavyFirst ? 1 : 0;
            var heavyOrigin = new Vector2Int(0, 1);
            var fragileOrigin = new Vector2Int(1, 1);
            Assert.That(board.Place(first, shape, heavyFirst ? heavyOrigin : fragileOrigin), Is.True);
            string before = PackingFixtures.Snapshot(board);

            var failure = board.CheckPlacement(second, shape, heavyFirst ? fragileOrigin : heavyOrigin);

            Assert.That(failure, Is.EqualTo(PlacementFailure.Fragile));
            Assert.That(board.Place(second, shape, heavyFirst ? fragileOrigin : heavyOrigin), Is.False);
            Assert.That(PackingFixtures.Snapshot(board), Is.EqualTo(before));
            board.Remove(first);
            Assert.That(board.CanPlace(second, shape, heavyFirst ? fragileOrigin : heavyOrigin), Is.True);
        }

        /// <summary>Only shared edges with heavy items are forbidden; diagonal and ordinary contact remain legal.</summary>
        [Test]
        public void FragilePiece_AllowsDiagonalHeavyAndEdgeOrdinaryNeighbors()
        {
            var fragile = new FragileRule(new[] { false, true, false }, new[] { true, false, false });
            var board = PackingFixtures.Board(3, 2, 3, fragile);
            var shape = PackingFixtures.Single();
            Assert.That(board.Place(0, shape, new Vector2Int(0, 1)), Is.True);
            Assert.That(board.Place(2, shape, new Vector2Int(2, 0)), Is.True);

            Assert.That(board.Place(1, shape, new Vector2Int(1, 0)), Is.True);

            Assert.That(board.CoveredCellCount, Is.EqualTo(3));
        }

        /// <summary>The loader rejects a reference that could never obey its own fragile adjacency rule.</summary>
        [Test]
        public void FragileHeavyReferenceConflict_LoaderRejectsBeforePlay()
        {
            var data = PackingFixtures.Data(new[] { "AAB", "HHC" }, PackingFixtures.Piece("A"),
                PackingFixtures.Piece("B"), PackingFixtures.Piece("H", heavy: true), PackingFixtures.Piece("C", fragile: true));

            var level = LevelLoader.Build(PackingFixtures.File(data), data, PackingFixtures.SmallLimits(), out string error);

            Assert.That(level, Is.Null);
            Assert.That(error, Does.Contain("Fragile"));
        }
    }
}
