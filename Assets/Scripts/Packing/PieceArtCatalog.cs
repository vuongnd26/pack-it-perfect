// Presentation-only artwork bindings. Geometry and rule data remain in levels.json.
// Wire schema: tools/asset-pipeline/README.md (snake_case JSON DTOs map once into immutable views).

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>A whole illustrated object in its authored orientation, with a transform into the solution shape.</summary>
    public sealed class PieceArt
    {
        /// <summary>Full-colour object sprite, with one authored cell per 128 pixels.</summary>
        public Sprite Sprite { get; }
        /// <summary>Human-readable object name for feedback, not its former placeholder item name.</summary>
        public string DisplayName { get; }
        /// <summary>Authored occupancy; used to verify the presentation never changes the packing footprint.</summary>
        public PieceShape AuthoredShape { get; }
        /// <summary>Clockwise quarter turns from the authored sprite to the solution orientation.</summary>
        public int SolutionTurns { get; }
        /// <summary>Reflect the authored illustration horizontally before rotating it.</summary>
        public bool Mirrored { get; }

        internal PieceArt(Sprite sprite, string displayName, PieceShape shape, int solutionTurns, bool mirrored)
        {
            Sprite = sprite;
            DisplayName = displayName;
            AuthoredShape = shape;
            SolutionTurns = solutionTurns;
            Mirrored = mirrored;
        }

        /// <summary>Returns the illustrated footprint after its presentation transform, for validation only.</summary>
        public PieceShape SolutionShape()
        {
            var cells = new List<Vector2Int>(AuthoredShape.Cells.Count);
            foreach (var cell in AuthoredShape.Cells)
            {
                cells.Add(new Vector2Int(Mirrored ? AuthoredShape.Width - 1 - cell.x : cell.x, cell.y));
            }
            return PieceShape.FromCells(cells).Rotated(SolutionTurns);
        }
    }

    /// <summary>Loads the independent art manifest once and caches per-piece bindings. Does not modify level data.</summary>
    public sealed class PieceArtCatalog
    {
        const string RESOURCE = "Art/item_catalog";
        readonly Dictionary<string, PieceArt> bindings = new Dictionary<string, PieceArt>();

        // Only these DTO fields use snake_case: they are the external wire-format boundary.
        [Serializable] sealed class CatalogDto
        {
            public int schema_version;
            public ArtDto[] objects;
            public BindingDto[] bindings;
        }
        [Serializable] sealed class ArtDto
        {
            public string id;
            public string display_name;
            public string sprite;
            public string[] cells;
        }
        [Serializable] sealed class BindingDto
        {
            public int level;
            public string piece;
            public string art_id;
            public int solution_turns;
            public bool mirrored;
        }

        /// <summary>Reads schema version 1 and resolves sprite assets through the supplied factory.</summary>
        public PieceArtCatalog(SpriteFactory factory)
        {
            var text = Resources.Load<TextAsset>(RESOURCE);
            if (text == null) return; // Legacy art remains a safe fallback while authoring.
            var dto = JsonUtility.FromJson<CatalogDto>(text.text);
            if (dto == null || dto.schema_version != 1 || dto.objects == null || dto.bindings == null)
            {
                Debug.LogError("PieceArtCatalog: unsupported or incomplete art manifest.");
                return;
            }
            var objects = new Dictionary<string, ArtDto>();
            foreach (var item in dto.objects) objects.Add(item.id, item);
            foreach (var binding in dto.bindings)
            {
                var item = objects[binding.art_id];
                var cells = new List<Vector2Int>();
                for (int y = 0; y < item.cells.Length; y++)
                    for (int x = 0; x < item.cells[y].Length; x++)
                        if (item.cells[y][x] == 'X') cells.Add(new Vector2Int(x, y));
                var sprite = factory.GetArt(item.sprite);
                if (sprite == null)
                {
                    Debug.LogError($"PieceArtCatalog: missing illustrated object {item.sprite}.");
                    continue;
                }
                bindings.Add(Key(binding.level, binding.piece), new PieceArt(sprite, item.display_name,
                    PieceShape.FromCells(cells), binding.solution_turns, binding.mirrored));
            }
        }

        /// <summary>Whole-object art for this level and letter, or null for a legacy/unbound piece.</summary>
        public PieceArt Get(int levelNumber, string pieceLetter) =>
            bindings.TryGetValue(Key(levelNumber, pieceLetter), out var art) ? art : null;

        static string Key(int levelNumber, string letter) => levelNumber + ":" + letter;
    }
}
