// Runtime UGUI builders: rounded 9-sliced sprite, panels, TextMeshPro labels and 3D-style buttons.
// No prefabs or assets: everything here is created in code and destroyed in Dispose.
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Colour scheme of a button.</summary>
    public enum ButtonStyle
    {
        /// <summary>Green, white text: the main action.</summary>
        Primary,
        /// <summary>Red, white text.</summary>
        Danger,
        /// <summary>Yellow, ink text.</summary>
        Accent,
        /// <summary>White, ink text.</summary>
        Secondary,
    }

    /// <summary>Handles of a button built by <see cref="UIFactory.Button"/>.</summary>
    public sealed class UIButton
    {
        internal UIButton(RectTransform root, Button button, Image face, TextMeshProUGUI label)
        {
            Root = root;
            Button = button;
            Face = face;
            Label = label;
        }

        /// <summary>Root rect (place/size this one).</summary>
        public RectTransform Root { get; }

        /// <summary>The UGUI button (on the root).</summary>
        public Button Button { get; }

        /// <summary>Coloured face (the shadow is the root's own image).</summary>
        public Image Face { get; }

        /// <summary>Label, stretched over the face.</summary>
        public TextMeshProUGUI Label { get; }

        /// <summary>Whether the button can be clicked (disabled buttons are tinted grey).</summary>
        public bool Interactable
        {
            get => Button.interactable;
            set => Button.interactable = value;
        }

        /// <summary>Sets the label text (cleaned for TMP).</summary>
        public void SetLabel(string text) => Label.text = TmpText.Clean(text);
    }

    /// <summary>Creates the UI elements and owns the generated rounded-rect sprite.</summary>
    public sealed class UIFactory : IDisposable
    {
        const int TEXTURE_SIZE = 128;
        const float CORNER_PIXELS = 48f;
        const float SPRITE_PIXELS_PER_UNIT = 100f;
        const float BUTTON_LABEL_PADDING = 16f;
        const float AUTO_SIZE_MIN_RATIO = 0.6f;

        static readonly Color PressedTint = new Color(0.85f, 0.85f, 0.85f, 1f);
        static readonly Color SelectedTint = new Color(0.95f, 0.95f, 0.95f, 1f);
        static readonly Color DisabledTint = new Color(0.75f, 0.75f, 0.75f, 0.6f);

        readonly UITuning tuning;
        readonly List<RectTransform> safeRects = new List<RectTransform>();
        Texture2D texture;
        Sprite rounded;

        /// <summary>Raised with the button's id on every click, before its action. Audio hook for UI click sounds (story 009).</summary>
        public event Action<string> ButtonClicked;

        /// <summary>Creates the factory and generates the rounded sprite.</summary>
        public UIFactory(UITuning tuning)
        {
            this.tuning = tuning;
            CreateRoundedSprite();
        }

        /// <summary>Rects created by <see cref="SafeContent"/>; the owner fits them to <c>Screen.safeArea</c>.</summary>
        public IReadOnlyList<RectTransform> SafeRects => safeRects;

        /// <summary>Creates an empty child rect.</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Stretches <paramref name="rect"/> over its parent with the given insets.</summary>
        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors and pivots <paramref name="rect"/> at <paramref name="anchor"/> (0..1) with a fixed size.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Full-width strip hanging <paramref name="top"/> below the parent's top edge.</summary>
        public static void TopStrip(RectTransform rect, float top, float height, float sideMargin)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-2f * sideMargin, height);
        }

        /// <summary>Creates a full-stretch rect that the owner keeps inside the device safe area.</summary>
        public RectTransform SafeContent(Transform parent)
        {
            var rect = CreateRect("Safe Area", parent);
            Stretch(rect);
            safeRects.Add(rect);
            return rect;
        }

        /// <summary>Creates an image; radius &gt; 0 gives it rounded corners.</summary>
        /// <param name="raycast">True to catch clicks (full-screen blockers); false lets them through.</param>
        public Image Panel(string name, Transform parent, Color color, float radius, bool raycast = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            SetRadius(image, radius);
            return image;
        }

        /// <summary>Gives <paramref name="image"/> rounded corners of <paramref name="radius"/> reference pixels (0 = square).</summary>
        public void SetRadius(Image image, float radius)
        {
            if (radius <= 0f)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                return;
            }
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = CORNER_PIXELS / radius;
        }

        /// <summary>Creates a TextMeshPro UI label (not a raycast target). <paramref name="text"/> is cleaned for TMP.</summary>
        public TextMeshProUGUI Text(string name, Transform parent, string text, float fontSize, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.color = color;
            label.fontStyle = style;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = TmpText.Clean(text);
            return label;
        }

        /// <summary>
        /// Creates a rounded button: a darker shadow band under a coloured face, a bold auto-sized label, and a press tint.
        /// Size it through <see cref="UIButton.Root"/>.
        /// </summary>
        /// <param name="id">GameObject name and the id passed to <see cref="ButtonClicked"/>.</param>
        public UIButton Button(string id, Transform parent, string label, ButtonStyle style, float fontSize, Action onClick)
        {
            var root = CreateRect(id, parent);
            var shadow = root.gameObject.AddComponent<Image>();
            shadow.color = ShadowOf(style);
            shadow.raycastTarget = true;
            SetRadius(shadow, tuning.cornerRadius);

            var face = Panel("Face", root, FaceOf(style), tuning.cornerRadius);
            Stretch(face.rectTransform, 0f, tuning.buttonShadowDepth, 0f, 0f);

            var text = Text("Label", face.transform, label, fontSize, TextOf(style), FontStyles.Bold);
            Stretch(text.rectTransform, BUTTON_LABEL_PADDING, 0f, BUTTON_LABEL_PADDING, 0f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * AUTO_SIZE_MIN_RATIO;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = PressedTint;
            colors.selectedColor = SelectedTint;
            colors.disabledColor = DisabledTint;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                ButtonClicked?.Invoke(id);
                onClick?.Invoke();
            });
            return new UIButton(root, button, face, text);
        }

        /// <summary>Destroys the generated sprite and texture.</summary>
        public void Dispose()
        {
            if (rounded != null)
            {
                UnityEngine.Object.Destroy(rounded);
                rounded = null;
            }
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
        }

        Color FaceOf(ButtonStyle style) => style switch
        {
            ButtonStyle.Primary => tuning.primary,
            ButtonStyle.Danger => tuning.danger,
            ButtonStyle.Accent => tuning.accent,
            _ => tuning.panel,
        };

        Color ShadowOf(ButtonStyle style) => style switch
        {
            ButtonStyle.Primary => tuning.primaryShadow,
            ButtonStyle.Danger => tuning.dangerShadow,
            ButtonStyle.Accent => tuning.accentShadow,
            _ => tuning.secondaryShadow,
        };

        Color TextOf(ButtonStyle style) =>
            style == ButtonStyle.Primary || style == ButtonStyle.Danger ? Color.white : tuning.ink;

        // White rounded square with anti-aliased corners; the corners are the 9-slice border.
        void CreateRoundedSprite()
        {
            texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                name = "UI Rounded Rect",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[TEXTURE_SIZE * TEXTURE_SIZE];
            for (int y = 0; y < TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < TEXTURE_SIZE; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, CORNER_PIXELS, TEXTURE_SIZE - CORNER_PIXELS);
                    float cy = Mathf.Clamp(py, CORNER_PIXELS, TEXTURE_SIZE - CORNER_PIXELS);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(CORNER_PIXELS - distance + 0.5f);
                    pixels[y * TEXTURE_SIZE + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = new Vector4(CORNER_PIXELS, CORNER_PIXELS, CORNER_PIXELS, CORNER_PIXELS);
            rounded = Sprite.Create(texture, new Rect(0f, 0f, TEXTURE_SIZE, TEXTURE_SIZE), new Vector2(0.5f, 0.5f),
                SPRITE_PIXELS_PER_UNIT, 0, SpriteMeshType.FullRect, border);
            rounded.name = "UI Rounded Rect";
            rounded.hideFlags = HideFlags.DontSave;
        }
    }
}
