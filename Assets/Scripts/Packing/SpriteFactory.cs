// Shared runtime sprite primitives for packing and procedural presentation, plus existing item icon assets.
// PatternSpriteCache owns the level-bounded patterned piece surfaces (story 009).
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-003 (zones, badges),
// story-004 (outlines), story-005 (hint glow).

using System.Collections.Generic;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Owns a white square and a white circle sprite generated at runtime and creates tinted, scaled quads from them;
    /// also loads and caches item icons. One instance per PackingGame; call <see cref="Dispose"/> when done.
    /// </summary>
    public sealed class SpriteFactory
    {
        /// <summary>Sorting order of the frame around the grid.</summary>
        public const int ORDER_FRAME = 0;
        /// <summary>Sorting order of the grid lining.</summary>
        public const int ORDER_LINING = 1;
        /// <summary>Sorting order of zone tints.</summary>
        public const int ORDER_ZONE_TINT = 2;
        /// <summary>Sorting order of the grid lines.</summary>
        public const int ORDER_GRID_LINE = 3;
        /// <summary>Sorting order of zone dividers.</summary>
        public const int ORDER_ZONE_DIVIDER = 4;
        /// <summary>Sorting order of blocked cells.</summary>
        public const int ORDER_BLOCKED = 5;
        /// <summary>Sorting order of the fill bar background (the fill is one above).</summary>
        public const int ORDER_FILL_BAR = 6;
        /// <summary>Sorting order of the soft shadow the frame casts onto the lining.</summary>
        public const int ORDER_INNER_SHADOW = 7;
        /// <summary>Sorting order of zone labels.</summary>
        public const int ORDER_ZONE_LABEL = 8;
        /// <summary>Sorting order of outline imprints (their icon is one above).</summary>
        public const int ORDER_OUTLINE = 9;
        /// <summary>Sorting order of the soft ground shadow under tray and dragged pieces.</summary>
        public const int ORDER_PIECE_SHADOW = 11;
        /// <summary>Sorting order of resting pieces (icon +1, badge circle +2, badge emoji +3).</summary>
        public const int ORDER_PIECE = 12;
        /// <summary>Sorting order of the hint glow.</summary>
        public const int ORDER_HINT = 18;
        /// <summary>Sorting order of the ghost preview.</summary>
        public const int ORDER_GHOST = 20;
        /// <summary>Sorting order of the dragged piece (icon and badges up to +3).</summary>
        public const int ORDER_DRAGGED = 30;
        /// <summary>Sorting order of the lid (zipper parts are one to three above).</summary>
        public const int ORDER_LID = 40;
        /// <summary>Sorting order of floating words (text is one above).</summary>
        public const int ORDER_WORD = 50;
        /// <summary>Sorting order of the toast (text is one above).</summary>
        public const int ORDER_TOAST = 60;

        /// <summary>Folder under Resources holding one icon sprite per item id.</summary>
        public const string ICON_FOLDER = "Icons/";
        /// <summary>Folder under Resources holding the illustrated art (frame, hand, backgrounds...).</summary>
        public const string ART_FOLDER = "Art/";

        const int TEXTURE_SIZE = 4;
        const int CIRCLE_TEXTURE_SIZE = 64;
        const int SOFT_TEXTURE_SIZE = 64;

        readonly Texture2D texture;
        readonly Sprite sprite;
        readonly Texture2D circleTexture;
        readonly Sprite circleSprite;
        readonly Texture2D softTexture;
        readonly Sprite softSprite;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        PieceArtCatalog pieceArtCatalog;

        /// <summary>Resources path of the unlit sprite material every world sprite shares.</summary>
        public const string SPRITE_MATERIAL = "Materials/PackSpriteUnlit";
        const string UNLIT_SHADER = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        static Material sharedUnlit;
        static bool unlitResolved;

        /// <summary>
        /// Unlit sprite material shared by every world sprite, so the 2D renderer never runs its lighting passes for a
        /// game that has no lights. Null (renderers keep their default material) when neither the asset nor the shader exists.
        /// </summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (!unlitResolved || (sharedUnlit == null && !ReferenceEquals(sharedUnlit, null)))
                {
                    unlitResolved = true;
                    sharedUnlit = Resources.Load<Material>(SPRITE_MATERIAL);
                    if (sharedUnlit == null)
                    {
                        var shader = Shader.Find(UNLIT_SHADER);
                        if (shader != null) sharedUnlit = new Material(shader) { name = "PackSpriteUnlit (runtime)" };
                    }
                }
                return sharedUnlit;
            }
        }

        /// <summary>The blurred disc texture (soft particles reuse it).</summary>
        public Texture2D SoftTexture => softTexture;

        /// <summary>Creates the white square and circle sprites, each 1×1 unit.</summary>
        public SpriteFactory()
        {
            texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                name = "PackingWhite",
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[TEXTURE_SIZE * TEXTURE_SIZE];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, TEXTURE_SIZE, TEXTURE_SIZE), new Vector2(0.5f, 0.5f),
                TEXTURE_SIZE, 0, SpriteMeshType.FullRect);
            sprite.name = "PackingWhite";

            // Anti-aliased disc: alpha falls from 1 to 0 across the last pixel of the radius.
            circleTexture = new Texture2D(CIRCLE_TEXTURE_SIZE, CIRCLE_TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                name = "PackingCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var circlePixels = new Color32[CIRCLE_TEXTURE_SIZE * CIRCLE_TEXTURE_SIZE];
            float radius = CIRCLE_TEXTURE_SIZE / 2f;
            for (int y = 0; y < CIRCLE_TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < CIRCLE_TEXTURE_SIZE; x++)
                {
                    float d = new Vector2(x + 0.5f - radius, y + 0.5f - radius).magnitude;
                    byte a = (byte)(Mathf.Clamp01(radius - d) * 255f);
                    circlePixels[y * CIRCLE_TEXTURE_SIZE + x] = new Color32(255, 255, 255, a);
                }
            }
            circleTexture.SetPixels32(circlePixels);
            circleTexture.Apply(false, true);
            circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, CIRCLE_TEXTURE_SIZE, CIRCLE_TEXTURE_SIZE),
                new Vector2(0.5f, 0.5f), CIRCLE_TEXTURE_SIZE, 0, SpriteMeshType.FullRect);
            circleSprite.name = "PackingCircle";

            // Blurred disc: alpha eases from 1 at the centre to 0 at the rim (ground shadows).
            softTexture = new Texture2D(SOFT_TEXTURE_SIZE, SOFT_TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                name = "PackingSoftDisc",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var softPixels = new Color32[SOFT_TEXTURE_SIZE * SOFT_TEXTURE_SIZE];
            float softRadius = SOFT_TEXTURE_SIZE / 2f;
            for (int y = 0; y < SOFT_TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < SOFT_TEXTURE_SIZE; x++)
                {
                    float d = new Vector2(x + 0.5f - softRadius, y + 0.5f - softRadius).magnitude / softRadius;
                    float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
                    softPixels[y * SOFT_TEXTURE_SIZE + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            softTexture.SetPixels32(softPixels);
            softTexture.Apply(false, true);
            softSprite = Sprite.Create(softTexture, new Rect(0, 0, SOFT_TEXTURE_SIZE, SOFT_TEXTURE_SIZE),
                new Vector2(0.5f, 0.5f), SOFT_TEXTURE_SIZE, 0, SpriteMeshType.FullRect);
            softSprite.name = "PackingSoftDisc";
        }

        /// <summary>Creates a solid rectangle as a child of <paramref name="parent"/>.</summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="center">Local position of the rectangle's centre.</param>
        /// <param name="size">Local size.</param>
        /// <param name="color">Tint.</param>
        /// <param name="sortingOrder">Sprite sorting order.</param>
        public SpriteRenderer CreateQuad(Transform parent, string name, Vector2 center, Vector2 size, Color color, int sortingOrder) =>
            CreateSprite(parent, name, sprite, center, size, color, sortingOrder);

        /// <summary>Creates a solid disc of <paramref name="diameter"/> as a child of <paramref name="parent"/>.</summary>
        public SpriteRenderer CreateCircle(Transform parent, string name, Vector2 center, float diameter, Color color, int sortingOrder) =>
            CreateSprite(parent, name, circleSprite, center, new Vector2(diameter, diameter), color, sortingOrder);

        /// <summary>Creates a blurred oval of <paramref name="size"/> (soft drop shadow) as a child of <paramref name="parent"/>.</summary>
        public SpriteRenderer CreateSoftOval(Transform parent, string name, Vector2 center, Vector2 size, Color color, int sortingOrder) =>
            CreateSprite(parent, name, softSprite, center, size, color, sortingOrder);

        /// <summary>
        /// Creates a line from <paramref name="from"/> to <paramref name="to"/> (local, axis-aligned) as
        /// <paramref name="dashes"/> evenly spaced dashes, each <paramref name="dashFill"/> of its period; dashes ≤ 0 = one solid quad.
        /// </summary>
        public void CreateDashedLine(Transform parent, string name, Vector2 from, Vector2 to, float width, int dashes, float dashFill,
            Color color, int sortingOrder, List<SpriteRenderer> created = null)
        {
            Vector2 delta = to - from;
            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            float length = delta.magnitude;
            int count = Mathf.Max(1, dashes);
            float fill = dashes <= 0 ? 1f : Mathf.Clamp01(dashFill);
            float period = length / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 center = from + delta * ((i + 0.5f) / count);
                float dash = period * fill;
                var size = horizontal ? new Vector2(dash, width) : new Vector2(width, dash);
                var r = CreateQuad(parent, name, center, size, color, sortingOrder);
                created?.Add(r);
            }
        }

        /// <summary>
        /// Creates a renderer for <paramref name="source"/> scaled so its larger side is <paramref name="size"/>.x
        /// (or exactly <paramref name="size"/> for the generated square and circle).
        /// </summary>
        public SpriteRenderer CreateSprite(Transform parent, string name, Sprite source, Vector2 center, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var renderer = go.AddComponent<SpriteRenderer>();
            var material = SpriteMaterial;
            if (material != null) renderer.sharedMaterial = material;
            renderer.sprite = source;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (source == sprite || source == circleSprite || source == softSprite)
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            else
            {
                FitSprite(renderer, size.x);
            }
            return renderer;
        }

        /// <summary>
        /// Creates a 9-sliced renderer of exactly <paramref name="size"/> local units (corners keep their drawn size).
        /// Falls back to a solid rectangle when <paramref name="source"/> is null.
        /// </summary>
        public SpriteRenderer CreateSliced(Transform parent, string name, Sprite source, Vector2 center, Vector2 size, Color color, int sortingOrder)
        {
            if (source == null)
            {
                return CreateQuad(parent, name, center, size, color, sortingOrder);
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var renderer = go.AddComponent<SpriteRenderer>();
            var material = SpriteMaterial;
            if (material != null) renderer.sharedMaterial = material;
            renderer.sprite = source;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>Illustrated sprite Resources/Art/[name]. Cached; returns null (callers fall back to flat shapes) if missing.</summary>
        public Sprite GetArt(string name)
        {
            if (!art.TryGetValue(name, out var found))
            {
                found = name.StartsWith("ui_", System.StringComparison.Ordinal)
                    ? Resources.Load<Sprite>(ART_FOLDER + "Sourced/UI/" + name) : null;
                found = found != null ? found : Resources.Load<Sprite>(ART_FOLDER + name);
                art[name] = found;
            }
            return found;
        }

        /// <summary>Forgets a cached illustration so its texture can be unloaded once nothing shows it.</summary>
        public void ForgetArt(string name) => art.Remove(name);

        /// <summary>Releases the illustrated objects of every level except <paramref name="levelNumber"/>.</summary>
        public void TrimPieceArt(int levelNumber) => pieceArtCatalog?.KeepOnly(levelNumber);

        /// <summary>Resolves the presentation-only whole-object binding for a level's piece.</summary>
        public PieceArt GetPieceArt(int levelNumber, string letter)
        {
            pieceArtCatalog ??= new PieceArtCatalog(this);
            return pieceArtCatalog.Get(levelNumber, letter);
        }

        /// <summary>Scales a renderer uniformly so the larger side of its sprite is <paramref name="size"/> local units.</summary>
        public static void FitSprite(SpriteRenderer renderer, float size)
        {
            if (renderer.sprite == null)
            {
                return;
            }
            var bounds = renderer.sprite.bounds.size;
            float largest = Mathf.Max(bounds.x, bounds.y);
            float scale = largest > 0f ? size / largest : 1f;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Icon sprite of an item: Resources/Icons/[itemId]. Cached; logs a warning once and returns null if missing.
        /// </summary>
        public Sprite GetIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }
            if (!icons.TryGetValue(itemId, out var icon))
            {
                icon = Resources.Load<Sprite>(ICON_FOLDER + itemId);
                if (icon == null)
                {
                    Debug.LogWarning($"SpriteFactory: no sprite at Resources/{ICON_FOLDER}{itemId}; the piece shows no icon.");
                }
                icons[itemId] = icon;
            }
            return icon;
        }

        /// <summary>Destroys the generated sprites and textures (icons are assets and are left alone).</summary>
        public void Dispose()
        {
            Object.Destroy(sprite);
            Object.Destroy(texture);
            Object.Destroy(circleSprite);
            Object.Destroy(circleTexture);
            Object.Destroy(softSprite);
            Object.Destroy(softTexture);
            icons.Clear();
            art.Clear();
        }
    }
}
