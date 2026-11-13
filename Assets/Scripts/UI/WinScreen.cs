// Win screen: compact celebratory card at the bottom (so the packed container stays visible): ribbon title, stars,
// level name, booster rewards, "New booster!" blocks, next-reward teaser, assist note, Next / Replay / Levels.
// After the last level an end card with Home / Levels.
// Implements: design/game-brief.md "Điều kiện thắng / thua" (3 sao / 2 sao), "Progression & Meta", "Nâng cấp Cozy Travel".
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md ("After a win, the player can go to the next
// level or back to level select")

using System;
using PackItPerfect.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Dimmed overlay with the win card. Blocks input to everything behind it.</summary>
    public sealed class WinScreen
    {
        const int CARD_PADDING = 40;
        const float CARD_SPACING = 14f;
        const float TITLE_HEIGHT = 120f;
        const float NAME_HEIGHT = 50f;
        const float NOTE_HEIGHT = 40f;
        const float END_BODY_HEIGHT = 110f;
        const float TEASER_HEIGHT = 44f;
        const float NEXT_HEIGHT = 130f;
        const float ROW_HEIGHT = 110f;
        const float ROW_SPACING = 24f;
        const float STAR_SPACING = 16f;
        const float ITEM_SPACING = 16f;
        const float ITEM_ICON_WIDTH = 80f;
        const float ITEM_ICON_HEIGHT = 64f;
        const float ITEM_PADDING = 16f;
        const float BLOCK_ICON_SIZE = 100f;
        const float BLOCK_TEXT_LEFT = 150f;
        const float BLOCK_TEXT_RIGHT = 16f;
        const float TAG_FONT_SIZE = 30f;
        const float DESC_FONT_SIZE = 30f;
        const float MIN_FONT_SHARE = 0.6f;
        const float POP_GAP = 0.15f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly GameObject root;
        readonly RectTransform card;
        readonly TextMeshProUGUI title;
        readonly TextMeshProUGUI nameLabel;
        readonly TextMeshProUGUI note;
        readonly TextMeshProUGUI endBody;
        readonly TextMeshProUGUI teaser;
        readonly Graphic[] stars;
        readonly RectTransform rewardRow;
        readonly RectTransform[] rewardItems = new RectTransform[BoosterBundle.TYPE_COUNT];
        readonly TextMeshProUGUI[] rewardCounts = new TextMeshProUGUI[BoosterBundle.TYPE_COUNT];
        readonly RectTransform[] blocks = new RectTransform[BoosterBundle.TYPE_COUNT];
        readonly TextMeshProUGUI[] blockNames = new TextMeshProUGUI[BoosterBundle.TYPE_COUNT];
        readonly TextMeshProUGUI[] blockDescs = new TextMeshProUGUI[BoosterBundle.TYPE_COUNT];
        readonly GameObject[][] blockIcons = new GameObject[BoosterBundle.TYPE_COUNT][];
        readonly UIButton next;
        readonly UIButton replay;
        readonly UIButton home;
        readonly UIButton levels;
        readonly object cardKey = new object();
        readonly object[] starKeys;
        readonly object[] rewardKeys = { new object(), new object(), new object() };
        readonly object[] blockKeys = { new object(), new object(), new object() };

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
            var dim = factory.Panel("Win Screen", parent, tuning.winDim, 0f, raycast: true);
            UIFactory.Stretch(dim.rectTransform);
            root = dim.gameObject;
            var safe = factory.SafeContent(dim.transform);

            var cardImage = factory.Panel("Card", safe, tuning.panel, tuning.cardRadius);
            factory.StyleCard(cardImage);
            card = cardImage.rectTransform;
            UIFactory.Place(card, new Vector2(0.5f, 0f), new Vector2(0f, tuning.winCardBottomMargin),
                new Vector2(tuning.winCardWidth, 0f));
            UIFactory.StackVertically(card, CARD_PADDING, CARD_SPACING);

            var titleBlock = UIFactory.CreateRect("Title Block", card);
            UIFactory.FixedSize(titleBlock, TITLE_HEIGHT);
            var ribbon = factory.Art("Ribbon", titleBlock, "ui_ribbon");
            if (ribbon != null)
            {
                UIFactory.Stretch(ribbon.rectTransform);
            }
            title = factory.Text("Title", titleBlock, UIStrings.WIN_TITLE, tuning.headingFontSize, tuning.ink, FontStyles.Bold);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(title.rectTransform);

            var starRow = UIFactory.CreateRect("Stars", card);
            UIFactory.FixedSize(starRow, tuning.winStarSize);
            var starLayout = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starLayout.spacing = STAR_SPACING;
            starLayout.childAlignment = TextAnchor.MiddleCenter;
            starLayout.childControlWidth = false;
            starLayout.childControlHeight = false;
            starLayout.childForceExpandWidth = false;
            starLayout.childForceExpandHeight = false;
            stars = new Graphic[starSlots];
            starKeys = new object[starSlots];
            for (int i = 0; i < starSlots; i++)
            {
                Graphic star = factory.Art($"Star {i + 1}", starRow, "ui_star");
                if (star == null)
                {
                    var fallback = factory.Text($"Star {i + 1}", starRow, UIStrings.STAR, tuning.winStarSize * 0.75f, Color.white);
                    fallback.textWrappingMode = TextWrappingModes.NoWrap;
                    star = fallback;
                }
                star.rectTransform.sizeDelta = Vector2.one * tuning.winStarSize;
                stars[i] = star;
                starKeys[i] = new object();
            }

            nameLabel = factory.Text("Level Name", card, string.Empty, tuning.bodyFontSize, tuning.muted, FontStyles.Bold);
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMax = tuning.bodyFontSize;
            nameLabel.fontSizeMin = tuning.smallFontSize * MIN_FONT_SHARE;
            UIFactory.FixedSize(nameLabel.rectTransform, NAME_HEIGHT);

            endBody = factory.Text("End Body", card, UIStrings.END_BODY, tuning.bodyFontSize, tuning.ink);
            UIFactory.FixedSize(endBody.rectTransform, END_BODY_HEIGHT);

            rewardRow = UIFactory.CreateRect("Rewards", card);
            UIFactory.FixedSize(rewardRow, tuning.winRewardItemHeight);
            rewardRow.gameObject.AddComponent<ResponsiveUILayout>().ConfigureFlow(ITEM_SPACING, tuning.winRewardItemHeight, 0f, false);
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                BuildRewardItem(factory, (BoosterType)i);
            }

            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                BuildBlock(factory, (BoosterType)i);
            }

            teaser = factory.Text("Teaser", card, string.Empty, tuning.smallFontSize, tuning.muted);
            teaser.textWrappingMode = TextWrappingModes.NoWrap;
            teaser.enableAutoSizing = true;
            teaser.fontSizeMax = tuning.smallFontSize;
            teaser.fontSizeMin = tuning.smallFontSize * MIN_FONT_SHARE;
            UIFactory.FixedSize(teaser.rectTransform, TEASER_HEIGHT);

            note = factory.Text("Note", card, UIStrings.WIN_ASSIST_NOTE, tuning.smallFontSize - 4f, tuning.muted);
            note.textWrappingMode = TextWrappingModes.NoWrap;
            note.enableAutoSizing = true;
            note.fontSizeMax = tuning.smallFontSize - 4f;
            note.fontSizeMin = tuning.smallFontSize * MIN_FONT_SHARE;
            UIFactory.FixedSize(note.rectTransform, NOTE_HEIGHT);

            // Actions remain outside the reward scroller: first-win unlock copy must never hide Continue.
            var actionPanel = factory.Panel("Win Actions", safe, tuning.panel, tuning.cardRadius);
            factory.StyleCard(actionPanel);
            var actions = actionPanel.rectTransform;
            UIFactory.StackVertically(actions, CARD_PADDING, CARD_SPACING);
            next = factory.Button("Win Next", actions, UIStrings.NEXT_LEVEL, ButtonStyle.Primary, tuning.buttonFontSize + 8f,
                () => NextClicked?.Invoke());
            UIFactory.FixedSize(next.Root, NEXT_HEIGHT);

            var row = UIFactory.CreateRect("Row", actions);
            UIFactory.FixedSize(row, ROW_HEIGHT);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = ROW_SPACING;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            replay = factory.Button("Win Replay", row, UIStrings.REPLAY, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => ReplayClicked?.Invoke());
            home = factory.Button("Win Home", row, UIStrings.HOME, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => HomeClicked?.Invoke());
            levels = factory.Button("Win Levels", row, UIStrings.LEVELS, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => LevelsClicked?.Invoke());
            float actionsHeight = CARD_PADDING * 2f + NEXT_HEIGHT + ROW_HEIGHT + CARD_SPACING;
            UIFactory.Place(actions, new Vector2(0.5f, 0f), new Vector2(0f, tuning.winCardBottomMargin),
                new Vector2(tuning.winCardWidth, actionsHeight));
            // Width follows the same safe-area margins as the scroll card.
            actions.anchorMin = new Vector2(0f, 0f);
            actions.anchorMax = new Vector2(1f, 0f);
            actions.offsetMin = new Vector2(tuning.sideMargin, tuning.winCardBottomMargin);
            actions.offsetMax = new Vector2(-tuning.sideMargin, tuning.winCardBottomMargin + actionsHeight);
            UIFactory.ScrollWithinSafeArea(card, safe, tuning.winCardWidth, tuning.resultCardHeightShare,
                tuning.sideMargin, bottom: true,
                bottomMargin: tuning.winCardBottomMargin + actionsHeight + CARD_SPACING);
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
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                animator.Cancel(rewardKeys[i]);
                animator.Cancel(blockKeys[i]);
                rewardItems[i].localScale = Vector3.one;
                blocks[i].localScale = Vector3.one;
            }
            card.localScale = Vector3.one;
            root.SetActive(false);
        }

        /// <summary>
        /// Shows the result: <paramref name="starCount"/> filled slots popping in one after another, then the rewards.
        /// </summary>
        /// <param name="starCount">Stars won.</param>
        /// <param name="isLastLevel">True for the end card (Home / Levels instead of Next / Replay).</param>
        /// <param name="levelName">Name shown under the stars.</param>
        /// <param name="reward">What the win paid out (null shows nothing).</param>
        /// <param name="assistCapped">True when a booster capped the result below 3 stars.</param>
        /// <param name="nextRewardLevel">Level that pays the next milestone reward, 0 for none (shown only when nothing was rewarded).</param>
        /// <param name="freezeSeconds">Freeze length, for the Freeze description.</param>
        public void Show(int starCount, bool isLastLevel, string levelName, LevelReward reward, bool assistCapped,
            int nextRewardLevel, float freezeSeconds)
        {
            root.SetActive(true);
            title.text = TmpText.Clean(isLastLevel ? UIStrings.END_TITLE : UIStrings.WIN_TITLE);
            nameLabel.text = TmpText.Clean(levelName);
            endBody.gameObject.SetActive(isLastLevel);
            note.gameObject.SetActive(assistCapped && starCount < stars.Length);

            bool hasReward = reward != null && !reward.Boosters.IsEmpty;
            rewardRow.gameObject.SetActive(hasReward);
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                int amount = hasReward ? reward.Boosters.Of((BoosterType)i) : 0;
                rewardItems[i].gameObject.SetActive(amount > 0);
                rewardCounts[i].text = TmpText.Clean(string.Format(UIStrings.WIN_REWARD_FORMAT, amount));
                blocks[i].gameObject.SetActive(false);
            }
            int opened = reward?.Unlocked?.Length ?? 0;
            for (int i = 0; i < opened; i++)
            {
                var type = reward.Unlocked[i];
                int slot = (int)type;
                blockNames[slot].text = TmpText.Clean(UIStrings.BoosterName(type));
                blockDescs[slot].text = TmpText.Clean(UIStrings.BoosterDescription(type, freezeSeconds));
                blocks[slot].gameObject.SetActive(true);
                blocks[slot].SetAsLastSibling();
            }
            // Keep the blocks between the rewards and the teaser regardless of unlock order.
            teaser.transform.SetAsLastSibling();
            note.transform.SetAsLastSibling();
            next.Root.SetAsLastSibling();
            replay.Root.parent.SetAsLastSibling();

            bool showTeaser = !hasReward && nextRewardLevel > 0 && !isLastLevel;
            teaser.gameObject.SetActive(showTeaser);
            if (showTeaser)
            {
                teaser.text = TmpText.Clean(string.Format(UIStrings.WIN_NEXT_REWARD_FORMAT, nextRewardLevel));
            }

            next.Root.gameObject.SetActive(!isLastLevel);
            replay.Root.gameObject.SetActive(!isLastLevel);
            home.Root.gameObject.SetActive(isLastLevel);

            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            UIMotion.Pop(animator, cardKey, card, 0f, tuning.cardPopDuration, tuning.popStartScale, tuning.reduceMotion);
            float sequence = tuning.cardPopDuration;
            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                bool filled = i < starCount;
                star.color = new Color(1f, 1f, 1f, filled ? 1f : tuning.emptyStarAlpha);
                if (filled)
                {
                    float delay = tuning.cardPopDuration + i * tuning.starPopStagger;
                    UIMotion.Pop(animator, starKeys[i], star.rectTransform, delay, tuning.starPopDuration, 0f, tuning.reduceMotion);
                    sequence = delay + tuning.starPopDuration * 0.5f;
                }
                else
                {
                    animator.Cancel(starKeys[i]);
                    star.rectTransform.localScale = Vector3.one;
                }
            }

            sequence += POP_GAP;
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                if (!rewardItems[i].gameObject.activeSelf)
                {
                    continue;
                }
                UIMotion.Pop(animator, rewardKeys[i], rewardItems[i], sequence, tuning.starPopDuration, 0f, tuning.reduceMotion);
                sequence += tuning.rewardPopStagger;
            }
            for (int i = 0; i < opened; i++)
            {
                int slot = (int)reward.Unlocked[i];
                UIMotion.Pop(animator, blockKeys[slot], blocks[slot], sequence, tuning.starPopDuration, 0f, tuning.reduceMotion);
                sequence += tuning.rewardPopStagger;
            }
        }

        void BuildRewardItem(UIFactory factory, BoosterType type)
        {
            int slot = (int)type;
            var item = factory.Panel($"Reward {type}", rewardRow, tuning.winBlockColor, tuning.winRewardItemHeight / 2f);
            var rect = item.rectTransform;
            rect.sizeDelta = new Vector2(tuning.winRewardItemWidth, tuning.winRewardItemHeight);
            var icon = factory.Icon("Icon", rect, UIStrings.BoosterArt(type), UIStrings.BoosterName(type),
                tuning.smallFontSize - 8f, tuning.ink);
            UIFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(ITEM_PADDING, 0f),
                new Vector2(ITEM_ICON_WIDTH, ITEM_ICON_HEIGHT));
            var count = factory.Text("Count", rect, string.Empty, tuning.bodyFontSize + 6f, tuning.ink, FontStyles.Bold);
            count.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(count.rectTransform, ITEM_PADDING + ITEM_ICON_WIDTH, 0f, ITEM_PADDING, 0f);
            rewardItems[slot] = rect;
            rewardCounts[slot] = count;
        }

        void BuildBlock(UIFactory factory, BoosterType type)
        {
            int slot = (int)type;
            var panel = factory.Panel($"New Booster {type}", card, tuning.winBlockColor, tuning.cornerRadius);
            var rect = panel.rectTransform;
            UIFactory.FixedSize(rect, tuning.winBoosterBlockHeight);
            var icon = factory.Icon("Icon", rect, UIStrings.BoosterArt(type), UIStrings.BoosterName(type),
                tuning.smallFontSize - 4f, tuning.ink);
            UIFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(CARD_PADDING * 0.5f, 0f),
                Vector2.one * BLOCK_ICON_SIZE);
            blockIcons[slot] = new[] { icon.gameObject };

            var tag = factory.Text("Tag", rect, UIStrings.WIN_NEW_BOOSTER, TAG_FONT_SIZE, tuning.primaryShadow, FontStyles.Bold,
                TextAlignmentOptions.Left);
            var name = factory.Text("Name", rect, string.Empty, tuning.bodyFontSize, tuning.ink, FontStyles.Bold,
                TextAlignmentOptions.Left);
            var desc = factory.Text("Description", rect, string.Empty, DESC_FONT_SIZE, tuning.muted, FontStyles.Normal,
                TextAlignmentOptions.Left);
            foreach (var label in new[] { tag, name, desc })
            {
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = label.fontSize * MIN_FONT_SHARE;
            }
            Band(tag.rectTransform, 0.66f, 1f);
            Band(name.rectTransform, 0.32f, 0.66f);
            Band(desc.rectTransform, 0f, 0.32f);
            blocks[slot] = rect;
            blockNames[slot] = name;
            blockDescs[slot] = desc;
            rect.gameObject.SetActive(false);
        }

        static void Band(RectTransform rect, float minY, float maxY)
        {
            rect.anchorMin = new Vector2(0f, minY);
            rect.anchorMax = new Vector2(1f, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(BLOCK_TEXT_LEFT, 0f);
            rect.offsetMax = new Vector2(-BLOCK_TEXT_RIGHT, 0f);
        }
    }
}
