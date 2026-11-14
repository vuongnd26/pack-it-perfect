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
        /// <summary>Natural sourced illustration displayed on a cell-shaped base instead of a silhouette illustration.</summary>
        public bool OnTile { get; internal set; }

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

        // Sprites are loaded when a level first asks for them and let go when another level is shown: loading every
        // illustrated object of every level up front made the first level hitch and kept ~100 textures in memory.
        sealed class Binding
        {
            public int level;
            public string spriteName;
            public string sourcedSpriteName;
            public string displayName;
            public PieceShape shape;
            public int solutionTurns;
            public bool mirrored;
            public PieceArt art;
            public bool missing;
        }

        readonly SpriteFactory factory;
        readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>();

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
            public string sourced_sprite;
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
            this.factory = factory;
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
                bindings.Add(Key(binding.level, binding.piece), new Binding
                {
                    level = binding.level,
                    spriteName = item.sprite,
                    sourcedSpriteName = item.sourced_sprite,
                    displayName = item.display_name,
                    shape = PieceShape.FromCells(cells),
                    solutionTurns = binding.solution_turns,
                    mirrored = binding.mirrored,
                });
            }
        }

        /// <summary>Whole-object art for this level and letter, or null for a legacy/unbound piece. Loads its sprite on first use.</summary>
        public PieceArt Get(int levelNumber, string pieceLetter)
        {
            if (!bindings.TryGetValue(Key(levelNumber, pieceLetter), out var binding) || binding.missing) return null;
            if (binding.art != null) return binding.art;
            var sprite = string.IsNullOrEmpty(binding.sourcedSpriteName) ? null : factory.GetArt(binding.sourcedSpriteName);
            bool onTile = sprite != null;
            if (sprite == null) sprite = factory.GetArt(binding.spriteName);
            if (sprite == null)
            {
                binding.missing = true;
                Debug.LogError($"PieceArtCatalog: missing illustrated object {binding.spriteName}.");
                return null;
            }
            binding.art = new PieceArt(sprite, binding.displayName, binding.shape, binding.solutionTurns, binding.mirrored)
                { OnTile = onTile };
            return binding.art;
        }

        /// <summary>
        /// Drops the sprites of every level except <paramref name="levelNumber"/> from the caches, so the engine can unload
        /// their textures. A sprite the shown level also uses is kept.
        /// </summary>
        public void KeepOnly(int levelNumber)
        {
            var keep = new HashSet<string>();
            foreach (var binding in bindings.Values)
            {
                if (binding.level != levelNumber) continue;
                keep.Add(binding.spriteName);
                if (!string.IsNullOrEmpty(binding.sourcedSpriteName)) keep.Add(binding.sourcedSpriteName);
            }
            foreach (var binding in bindings.Values)
            {
                if (binding.level == levelNumber || binding.art == null) continue;
                binding.art = null;
                if (!keep.Contains(binding.spriteName)) factory.ForgetArt(binding.spriteName);
                if (!string.IsNullOrEmpty(binding.sourcedSpriteName) && !keep.Contains(binding.sourcedSpriteName))
                    factory.ForgetArt(binding.sourcedSpriteName);
            }
        }

        static string Key(int levelNumber, string letter) => levelNumber + ":" + letter;
    }
}
