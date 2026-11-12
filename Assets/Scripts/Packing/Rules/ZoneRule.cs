// Implements: design/game-brief.md "Obstacles" (Khu vực: an item with a zone icon must lie entirely inside its zone)
// and "Quyết định đã chốt" (zone is checked on drop).
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md
// Pure C#: no MonoBehaviour dependency.

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Zones: every cell of a zoned piece must carry that piece's zone key in the level's zoneMap.</summary>
    public sealed class ZoneRule : IPackingRule
    {
        /// <summary>Returned by <see cref="ZoneAt"/> and <see cref="ZoneOf"/> for "no zone".</summary>
        public const char NO_ZONE = '\0';

        /// <summary>Character marking a blocked cell in the zoneMap.</summary>
        public const char BLOCKED_ZONE = '#';

        readonly string[] zoneMap;
        readonly char[] pieceZones;

        /// <summary>Creates the rule.</summary>
        /// <param name="zoneMap">Rows of single-character zone keys, row 0 at the top ('#' = blocked).</param>
        /// <param name="pieces">The level's pieces; a piece's <c>zone</c> key, "" = may go anywhere.</param>
        public ZoneRule(string[] zoneMap, PieceData[] pieces)
        {
            this.zoneMap = zoneMap;
            pieceZones = new char[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieceZones[i] = string.IsNullOrEmpty(pieces[i].zone) ? NO_ZONE : pieces[i].zone[0];
            }
        }

        /// <summary>Zone key of a cell, or <see cref="NO_ZONE"/> outside the map.</summary>
        public char ZoneAt(Vector2Int cell)
        {
            if (cell.y < 0 || cell.y >= zoneMap.Length || cell.x < 0 || cell.x >= zoneMap[cell.y].Length)
            {
                return NO_ZONE;
            }
            return zoneMap[cell.y][cell.x];
        }

        /// <summary>Zone key a piece must sit in, or <see cref="NO_ZONE"/> if it may go anywhere.</summary>
        public char ZoneOf(int pieceId) => pieceZones[pieceId];

        /// <inheritdoc/>
        public PlacementFailure CheckPlacement(int pieceId, PieceShape shape, Vector2Int origin)
        {
            char zone = pieceZones[pieceId];
            if (zone == NO_ZONE)
            {
                return PlacementFailure.None;
            }
            foreach (var c in shape.Cells)
            {
                if (ZoneAt(origin + c) != zone)
                {
                    return PlacementFailure.Zone;
                }
            }
            return PlacementFailure.None;
        }

        /// <summary>Zones are enforced on drop, so a full board never breaks them.</summary>
        public int FindCompletionViolation(BoardModel board) => BoardModel.NO_PIECE;

        /// <inheritdoc/>
        public bool AllowsRotation(int pieceId) => true;
    }
}
