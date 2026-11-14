// Transient level notices share the HUD info strip, so they never cover playable cells.
using System;
using TMPro;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Non-blocking one-line notice inside the HUD; longer explanations remain in Rules.</summary>
    public sealed class BannerView
    {
        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly RectTransform card;
        readonly CanvasGroup group;
        readonly TextMeshProUGUI title;
        readonly Action<bool> visibilityChanged;
        readonly object fadeKey = new object();
        readonly object copyKey = new object();

        public BannerView(UIFactory factory, RectTransform host, UITuning tuning, UIAnimator animator,
            Action<bool> visibilityChanged)
        {
            this.tuning = tuning;
            this.animator = animator;
            this.visibilityChanged = visibilityChanged;
            var image = factory.Panel("Banner", host, tuning.bannerBackground, tuning.chipHeight / 2f);
            image.raycastTarget = false;
            card = image.rectTransform;
            UIFactory.Stretch(card);
            group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            title = factory.Text("Title", card, string.Empty, tuning.hudNameFontSize, tuning.accent, FontStyles.Bold);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Truncate;
            UIFactory.Stretch(title.rectTransform, 12f, 0f, 12f, 0f);
            card.gameObject.SetActive(false);
        }

        public void Show(string titleText, string subtitleText, bool isNew = false)
        {
            animator.Cancel(fadeKey);
            animator.Cancel(copyKey);
            title.text = TmpText.Clean(titleText);
            title.color = tuning.accent;
            card.gameObject.SetActive(true);
            visibilityChanged?.Invoke(true);
            group.alpha = 1f;
            // Each line gets its own time at full size, instead of squeezing two lines into the strip.
            if (!string.IsNullOrEmpty(subtitleText))
                animator.Play(copyKey, tuning.bannerPopIn + tuning.bannerHold * 0.5f, 0f, _ => { }, () =>
                {
                    title.text = TmpText.Clean(subtitleText);
                    title.color = Color.white;
                });
            animator.Play(fadeKey, tuning.bannerPopIn + tuning.bannerHold, tuning.bannerFadeOut,
                t => group.alpha = 1f - t, Hide);
        }

        public void Hide()
        {
            animator.Cancel(copyKey);
            animator.Cancel(fadeKey);
            card.gameObject.SetActive(false);
            visibilityChanged?.Invoke(false);
        }
    }
}
