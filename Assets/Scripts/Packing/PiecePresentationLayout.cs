using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Pure geometry for fitting a natural illustration inside the occupied cells of a packing tile.</summary>
    public static class PiecePresentationLayout
    {
        /// <summary>
        /// Returns the largest aspect-preserving image rectangle wholly inside an occupied rectangle, in cell coordinates
        /// (x right, y down). Ties prefer the shape's centre. No image pixels can cover a hole or a neighbour's cell.
        /// </summary>
        public static Rect Fit(PieceShape shape, float aspect, float padding, Rect? reserved = null)
        {
            if (shape == null || shape.Cells.Count == 0 || aspect <= 0f || float.IsNaN(aspect) || float.IsInfinity(aspect))
                throw new System.ArgumentException("A nonempty footprint and positive finite aspect are required.");
            padding = Mathf.Clamp(padding, 0f, 0.45f);
            Rect best = default;
            float bestArea = -1f;
            float bestDistance = float.PositiveInfinity;
            Vector2 centre = new Vector2(shape.Width, shape.Height) / 2f;
            for (int y = 0; y < shape.Height; y++)
            for (int x = 0; x < shape.Width; x++)
            for (int h = 1; y + h <= shape.Height; h++)
            for (int w = 1; x + w <= shape.Width; w++)
            {
                bool filled = true;
                for (int cy = y; cy < y + h && filled; cy++)
                for (int cx = x; cx < x + w; cx++)
                    if (!shape.Contains(new Vector2Int(cx, cy))) { filled = false; break; }
                if (!filled) continue;
                var space = new Rect(x + padding, y + padding, w - 2f * padding, h - 2f * padding);
                if (reserved.HasValue && space.Overlaps(reserved.Value))
                {
                    // A marker's cell remains occupied; only its little rectangle is unavailable to the image.
                    var block = reserved.Value;
                    Consider(Rect.MinMaxRect(space.xMin, space.yMin, Mathf.Min(space.xMax, block.xMin), space.yMax));
                    Consider(Rect.MinMaxRect(Mathf.Max(space.xMin, block.xMax), space.yMin, space.xMax, space.yMax));
                    Consider(Rect.MinMaxRect(space.xMin, space.yMin, space.xMax, Mathf.Min(space.yMax, block.yMin)));
                    Consider(Rect.MinMaxRect(space.xMin, Mathf.Max(space.yMin, block.yMax), space.xMax, space.yMax));
                }
                else Consider(space);
            }
            return best;

            void Consider(Rect space)
            {
                if (space.width <= 0f || space.height <= 0f) return;
                float imageWidth = Mathf.Min(space.width, space.height * aspect);
                float imageHeight = imageWidth / aspect;
                var candidate = new Rect(space.center.x - imageWidth / 2f, space.center.y - imageHeight / 2f,
                    imageWidth, imageHeight);
                float area = imageWidth * imageHeight;
                float distance = (candidate.center - centre).sqrMagnitude;
                if (area > bestArea + 0.00001f || (Mathf.Abs(area - bestArea) <= 0.00001f && distance < bestDistance))
                {
                    best = candidate;
                    bestArea = area;
                    bestDistance = distance;
                }
            }
        }
    }
}
