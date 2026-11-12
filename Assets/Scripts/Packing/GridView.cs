// Implements: design/game-brief.md "Core loop" (the container grid), "Cơ chế chính"
// (Kéo thả đặt đồ: a % bar shows how many cells are filled) and "Obstacles" (Khu vực: the container is split into
// coloured, labelled zones; Cặp đồ: the container shakes when pairs do not touch).
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-003-obstacle-rules.md

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Draws the container (frame, lining, grid lines, blocked cells) and the fill bar above it,
    /// and converts between world positions and grid cells. The grid's top edge is at world y = 0,
    /// centred on x = 0, one world unit per cell.
    /// </summary>
    public sealed class GridView
    {
        readonly BoardModel board;
        readonly PackingTuning tuning;
        readonly Transform root;
        readonly ShakeMotion shake = new ShakeMotion();
        readonly SpriteRenderer fillBar;
        readonly float fillBarLeft;
        readonly float fillBarWidth;
        readonly float fillBarCenterY;
        readonly float fillBarHeight;

        /// <summary>World x of the grid's left edge.</summary>
        public float Left { get; }

        /// <summary>World y of the grid's top edge.</summary>
        public float Top { get; }

        /// <summary>World rectangle of the frame (grid plus frame padding).</summary>
        public Rect FrameRect { get; }

        /// <summary>World rectangle of the frame plus the fill bar above it.</summary>
        public Rect ContentRect { get; }

        /// <summary>
        /// Root of the container visuals. Children (outlines, hint glow) move with the container when it shakes.
        /// Local space = world space at rest.
        /// </summary>
        public Transform Root => root;

        /// <summary>Builds the container visuals for <paramref name="level"/> under <paramref name="parent"/>.</summary>
        public GridView(SpriteFactory factory, Transform parent, LoadedLevel level, PackingTuning tuning)
        {
            board = level.Board;
            this.tuning = tuning;
            var theme = level.Data.theme;
            Left = -board.Width / 2f;
            Top = 0f;
            float pad = tuning.framePadding;
            FrameRect = new Rect(Left - pad, Top - board.Height - pad, board.Width + 2f * pad, board.Height + 2f * pad);

            root = new GameObject("Grid").transform;
            root.SetParent(parent, false);

            Color frameColor = LevelLoader.ParseColor(theme.frame);
            Color liningColor = LevelLoader.ParseColor(theme.lining);
            Color lineColor = LevelLoader.ParseColor(theme.line);
            Color blockColor = LevelLoader.ParseColor(theme.block);

            factory.CreateQuad(root, "Frame", FrameRect.center, FrameRect.size, frameColor, SpriteFactory.ORDER_FRAME);
            var gridCenter = new Vector2(Left + board.Width / 2f, Top - board.Height / 2f);
            factory.CreateQuad(root, "Lining", gridCenter, new Vector2(board.Width, board.Height), liningColor, SpriteFactory.ORDER_LINING);

            if (level.Rules.Zones != null)
            {
                BuildZones(factory, level);
            }

            for (int x = 1; x < board.Width; x++)
            {
                factory.CreateQuad(root, "LineV", new Vector2(Left + x, gridCenter.y),
                    new Vector2(tuning.gridLineWidth, board.Height), lineColor, SpriteFactory.ORDER_GRID_LINE);
            }
            for (int y = 1; y < board.Height; y++)
            {
                factory.CreateQuad(root, "LineH", new Vector2(gridCenter.x, Top - y),
                    new Vector2(board.Width, tuning.gridLineWidth), lineColor, SpriteFactory.ORDER_GRID_LINE);
            }

            float blockedSize = 1f - 2f * tuning.blockedCellInset;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.IsBlocked(new Vector2Int(x, y)))
                    {
                        factory.CreateQuad(root, "Blocked", new Vector2(Left + x + 0.5f, Top - y - 0.5f),
                            new Vector2(blockedSize, blockedSize), blockColor, SpriteFactory.ORDER_BLOCKED);
                    }
                }
            }

            fillBarLeft = FrameRect.xMin;
            fillBarWidth = FrameRect.width;
            fillBarHeight = tuning.fillBarHeight;
            fillBarCenterY = FrameRect.yMax + tuning.fillBarGap + fillBarHeight / 2f;
            factory.CreateQuad(root, "FillBarBack", new Vector2(FrameRect.center.x, fillBarCenterY),
                new Vector2(fillBarWidth, fillBarHeight), tuning.fillBarBackColor, SpriteFactory.ORDER_FILL_BAR);
            fillBar = factory.CreateQuad(root, "FillBar", Vector2.zero, Vector2.zero, tuning.fillBarColor, SpriteFactory.ORDER_FILL_BAR + 1);
            SetFill(0f);

            float contentTop = fillBarCenterY + fillBarHeight / 2f;
            ContentRect = new Rect(FrameRect.xMin, FrameRect.yMin, FrameRect.width, contentTop - FrameRect.yMin);
        }

        // Zone tint per cell, a divider wherever neighbouring cells change zone, and an "icon name" label
        // on the zone's cell nearest the zone's centroid.
        void BuildZones(SpriteFactory factory, LoadedLevel level)
        {
            var zones = level.Rules.Zones;
            var zoneRoot = new GameObject("Zones").transform;
            zoneRoot.SetParent(root, false);

            var cellsByZone = new Dictionary<char, List<Vector2Int>>();
            foreach (var zone in level.Data.zones)
            {
                cellsByZone[zone.key[0]] = new List<Vector2Int>();
            }

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    char key = zones.ZoneAt(cell);
                    if (!cellsByZone.TryGetValue(key, out var list))
                    {
                        continue; // blocked
                    }
                    list.Add(cell);
                    var data = System.Array.Find(level.Data.zones, z => z.key[0] == key);
                    factory.CreateQuad(zoneRoot, "ZoneTint", CellCenter(cell), Vector2.one, LevelLoader.ParseColor(data.color),
                        SpriteFactory.ORDER_ZONE_TINT);

                    // Right and lower edges: a divider where both cells are zoned and differ.
                    if (x + 1 < board.Width && ChangesZone(zones, key, new Vector2Int(x + 1, y), cellsByZone))
                    {
                        factory.CreateDashedLine(zoneRoot, "ZoneDivider", new Vector2(Left + x + 1, Top - y), new Vector2(Left + x + 1, Top - y - 1),
                            tuning.zoneDividerWidth, tuning.zoneDividerDashesPerCell, tuning.zoneDividerDashFill,
                            tuning.zoneDividerColor, SpriteFactory.ORDER_ZONE_DIVIDER);
                    }
                    if (y + 1 < board.Height && ChangesZone(zones, key, new Vector2Int(x, y + 1), cellsByZone))
                    {
                        factory.CreateDashedLine(zoneRoot, "ZoneDivider", new Vector2(Left + x, Top - y - 1), new Vector2(Left + x + 1, Top - y - 1),
                            tuning.zoneDividerWidth, tuning.zoneDividerDashesPerCell, tuning.zoneDividerDashFill,
                            tuning.zoneDividerColor, SpriteFactory.ORDER_ZONE_DIVIDER);
                    }
                }
            }

            foreach (var zone in level.Data.zones)
            {
                var cells = cellsByZone[zone.key[0]];
                if (cells.Count == 0)
                {
                    continue;
                }
                var centroid = Vector2.zero;
                foreach (var c in cells)
                {
                    centroid += (Vector2)CellCenter(c);
                }
                centroid /= cells.Count;
                var best = cells[0];
                foreach (var c in cells)
                {
                    if (((Vector2)CellCenter(c) - centroid).sqrMagnitude < ((Vector2)CellCenter(best) - centroid).sqrMagnitude)
                    {
                        best = c;
                    }
                }
                var label = TmpText.Create(zoneRoot, "ZoneLabel", $"{zone.icon} {zone.name}", tuning.zoneLabelSize, tuning.zoneLabelColor,
                    SpriteFactory.ORDER_ZONE_LABEL);
                label.transform.localPosition = CellCenter(best);
            }
        }

        static bool ChangesZone(ZoneRule zones, char key, Vector2Int neighbour, Dictionary<char, List<Vector2Int>> zoneKeys)
        {
            char other = zones.ZoneAt(neighbour);
            return other != key && zoneKeys.ContainsKey(other);
        }

        /// <summary>World position of the centre of <paramref name="cell"/> (at rest).</summary>
        public Vector3 CellCenter(Vector2Int cell) => new Vector3(Left + cell.x + 0.5f, Top - cell.y - 0.5f, 0f);

        /// <summary>Shakes the whole container sideways (pairs not touching).</summary>
        public void StartShake() => shake.Start(tuning.shakeDuration, tuning.shakeAmplitude, tuning.shakeCycles);

        /// <summary>Advances the container shake; call every frame.</summary>
        public void Tick(float deltaTime)
        {
            if (shake.Active)
            {
                root.localPosition = new Vector3(shake.Tick(deltaTime), 0f, 0f);
            }
        }

        /// <summary>Sets the fill bar to <paramref name="ratio"/> (0..1) of its width, growing from the left.</summary>
        public void SetFill(float ratio)
        {
            float width = fillBarWidth * Mathf.Clamp01(ratio);
            fillBar.transform.localPosition = new Vector3(fillBarLeft + width / 2f, fillBarCenterY, 0f);
            fillBar.transform.localScale = new Vector3(width, fillBarHeight, 1f);
        }

        /// <summary>World position of the top-left corner of <paramref name="cell"/>.</summary>
        public Vector3 CellTopLeft(Vector2Int cell) => new Vector3(Left + cell.x, Top - cell.y, 0f);

        /// <summary>The cell containing the world point (may be out of bounds).</summary>
        public Vector2Int WorldToCell(Vector2 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x - Left), Mathf.FloorToInt(Top - world.y));

        /// <summary>The grid origin a piece snaps to when its top-left corner is at <paramref name="pieceTopLeft"/>.</summary>
        public Vector2Int SnapOrigin(Vector2 pieceTopLeft) =>
            new Vector2Int(Mathf.RoundToInt(pieceTopLeft.x - Left), Mathf.RoundToInt(Top - pieceTopLeft.y));

        /// <summary>True if the world point is inside the grid or within <paramref name="distance"/> of its edge.</summary>
        public bool IsNear(Vector2 world, float distance) =>
            world.x >= Left - distance && world.x <= Left + board.Width + distance &&
            world.y <= Top + distance && world.y >= Top - board.Height - distance;
    }
}
