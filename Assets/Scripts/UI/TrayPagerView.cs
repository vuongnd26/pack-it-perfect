// Previous / next row for the two-row tray: shown above the booster dock only when a level's items span several pages.
// Presentation only: it raises events; PackingGame owns the page.
// Implements: design/game-brief.md "Readability B" (khay hai hàng phân trang).

using System;
using TMPro;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Two buttons and a "Tray 1/3 · 9 left" label.</summary>
    public sealed class TrayPagerView
    {
        readonly UITuning tuning;
        readonly TextMeshProUGUI label;
        int shownPage = -1;
        int shownCount = -1;
        int shownLeft = -1;

        /// <summary>Previous page requested.</summary>
        public event Action PreviousClicked;
        /// <summary>Next page requested.</summary>
        public event Action NextClicked;

        /// <summary>Builds the (hidden) row under <paramref name="parent"/>; the owner positions <see cref="Root"/>.</summary>
        public TrayPagerView(UIFactory factory, Transform parent, UITuning tuning)
        {
            this.tuning = tuning;
            Root = UIFactory.CreateRect("Tray Pager", parent);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0f);
            Root.pivot = new Vector2(0.5f, 0f);
            Root.sizeDelta = new Vector2(tuning.pagerWidth, tuning.pagerHeight);

            var back = factory.Panel("Back", Root, tuning.panel, tuning.pagerHeight / 2f);
            UIFactory.Stretch(back.rectTransform, tuning.pagerButtonWidth * 0.5f, tuning.buttonShadowDepth * 0.5f,
                tuning.pagerButtonWidth * 0.5f, 0f);
            label = factory.Text("Pages", Root, string.Empty, tuning.pagerFontSize, tuning.ink, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(label.rectTransform, tuning.pagerButtonWidth, tuning.buttonShadowDepth * 0.5f,
                tuning.pagerButtonWidth, 0f);

            Previous = factory.Button("Tray Previous", Root, UIStrings.TRAY_PREVIOUS, ButtonStyle.Accent,
                tuning.buttonFontSize, () => PreviousClicked?.Invoke());
            UIFactory.Place(Previous.Root, new Vector2(0f, 0f), Vector2.zero,
                new Vector2(tuning.pagerButtonWidth, tuning.pagerHeight));
            Next = factory.Button("Tray Next", Root, UIStrings.TRAY_NEXT, ButtonStyle.Accent,
                tuning.buttonFontSize, () => NextClicked?.Invoke());
            UIFactory.Place(Next.Root, new Vector2(1f, 0f), Vector2.zero,
                new Vector2(tuning.pagerButtonWidth, tuning.pagerHeight));
            Root.gameObject.SetActive(false);
        }

        /// <summary>Root rect of the row.</summary>
        public RectTransform Root { get; }

        /// <summary>Previous-page button.</summary>
        public UIButton Previous { get; }

        /// <summary>Next-page button.</summary>
        public UIButton Next { get; }

        /// <summary>True while the row is shown (more than one page).</summary>
        public bool IsVisible => Root.gameObject.activeSelf;

        /// <summary>Shows page <paramref name="page"/> (zero-based) of <paramref name="pageCount"/>; hidden for a single page.</summary>
        /// <param name="left">Items still in the tray, over all pages.</param>
        /// <param name="interactable">False while a piece is held or a booster is running.</param>
        public void Set(int page, int pageCount, int left, bool interactable)
        {
            bool visible = pageCount > 1;
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
            if (!visible) return;
            Previous.Interactable = interactable && page > 0;
            Next.Interactable = interactable && page < pageCount - 1;
            if (page == shownPage && pageCount == shownCount && left == shownLeft) return;
            shownPage = page;
            shownCount = pageCount;
            shownLeft = left;
            label.text = TmpText.Clean(string.Format(UIStrings.TRAY_PAGE_FORMAT, page + 1, pageCount, left));
        }

        /// <summary>Keeps the row inside a narrow safe area.</summary>
        public void Fit(float availableWidth, float bottom)
        {
            float width = Mathf.Max(1f, Mathf.Min(tuning.pagerWidth, availableWidth));
            if (!Mathf.Approximately(Root.sizeDelta.x, width)) Root.sizeDelta = new Vector2(width, tuning.pagerHeight);
            if (!Mathf.Approximately(Root.anchoredPosition.y, bottom)) Root.anchoredPosition = new Vector2(0f, bottom);
        }
    }
}
