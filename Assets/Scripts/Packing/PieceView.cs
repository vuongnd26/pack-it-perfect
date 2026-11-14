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
        /// <summary>Whole-object artwork; replaces both the patterned block and its small icon when present.</summary>
        public PieceArt art;
        /// <summary>Shared runtime pattern cache, or null for plain ghosts and outline imprints.</summary>
        public PatternSpriteCache patterns;
        /// <summary>Pattern key from item data.</summary>
        public string pattern;
        /// <summary>Exterior outline colour for decorated pieces.</summary>
        public Color outlineColor;
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
        /// <summary>Shows the fragile badge.</summary>
        public bool fragile;
    }

    /// <summary>
    /// Draws whole illustrated objects, with patterned surfaces and small icons as the legacy fallback.
    /// Plain ghosts retain inset cells and bridge quads. Rule badges stay upright while the object rotates.
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
            public readonly SpriteRenderer art;
            public readonly Corner corner;

            public Badge(SpriteRenderer circle, TextMeshPro emoji, SpriteRenderer art, Corner corner)
            {
                this.circle = circle;
                this.emoji = emoji;
                this.art = art;
                this.corner = corner;
            }
        }

        enum Corner { TopLeft, BottomLeft, TopRight, BottomRight }

        // Illustrated badge art (Resources/Art); the emoji text is the fallback when a sprite is missing.
        const string ART_HEAVY = "ui_heavy";
        const string ART_UPRIGHT = "ui_upright";
        const string ART_PAIR = "ui_pair";
        const string ART_FRAGILE = "ui_fragile";
        const float BADGE_ART_FILL = 0.78f;

        readonly SpriteFactory factory;
        readonly PackingTuning tuning;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        readonly SpriteRenderer icon;
        readonly List<Badge> badges = new List<Badge>();
        readonly Vector3[] badgePositions = new Vector3[5];
        Badge summaryBadge;
        bool hasSummaryBadge;
        bool aggregateBadges;
        float worldUnitsPerScreenPixel;
        float badgeDiameter;
        float badgeReferenceScale = 1f;
        Vector3 summaryPosition;
        bool pageVisible = true;
        bool glideVisible;
        readonly ShakeMotion shake = new ShakeMotion();
        readonly ShakeMotion wobble = new ShakeMotion();
        readonly PieceDecor decor;
        SpriteRenderer border;
        SpriteRenderer objectRenderer;
        Rect illustrationRect;
        readonly List<SpriteRenderer> sourcedCells = new List<SpriteRenderer>();
        int artQuarterTurns;
        readonly SpriteRenderer shadow;
        float bounceAge;
        float bounceDuration;
        float bounceAmount;
        float restScale = 1f;
        bool bouncing;
        // Visual-only travel towards the rest pose (snap into a cell, flight back to the tray).
        bool gliding;
        Vector3 glideFrom;
        float glideFromScale;
        float glideAge;
        float glideDuration;
        // Visual-only quarter-turn of the illustrated object after a rotation.
        bool spinning;
        float spinAge;
        float spinDuration;
        // Visual-only scale-in when a level is dealt.
        bool popping;
        float popAge;
        float popDuration;
        /// <summary>Suppresses shake, wobble and placement bounce, including an animation already in progress.</summary>
        public bool ReduceMotion { get; set; }

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

        /// <summary>Uniform scale of the piece at rest (a glide, bounce or pop only changes what is drawn).</summary>
        public float Scale => restScale;

        /// <summary>World position of the shape's top-left corner at rest (where a running glide will end).</summary>
        public Vector3 RestTopLeft => restPosition;

        /// <summary>World position of the centre of the shape's bounding box at rest.</summary>
        public Vector3 RestCenter => restPosition + new Vector3(Shape.Width / 2f, -Shape.Height / 2f, 0f) * restScale;

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
            this.decor = decor;
            this.color = color;
            this.sortingOrder = sortingOrder;
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            if (decor == null)
            {
                return;
            }

            iconAlpha = decor.iconAlpha;
            if (decor.patterns != null)
            {
                // Real pieces only: ghosts and outline imprints cast no shadow.
                shadow = factory.CreateSoftOval(Root, "Shadow", Vector2.zero, Vector2.one, tuning.pieceShadowColor,
                    SpriteFactory.ORDER_PIECE_SHADOW);
                shadow.enabled = false;
            }
            if (decor.art == null && decor.icon != null)
            {
                icon = factory.CreateSprite(Root, "Icon", decor.icon, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, decor.iconAlpha),
                    sortingOrder + ICON_ORDER_OFFSET);
            }
            if (!string.IsNullOrEmpty(decor.zoneIcon))
            {
                AddBadge("ZoneBadge", decor.zoneIcon, tuning.zoneBadgeColor, Corner.TopLeft, null);
            }
            if (decor.heavy)
            {
                AddBadge("HeavyBadge", GameTexts.HEAVY_ICON, tuning.heavyBadgeColor, Corner.BottomLeft, ART_HEAVY);
            }
            if (decor.upright)
            {
                AddBadge("UprightBadge", GameTexts.UPRIGHT_ICON, tuning.uprightBadgeColor, Corner.BottomLeft, ART_UPRIGHT);
            }
            if (decor.paired)
            {
                AddBadge("PairBadge", GameTexts.PAIR_ICON, tuning.pairBadgeColor, Corner.TopRight, ART_PAIR);
            }
            if (decor.fragile)
            {
                AddBadge("FragileBadge", GameTexts.FRAGILE_ICON, tuning.fragileBadgeColor, Corner.BottomRight, ART_FRAGILE);
            }
            if (badges.Count > 1)
            {
                var circle = factory.CreateCircle(Root, "RuleSummaryBadge", Vector2.zero, tuning.badgeSize,
                    tuning.heavyBadgeColor, sortingOrder + BADGE_ORDER_OFFSET);
                var text = TmpText.Create(Root, "RuleSummaryCount", "+" + badges.Count,
                    tuning.badgeSize * tuning.badgeEmojiScale, Color.white, sortingOrder + BADGE_TEXT_ORDER_OFFSET);
                summaryBadge = new Badge(circle, text, null, Corner.TopLeft);
                hasSummaryBadge = true;
                SetBadgeVisible(summaryBadge, false);
            }
        }

        void AddBadge(string name, string emoji, Color circleColor, Corner corner, string artName)
        {
            var circle = factory.CreateCircle(Root, name, Vector2.zero, tuning.badgeSize, circleColor, sortingOrder + BADGE_ORDER_OFFSET);
            var sprite = artName != null ? factory.GetArt(artName) : null;
            TextMeshPro text = null;
            SpriteRenderer art = null;
            if (sprite != null)
            {
                art = factory.CreateSprite(Root, name + "Art", sprite, Vector2.zero, Vector2.one * tuning.badgeSize * BADGE_ART_FILL,
                    Color.white, sortingOrder + BADGE_TEXT_ORDER_OFFSET);
            }
            else
            {
                text = TmpText.Create(Root, name + "Icon", emoji, tuning.badgeSize * tuning.badgeEmojiScale, Color.white,
                    sortingOrder + BADGE_TEXT_ORDER_OFFSET);
            }
            badges.Add(new Badge(circle, text, art, corner));
        }

        /// <summary>Explicit visual orientation: clockwise quarter turns from the solution orientation (0–3).</summary>
        public int ArtQuarterTurns => artQuarterTurns;

        /// <summary>Updates the footprint and draws the whole object, or legacy cells when no artwork is bound.</summary>
        /// <param name="shape">The existing logical footprint; never changed by this view.</param>
        /// <param name="quarterTurnsFromSolution">Explicit visual orientation. Pass zero after Hint; null preserves it.</param>
        public void SetShape(PieceShape shape, int? quarterTurnsFromSolution = null)
        {
            if (quarterTurnsFromSolution.HasValue) artQuarterTurns = quarterTurnsFromSolution.Value;
            if (decor?.art != null)
            {
                if (decor.art.OnTile)
                {
                    SetSourcedShape(shape);
                    return;
                }
                Shape = shape;
                if (objectRenderer == null)
                {
                    objectRenderer = factory.CreateSprite(Root, "Whole object", decor.art.Sprite, Vector2.zero,
                        Vector2.one * decor.art.AuthoredShape.MaxDimension, ObjectColor, sortingOrder);
                    objectRenderer.flipX = decor.art.Mirrored;
                    renderers.Add(objectRenderer);
                }
                objectRenderer.transform.localPosition = new Vector3(shape.Width / 2f, -shape.Height / 2f, 0f);
                spinning = false;
                ApplyArtRotation(0f);
                UpdateAdornments();
                return;
            }
            foreach (var r in renderers)
            {
                r.gameObject.SetActive(false);
                Object.Destroy(r.gameObject);
            }
            renderers.Clear();
            Shape = shape;

            if (border != null)
            {
                border.gameObject.SetActive(false);
                Object.Destroy(border.gameObject);
                border = null;
            }
            if (decor?.patterns != null)
            {
                var surface = decor.patterns.Get(shape, decor.pattern, tuning.pieceCellInset);
                var center = new Vector2(shape.Width / 2f, -shape.Height / 2f);
                renderers.Add(factory.CreateSprite(Root, "Pattern body", surface.Body, center,
                    Vector2.one * shape.MaxDimension, CellColor, sortingOrder));
                var edgeColor = decor.outlineColor;
                edgeColor.a *= color.a * opacity;
                border = factory.CreateSprite(Root, "Exterior border", surface.Border, center,
                    Vector2.one * shape.MaxDimension, edgeColor, sortingOrder);
            }
            else
            {
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

            if (icon != null)
            {
                // Only the cell layout turns; the icon stays upright and moves to the new anchor.
                var anchor = shape.IconAnchor(out bool onBlock);
                icon.transform.localPosition = new Vector3(anchor.x, -anchor.y, 0f);
                SpriteFactory.FitSprite(icon, onBlock ? tuning.iconSizeOnBlock : tuning.iconSizeOnCell);
            }

            UpdateAdornments();
        }

        void SetSourcedShape(PieceShape shape)
        {
            Shape = shape;
            var tile = factory.GetArt("Sourced/UI/piece_tile");
            while (sourcedCells.Count < shape.Cells.Count)
            {
                var cell = factory.CreateSprite(Root, "Sourced base cell", tile, Vector2.zero,
                    Vector2.one, BaseColor, sortingOrder);
                sourcedCells.Add(cell);
                renderers.Add(cell);
            }
            for (int i = 0; i < sourcedCells.Count; i++)
            {
                bool used = i < shape.Cells.Count;
                sourcedCells[i].gameObject.SetActive(used);
                if (!used) continue;
                var c = shape.Cells[i];
                sourcedCells[i].transform.localPosition = new Vector3(c.x + 0.5f, -c.y - 0.5f);
                SpriteFactory.FitSprite(sourcedCells[i], 0.98f);
            }
            if (objectRenderer == null)
            {
                objectRenderer = factory.CreateSprite(Root, "Sourced object", decor.art.Sprite,
                    Vector2.zero, Vector2.one, ObjectColor, sortingOrder + ICON_ORDER_OFFSET);
                renderers.Add(objectRenderer);
            }
            // Source PNGs are natural upright illustrations, not pre-warped silhouette art. Their orientation follows
            // the player's quarter turns; the immutable catalog solution transform still belongs to the logical base.
            float aspect = decor.art.Sprite.rect.width / decor.art.Sprite.rect.height;
            bool sideways = artQuarterTurns % 2 != 0;
            illustrationRect = PiecePresentationLayout.Fit(shape, sideways ? 1f / aspect : aspect, 0.08f);
            objectRenderer.transform.localPosition = new Vector3(illustrationRect.center.x, -illustrationRect.center.y);
            objectRenderer.flipX = false;
            spinning = false;
            ApplyArtRotation(0f);
            UpdateAdornments();
        }

        /// <summary>Advances only the illustrated object's orientation after the controller rotates its footprint.</summary>
        public void TurnArtClockwise() => artQuarterTurns = (artQuarterTurns + 1) % 4;

        // extraDegrees > 0 shows the object part-way through its last clockwise quarter turn.
        void ApplyArtRotation(float extraDegrees)
        {
            if (objectRenderer == null) return;
            if (decor.art.OnTile)
            {
                float angle = -90f * artQuarterTurns + extraDegrees;
                objectRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                var bounds = objectRenderer.sprite.bounds.size;
                float radians = angle * Mathf.Deg2Rad;
                float w = Mathf.Abs(Mathf.Cos(radians)) * bounds.x + Mathf.Abs(Mathf.Sin(radians)) * bounds.y;
                float h = Mathf.Abs(Mathf.Sin(radians)) * bounds.x + Mathf.Abs(Mathf.Cos(radians)) * bounds.y;
                float fit = Mathf.Min(illustrationRect.width / w, illustrationRect.height / h);
                objectRenderer.transform.localScale = new Vector3(fit, fit, 1f);
                return;
            }
            objectRenderer.transform.localRotation =
                Quaternion.Euler(0f, 0f, -90f * (decor.art.SolutionTurns + artQuarterTurns) + extraDegrees);
        }

        void UpdateAdornments()
        {
            UpdateShadow();
            UpdateBadgeSizing(worldUnitsPerScreenPixel);
        }

        /// <summary>
        /// Updates rule markers after camera/layout or resting scale changes. The screen-pixel floor is bounded by an
        /// occupied cell, never allowed to overlap neighboring pieces. Traits aggregate when separate markers cannot fit.
        /// </summary>
        public void UpdateBadgeSizing(float worldUnitsPerScreenPixel)
        {
            this.worldUnitsPerScreenPixel = Mathf.Max(0f, worldUnitsPerScreenPixel);
            if (Shape == null || badges.Count == 0) return;
            float available = Mathf.Max(0.01f, 1f - tuning.badgeMargin * 2f);
            badgeReferenceScale = gliding ? Mathf.Min(restScale, glideFromScale) : restScale;
            float readable = this.worldUnitsPerScreenPixel * tuning.badgeMinimumScreenPixels / Mathf.Max(0.001f, badgeReferenceScale);
            badgeDiameter = Mathf.Min(available, Mathf.Max(tuning.badgeSize, readable));
            if (decor?.art != null && decor.art.OnTile)
            {
                // Re-evaluate from the unconstrained image on camera/scale changes, so shrinking is never cumulative.
                float aspect = decor.art.Sprite.rect.width / decor.art.Sprite.rect.height;
                if (artQuarterTurns % 2 != 0) aspect = 1f / aspect;
                illustrationRect = PiecePresentationLayout.Fit(Shape, aspect, 0.08f);
            }
            aggregateBadges = !TryLayoutBadges(badgeDiameter);
            if (aggregateBadges && decor?.art != null && decor.art.OnTile)
            {
                var cell = Shape.FirstCell;
                float inset = badgeDiameter / 2f + tuning.badgeMargin;
                summaryPosition = new Vector3(cell.x + inset, -cell.y - inset);
                var reserved = new Rect(cell.x, cell.y,
                    badgeDiameter + tuning.badgeMargin + tuning.badgeMarkerGap,
                    badgeDiameter + tuning.badgeMargin + tuning.badgeMarkerGap);
                float aspect = decor.art.Sprite.rect.width / decor.art.Sprite.rect.height;
                illustrationRect = PiecePresentationLayout.Fit(Shape, artQuarterTurns % 2 != 0 ? 1f / aspect : aspect,
                    0.08f, reserved);
                if (!hasSummaryBadge)
                {
                    badgePositions[0] = summaryPosition;
                    aggregateBadges = false;
                }
            }
            else
            {
                var cell = Shape.FirstCell;
                summaryPosition = new Vector3(cell.x + 0.5f, -cell.y - 0.5f);
            }
            if (decor?.art != null && decor.art.OnTile && objectRenderer != null)
            {
                objectRenderer.transform.localPosition = new Vector3(illustrationRect.center.x, -illustrationRect.center.y);
                ApplyArtRotation(0f);
            }
            for (int i = 0; i < badges.Count; i++)
            {
                SetBadgeVisible(badges[i], !aggregateBadges);
            }
            if (hasSummaryBadge)
            {
                SetBadgeVisible(summaryBadge, aggregateBadges);
            }
            ApplyBadgeTransforms(Root.localScale.x, Root.localEulerAngles.z);
        }

        bool TryLayoutBadges(float diameter)
        {
            float inset = diameter / 2f + tuning.badgeMargin;
            float separation = diameter + tuning.badgeMarkerGap;
            for (int index = 0; index < badges.Count; index++)
            {
                bool found = false;
                for (int cellIndex = 0; cellIndex < Shape.Cells.Count && !found; cellIndex++)
                {
                    var cell = Shape.Cells[cellIndex];
                    for (int attempt = 0; attempt < 5 && !found; attempt++)
                    {
                        int corner = ((int)badges[index].corner + attempt) % 4;
                        float x = attempt == 4 ? 0.5f : corner == 2 || corner == 3 ? 1f - inset : inset;
                        float y = attempt == 4 ? 0.5f : corner == 1 || corner == 3 ? 1f - inset : inset;
                        var position = new Vector3(cell.x + x, -cell.y - y, 0f);
                        if (decor?.art != null && decor.art.OnTile)
                        {
                            float half = diameter / 2f + tuning.badgeMarkerGap;
                            var marker = new Rect(position.x - half, -position.y - half, half * 2f, half * 2f);
                            if (marker.Overlaps(illustrationRect)) continue;
                        }
                        bool clear = true;
                        for (int earlier = 0; earlier < index; earlier++)
                        {
                            if ((badgePositions[earlier] - position).sqrMagnitude < separation * separation)
                            {
                                clear = false;
                                break;
                            }
                        }
                        if (!clear) continue;
                        badgePositions[index] = position;
                        found = true;
                    }
                }
                if (!found) return false;
            }
            return true;
        }

        static void SetBadgeVisible(Badge badge, bool visible)
        {
            badge.circle.gameObject.SetActive(visible);
            if (badge.emoji != null) badge.emoji.gameObject.SetActive(visible);
            if (badge.art != null) badge.art.gameObject.SetActive(visible);
        }

        void ApplyBadgeTransforms(float drawnScale, float angle)
        {
            if (badges.Count == 0 || Shape == null) return;
            // Transforms only during motion: no text mesh work and no allocations in TickShake.
            float compensation = drawnScale > 0.001f ? Mathf.Min(badgeReferenceScale / drawnScale, 1f) : 1f;
            float diameter = badgeDiameter * compensation;
            float textScale = diameter / Mathf.Max(0.001f, tuning.badgeSize);
            var upright = Quaternion.Euler(0f, 0f, -angle);
            if (aggregateBadges && hasSummaryBadge)
            {
                PositionBadge(summaryBadge, summaryPosition, diameter, textScale, upright);
                return;
            }
            for (int i = 0; i < badges.Count; i++)
                PositionBadge(badges[i], badgePositions[i], diameter, textScale, upright);
        }

        static void PositionBadge(Badge badge, Vector3 position, float diameter, float compensation, Quaternion upright)
        {
            badge.circle.transform.localPosition = position;
            badge.circle.transform.localScale = new Vector3(diameter, diameter, 1f);
            if (badge.emoji != null)
            {
                badge.emoji.transform.localPosition = position;
                badge.emoji.transform.localRotation = upright;
                badge.emoji.transform.localScale = new Vector3(compensation, compensation, 1f);
            }
            if (badge.art != null)
            {
                badge.art.transform.localPosition = position;
                badge.art.transform.localRotation = upright;
                SpriteFactory.FitSprite(badge.art, diameter * BADGE_ART_FILL);
            }
        }

        void AddQuad(Vector2 center, Vector2 size)
        {
            renderers.Add(factory.CreateQuad(Root, "Cell", center, size, CellColor, sortingOrder));
        }

        Color BaseColor => new Color(color.r, color.g, color.b, color.a * opacity);
        Color CellColor => decor?.art != null && !decor.art.OnTile ? ObjectColor : BaseColor;
        Color ObjectColor => new Color(1f, 1f, 1f, color.a * opacity);

        // Ground shadow: shown in the tray (scaled down) and while dragged (lifted: lower and smaller).
        // A piece resting at full scale is seated inside the container and casts none.
        void UpdateShadow()
        {
            if (shadow == null || Shape == null) return;
            bool lifted = sortingOrder >= SpriteFactory.ORDER_DRAGGED;
            shadow.enabled = lifted || restScale < 1f;
            float size = lifted ? tuning.pieceShadowLiftedScale : 1f;
            float drop = lifted ? tuning.pieceShadowLiftedDrop : tuning.pieceShadowDrop;
            shadow.transform.localPosition = new Vector3(Shape.Width / 2f, -Shape.Height - drop, 0f);
            shadow.transform.localScale = new Vector3(Shape.Width * tuning.pieceShadowWidthScale * size,
                tuning.pieceShadowHeight * size, 1f);
            var tint = tuning.pieceShadowColor;
            tint.a *= opacity;
            shadow.color = tint;
        }

        void UpdateBorderColor()
        {
            if (border == null) return;
            var tint = decor.outlineColor;
            tint.a *= color.a * opacity;
            border.color = tint;
        }

        /// <summary>Tints every cell quad (not the icon or badges).</summary>
        public void SetColor(Color newColor)
        {
            color = newColor;
            UpdateBorderColor();
            foreach (var r in renderers)
            {
                r.color = r == objectRenderer ? ObjectColor : CellColor;
            }
        }

        /// <summary>Multiplies the opacity of the cells, icon and badges by <paramref name="value"/> (0..1).</summary>
        public void SetOpacity(float value)
        {
            opacity = Mathf.Clamp01(value);
            UpdateBorderColor();
            UpdateShadow();
            foreach (var r in renderers)
            {
                r.color = r == objectRenderer ? ObjectColor : CellColor;
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
                if (badge.emoji != null) badge.emoji.alpha = opacity;
                if (badge.art != null) badge.art.color = new Color(1f, 1f, 1f, opacity);
            }
            if (hasSummaryBadge)
            {
                var tint = summaryBadge.circle.color;
                tint.a = opacity;
                summaryBadge.circle.color = tint;
                summaryBadge.emoji.alpha = opacity;
            }
        }

        /// <summary>Sets the sorting order of the cells; the icon and badges follow just above.</summary>
        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            UpdateShadow();
            if (border != null) border.sortingOrder = order;
            foreach (var r in renderers)
            {
                r.sortingOrder = order + (r == objectRenderer && decor.art.OnTile ? ICON_ORDER_OFFSET : 0);
            }
            if (icon != null)
            {
                icon.sortingOrder = order + ICON_ORDER_OFFSET;
            }
            foreach (var badge in badges)
            {
                badge.circle.sortingOrder = order + BADGE_ORDER_OFFSET;
                if (badge.emoji != null) badge.emoji.sortingOrder = order + BADGE_TEXT_ORDER_OFFSET;
                if (badge.art != null) badge.art.sortingOrder = order + BADGE_TEXT_ORDER_OFFSET;
            }
            if (hasSummaryBadge)
            {
                summaryBadge.circle.sortingOrder = order + BADGE_ORDER_OFFSET;
                summaryBadge.emoji.sortingOrder = order + BADGE_TEXT_ORDER_OFFSET;
            }
        }

        /// <summary>True while a visual travel animation is still running.</summary>
        public bool IsGliding => gliding;

        /// <summary>Shows or hides immediately (ghosts and outlines); page visibility uses <see cref="SetPageVisible"/>.</summary>
        public void SetVisible(bool visible)
        {
            pageVisible = visible;
            glideVisible = false;
            Root.gameObject.SetActive(visible);
        }

        /// <summary>Changes page visibility, preserving an already visible returning glide until it finishes.</summary>
        public void SetPageVisible(bool visible)
        {
            if (pageVisible == visible) return;
            pageVisible = visible;
            if (!visible && gliding && Root.gameObject.activeSelf) glideVisible = true;
            ApplyPageVisibility();
        }

        void ApplyPageVisibility()
        {
            bool visible = pageVisible || (gliding && glideVisible);
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
        }

        /// <summary>Moves the top-left corner to <paramref name="topLeft"/> at <paramref name="scale"/> and stops any shake or wobble.</summary>
        public void PlaceAt(Vector3 topLeft, float scale)
        {
            bool scaleChanged = !Mathf.Approximately(restScale, scale);
            restPosition = topLeft;
            restScale = scale;
            bouncing = false;
            gliding = false;
            popping = false;
            if (spinning)
            {
                spinning = false;
                ApplyArtRotation(0f);
            }
            shake.Stop();
            wobble.Stop();
            animating = false;
            Root.SetPositionAndRotation(topLeft, Quaternion.identity);
            Root.localScale = new Vector3(scale, scale, 1f);
            UpdateShadow();
            if (scaleChanged) UpdateBadgeSizing(worldUnitsPerScreenPixel);
            else ApplyBadgeTransforms(scale, 0f);
            glideVisible = false;
            ApplyPageVisibility();
        }

        /// <summary>
        /// Like <see cref="PlaceAt"/>, but the drawn piece travels there over <paramref name="seconds"/> from where it is
        /// shown now. The rest pose (<see cref="RestTopLeft"/>, <see cref="Scale"/>) changes at once, so rules, hit tests
        /// and follow-up effects never wait for the motion. Instant under reduced motion.
        /// </summary>
        public void GlideTo(Vector3 topLeft, float scale, float seconds)
        {
            Vector3 from = Root.position;
            float fromScale = Root.localScale.x;
            bool wasVisible = Root.gameObject.activeSelf;
            bool keepSpin = spinning;
            PlaceAt(topLeft, scale);
            if (ReduceMotion || seconds <= 0f || ((from - topLeft).sqrMagnitude < 1e-6f && Mathf.Approximately(fromScale, scale)))
            {
                return;
            }
            spinning = keepSpin;
            glideFrom = from;
            glideFromScale = fromScale;
            glideAge = 0f;
            glideDuration = seconds;
            gliding = true;
            animating = true;
            Root.SetPositionAndRotation(from, Quaternion.identity);
            Root.localScale = new Vector3(fromScale, fromScale, 1f);
            glideVisible = wasVisible;
            UpdateBadgeSizing(worldUnitsPerScreenPixel);
            ApplyPageVisibility();
        }

        /// <summary>Shows the illustrated object turning its last clockwise quarter turn (legacy block pieces skip it).</summary>
        public void StartSpin(float seconds)
        {
            if (ReduceMotion || seconds <= 0f || objectRenderer == null) return;
            spinAge = 0f;
            spinDuration = seconds;
            spinning = true;
            animating = true;
            ApplyArtRotation(90f);
        }

        /// <summary>Scales the drawn piece in from nothing after <paramref name="delay"/> seconds (dealing a level).</summary>
        public void StartPopIn(float delay, float seconds)
        {
            if (ReduceMotion || seconds <= 0f) return;
            popAge = -Mathf.Max(0f, delay);
            popDuration = seconds;
            popping = true;
            animating = true;
            Root.localScale = Vector3.zero;
        }

        /// <summary>Starts a horizontal shake around the current rest position.</summary>
        public void StartShake()
        {
            if (ReduceMotion) return;
            shake.Start(tuning.shakeDuration, tuning.shakeAmplitude, tuning.shakeCycles);
            animating = true;
        }

        /// <summary>Starts a rotation wobble around the piece's centre (upright tap, hint highlight).</summary>
        /// <param name="seconds">Duration.</param>
        /// <param name="cycles">Full left-right swings.</param>
        public void StartWobble(float seconds, float cycles)
        {
            if (ReduceMotion) return;
            wobble.Start(seconds, tuning.wobbleAngle, cycles);
            animating = true;
        }

        /// <summary>Starts a centre-anchored visual bounce without changing the logical placement.</summary>
        public void StartBounce(float seconds, float amount)
        {
            if (ReduceMotion) return;
            bounceAge = 0f;
            bounceDuration = Mathf.Max(0.01f, seconds);
            bounceAmount = Mathf.Clamp(amount, 0f, 0.2f);
            bouncing = true;
            animating = true;
        }

        /// <summary>Advances shake, wobble and bounce; call every frame.</summary>
        public void TickShake(float deltaTime)
        {
            if (!animating) return;
            if (ReduceMotion)
            {
                PlaceAt(restPosition, restScale);
                return;
            }
            bool wasGliding = gliding;
            float offset = shake.Tick(deltaTime);
            float angle = wobble.Tick(deltaTime);
            Vector3 basePosition = restPosition;
            float baseScale = restScale;
            if (gliding)
            {
                glideAge += deltaTime;
                float g = Mathf.Clamp01(glideAge / glideDuration);
                float eased = 1f - (1f - g) * (1f - g) * (1f - g);
                basePosition = Vector3.LerpUnclamped(glideFrom, restPosition, eased);
                baseScale = Mathf.LerpUnclamped(glideFromScale, restScale, eased);
                gliding = g < 1f;
            }
            if (spinning)
            {
                spinAge += deltaTime;
                float s = Mathf.Clamp01(spinAge / spinDuration);
                float eased = 1f - (1f - s) * (1f - s);
                spinning = s < 1f;
                ApplyArtRotation(90f * (1f - eased));
            }
            float scale = baseScale;
            if (popping)
            {
                popAge += deltaTime;
                float p = Mathf.Clamp01(popAge / popDuration);
                // Ease-out-back: a small overshoot so pieces land with a soft pop.
                float u = p - 1f;
                float eased = p <= 0f ? 0f : 1f + 2.70158f * u * u * u + 1.70158f * u * u;
                scale *= eased;
                popping = p < 1f;
            }
            if (bouncing)
            {
                bounceAge += deltaTime;
                float k = Mathf.Clamp01(bounceAge / bounceDuration);
                scale *= 1f + Mathf.Sin(k * Mathf.PI) * (1f - k) * bounceAmount;
                bouncing = k < 1f;
            }
            animating = shake.Active || wobble.Active || bouncing || gliding || spinning || popping;
            Root.localScale = new Vector3(scale, scale, 1f);
            var rotation = Quaternion.Euler(0f, 0f, angle);
            var localCenter = new Vector3(Shape.Width / 2f, -Shape.Height / 2f, 0f);
            Root.SetPositionAndRotation(basePosition + new Vector3(offset, 0f, 0f) +
                localCenter * baseScale - rotation * (localCenter * scale), rotation);
            if (wasGliding && !gliding)
            {
                glideVisible = false;
                UpdateBadgeSizing(worldUnitsPerScreenPixel);
                ApplyPageVisibility();
            }
            ApplyBadgeTransforms(scale, angle);
        }
    }
}
