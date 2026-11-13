// A rounded pill (two discs + a quad) behind a centred TMP label, used by the toast and floating words.
// Story: production/epics/pack-it-perfect/story-003-obstacle-rules.md

using TMPro;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Rounded pill with centred text; its width follows the text. Local units; scale the root to resize.</summary>
    public sealed class PillLabel
    {
        readonly SpriteRenderer body;
        readonly SpriteRenderer leftCap;
        readonly SpriteRenderer rightCap;
        readonly TextMeshPro text;
        readonly Color backColor;
        readonly Color textColor;
        readonly float height;
        readonly float paddingX;

        /// <summary>Full width of the pill in local units (text + padding), as of the last <see cref="SetText"/>.</summary>
        public float Width { get; private set; }

        /// <summary>Root transform; position = pill centre.</summary>
        public Transform Root { get; }

        /// <summary>Builds an empty pill under <paramref name="parent"/>.</summary>
        /// <param name="factory">Quad and disc factory.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="name">GameObject name.</param>
        /// <param name="textSize">Text height.</param>
        /// <param name="height">Pill height (the end caps' diameter).</param>
        /// <param name="paddingX">Space between the text and each end.</param>
        /// <param name="backColor">Pill colour.</param>
        /// <param name="textColor">Text colour.</param>
        /// <param name="sortingOrder">Pill order; text is one above.</param>
        public PillLabel(SpriteFactory factory, Transform parent, string name, float textSize, float height, float paddingX,
            Color backColor, Color textColor, int sortingOrder)
        {
            this.backColor = backColor;
            this.textColor = textColor;
            this.height = height;
            this.paddingX = paddingX;
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            body = factory.CreateQuad(Root, "PillBody", Vector2.zero, new Vector2(0f, height), backColor, sortingOrder);
            leftCap = factory.CreateCircle(Root, "PillLeft", Vector2.zero, height, backColor, sortingOrder);
            rightCap = factory.CreateCircle(Root, "PillRight", Vector2.zero, height, backColor, sortingOrder);
            text = TmpText.Create(Root, "PillText", string.Empty, textSize, textColor, sortingOrder + 1);
        }

        /// <summary>Sets the text (verbatim; cleaned for TMP) and resizes the pill around it.</summary>
        public void SetText(string message)
        {
            float textWidth = TmpText.SetText(text, message);
            float straight = textWidth + 2f * paddingX - height;
            straight = Mathf.Max(0f, straight);
            Width = straight + height;
            body.transform.localScale = new Vector3(straight, height, 1f);
            leftCap.transform.localPosition = new Vector3(-straight / 2f, 0f, 0f);
            rightCap.transform.localPosition = new Vector3(straight / 2f, 0f, 0f);
        }

        /// <summary>Multiplies the pill's and text's opacity by <paramref name="alpha"/> (0..1).</summary>
        public void SetAlpha(float alpha)
        {
            var back = backColor;
            back.a *= alpha;
            body.color = back;
            leftCap.color = back;
            rightCap.color = back;
            text.alpha = textColor.a * alpha;
        }

        /// <summary>Shows or hides the pill.</summary>
        public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

        /// <summary>Destroys the pill.</summary>
        public void Destroy() => Object.Destroy(Root.gameObject);
    }
}
