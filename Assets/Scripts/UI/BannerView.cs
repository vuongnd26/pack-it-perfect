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
        const float CARD_WIDTH = 900f;
        const float CARD_HEIGHT = 230f;
        const float CARD_GAP = 24f;
        const float TITLE_HEIGHT_SHARE = 0.55f;
        const float PADDING = 24f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly RectTransform card;
        readonly CanvasGroup group;
        readonly TextMeshProUGUI title;
        readonly TextMeshProUGUI subtitle;
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
                new Vector2(CARD_WIDTH, CARD_HEIGHT));
            group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            title = factory.Text("Title", card, string.Empty, tuning.headingFontSize * 0.75f, tuning.accent, FontStyles.Bold);
            subtitle = factory.Text("Subtitle", card, string.Empty, tuning.bodyFontSize, Color.white);
            card.gameObject.SetActive(false);
        }

        /// <summary>Shows the banner (cleaned for TMP); an empty subtitle centres the title alone.</summary>
        public void Show(string titleText, string subtitleText)
        {
            bool hasSubtitle = !string.IsNullOrEmpty(subtitleText);
            title.text = TmpText.Clean(titleText);
            subtitle.text = TmpText.Clean(subtitleText);
            subtitle.gameObject.SetActive(hasSubtitle);
            float split = CARD_HEIGHT * (1f - TITLE_HEIGHT_SHARE);
            UIFactory.Stretch(title.rectTransform, PADDING, hasSubtitle ? split : 0f, PADDING, 0f);
            UIFactory.Stretch(subtitle.rectTransform, PADDING, PADDING, PADDING, CARD_HEIGHT * TITLE_HEIGHT_SHARE);

            card.gameObject.SetActive(true);
            group.alpha = 1f;
            UIMotion.Pop(animator, popKey, card, 0f, tuning.bannerPopIn, tuning.popStartScale, tuning.reduceMotion);
            animator.Play(fadeKey, tuning.bannerPopIn + tuning.bannerHold, tuning.bannerFadeOut,
                t => group.alpha = 1f - t, () => card.gameObject.SetActive(false));
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
