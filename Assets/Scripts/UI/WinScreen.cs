// Win screen: star slots (filled = stars won), hint note, Next level / Replay / Levels; after the last level an end card
// with Home / Levels.
// Implements: design/game-brief.md "Điều kiện thắng / thua" (3 sao / 2 sao), "Progression & Meta".
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md ("After a win, the player can go to the next
// level or back to level select")

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Dimmed overlay with the win card. Blocks input to everything behind it.</summary>
    public sealed class WinScreen
    {
        const float CARD_WIDTH = 900f;
        const float CARD_HEIGHT = 1000f;
        const float CONTENT_WIDTH = 780f;
        const float TITLE_TOP = 60f;
        const float TITLE_HEIGHT = 120f;
        const float STARS_TOP = 200f;
        const float STAR_SIZE = 170f;
        const float STAR_SPACING = 210f;
        const float STAR_FONT_SIZE = 130f;
        const float NOTE_TOP = 390f;
        const float NOTE_HEIGHT = 160f;
        const float NEXT_TOP = 570f;
        const float NEXT_HEIGHT = 160f;
        const float ROW_TOP = 760f;
        const float ROW_TOP_END_CARD = 600f;
        const float ROW_BUTTON_WIDTH = 375f;
        const float ROW_BUTTON_HEIGHT = 140f;
        const float ROW_OFFSET = 202f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly GameObject root;
        readonly RectTransform card;
        readonly TextMeshProUGUI title;
        readonly TextMeshProUGUI note;
        readonly Graphic[] stars;
        readonly UIButton next;
        readonly UIButton replay;
        readonly UIButton home;
        readonly UIButton levels;
        readonly object cardKey = new object();
        readonly object[] starKeys;

        /// <summary>"Next level" clicked.</summary>
        public event Action NextClicked;
        /// <summary>"Replay" clicked.</summary>
        public event Action ReplayClicked;
        /// <summary>"Levels" clicked.</summary>
        public event Action LevelsClicked;
        /// <summary>"Home" clicked (end card only).</summary>
        public event Action HomeClicked;

        /// <summary>Builds the (hidden) screen with <paramref name="starSlots"/> star slots under <paramref name="parent"/>.</summary>
        public WinScreen(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator, int starSlots)
        {
            this.tuning = tuning;
            this.animator = animator;
            var dim = factory.Panel("Win Screen", parent, tuning.overlay, 0f, raycast: true);
            UIFactory.Stretch(dim.rectTransform);
            root = dim.gameObject;
            var safe = factory.SafeContent(dim.transform);

            var cardImage = factory.Panel("Card", safe, tuning.panel, tuning.cardRadius);
            factory.StyleCard(cardImage);
            card = cardImage.rectTransform;
            UIFactory.Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT));
            var top = new Vector2(0.5f, 1f);

            var ribbon = factory.Art("Ribbon", card, "ui_ribbon");
            if (ribbon != null)
                UIFactory.Place(ribbon.rectTransform, top, new Vector2(0f, -24f), new Vector2(CARD_WIDTH + 80f, 196f));
            title = factory.Text("Title", card, UIStrings.WIN_TITLE, tuning.headingFontSize, tuning.ink, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, top, new Vector2(0f, -TITLE_TOP), new Vector2(CONTENT_WIDTH, TITLE_HEIGHT));

            stars = new Graphic[starSlots];
            starKeys = new object[starSlots];
            float firstX = -(starSlots - 1) * STAR_SPACING / 2f;
            for (int i = 0; i < starSlots; i++)
            {
                Graphic star = factory.Art($"Star {i + 1}", card, "ui_star");
                if (star == null)
                {
                    var fallback = factory.Text($"Star {i + 1}", card, UIStrings.STAR, STAR_FONT_SIZE, Color.white);
                    fallback.textWrappingMode = TextWrappingModes.NoWrap;
                    star = fallback;
                }
                UIFactory.Place(star.rectTransform, top, new Vector2(firstX + i * STAR_SPACING, -STARS_TOP),
                    Vector2.one * STAR_SIZE);
                star.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                star.rectTransform.anchoredPosition = new Vector2(firstX + i * STAR_SPACING, -STARS_TOP - STAR_SIZE / 2f);
                stars[i] = star;
                starKeys[i] = new object();
            }

            note = factory.Text("Note", card, string.Empty, tuning.bodyFontSize, tuning.muted);
            UIFactory.Place(note.rectTransform, top, new Vector2(0f, -NOTE_TOP), new Vector2(CONTENT_WIDTH, NOTE_HEIGHT));

            next = factory.Button("Win Next", card, UIStrings.NEXT_LEVEL, ButtonStyle.Primary, tuning.buttonFontSize + 8f,
                () => NextClicked?.Invoke());
            UIFactory.Place(next.Root, top, new Vector2(0f, -NEXT_TOP), new Vector2(CONTENT_WIDTH, NEXT_HEIGHT));

            replay = factory.Button("Win Replay", card, UIStrings.REPLAY, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => ReplayClicked?.Invoke());
            home = factory.Button("Win Home", card, UIStrings.HOME, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => HomeClicked?.Invoke());
            levels = factory.Button("Win Levels", card, UIStrings.LEVELS, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => LevelsClicked?.Invoke());
            root.SetActive(false);
        }

        /// <summary>The button to focus first for gamepads (Next level, or Levels on the end card).</summary>
        public UIButton PrimaryButton => next.Root.gameObject.activeSelf ? next : levels;

        /// <summary>Hides the screen.</summary>
        public void Hide()
        {
            animator.Cancel(cardKey);
            foreach (var key in starKeys)
            {
                animator.Cancel(key);
            }
            card.localScale = Vector3.one;
            root.SetActive(false);
        }

        /// <summary>
        /// Shows the result: <paramref name="starCount"/> filled slots popping in one after another, a hint note when
        /// fewer than all stars were won, and the end card when <paramref name="isLastLevel"/>.
        /// </summary>
        public void Show(int starCount, bool isLastLevel)
        {
            root.SetActive(true);
            bool capped = starCount < stars.Length;
            string hintNote = capped ? string.Format(UIStrings.WIN_HINT_NOTE_FORMAT, starCount) : string.Empty;

            title.text = TmpText.Clean(isLastLevel ? UIStrings.END_TITLE : UIStrings.WIN_TITLE);
            note.text = TmpText.Clean(isLastLevel
                ? (capped ? UIStrings.END_BODY + "\n" + hintNote : UIStrings.END_BODY)
                : hintNote);

            next.Root.gameObject.SetActive(!isLastLevel);
            replay.Root.gameObject.SetActive(!isLastLevel);
            home.Root.gameObject.SetActive(isLastLevel);
            float rowTop = isLastLevel ? ROW_TOP_END_CARD : ROW_TOP;
            var rowSize = new Vector2(ROW_BUTTON_WIDTH, ROW_BUTTON_HEIGHT);
            var top = new Vector2(0.5f, 1f);
            UIFactory.Place(isLastLevel ? home.Root : replay.Root, top, new Vector2(-ROW_OFFSET, -rowTop), rowSize);
            UIFactory.Place(levels.Root, top, new Vector2(ROW_OFFSET, -rowTop), rowSize);

            UIMotion.Pop(animator, cardKey, card, 0f, tuning.cardPopDuration, tuning.popStartScale, tuning.reduceMotion);
            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                bool filled = i < starCount;
                star.color = new Color(1f, 1f, 1f, filled ? 1f : tuning.emptyStarAlpha);
                if (filled)
                {
                    float delay = tuning.cardPopDuration + i * tuning.starPopStagger;
                    UIMotion.Pop(animator, starKeys[i], star.rectTransform, delay, tuning.starPopDuration, 0f, tuning.reduceMotion);
                }
                else
                {
                    animator.Cancel(starKeys[i]);
                    star.rectTransform.localScale = Vector3.one;
                }
            }
        }
    }
}
