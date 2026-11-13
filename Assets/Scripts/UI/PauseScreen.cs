// Pause overlay: dimmer plus a card with Resume (primary), Restart, Home and the Sound / Reduce-motion toggles.
// Implements: design/game-brief.md "Nâng cấp Cozy Travel" (Restart and Home live in the pause overlay).

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Dimmed overlay with the pause card. Blocks input to everything behind it.</summary>
    public sealed class PauseScreen
    {
        const int CARD_PADDING = 48;
        const float CARD_SPACING = 20f;
        const float TITLE_HEIGHT = 100f;
        const float ROW_SPACING = 20f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly GameObject root;
        readonly RectTransform card;
        readonly UIButton sound;
        readonly UIButton motion;
        readonly UIButton uiSize;
        /// <summary>Requests the next readable interface size.</summary>
        public event Action UISizeClicked;
        readonly object popKey = new object();

        /// <summary>Resume clicked.</summary>
        public event Action ResumeClicked;
        /// <summary>Restart clicked.</summary>
        public event Action RestartClicked;
        /// <summary>Home clicked.</summary>
        public event Action HomeClicked;
        /// <summary>Sound toggle clicked.</summary>
        public event Action SoundClicked;
        /// <summary>Reduce-motion toggle clicked.</summary>
        public event Action MotionClicked;

        /// <summary>Builds the (hidden) screen under <paramref name="parent"/>.</summary>
        public PauseScreen(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator)
        {
            this.tuning = tuning;
            this.animator = animator;
            var dim = factory.Panel("Pause Screen", parent, tuning.overlay, 0f, raycast: true);
            UIFactory.Stretch(dim.rectTransform);
            root = dim.gameObject;
            var safe = factory.SafeContent(dim.transform);

            var cardImage = factory.Panel("Card", safe, tuning.panel, tuning.cardRadius);
            factory.StyleCard(cardImage);
            card = cardImage.rectTransform;
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(tuning.pauseCardWidth, 0f));
            card.pivot = new Vector2(0.5f, 0.5f);
            UIFactory.StackVertically(card, CARD_PADDING, CARD_SPACING);

            var title = factory.Text("Title", card, UIStrings.PAUSED_TITLE, tuning.headingFontSize, tuning.ink, FontStyles.Bold);
            UIFactory.FixedSize(title.rectTransform, TITLE_HEIGHT);

            ResumeButton = factory.Button("Pause Resume", card, UIStrings.RESUME, ButtonStyle.Primary, tuning.buttonFontSize + 8f,
                () => ResumeClicked?.Invoke());
            UIFactory.FixedSize(ResumeButton.Root, tuning.pauseButtonHeight);

            var restart = factory.Button("Pause Restart", card, UIStrings.RESTART, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => RestartClicked?.Invoke());
            UIFactory.FixedSize(restart.Root, tuning.pauseButtonHeight);
            factory.ButtonIcon(restart, "ui_restart");

            var home = factory.Button("Pause Home", card, UIStrings.HOME, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => HomeClicked?.Invoke());
            UIFactory.FixedSize(home.Root, tuning.pauseButtonHeight);

            var row = UIFactory.CreateRect("Preferences", card);
            UIFactory.FixedSize(row, tuning.pauseToggleHeight * 3f + ROW_SPACING * 2f);
            var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = ROW_SPACING;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            sound = factory.Button("Pause Sound", row, UIStrings.SOUND_ON, ButtonStyle.Secondary, tuning.smallFontSize,
                () => SoundClicked?.Invoke());
            motion = factory.Button("Pause Motion", row, UIStrings.MOTION_ON, ButtonStyle.Secondary, tuning.smallFontSize,
                () => MotionClicked?.Invoke());
            uiSize = factory.Button("Pause UI Size", row, string.Format(UIStrings.UI_SIZE_FORMAT, 100),
                ButtonStyle.Secondary, tuning.smallFontSize, () => UISizeClicked?.Invoke());
            UIFactory.ScrollWithinSafeArea(card, safe, tuning.pauseCardWidth, 1f, tuning.sideMargin);
            root.SetActive(false);
        }

        /// <summary>The Resume button (first focus for gamepads).</summary>
        public UIButton ResumeButton { get; }

        /// <summary>True while the overlay is shown.</summary>
        public bool IsVisible => root.activeSelf;

        /// <summary>Updates the toggle labels.</summary>
        public void Refresh(bool muted, bool reduceMotion, int uiScalePercent = 100)
        {
            uiSize.SetLabel(string.Format(UIStrings.UI_SIZE_FORMAT, uiScalePercent));
            sound.SetLabel(muted ? UIStrings.SOUND_OFF : UIStrings.SOUND_ON);
            motion.SetLabel(reduceMotion ? UIStrings.MOTION_REDUCED : UIStrings.MOTION_ON);
        }

        /// <summary>Shows the overlay with a card pop.</summary>
        public void Show()
        {
            if (root.activeSelf)
            {
                return;
            }
            root.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            UIMotion.Pop(animator, popKey, card, 0f, tuning.cardPopDuration, tuning.popStartScale, tuning.reduceMotion);
        }

        /// <summary>Hides the overlay.</summary>
        public void Hide()
        {
            animator.Cancel(popKey);
            card.localScale = Vector3.one;
            root.SetActive(false);
        }
    }
}
