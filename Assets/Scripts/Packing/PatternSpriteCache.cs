using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Level-owned cache of two small sprites per shape/pattern, never one object per pattern mark.</summary>
    public sealed class PatternSpriteCache : IDisposable
    {
        /// <summary>White/darkened surface for tinting and a separate exterior border for stable opacity/sorting.</summary>
        public sealed class Surface
        {
            /// <summary>Grayscale patterned surface, tinted by the item colour.</summary>
            public Sprite Body { get; internal set; }
            /// <summary>White exterior border, tinted by the outline colour.</summary>
            public Sprite Border { get; internal set; }
        }

        readonly Dictionary<string, Surface> _surfaces = new Dictionary<string, Surface>();
        readonly FeelTuning _tuning;

        /// <summary>Creates a cache using serialized presentation tuning.</summary>
        public PatternSpriteCache(FeelTuning tuning) => _tuning = tuning;

        /// <summary>Gets a shared pair of sprites for a normalized shape and a data pattern name.</summary>
        public Surface Get(PieceShape shape, string pattern, float inset)
        {
            var key = new StringBuilder().Append(pattern).Append(':').Append(shape.Width).Append('x').Append(shape.Height);
            for (int y = 0; y < shape.Height; y++)
                for (int x = 0; x < shape.Width; x++)
                    key.Append(shape.Contains(new Vector2Int(x, y)) ? '1' : '0');
            string id = key.ToString();
            if (_surfaces.TryGetValue(id, out var found)) return found;

            int ppc = Mathf.Clamp(_tuning.pixelsPerCell, 16, 48);
            int width = shape.Width * ppc, height = shape.Height * ppc;
            var body = new Color32[width * height];
            var border = new Color32[body.Length];
            float edge = Mathf.Clamp(inset, 0f, 0.2f);
            float stroke = Mathf.Clamp(_tuning.outlineWidth, 0.02f, 0.12f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sx = (x + 0.5f) / ppc;
                    float sy = shape.Height - (y + 0.5f) / ppc;
                    int index = y * width + x;
                    if (!InsideInset(shape, sx, sy, edge)) continue;
                    if (!InsideInset(shape, sx, sy, edge + stroke))
                        border[index] = new Color32(255, 255, 255, 255);
                    else
                    {
                        float shade = Mark(pattern, sx, sy) ? 1f - Mathf.Clamp01(_tuning.patternContrast) : 1f;
                        byte value = (byte)(shade * 255f);
                        body[index] = new Color32(value, value, value, 255);
                    }
                }
            }
            var surface = new Surface { Body = MakeSprite(body, width, height, ppc, "Pattern " + pattern),
                Border = MakeSprite(border, width, height, ppc, "Exterior outline") };
            _surfaces.Add(id, surface);
            return surface;
        }

        static bool Inside(PieceShape shape, float x, float y) =>
            shape.Contains(new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y)));

        static bool InsideInset(PieceShape shape, float x, float y, float inset) => Inside(shape, x, y) &&
            Inside(shape, x - inset, y) && Inside(shape, x + inset, y) &&
            Inside(shape, x, y - inset) && Inside(shape, x, y + inset) &&
            Inside(shape, x - inset, y - inset) && Inside(shape, x + inset, y - inset) &&
            Inside(shape, x - inset, y + inset) && Inside(shape, x + inset, y + inset);

        bool Mark(string pattern, float x, float y)
        {
            float repeats = Mathf.Clamp(_tuning.patternRepeats, 1, 6);
            float u = Mathf.Repeat(x * repeats, 1f), v = Mathf.Repeat(y * repeats, 1f);
            switch (pattern)
            {
                case "stripe": return v < 0.25f;
                case "dots": return (new Vector2(u, v) - Vector2.one * 0.5f).sqrMagnitude < 0.035f;
                case "check": case "plaid": return u < 0.18f || v < 0.18f;
                case "knit": return Mathf.Abs(v - Mathf.Abs(u - 0.5f) * 1.5f) < 0.12f;
                case "diag": return Mathf.Repeat((x + y) * repeats, 1f) < 0.24f;
                default: return false;
            }
        }

        static Sprite MakeSprite(Color32[] pixels, int width, int height, int ppc, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f,
                ppc, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        /// <summary>Releases level-owned textures and sprites. Safe to call more than once.</summary>
        public void Dispose()
        {
            foreach (var surface in _surfaces.Values)
            {
                UnityEngine.Object.Destroy(surface.Body.texture);
                UnityEngine.Object.Destroy(surface.Border.texture);
                UnityEngine.Object.Destroy(surface.Body);
                UnityEngine.Object.Destroy(surface.Border);
            }
            _surfaces.Clear();
        }
    }
}
