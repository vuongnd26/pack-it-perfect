// In-level HUD: top band (home, level pill, timer, level name, fill %, rule chips) and bottom bar (Hint, Restart).
// Implements: design/game-brief.md "Cơ chế chính" (Thanh %), "Booster" ("💡 Hint"), "Cơ chế chính" (Hẹn giờ).
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Displays level state; raises button events and never changes game state itself.</summary>
    public sealed class HudView
    {
        const float ROW_GAP = 14f;
        const float CHIP_SPACING = 16f;
        const float CHIP_PADDING_X = 24;
        const float BUTTON_GAP = 32f;
        const float PERCENT = 100f;

        readonly UIFactory factory;
        readonly UITuning tuning;
        readonly GameObject root;
        readonly CanvasGroup interaction;
        readonly TextMeshProUGUI levelLabel;
        readonly TextMeshProUGUI nameLabel;
        readonly TextMeshProUGUI fillLabel;
        readonly RectTransform chipRow;
        readonly List<GameObject> chips = new List<GameObject>();

        /// <summary>Home button clicked.</summary>
        public event Action HomeClicked;
        /// <summary>"💡 Hint" clicked.</summary>
        public event Action HintClicked;
        /// <summary>Restart clicked.</summary>
        public event Action RestartClicked;

        /// <summary>Builds the HUD under <paramref name="parent"/> (hidden until <see cref="SetVisible"/>).</summary>
        public HudView(UIFactory factory, Transform parent, UITuning tuning)
        {
            this.factory = factory;
            this.tuning = tuning;
            var rootRect = UIFactory.CreateRect("HUD", parent);
            UIFactory.Stretch(rootRect);
            root = rootRect.gameObject;
            interaction = root.AddComponent<CanvasGroup>();
            var safe = factory.SafeContent(rootRect);

            var top = UIFactory.CreateRect("Top Band", safe);
            UIFactory.TopStrip(top, 0f, tuning.hudHeight, tuning.sideMargin);
            float rowTop = ROW_GAP;

            var home = factory.Button("HUD Home", top, UIStrings.HOME_ICON, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => HomeClicked?.Invoke());
            UIFactory.Place(home.Root, new Vector2(0f, 1f), new Vector2(0f, -rowTop), Vector2.one * tuning.hudButtonSize);
            factory.ButtonIcon(home, "ui_home", iconOnly: true);

            var levelPill = factory.Panel("Level Pill", top, tuning.ink, tuning.hudRowHeight / 2f);
            UIFactory.Place(levelPill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -rowTop),
                new Vector2(tuning.levelPillWidth, tuning.hudRowHeight));
            levelLabel = factory.Text("Level", levelPill.transform, string.Empty, tuning.bodyFontSize, Color.white, FontStyles.Bold);
            levelLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(levelLabel.rectTransform);

            Timer = new TimerView(factory, top, tuning);
            UIFactory.Place(Timer.Root, new Vector2(1f, 1f), new Vector2(0f, -rowTop),
                new Vector2(tuning.timerPillWidth, tuning.hudRowHeight));
            Timer.SetVisible(false);

            float nameTop = rowTop + tuning.hudRowHeight + ROW_GAP;
            float nameHeight = tuning.chipHeight + ROW_GAP;
            nameLabel = factory.Text("Level Name", top, string.Empty, tuning.bodyFontSize + 6f, tuning.ink, FontStyles.Bold);
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMax = tuning.bodyFontSize + 6f;
            nameLabel.fontSizeMin = tuning.smallFontSize;
            UIFactory.TopStrip(nameLabel.rectTransform, nameTop, nameHeight, tuning.fillPillWidth + ROW_GAP);

            var fillPill = factory.Panel("Fill Pill", top, tuning.panel, nameHeight / 2f);
            UIFactory.Place(fillPill.rectTransform, new Vector2(1f, 1f), new Vector2(0f, -nameTop),
                new Vector2(tuning.fillPillWidth, nameHeight));
            fillLabel = factory.Text("Fill", fillPill.transform, string.Empty, tuning.bodyFontSize, tuning.primaryShadow, FontStyles.Bold);
            fillLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(fillLabel.rectTransform);

            chipRow = UIFactory.CreateRect("Rule Chips", top);
            UIFactory.TopStrip(chipRow, nameTop + nameHeight + ROW_GAP, tuning.chipHeight, 0f);
            var rowLayout = chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = CHIP_SPACING;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var bottom = UIFactory.CreateRect("Bottom Bar", safe);
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = new Vector2(0f, tuning.bottomBarMargin);
            bottom.sizeDelta = new Vector2(-2f * tuning.sideMargin, tuning.bottomBarHeight);

            var hint = factory.Button("HUD Hint", bottom, UIStrings.HINT_LABEL, ButtonStyle.Accent, tuning.buttonFontSize,
                () => HintClicked?.Invoke());
            HalfWidth(hint.Root, left: true);
            var restart = factory.Button("HUD Restart", bottom, UIStrings.RESTART, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => RestartClicked?.Invoke());
            HalfWidth(restart.Root, left: false);
            factory.ButtonIcon(hint, "ui_hint");
            factory.ButtonIcon(restart, "ui_restart");
            HintButton = hint;

            root.SetActive(false);
        }

        /// <summary>The clock pill.</summary>
        public TimerView Timer { get; }

        /// <summary>The Hint button (first focus for gamepads).</summary>
        public UIButton HintButton { get; }

        /// <summary>Height of the top band in reference pixels, below the safe-area top.</summary>
        public float TopBandHeight => tuning.hudHeight;

        /// <summary>Height of the bottom bar plus its margin in reference pixels, above the safe-area bottom.</summary>
        public float BottomBandHeight => tuning.bottomBarHeight + tuning.bottomBarMargin;

        /// <summary>Shows or hides the whole HUD.</summary>
        public void SetVisible(bool visible) => root.SetActive(visible);

        /// <summary>Keeps the HUD visible but excludes covered controls from pointer and navigation input.</summary>
        public void SetInteractable(bool enabled)
        {
            interaction.interactable = enabled;
            interaction.blocksRaycasts = enabled;
        }

        /// <summary>Shows a level's number, name and rule chips.</summary>
        public void Bind(LevelInfo info, int levelCount)
        {
            levelLabel.text = TmpText.Clean(string.Format(UIStrings.LEVEL_PILL_FORMAT, info?.Number ?? 0, levelCount));
            nameLabel.text = TmpText.Clean(info?.Name);
            foreach (var chip in chips)
            {
                UnityEngine.Object.Destroy(chip);
            }
            chips.Clear();
            if (info?.RuleChips == null)
            {
                return;
            }
            foreach (string text in info.RuleChips)
            {
                chips.Add(CreateChip(text));
            }
        }

        /// <summary>Shows the filled share (0..1) as a whole percentage.</summary>
        public void SetFill(float fill)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(fill) * PERCENT);
            fillLabel.text = string.Format(UIStrings.FILL_FORMAT, percent);
        }

        GameObject CreateChip(string text)
        {
            var chip = factory.Panel("Chip", chipRow, tuning.panel, tuning.chipHeight / 2f);
            var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)CHIP_PADDING_X, (int)CHIP_PADDING_X, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var label = factory.Text("Text", chip.transform, text, tuning.chipFontSize, tuning.ink);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return chip.gameObject;
        }

        void HalfWidth(RectTransform rect, bool left)
        {
            rect.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rect.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left ? 0f : BUTTON_GAP / 2f, 0f);
            rect.offsetMax = new Vector2(left ? -BUTTON_GAP / 2f : 0f, 0f);
        }
    }
}
