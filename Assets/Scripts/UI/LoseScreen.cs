// Lose screen of the timed levels: "Out of time! The taxi is leaving and the trunk won't close.", "Try again",
// "Install to keep packing" (no link: raises an event), plus a small Levels button.
// Implements: design/game-brief.md "Điều kiện thắng / thua" (Thua — chế độ Hẹn giờ), "Quyết định đã chốt" (Install).
// Story: production/epics/pack-it-perfect/story-006-timer-mode.md

using System;
using TMPro;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Dimmed overlay with the lose card. Blocks input to everything behind it.</summary>
    public sealed class LoseScreen
    {
        const float CARD_WIDTH = 900f;
        const float CARD_HEIGHT = 920f;
        const float BUTTON_WIDTH = 780f;
        const float TITLE_TOP = 60f;
        const float TITLE_HEIGHT = 120f;
        const float BODY_TOP = 190f;
        const float BODY_HEIGHT = 170f;
        const float TRY_TOP = 400f;
        const float TRY_HEIGHT = 160f;
        const float INSTALL_TOP = 590f;
        const float INSTALL_HEIGHT = 140f;
        const float LEVELS_TOP = 760f;
        const float LEVELS_WIDTH = 360f;
        const float LEVELS_HEIGHT = 120f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly GameObject root;
        readonly RectTransform card;
        readonly object popKey = new object();

        /// <summary>"Try again" clicked.</summary>
        public event Action TryAgainClicked;
        /// <summary>"Install to keep packing" clicked. Nothing else happens in this build; attach a store link here later.</summary>
        public event Action InstallClicked;
        /// <summary>"Levels" clicked.</summary>
        public event Action LevelsClicked;

        /// <summary>Builds the (hidden) screen under <paramref name="parent"/>.</summary>
        public LoseScreen(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator)
        {
            this.tuning = tuning;
            this.animator = animator;
            var dim = factory.Panel("Lose Screen", parent, tuning.overlay, 0f, raycast: true);
            UIFactory.Stretch(dim.rectTransform);
            root = dim.gameObject;
            var safe = factory.SafeContent(dim.transform);

            var cardImage = factory.Panel("Card", safe, tuning.panel, tuning.cardRadius);
            factory.StyleCard(cardImage);
            card = cardImage.rectTransform;
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            var top = new Vector2(0.5f, 1f);

            var clock = factory.Art("Timeout Clock", card, "ui_clock");
            if (clock != null)
                UIFactory.Place(clock.rectTransform, top, new Vector2(0f, 130f), new Vector2(180f, 180f));
            var title = factory.Text("Title", card, UIStrings.LOSE_TITLE, tuning.headingFontSize, tuning.danger, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, top, new Vector2(0f, -TITLE_TOP), new Vector2(BUTTON_WIDTH, TITLE_HEIGHT));

            var body = factory.Text("Body", card, UIStrings.LOSE_BODY, tuning.bodyFontSize, tuning.ink);
            UIFactory.Place(body.rectTransform, top, new Vector2(0f, -BODY_TOP), new Vector2(BUTTON_WIDTH, BODY_HEIGHT));

            TryAgainButton = factory.Button("Lose Try Again", card, UIStrings.TRY_AGAIN, ButtonStyle.Primary,
                tuning.buttonFontSize + 8f, () => TryAgainClicked?.Invoke());
            UIFactory.Place(TryAgainButton.Root, top, new Vector2(0f, -TRY_TOP), new Vector2(BUTTON_WIDTH, TRY_HEIGHT));

            var install = factory.Button("Lose Install", card, UIStrings.INSTALL, ButtonStyle.Accent, tuning.buttonFontSize - 4f,
                () => InstallClicked?.Invoke());
            UIFactory.Place(install.Root, top, new Vector2(0f, -INSTALL_TOP), new Vector2(BUTTON_WIDTH, INSTALL_HEIGHT));

            var levels = factory.Button("Lose Levels", card, UIStrings.LEVELS, ButtonStyle.Secondary, tuning.smallFontSize + 4f,
                () => LevelsClicked?.Invoke());
            UIFactory.Place(levels.Root, top, new Vector2(0f, -LEVELS_TOP), new Vector2(LEVELS_WIDTH, LEVELS_HEIGHT));

            root.SetActive(false);
        }

        /// <summary>The "Try again" button (first focus for gamepads).</summary>
        public UIButton TryAgainButton { get; }

        /// <summary>Shows the screen with a card pop, or hides it.</summary>
        public void SetVisible(bool visible)
        {
            if (visible == root.activeSelf)
            {
                return;
            }
            root.SetActive(visible);
            if (visible)
            {
                UIMotion.Pop(animator, popKey, card, 0f, tuning.cardPopDuration, tuning.popStartScale, tuning.reduceMotion);
            }
            else
            {
                animator.Cancel(popKey);
                card.localScale = Vector3.one;
            }
        }
    }
}
