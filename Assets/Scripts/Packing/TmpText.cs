// World-space TextMesh Pro helpers. The TMP default font (Fredoka SDF) and default sprite asset (colour emoji,
// mapped by Unicode) are set in TMP Settings, so plain strings with emoji render as-is.
// Stories: production/epics/pack-it-perfect/story-003-obstacle-rules.md (badges, zone labels, toasts).

using TMPro;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Creates world-space TextMeshPro labels and cleans strings for TMP.</summary>
    public static class TmpText
    {
        /// <summary>
        /// For a world-space TextMeshPro, fontSize 10 is one local unit per em
        /// (TMP scales non-orthographic text by 0.1).
        /// </summary>
        public const float FONT_SIZE_PER_UNIT = 10f;

        /// <summary>Width of the layout rectangle; text never wraps, so this only centres it.</summary>
        const float RECT_WIDTH = 40f;

        const char VARIATION_SELECTOR_16 = '️';

        /// <summary>
        /// Returns <paramref name="text"/> without U+FE0F (emoji variation selector), which TMP draws as a gap.
        /// Keep the verbatim string (with U+FE0F) in code and data; clean it only when handing it to TMP.
        /// </summary>
        public static string Clean(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty : text.Replace(VARIATION_SELECTOR_16.ToString(), string.Empty);

        /// <summary>Creates a centred, non-wrapping world-space label.</summary>
        /// <param name="parent">Parent transform.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="text">Text, verbatim (cleaned here).</param>
        /// <param name="emSize">Text height in local units.</param>
        /// <param name="color">Text colour (emoji keep their own colours and take only the alpha).</param>
        /// <param name="sortingOrder">Renderer sorting order (applied to emoji sub-meshes too).</param>
        public static TextMeshPro Create(Transform parent, string name, string text, float emSize, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(RECT_WIDTH, emSize * 2f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontSize = emSize * FONT_SIZE_PER_UNIT;
            tmp.color = color;
            tmp.text = Clean(text);
            tmp.sortingOrder = sortingOrder;
            return tmp;
        }

        /// <summary>Sets the label's text (cleaned) and returns its rendered width in local units.</summary>
        public static float SetText(TextMeshPro tmp, string text)
        {
            string clean = Clean(text);
            tmp.text = clean;
            return tmp.GetPreferredValues(clean).x;
        }
    }
}
