// Level-load banner: "Hard level!" / "Pack before the taxi leaves" on timed levels, else the level's bannerTitle/Subtitle.
// Pops in, holds, fades out; never blocks input.
// Implements: design/game-brief.md "Cơ chế chính" (Hẹn giờ banner, Khuôn gợi ý banner), "Quyết định đã chốt" (Banner luật mới).
// Story: production/epics/pack-it-perfect/story-006-timer-mode.md

using TMPro;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Transient, non-blocking banner card under the HUD.</summary>
    public sealed class BannerView
    {
        const float CARD_GAP = 20f;
        const float TITLE_HEIGHT_SHARE = 0.55f;
        const float PADDING = 24f;
        const float TITLE_SIZE_SHARE = 0.62f;
        const float TAG_WIDTH = 140f;
        const float TAG_HEIGHT = 46f;
        const float TAG_INSET = 28f;
        const float TAG_FONT_SIZE = 30f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly RectTransform card;
        readonly CanvasGroup group;
        readonly TextMeshProUGUI title;
        readonly TextMeshProUGUI subtitle;
        readonly GameObject tag;
        readonly object popKey = new object();
        readonly object fadeKey = new object();

        /// <summary>Builds the (hidden) banner under <paramref name="parent"/>, just below the HUD band.</summary>
        public BannerView(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator)
        {
            this.tuning = tuning;
            this.animator = animator;
            var safe = factory.SafeContent(parent);
            var image = factory.Panel("Banner", safe, tuning.bannerBackground, tuning.cardRadius);
            card = image.rectTransform;
            UIFactory.Place(card, new Vector2(0.5f, 1f), new Vector2(0f, -(tuning.hudHeight + CARD_GAP)),
                new Vector2(tuning.bannerWidth, tuning.bannerHeight));
            group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            title = factory.Text("Title", card, string.Empty, tuning.headingFontSize * TITLE_SIZE_SHARE, tuning.accent, FontStyles.Bold);
            subtitle = factory.Text("Subtitle", card, string.Empty, tuning.bodyFontSize - 8f, Color.white);
            var tagImage = factory.Panel("New Tag", card, tuning.accent, TAG_HEIGHT / 2f);
            UIFactory.Place(tagImage.rectTransform, new Vector2(0f, 1f), new Vector2(TAG_INSET, TAG_HEIGHT / 2f),
                new Vector2(TAG_WIDTH, TAG_HEIGHT));
            tagImage.rectTransform.pivot = new Vector2(0f, 0.5f);
            var tagText = factory.Text("Text", tagImage.transform, UIStrings.NEW_TAG, TAG_FONT_SIZE, tuning.ink, FontStyles.Bold);
            tagText.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(tagText.rectTransform);
            tag = tagImage.gameObject;
            card.gameObject.SetActive(false);
        }

        /// <summary>Shows the banner (cleaned for TMP); an empty subtitle centres the title alone.</summary>
        /// <param name="isNew">True to add the "New!" tag (a level that introduces a rule).</param>
        public void Show(string titleText, string subtitleText, bool isNew = false)
        {
            bool hasSubtitle = !string.IsNullOrEmpty(subtitleText);
            title.text = TmpText.Clean(titleText);
            subtitle.text = TmpText.Clean(subtitleText);
            subtitle.gameObject.SetActive(hasSubtitle);
            float height = hasSubtitle ? tuning.bannerHeight : tuning.bannerHeightSingle;
            card.sizeDelta = new Vector2(tuning.bannerWidth, height);
            tag.SetActive(isNew);
            float split = height * (1f - TITLE_HEIGHT_SHARE);
            UIFactory.Stretch(title.rectTransform, PADDING, hasSubtitle ? split : 0f, PADDING, 0f);
            UIFactory.Stretch(subtitle.rectTransform, PADDING, PADDING, PADDING, height * TITLE_HEIGHT_SHARE);

            card.gameObject.SetActive(true);
            group.alpha = 1f;
            UIMotion.Pop(animator, popKey, card, 0f, tuning.bannerPopIn, tuning.popStartScale, tuning.reduceMotion);
            animator.Play(fadeKey, tuning.bannerPopIn + tuning.bannerHold, tuning.bannerFadeOut,
                t => group.alpha = 1f - t, () => card.gameObject.SetActive(false));
        }

        /// <summary>Positions transient copy below the actual wrapped HUD and inside safe-area width.</summary>
        public void FitBelowHUD(float topBandHeight)
        {
            var safe = (RectTransform)card.parent;
            card.anchoredPosition = new Vector2(0f, -(topBandHeight + CARD_GAP));
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(1f, Mathf.Min(tuning.bannerWidth, safe.rect.width - tuning.sideMargin * 2f)));
        }

        /// <summary>Hides the banner at once.</summary>
        public void Hide()
        {
            animator.Cancel(popKey);
            animator.Cancel(fadeKey);
            card.gameObject.SetActive(false);
        }
    }
}
