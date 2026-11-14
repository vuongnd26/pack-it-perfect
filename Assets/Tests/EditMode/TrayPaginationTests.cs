using NUnit.Framework;
using UnityEngine;

namespace PackItPerfect.Tests
{
    /// <summary>Deterministic tray geometry and page invariants, independent of a scene or camera.</summary>
    public sealed class TrayPaginationTests
    {
        const int CrowdedPieceCount = 13;
        const int VisibleRowCount = 2;
        const float TrayTop = -2f;
        const float NarrowFrameWidth = 1.5f;
        const float FixtureScale = 0.6f;
        const float FixtureSpacing = 0.28f;

        /// <summary>Extra items must create pages rather than increasing the camera's world-space tray viewport.</summary>
        [Test]
        public void CrowdedTray_ViewportHeightNeverExceedsTwoRows()
        {
            // Arrange: one rotation-safe domino slot fits per row, so the old layout creates thirteen rows.
            var shape = PackingFixtures.Domino();
            var shapes = new PieceShape[CrowdedPieceCount];
            for (int i = 0; i < shapes.Length; i++) shapes[i] = shape;
            var tuning = new PackingTuning
            {
                trayScale = FixtureScale,
                traySlotSpacing = FixtureSpacing,
                trayRowsPerPage = TrayPagination.ROWS_PER_PAGE,
            };
            float maximumTwoRowHeight = VisibleRowCount * shape.MaxDimension * tuning.trayScale + tuning.traySlotSpacing;

            // Act: this deliberately uses the pre-pagination public API, not a missing future API.
            var tray = new TrayView(shapes, TrayTop, NarrowFrameWidth, tuning);

            // Assert
            Assert.That(tray.Bounds.height, Is.LessThanOrEqualTo(maximumTwoRowHeight + PackingFixtures.Tolerance),
                "More tray items must add pages, not world-space rows that shrink the board's camera fit.");
        }
        /// <summary>Every item gets exactly one stable page, in data order, and turning an item never moves it.</summary>
        [Test]
        public void Pages_AssignEveryPieceInDataOrder_AndIgnoreRotation()
        {
            // Arrange
            var shape = PackingFixtures.Domino();
            var shapes = new PieceShape[CrowdedPieceCount];
            for (int i = 0; i < shapes.Length; i++) shapes[i] = shape;
            var turned = (PieceShape[])shapes.Clone();
            for (int i = 0; i < turned.Length; i += 2) turned[i] = shape.Rotated();

            // Act: one slot per row, two rows per page.
            var pages = new TrayPagination(shapes, TrayTop, NarrowFrameWidth, FixtureScale, FixtureSpacing);
            var pagesTurned = new TrayPagination(turned, TrayTop, NarrowFrameWidth, FixtureScale, FixtureSpacing);

            // Assert
            Assert.That(pages.PageCount, Is.EqualTo((CrowdedPieceCount + 1) / TrayPagination.ROWS_PER_PAGE));
            for (int i = 0; i < CrowdedPieceCount; i++)
            {
                Assert.That(pages.PageFor(i), Is.EqualTo(i / TrayPagination.ROWS_PER_PAGE));
                Assert.That(pagesTurned.PageFor(i), Is.EqualTo(pages.PageFor(i)), "Rotation must not change the page.");
                Assert.That(pagesTurned.SlotFor(i), Is.EqualTo(pages.SlotFor(i)), "Rotation must not move the home slot.");
                var slot = pages.SlotFor(i);
                Assert.That(slot.yMin, Is.GreaterThanOrEqualTo(pages.Viewport.yMin - PackingFixtures.Tolerance));
                Assert.That(slot.yMax, Is.LessThanOrEqualTo(pages.Viewport.yMax + PackingFixtures.Tolerance));
            }
            Assert.That(pages.PageFor(-1), Is.EqualTo(-1));
            Assert.That(pages.PageFor(CrowdedPieceCount), Is.EqualTo(-1));
        }

        /// <summary>A slot on a hidden page must not catch a press, exact or padded.</summary>
        [Test]
        public void HitTest_HiddenPage_IsIgnoredUntilThatPageIsShown()
        {
            // Arrange
            var shape = PackingFixtures.Domino();
            var shapes = new PieceShape[CrowdedPieceCount];
            for (int i = 0; i < shapes.Length; i++) shapes[i] = shape;
            var tuning = new PackingTuning { trayScale = FixtureScale, traySlotSpacing = FixtureSpacing, trayRowsPerPage = TrayPagination.ROWS_PER_PAGE };
            var tray = new TrayView(shapes, TrayTop, NarrowFrameWidth, tuning);
            const int firstOnSecondPage = TrayPagination.ROWS_PER_PAGE;
            Vector2 firstSlot = tray.TopLeftFor(0, shape) + new Vector3(0.1f, -0.1f);

            // Act + Assert: pages share the viewport, so the same point belongs to whichever page is shown.
            Assert.That(tray.HitTest(firstSlot, i => true), Is.EqualTo(0));
            Assert.That(tray.SetPage(1), Is.True);
            Assert.That(tray.HitTest(firstSlot, i => true), Is.EqualTo(firstOnSecondPage));
            Assert.That(tray.HitTestExpanded(firstSlot, i => i != firstOnSecondPage, 0.05f), Is.EqualTo(-1),
                "An empty visible slot must not fall through to a hidden page's piece.");
            Assert.That(tray.SetPage(1), Is.False, "Showing the current page is not a change.");
            Assert.That(tray.SetPage(tray.PageCount), Is.False);
        }

        /// <summary>The default tray shows every item on one page and shrinks only as far as the room needs.</summary>
        [Test]
        public void SinglePageTray_CrowdedCatalog_ShowsEveryItemAndFitsTheRoom()
        {
            // Arrange
            var shape = PackingFixtures.Single();
            var shapes = new PieceShape[CrowdedPieceCount];
            for (int i = 0; i < shapes.Length; i++) shapes[i] = shape;
            var tuning = new PackingTuning { traySlotSpacing = FixtureSpacing };
            var roomy = new TrayView(shapes, TrayTop, NarrowFrameWidth, tuning);
            float room = roomy.Bounds.height * 0.6f;

            // Act
            float scale = TrayPagination.FitScale(shapes, NarrowFrameWidth, tuning.trayMinScale, tuning.trayScale,
                tuning.traySlotSpacing, room, tuning.traySortBySize);
            var tray = new TrayView(shapes, TrayTop, NarrowFrameWidth, tuning, scale);

            // Assert
            Assert.That(tray.PageCount, Is.EqualTo(1));
            for (int i = 0; i < shapes.Length; i++) Assert.That(tray.IsOnVisiblePage(i), Is.True, $"piece {i}");
            Assert.That(scale, Is.LessThan(tuning.trayScale));
            Assert.That(scale, Is.GreaterThanOrEqualTo(tuning.trayMinScale));
            Assert.That(tray.Bounds.height, Is.LessThanOrEqualTo(room + 0.001f));
        }
    }
}
