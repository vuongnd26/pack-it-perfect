using NUnit.Framework;
using UnityEngine;

namespace PackItPerfect.Tests
{
    /// <summary>Natural-image fitting never fills the holes of concave packing shapes.</summary>
    public sealed class PiecePresentationLayoutTests
    {
        [TestCase(0.35f)]
        [TestCase(1f)]
        [TestCase(2.8f)]
        public void ConcaveShape_AllRotations_ImageFitsOccupiedCells(float aspect)
        {
            var shape = PieceShape.FromCells(new[] { new Vector2Int(0,0), new Vector2Int(0,1),
                new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2) });
            for (int turn = 0; turn < 4; turn++)
            {
                var rotated = shape.Rotated(turn);
                Rect image = PiecePresentationLayout.Fit(rotated, aspect, 0.08f);
                Assert.That(image.width / image.height, Is.EqualTo(aspect).Within(0.0001f));
                for (int y = 0; y <= 10; y++)
                for (int x = 0; x <= 10; x++)
                {
                    var point = new Vector2(Mathf.Lerp(image.xMin, image.xMax, x / 10f), Mathf.Lerp(image.yMin, image.yMax, y / 10f));
                    Assert.That(rotated.Contains(new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y))), Is.True);
                }
            }
        }

        [Test]
        public void SquareFootprint_SquareIllustration_UsesLargestOccupiedRectangle()
        {
            var shape = PieceShape.FromCells(new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up, Vector2Int.one });
            Rect rect = PiecePresentationLayout.Fit(shape, 1f, 0.1f);
            Assert.That(rect.width, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(rect.center, Is.EqualTo(Vector2.one));
        }

        [TestCase("XXX/X.X", 0.35f)]
        [TestCase("XXX/.X.", 1f)]
        [TestCase("X../XXX", 2.8f)]
        [TestCase("XX/XX", 1f)]
        public void ReservedMarker_AllRotations_ImageAvoidsMarkerAndEmptyCells(string rows, float aspect)
        {
            var cells = new System.Collections.Generic.List<Vector2Int>();
            string[] map = rows.Split('/');
            for (int y = 0; y < map.Length; y++)
            for (int x = 0; x < map[y].Length; x++)
                if (map[y][x] == 'X') cells.Add(new Vector2Int(x, y));
            var shape = PieceShape.FromCells(cells);
            for (int turn = 0; turn < 4; turn++)
            {
                var rotated = shape.Rotated(turn);
                var cell = rotated.FirstCell;
                var reserved = new Rect(cell.x, cell.y, 0.9f, 0.9f);
                Rect image = PiecePresentationLayout.Fit(rotated, aspect, 0.08f, reserved);
                Assert.That(image.width, Is.GreaterThan(0f));
                Assert.That(image.width / image.height, Is.EqualTo(aspect).Within(0.0001f));
                Assert.That(image.Overlaps(reserved), Is.False);
                for (int y = 0; y <= 10; y++)
                for (int x = 0; x <= 10; x++)
                    Assert.That(rotated.Contains(new Vector2Int(
                        Mathf.FloorToInt(Mathf.Lerp(image.xMin, image.xMax, x / 10f)),
                        Mathf.FloorToInt(Mathf.Lerp(image.yMin, image.yMax, y / 10f)))), Is.True);
            }
        }
    }
}
