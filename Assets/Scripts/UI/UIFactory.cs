// Runtime UGUI builders: rounded 9-sliced sprites, panels, TextMeshPro labels and glossy press-in buttons.
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

        /// <summary>Coloured face (the darker base lip sits behind it).</summary>
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
        const string ART_FOLDER = "Art/";
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
        Texture2D glossTexture;
        Sprite gloss;

        /// <summary>Raised with the button's id on every click, before its action. Audio hook for UI click sounds (story 009).</summary>
        public event Action<string> ButtonClicked;

        /// <summary>Creates the factory and generates the rounded sprite.</summary>
        public UIFactory(UITuning tuning)
        {
            this.tuning = tuning;
            CreateRoundedSprite();
            CreateGlossSprite();
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

        /// <summary>
        /// Gives <paramref name="target"/> its own nested canvas (with a raycaster so its buttons still work). Graphics
        /// below it rebuild and batch on their own, so an animation in one part of the UI never re-meshes the rest.
        /// </summary>
        public static void Isolate(GameObject target)
        {
            if (target.GetComponent<Canvas>() != null) return;
            target.AddComponent<Canvas>();
            target.AddComponent<GraphicRaycaster>();
        }

        /// <summary>Returns the object's <see cref="CanvasGroup"/>, adding one if needed (used to fade whole screens).</summary>
        public static CanvasGroup Group(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
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

        /// <summary>Gives <paramref name="rect"/> a fixed height inside a layout group (and optionally a fixed width).</summary>
        public static LayoutElement FixedSize(RectTransform rect, float height, float width = -1f)
        {
            var element = rect.gameObject.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            if (width >= 0f)
            {
                element.minWidth = width;
                element.preferredWidth = width;
            }
            return element;
        }

        /// <summary>
        /// Adds a vertical layout group to <paramref name="rect"/> that stacks children top to bottom, stretches them to the
        /// width and grows the rect to fit (use <see cref="FixedSize"/> on children).
        /// </summary>
        public static VerticalLayoutGroup StackVertically(RectTransform rect, int padding, float spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return layout;
        }

        /// <summary>
        /// Wraps existing content in a clipped, vertically scrollable safe-area viewport. The content keeps its layout
        /// and preferred height; keyboard/gamepad selection is revealed automatically when focus moves off screen.
        /// </summary>
        public static RectTransform ScrollWithinSafeArea(RectTransform content, RectTransform safe, float maximumWidth,
            float maximumHeightShare, float margin, bool bottom = false, float bottomMargin = 0f)
        {
            var viewport = CreateRect(content.name + " Viewport", safe);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 48f;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            scroll.viewport = viewport;
            scroll.content = content;
            viewport.gameObject.AddComponent<ResponsiveUILayout>().ConfigureScroll(safe, content, scroll,
                maximumWidth, maximumHeightShare, margin, bottom, bottomMargin);
            return viewport;
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

        /// <summary>Illustrated sprite Resources/Art/[name], or null when missing.</summary>
        public static Sprite LoadArt(string name) =>
            Resources.Load<Sprite>(ART_FOLDER + "Sourced/UI/" + name) ?? Resources.Load<Sprite>(ART_FOLDER + name);

        /// <summary>
        /// Adds a full-screen illustrated background behind the other children of <paramref name="parent"/>.
        /// It always covers the parent, cropping the art's sides or ends on other aspect ratios. No-op if the art is missing.
        /// </summary>
        public void Backdrop(Transform parent, string artName)
        {
            var sprite = LoadArt(artName);
            if (sprite == null)
            {
                return;
            }
            var rect = CreateRect("Backdrop", parent);
            rect.SetAsFirstSibling();
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        /// <summary>Creates an image showing illustrated art at its own aspect ratio, or returns null if the art is missing.</summary>
        public Image Art(string name, Transform parent, string artName)
        {
            var sprite = LoadArt(artName);
            if (sprite == null)
            {
                return null;
            }
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Applies the illustrated nine-slice card border, keeping all existing layout and input behaviour.</summary>
        public void StyleCard(Image image)
        {
            var sprite = LoadArt("ui_card");
            if (sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        /// <summary>Adds an illustrated button icon; the separate text label remains available to navigation/accessibility.</summary>
        public void ButtonIcon(UIButton button, string artName, bool iconOnly = false)
        {
            var image = Art("Illustrated Icon", button.Face.transform, artName);
            if (image == null) return;
            if (iconOnly)
            {
                Stretch(image.rectTransform, 24f, 24f, 24f, 24f);
                button.Label.gameObject.SetActive(false);
                return;
            }
            Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(66f, 74f));
            Stretch(button.Label.rectTransform, 92f, 0f, BUTTON_LABEL_PADDING, 0f);
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
            Slice(image, rounded, radius);
        }

        static void Slice(Image image, Sprite sprite, float radius)
        {
            image.sprite = sprite;
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
            // Labels can be created under a hidden info row. Their Awake has not run when layout measures them.
            label.font = TMP_Settings.defaultFontAsset;
            if (label.font != null) label.fontSharedMaterial = label.font.material;
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
        /// Creates a chunky rounded button: a darker base lip under a coloured face with a light top edge and a dark
        /// bottom edge, a bold auto-sized label, and a press-in (scale down + sink) animation.
        /// Size it through <see cref="UIButton.Root"/>.
        /// </summary>
        /// <param name="id">GameObject name and the id passed to <see cref="ButtonClicked"/>.</param>
        /// <param name="radius">Corner radius in reference pixels; negative uses <see cref="UITuning.cornerRadius"/>.</param>
        public UIButton Button(string id, Transform parent, string label, ButtonStyle style, float fontSize, Action onClick,
            float radius = -1f)
        {
            float corner = radius >= 0f ? radius : tuning.cornerRadius;
            // Root: invisible hit area that keeps its layout rect while the visual scales about its centre.
            var root = CreateRect(id, parent);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            var visual = CreateRect("Visual", root);
            Stretch(visual);

            var shadow = Panel("Base", visual, ShadowOf(style), corner);
            Stretch(shadow.rectTransform);

            var face = Panel("Face", visual, FaceOf(style), corner);
            Stretch(face.rectTransform, 0f, tuning.buttonShadowDepth, 0f, 0f);

            var sheen = CreateRect("Gloss", face.transform).gameObject.AddComponent<Image>();
            sheen.raycastTarget = false;
            Slice(sheen, gloss, corner);
            Stretch(sheen.rectTransform);

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
            root.gameObject.AddComponent<ButtonPressScale>().Init(button, visual, face.rectTransform, tuning);
            return new UIButton(root, button, face, text);
        }

        /// <summary>
        /// A round glossy button of <paramref name="diameter"/> reference pixels. Uses the illustrated ui_slot base when it
        /// exists, else the generated rounded shape. The caller places <see cref="UIButton.Root"/> and sizes it to the diameter.
        /// </summary>
        public UIButton RoundButton(string id, Transform parent, string label, ButtonStyle style, float fontSize,
            float diameter, Action onClick)
        {
            float radius = Mathf.Max(1f, (diameter - tuning.buttonShadowDepth) / 2f);
            var button = Button(id, parent, label, style, fontSize, onClick, radius);
            var art = LoadArt("ui_slot");
            if (art != null)
            {
                button.Face.sprite = art;
                button.Face.type = Image.Type.Simple;
                button.Face.preserveAspect = true;
                button.Face.color = Color.white;
                var baseLip = button.Root.Find("Visual/Base");
                if (baseLip != null) baseLip.gameObject.SetActive(false);
                var sheen = button.Face.transform.Find("Gloss");
                if (sheen != null) sheen.gameObject.SetActive(false);
            }
            return button;
        }

        /// <summary>
        /// Creates an icon: the art when it exists, else a text label of <paramref name="fallbackText"/>. The result never
        /// catches clicks. Size it through its rect.
        /// </summary>
        public Graphic Icon(string name, Transform parent, string artName, string fallbackText, float fallbackFontSize,
            Color fallbackColor)
        {
            var image = Art(name, parent, artName);
            if (image != null)
            {
                return image;
            }
            var label = Text(name, parent, fallbackText, fallbackFontSize, fallbackColor, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMax = fallbackFontSize;
            label.fontSizeMin = fallbackFontSize * AUTO_SIZE_MIN_RATIO;
            return label;
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
            if (gloss != null)
            {
                UnityEngine.Object.Destroy(gloss);
                gloss = null;
            }
            if (glossTexture != null)
            {
                UnityEngine.Object.Destroy(glossTexture);
                glossTexture = null;
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

        // Same rounded shape as an overlay: white fading down from the top edge, black fading up from the bottom edge.
        // Both bands live in the 9-slice borders, so they keep their thickness on any button size.
        void CreateGlossSprite()
        {
            glossTexture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                name = "UI Button Gloss",
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
                    float mask = Mathf.Clamp01(CORNER_PIXELS - distance + 0.5f);
                    float top = Mathf.Clamp01((py - (TEXTURE_SIZE - CORNER_PIXELS)) / CORNER_PIXELS);
                    float bottom = Mathf.Clamp01((CORNER_PIXELS - py) / CORNER_PIXELS);
                    bool light = top > 0f;
                    float band = light ? top : bottom;
                    float alpha = mask * band * band * (light ? tuning.buttonGlossAlpha : tuning.buttonShadeAlpha);
                    byte value = light ? (byte)255 : (byte)0;
                    pixels[y * TEXTURE_SIZE + x] = new Color32(value, value, value, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            glossTexture.SetPixels32(pixels);
            glossTexture.Apply(false, true);
            var border = new Vector4(CORNER_PIXELS, CORNER_PIXELS, CORNER_PIXELS, CORNER_PIXELS);
            gloss = Sprite.Create(glossTexture, new Rect(0f, 0f, TEXTURE_SIZE, TEXTURE_SIZE), new Vector2(0.5f, 0.5f),
                SPRITE_PIXELS_PER_UNIT, 0, SpriteMeshType.FullRect, border);
            gloss.name = "UI Button Gloss";
            gloss.hideFlags = HideFlags.DontSave;
        }
    }
}
