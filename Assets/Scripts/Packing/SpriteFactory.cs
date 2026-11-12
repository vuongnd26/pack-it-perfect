// Runtime-generated placeholder visuals for story 001 (final art is story 009).
// Story: production/epics/pack-it-perfect/story-001-core-packing-loop.md

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>
    /// Owns one white sprite generated at runtime and creates tinted, scaled quads from it.
    /// One instance per PackingGame; call <see cref="Dispose"/> when done.
    /// </summary>
    public sealed class SpriteFactory
    {
        /// <summary>Sorting order of the frame around the grid.</summary>
        public const int ORDER_FRAME = 0;
        /// <summary>Sorting order of the grid lining.</summary>
        public const int ORDER_LINING = 1;
        /// <summary>Sorting order of the grid lines.</summary>
        public const int ORDER_GRID_LINE = 2;
        /// <summary>Sorting order of blocked cells.</summary>
        public const int ORDER_BLOCKED = 3;
        /// <summary>Sorting order of the fill bar background (the fill is one above).</summary>
        public const int ORDER_FILL_BAR = 4;
        /// <summary>Sorting order of resting pieces (tray and grid).</summary>
        public const int ORDER_PIECE = 10;
        /// <summary>Sorting order of the ghost preview.</summary>
        public const int ORDER_GHOST = 20;
        /// <summary>Sorting order of the dragged piece.</summary>
        public const int ORDER_DRAGGED = 30;
        /// <summary>Sorting order of the lid (zipper parts are one to three above).</summary>
        public const int ORDER_LID = 40;

        const int TEXTURE_SIZE = 4;

        readonly Texture2D texture;
        readonly Sprite sprite;

        /// <summary>Creates the white texture and the 1×1-unit sprite.</summary>
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
        }

        /// <summary>Creates a solid rectangle as a child of <paramref name="parent"/>.</summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="center">Local position of the rectangle's centre.</param>
        /// <param name="size">Local size.</param>
        /// <param name="color">Tint.</param>
        /// <param name="sortingOrder">Sprite sorting order.</param>
        public SpriteRenderer CreateQuad(Transform parent, string name, Vector2 center, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>Destroys the generated sprite and texture.</summary>
        public void Dispose()
        {
            Object.Destroy(sprite);
            Object.Destroy(texture);
        }
    }
}
