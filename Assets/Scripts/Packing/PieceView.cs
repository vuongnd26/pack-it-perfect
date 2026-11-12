// Implements: design/game-brief.md "Cơ chế chính" (Kéo thả đặt đồ: wrong drop returns to the tray and shakes).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Draws a <see cref="PieceShape"/> as inset cells plus bridge quads between neighbouring cells,
    /// so the piece reads as one solid shape. The root sits at the shape's top-left corner;
    /// cell (x, y) is centred at local (x + 0.5, −(y + 0.5)), one unit per cell.
    /// Also used for the ghost preview.
    /// </summary>
    public sealed class PieceView
    {
        readonly SpriteFactory factory;
        readonly PackingTuning tuning;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        Color color;
        int sortingOrder;
        Vector3 restPosition;
        float shakeTimeLeft;

        /// <summary>Root transform; position = top-left corner of the shape.</summary>
        public Transform Root { get; }

        /// <summary>Shape currently drawn.</summary>
        public PieceShape Shape { get; private set; }

        /// <summary>Current uniform scale of the root.</summary>
        public float Scale => Root.localScale.x;

        /// <summary>Creates an empty view; call <see cref="SetShape"/> to draw.</summary>
        public PieceView(SpriteFactory factory, Transform parent, string name, Color color, int sortingOrder, PackingTuning tuning)
        {
            this.factory = factory;
            this.tuning = tuning;
            this.color = color;
            this.sortingOrder = sortingOrder;
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
        }

        /// <summary>Redraws the view for <paramref name="shape"/>.</summary>
        public void SetShape(PieceShape shape)
        {
            foreach (var r in renderers)
            {
                r.gameObject.SetActive(false);
                Object.Destroy(r.gameObject);
            }
            renderers.Clear();
            Shape = shape;

            float inset = tuning.pieceCellInset;
            float inner = 1f - 2f * inset;
            float bridge = 2f * inset;
            var right = new Vector2Int(1, 0);
            var down = new Vector2Int(0, 1);
            foreach (var c in shape.Cells)
            {
                AddQuad(new Vector2(c.x + 0.5f, -(c.y + 0.5f)), new Vector2(inner, inner));
                bool hasRight = shape.Contains(c + right);
                bool hasDown = shape.Contains(c + down);
                if (hasRight)
                {
                    AddQuad(new Vector2(c.x + 1f, -(c.y + 0.5f)), new Vector2(bridge, inner));
                }
                if (hasDown)
                {
                    AddQuad(new Vector2(c.x + 0.5f, -(c.y + 1f)), new Vector2(inner, bridge));
                }
                if (hasRight && hasDown && shape.Contains(c + right + down))
                {
                    AddQuad(new Vector2(c.x + 1f, -(c.y + 1f)), new Vector2(bridge, bridge));
                }
            }
        }

        void AddQuad(Vector2 center, Vector2 size)
        {
            renderers.Add(factory.CreateQuad(Root, "Cell", center, size, color, sortingOrder));
        }

        /// <summary>Tints every quad.</summary>
        public void SetColor(Color newColor)
        {
            color = newColor;
            foreach (var r in renderers)
            {
                r.color = newColor;
            }
        }

        /// <summary>Sets the sorting order of every quad.</summary>
        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            foreach (var r in renderers)
            {
                r.sortingOrder = order;
            }
        }

        /// <summary>Shows or hides the view.</summary>
        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        /// <summary>Moves the top-left corner to <paramref name="topLeft"/> at <paramref name="scale"/> and stops any shake.</summary>
        public void PlaceAt(Vector3 topLeft, float scale)
        {
            restPosition = topLeft;
            shakeTimeLeft = 0f;
            Root.position = topLeft;
            Root.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Starts a horizontal shake around the current rest position.</summary>
        public void StartShake() => shakeTimeLeft = tuning.shakeDuration;

        /// <summary>Advances the shake; call every frame.</summary>
        public void TickShake(float deltaTime)
        {
            if (shakeTimeLeft <= 0f)
            {
                return;
            }
            shakeTimeLeft = Mathf.Max(0f, shakeTimeLeft - deltaTime);
            float elapsed = tuning.shakeDuration - shakeTimeLeft;
            float decay = shakeTimeLeft / tuning.shakeDuration;
            float offset = tuning.shakeAmplitude * decay * Mathf.Sin(elapsed / tuning.shakeDuration * tuning.shakeCycles * 2f * Mathf.PI);
            Root.position = restPosition + new Vector3(offset, 0f, 0f);
        }
    }
}
