// Implements: design/game-brief.md "Cơ chế chính" (Kéo thả đặt đồ: wrong drop returns to the tray and shakes)
// and "Obstacles" (pieces carry their zone icon, the ⚓ / ⬆️ icons and a pair icon; upright pieces wobble when tapped).
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-003-obstacle-rules.md,
// story-004-outline-imprints.md (outline imprint uses this view), story-005-hint-and-stars.md (hint wobble).

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>What a piece shows on top of its cells: item icon and rule badges. Null for the ghost.</summary>
    public sealed class PieceDecor
    {
        /// <summary>Item icon, or null.</summary>
        public Sprite icon;
        /// <summary>Icon opacity (1 for pieces, lower for outline imprints).</summary>
        public float iconAlpha = 1f;
        /// <summary>Zone icon emoji (zones[].icon) for a zoned piece, or null.</summary>
        public string zoneIcon;
        /// <summary>Shows the ⚓ badge.</summary>
        public bool heavy;
        /// <summary>Shows the ⬆️ badge.</summary>
        public bool upright;
        /// <summary>Shows the 🔗 pair badge.</summary>
        public bool paired;
    }

    /// <summary>
    /// Draws a <see cref="PieceShape"/> as inset cells plus bridge quads between neighbouring cells,
    /// so the piece reads as one solid shape, with an upright item icon and rule badges on top.
    /// The root sits at the shape's top-left corner; cell (x, y) is centred at local (x + 0.5, −(y + 0.5)), one unit per cell.
    /// Also used for the ghost preview and outline imprints.
    /// </summary>
    public sealed class PieceView
    {
        const int ICON_ORDER_OFFSET = 1;
        const int BADGE_ORDER_OFFSET = 2;
        const int BADGE_TEXT_ORDER_OFFSET = 3;

        readonly struct Badge
        {
            public readonly SpriteRenderer circle;
            public readonly TextMeshPro emoji;
            public readonly Corner corner;

            public Badge(SpriteRenderer circle, TextMeshPro emoji, Corner corner)
            {
                this.circle = circle;
                this.emoji = emoji;
                this.corner = corner;
            }
        }

        enum Corner { TopLeft, BottomLeft, TopRight }

        readonly SpriteFactory factory;
        readonly PackingTuning tuning;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        readonly SpriteRenderer icon;
        readonly List<Badge> badges = new List<Badge>();
        readonly ShakeMotion shake = new ShakeMotion();
        readonly ShakeMotion wobble = new ShakeMotion();
        Color color;
        float opacity = 1f;
        readonly float iconAlpha = 1f;
        int sortingOrder;
        Vector3 restPosition;
        bool animating;

        /// <summary>Root transform; position = top-left corner of the shape.</summary>
        public Transform Root { get; }

        /// <summary>Shape currently drawn.</summary>
        public PieceShape Shape { get; private set; }

        /// <summary>Current uniform scale of the root.</summary>
        public float Scale => Root.localScale.x;

        /// <summary>World position of the centre of the shape's bounding box at rest.</summary>
        public Vector3 RestCenter => restPosition + new Vector3(Shape.Width / 2f, -Shape.Height / 2f, 0f) * Scale;

        /// <summary>Creates an empty view; call <see cref="SetShape"/> to draw.</summary>
        /// <param name="factory">Quad factory.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="color">Cell colour.</param>
        /// <param name="sortingOrder">Sorting order of the cells; icon and badges sort just above.</param>
        /// <param name="tuning">Sizes and timings.</param>
        /// <param name="decor">Icon and badges, or null for none.</param>
        public PieceView(SpriteFactory factory, Transform parent, string name, Color color, int sortingOrder, PackingTuning tuning,
            PieceDecor decor = null)
        {
            this.factory = factory;
            this.tuning = tuning;
            this.color = color;
            this.sortingOrder = sortingOrder;
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            if (decor == null)
            {
                return;
            }

            iconAlpha = decor.iconAlpha;
            if (decor.icon != null)
            {
                icon = factory.CreateSprite(Root, "Icon", decor.icon, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, decor.iconAlpha),
                    sortingOrder + ICON_ORDER_OFFSET);
            }
            if (!string.IsNullOrEmpty(decor.zoneIcon))
            {
                AddBadge("ZoneBadge", decor.zoneIcon, tuning.zoneBadgeColor, Corner.TopLeft);
            }
            if (decor.heavy)
            {
                AddBadge("HeavyBadge", GameTexts.HEAVY_ICON, tuning.heavyBadgeColor, Corner.BottomLeft);
            }
            else if (decor.upright)
            {
                AddBadge("UprightBadge", GameTexts.UPRIGHT_ICON, tuning.uprightBadgeColor, Corner.BottomLeft);
            }
            if (decor.paired)
            {
                AddBadge("PairBadge", GameTexts.PAIR_ICON, tuning.pairBadgeColor, Corner.TopRight);
            }
        }

        void AddBadge(string name, string emoji, Color circleColor, Corner corner)
        {
            var circle = factory.CreateCircle(Root, name, Vector2.zero, tuning.badgeSize, circleColor, sortingOrder + BADGE_ORDER_OFFSET);
            var text = TmpText.Create(Root, name + "Icon", emoji, tuning.badgeSize * tuning.badgeEmojiScale, Color.white,
                sortingOrder + BADGE_TEXT_ORDER_OFFSET);
            badges.Add(new Badge(circle, text, corner));
        }

        /// <summary>Redraws the cells for <paramref name="shape"/> and moves the icon and badges to suit it.</summary>
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

            if (icon != null)
            {
                // Only the cell layout turns; the icon stays upright and moves to the new anchor.
                var anchor = shape.IconAnchor(out bool onBlock);
                icon.transform.localPosition = new Vector3(anchor.x, -anchor.y, 0f);
                SpriteFactory.FitSprite(icon, onBlock ? tuning.iconSizeOnBlock : tuning.iconSizeOnCell);
            }

            var first = shape.FirstCell;
            float half = tuning.badgeSize / 2f + tuning.badgeMargin;
            foreach (var badge in badges)
            {
                float x = badge.corner == Corner.TopRight ? first.x + 1f - half : first.x + half;
                float y = badge.corner == Corner.BottomLeft ? first.y + 1f - half : first.y + half;
                var position = new Vector3(x, -y, 0f);
                badge.circle.transform.localPosition = position;
                badge.emoji.transform.localPosition = position;
            }
        }

        void AddQuad(Vector2 center, Vector2 size)
        {
            renderers.Add(factory.CreateQuad(Root, "Cell", center, size, CellColor, sortingOrder));
        }

        Color CellColor => new Color(color.r, color.g, color.b, color.a * opacity);

        /// <summary>Tints every cell quad (not the icon or badges).</summary>
        public void SetColor(Color newColor)
        {
            color = newColor;
            foreach (var r in renderers)
            {
                r.color = CellColor;
            }
        }

        /// <summary>Multiplies the opacity of the cells, icon and badges by <paramref name="value"/> (0..1).</summary>
        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            foreach (var r in renderers)
            {
                r.color = CellColor;
            }
            if (icon != null)
            {
                icon.color = new Color(1f, 1f, 1f, iconAlpha * opacity);
            }
            foreach (var badge in badges)
            {
                var c = badge.circle.color;
                c.a = opacity;
                badge.circle.color = c;
                badge.emoji.alpha = opacity;
            }
        }

        /// <summary>Sets the sorting order of the cells; the icon and badges follow just above.</summary>
        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            foreach (var r in renderers)
            {
                r.sortingOrder = order;
            }
            if (icon != null)
            {
                icon.sortingOrder = order + ICON_ORDER_OFFSET;
            }
            foreach (var badge in badges)
            {
                badge.circle.sortingOrder = order + BADGE_ORDER_OFFSET;
                badge.emoji.sortingOrder = order + BADGE_TEXT_ORDER_OFFSET;
            }
        }

        /// <summary>Shows or hides the view.</summary>
        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        /// <summary>Moves the top-left corner to <paramref name="topLeft"/> at <paramref name="scale"/> and stops any shake or wobble.</summary>
        public void PlaceAt(Vector3 topLeft, float scale)
        {
            restPosition = topLeft;
            shake.Stop();
            wobble.Stop();
            animating = false;
            Root.SetPositionAndRotation(topLeft, Quaternion.identity);
            Root.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Starts a horizontal shake around the current rest position.</summary>
        public void StartShake()
        {
            shake.Start(tuning.shakeDuration, tuning.shakeAmplitude, tuning.shakeCycles);
            animating = true;
        }

        /// <summary>Starts a rotation wobble around the piece's centre (upright tap, hint highlight).</summary>
        /// <param name="seconds">Duration.</param>
        /// <param name="cycles">Full left-right swings.</param>
        public void StartWobble(float seconds, float cycles)
        {
            wobble.Start(seconds, tuning.wobbleAngle, cycles);
            animating = true;
        }

        /// <summary>Advances the shake and wobble; call every frame.</summary>
        public void TickShake(float deltaTime)
        {
            if (!animating)
            {
                return;
            }
            float offset = shake.Tick(deltaTime);
            float angle = wobble.Tick(deltaTime);
            animating = shake.Active || wobble.Active;

            var rotation = Quaternion.Euler(0f, 0f, angle);
            var center = new Vector3(Shape.Width / 2f, -Shape.Height / 2f, 0f) * Scale;
            Root.SetPositionAndRotation(restPosition + new Vector3(offset, 0f, 0f) + center - rotation * center, rotation);
        }
    }
}
