// In-level HUD: top band (round pause button, level pill with the level name, timer pill, compact rule chips) and the
// bottom booster dock (Hint, Auto Pack, Freeze) with a toast line above it.
// Implements: design/game-brief.md "Cơ chế chính" (Hẹn giờ), "Nâng cấp Cozy Travel" (HUD gọn, booster dock).
// Stories: production/epics/pack-it-perfect/story-006-timer-mode.md, story-007-progression-meta.md

using System;
using System.Collections.Generic;
using PackItPerfect.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Displays level state; raises button events and never changes game state itself.</summary>
    public sealed class HudView
    {
        const float TOAST_RADIUS = 32f;
        const float TOAST_PADDING = 28f;
        const float CHIP_MIN_FONT_SHARE = 0.6f;
        const float ROW_GAP = 8f;
        const float FILL_WIDTH = 170f;
        const float PILL_FILL_INSET = 7f;
        const float PILL_TEXT_PADDING = 30f;
        const float CHIP_ICON_PADDING = 12f;

        readonly UIFactory factory;
        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly GameObject root;
        readonly CanvasGroup interaction;
        readonly TextMeshProUGUI levelLabel;
        readonly TextMeshProUGUI nameLabel;
        readonly TextMeshProUGUI fillLabel;
        readonly RectTransform chipRow;
        readonly RectTransform dock;
        readonly RectTransform top;
        readonly RectTransform levelPill;
        readonly RectTransform infoRow;
        readonly DockSlot[] dockOrder;
        readonly RectTransform infoContent;
        readonly UIButton rulesButton;
        string levelName = string.Empty;
        bool timed;
        bool showingItem;
        bool showingNotice;
        float layoutWidth = -1f;
        float layoutPillWidth = -1f;
        int layoutTiles = -1;
        float topBand;
        float bottomBand;
        readonly List<GameObject> chips = new List<GameObject>();
        readonly List<TextMeshProUGUI> chipLabels = new List<TextMeshProUGUI>();
        readonly List<string> chipTexts = new List<string>();
        readonly RectTransform pillFill;
        readonly object pillFillKey = new object();
        bool chipsDirty = true;
        float pillFillShown;
        readonly DockSlot[] boosterSlots = new DockSlot[BoosterBundle.TYPE_COUNT];
        readonly GameObject toast;
        readonly CanvasGroup toastGroup;
        readonly TextMeshProUGUI toastText;
        readonly object toastKey = new object();
        readonly object toastPopKey = new object();
        readonly object fillKey = new object();
        readonly RectTransform toastRect;
        readonly RectTransform safeRect;
        int shownFillPercent = -1;

        /// <summary>Pause button clicked.</summary>
        public event Action PauseClicked;
        /// <summary>A booster slot (Hint, Auto Pack, Freeze) clicked, locked or not.</summary>
        public event Action<BoosterType> BoosterClicked;
        /// <summary>Requests the pause overlay's complete rule explanation.</summary>
        public event Action RulesClicked;

        /// <summary>Builds the HUD under <paramref name="parent"/> (hidden until <see cref="SetVisible"/>).</summary>
        public HudView(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator)
        {
            this.factory = factory;
            this.tuning = tuning;
            this.animator = animator;
            var rootRect = UIFactory.CreateRect("HUD", parent);
            UIFactory.Stretch(rootRect);
            root = rootRect.gameObject;
            UIFactory.Isolate(root);
            interaction = root.AddComponent<CanvasGroup>();
            var safe = factory.SafeContent(rootRect);
            safeRect = safe;

            top = UIFactory.CreateRect("Top Band", safe);
            UIFactory.TopStrip(top, 0f, tuning.hudHeight, tuning.sideMargin);
            float rowTop = tuning.hudTopMargin;
            float pillTop = rowTop + (tuning.hudButtonSize - tuning.hudPillHeight) / 2f;

            PauseButton = factory.RoundButton("HUD Pause", top, UIStrings.PAUSE_ICON, ButtonStyle.Secondary,
                tuning.buttonFontSize, tuning.hudButtonSize, () => PauseClicked?.Invoke());
            UIFactory.Place(PauseButton.Root, new Vector2(0f, 1f), new Vector2(0f, -rowTop), Vector2.one * tuning.hudButtonSize);
            factory.ButtonIcon(PauseButton, "ui_pause", iconOnly: true);

            var levelPillImage = factory.Panel("Level Pill", top, tuning.ink, tuning.hudPillHeight / 2f);
            levelPill = levelPillImage.rectTransform;
            UIFactory.TopStrip(levelPill, pillTop, tuning.hudPillHeight, 0f);
            // The pill is the progress bar: a green fill grows behind the level number, the share sits on the right.
            var pillFillImage = factory.Panel("Fill", levelPill, tuning.primary, (tuning.hudPillHeight - PILL_FILL_INSET * 2f) / 2f);
            pillFillImage.raycastTarget = false;
            pillFill = pillFillImage.rectTransform;
            pillFill.anchorMin = new Vector2(0f, 0f);
            pillFill.anchorMax = new Vector2(0f, 1f);
            pillFill.pivot = new Vector2(0f, 0.5f);
            pillFill.anchoredPosition = new Vector2(PILL_FILL_INSET, 0f);
            pillFill.sizeDelta = new Vector2(0f, -PILL_FILL_INSET * 2f);
            pillFill.gameObject.SetActive(false);
            levelLabel = factory.Text("Level", levelPill, string.Empty, tuning.buttonFontSize - 8f, Color.white, FontStyles.Bold);
            levelLabel.textWrappingMode = TextWrappingModes.NoWrap;
            levelLabel.enableAutoSizing = true;
            levelLabel.fontSizeMax = tuning.buttonFontSize - 8f;
            levelLabel.fontSizeMin = tuning.hudNameFontSize;
            levelLabel.overflowMode = TextOverflowModes.Truncate;
            UIFactory.Stretch(levelLabel.rectTransform);

            Timer = new TimerView(factory, top, tuning);
            UIFactory.Place(Timer.Root, new Vector2(1f, 1f), new Vector2(0f, -pillTop),
                new Vector2(tuning.timerPillWidth, tuning.hudPillHeight));
            Timer.SetVisible(false);
            FitLevelPill(false);

            // One line under the buttons: the level's rule chips, or its name when it has none; while an item is held
            // the line names the item and its rules instead.
            infoRow = UIFactory.CreateRect("Info", top);
            UIFactory.TopStrip(infoRow, tuning.hudTopMargin + tuning.hudButtonSize + ROW_GAP, tuning.chipHeight, 0f);
            rulesButton = factory.Button("HUD Rules", infoRow, UIStrings.RULES_BUTTON, ButtonStyle.Secondary,
                tuning.chipFontSize, () => RulesClicked?.Invoke(), tuning.chipHeight / 2f);
            UIFactory.Place(rulesButton.Root, new Vector2(1f, 0.5f), Vector2.zero,
                new Vector2(tuning.rulesButtonWidth, tuning.chipHeight));
            infoContent = UIFactory.CreateRect("Info Content", infoRow);
            UIFactory.Stretch(infoContent, 0f, 0f, tuning.rulesButtonWidth + tuning.chipSpacing, 0f);
            chipRow = UIFactory.CreateRect("Rule Chips", infoContent);
            UIFactory.Stretch(chipRow);
            nameLabel = factory.Text("Level Name", infoContent, string.Empty, tuning.hudNameFontSize, tuning.ink, FontStyles.Bold,
                TextAlignmentOptions.Center);
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.overflowMode = TextOverflowModes.Truncate;
            UIFactory.Stretch(nameLabel.rectTransform, 8f, 0f, 8f, 0f);
            fillLabel = factory.Text("Fill", levelPill, string.Empty, tuning.hudNameFontSize, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Right);
            fillLabel.textWrappingMode = TextWrappingModes.NoWrap;
            fillLabel.rectTransform.anchorMin = new Vector2(1f, 0f);
            fillLabel.rectTransform.anchorMax = Vector2.one;
            fillLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            fillLabel.rectTransform.sizeDelta = new Vector2(FILL_WIDTH, 0f);
            fillLabel.rectTransform.anchoredPosition = new Vector2(-PILL_TEXT_PADDING, 0f);
            fillLabel.gameObject.SetActive(false);

            dock = UIFactory.CreateRect("Booster Dock", safe);
            dock.anchorMin = new Vector2(0f, 0f);
            dock.anchorMax = new Vector2(1f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);
            dock.anchoredPosition = new Vector2(0f, tuning.bottomBarMargin);
            dock.sizeDelta = new Vector2(-2f * tuning.sideMargin, tuning.dockTileHeight);

            boosterSlots[(int)BoosterType.Hint] = CreateBoosterSlot(BoosterType.Hint, "HUD Hint");
            boosterSlots[(int)BoosterType.AutoPack] = CreateBoosterSlot(BoosterType.AutoPack, "HUD AutoPack");
            boosterSlots[(int)BoosterType.Freeze] = CreateBoosterSlot(BoosterType.Freeze, "HUD Freeze");
            dockOrder = new[] { boosterSlots[(int)BoosterType.Hint], boosterSlots[(int)BoosterType.AutoPack],
                boosterSlots[(int)BoosterType.Freeze] };

            var toastImage = factory.Panel("Toast", safe, tuning.bannerBackground, TOAST_RADIUS);
            toast = toastImage.gameObject;
            toastRect = toastImage.rectTransform;
            // Its fade changes every frame: keep that off the HUD's own canvas.
            UIFactory.Isolate(toast);
            toastGroup = toast.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;
            UIFactory.Place(toastImage.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(0f, tuning.bottomBarMargin + tuning.bottomBarHeight + tuning.toastGap),
                new Vector2(tuning.toastWidth, tuning.toastHeight));
            toastText = factory.Text("Message", toast.transform, string.Empty, tuning.toastFontSize, Color.white, FontStyles.Bold);
            toastText.enableAutoSizing = true;
            toastText.fontSizeMax = tuning.toastFontSize;
            toastText.fontSizeMin = tuning.toastFontSize * CHIP_MIN_FONT_SHARE;
            UIFactory.Stretch(toastText.rectTransform, TOAST_PADDING, 0f, TOAST_PADDING, 0f);
            toast.SetActive(false);

            root.SetActive(false);
        }

        /// <summary>The clock pill.</summary>
        public TimerView Timer { get; }

        /// <summary>The round pause button.</summary>
        public UIButton PauseButton { get; }

        /// <summary>The Hint slot's button (first focus for gamepads).</summary>
        public UIButton FirstFocus => boosterSlots[(int)BoosterType.Hint].Button;

        /// <summary>Height of the top band in reference pixels, below the safe-area top.</summary>
        public float TopBandHeight { get { Relayout(); return topBand; } }

        /// <summary>Fixed information strip used by transient level notices, without covering the board.</summary>
        public RectTransform NoticeHost => infoContent;

        /// <summary>Temporarily hides the ordinary information while a notice occupies the same fixed strip.</summary>
        public void SetNoticeVisible(bool visible)
        {
            showingNotice = visible;
            ShowInfoLine();
        }

        /// <summary>Shows the held item and its rules on the info line; null or empty restores the level's name.</summary>
        public void SetItemInfo(string text)
        {
            bool item = !string.IsNullOrEmpty(text);
            if (!item && !showingItem) return;
            showingItem = item;
            nameLabel.text = TmpText.Clean(item ? text : levelName);
            nameLabel.color = item ? tuning.primaryShadow : tuning.ink;
            ShowInfoLine();
        }

        // Chips show the level's rules; the text line replaces them while an item is held or when there are none.
        void ShowInfoLine()
        {
            bool text = showingItem || chips.Count == 0;
            nameLabel.gameObject.SetActive(text && !showingNotice);
            chipRow.gameObject.SetActive(!text && !showingNotice);
        }

        // Chips keep their words when the whole row fits on one line; otherwise they shrink to their icons (the held-item
        // line and the labels on the board still spell the rules out).
        void LayoutChips(float width)
        {
            bool compact = false;
            float total = -tuning.chipSpacing;
            for (int i = 0; i < chips.Count; i++)
            {
                string full = TmpText.Clean(chipTexts[i]);
                chipLabels[i].text = full;
                total += chipLabels[i].GetPreferredValues(full).x + tuning.chipPaddingX * 2f + tuning.chipSpacing;
            }
            if (total > width)
            {
                compact = true;
                total = -tuning.chipSpacing;
                for (int i = 0; i < chips.Count; i++)
                {
                    string full = TmpText.Clean(chipTexts[i]);
                    string icon = TmpText.Clean(UIStrings.RuleIcon(full));
                    chipLabels[i].text = icon;
                    total += Mathf.Max(tuning.chipHeight, chipLabels[i].GetPreferredValues(icon).x + CHIP_ICON_PADDING * 2f) + tuning.chipSpacing;
                }
            }
            float x = Mathf.Max(0f, (width - total) / 2f);
            for (int i = 0; i < chips.Count; i++)
            {
                var label = chipLabels[i];
                float chipWidth = compact
                    ? Mathf.Max(tuning.chipHeight, label.GetPreferredValues(label.text).x + CHIP_ICON_PADDING * 2f)
                    : label.GetPreferredValues(label.text).x + tuning.chipPaddingX * 2f;
                UIFactory.Place((RectTransform)chips[i].transform, new Vector2(0f, 0.5f), new Vector2(x, 0f),
                    new Vector2(chipWidth, tuning.chipHeight));
                x += chipWidth + tuning.chipSpacing;
            }
        }

        // The level pill takes the clock's place on untimed levels.
        void FitLevelPill(bool timed)
        {
            this.timed = timed;
            float width = Mathf.Max(1f, top.rect.width);
            float timerWidth = Mathf.Min(tuning.timerPillWidth, width * 0.26f);
            Timer.Root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timerWidth);
            levelPill.offsetMin = new Vector2(tuning.hudButtonSize + tuning.chipSpacing, levelPill.offsetMin.y);
            levelPill.offsetMax = new Vector2(timed ? -timerWidth - tuning.chipSpacing : 0f, levelPill.offsetMax.y);
            if (fillLabel != null) FitPillText();
        }

        void FitPillText()
        {
            layoutPillWidth = levelPill.rect.width;
            float fillWidth = Mathf.Min(FILL_WIDTH, layoutPillWidth * 0.38f);
            fillLabel.rectTransform.sizeDelta = new Vector2(fillWidth, 0f);
            levelLabel.alignment = TextAlignmentOptions.Left;
            UIFactory.Stretch(levelLabel.rectTransform, PILL_TEXT_PADDING, 0f,
                fillWidth + PILL_TEXT_PADDING + 8f, 0f);
            DrawPillFill(pillFillShown);
        }

        // Places the info line under the wrapped chips, lays the dock tiles out and measures both bands. Cheap when
        // nothing changed: it compares four values and returns.
        void Relayout()
        {
            float width = dock.rect.width;
            int tiles = 0;
            foreach (var slot in dockOrder) if (slot.IsVisible) tiles++;
            if (!Mathf.Approximately(levelPill.rect.width, layoutPillWidth)) FitPillText();
            if (Mathf.Approximately(width, layoutWidth) && !chipsDirty && tiles == layoutTiles)
            {
                return;
            }
            layoutWidth = width;
            chipsDirty = false;
            layoutTiles = tiles;
            FitLevelPill(timed);

            // Notices, held-item names and rules share one fixed-height strip.
            LayoutChips(Mathf.Max(1f, width - tuning.rulesButtonWidth - tuning.chipSpacing));
            topBand = tuning.hudTopMargin + tuning.hudButtonSize + ROW_GAP + tuning.chipHeight;
            top.sizeDelta = new Vector2(top.sizeDelta.x, topBand);

            int columns = Mathf.Max(1, tiles);
            float tileWidth = (width - tuning.dockSpacing * (columns - 1)) / columns;
            // Too narrow for icon-beside-name (large UI size on a small phone): stack the icon above the name and keep
            // one row, rather than a second dock row that would take the board's space.
            bool compactTiles = tileWidth < tuning.dockTileMinWidth;
            tileWidth = Mathf.Max(1f, Mathf.Min(tuning.dockTileMaxWidth, tileWidth));
            float tileHeight = compactTiles ? tuning.dockTileCompactHeight : tuning.dockTileHeight;
            int rows = tiles == 0 ? 0 : 1;
            float rowStep = tileHeight + tuning.badgeRise + tuning.dockSpacing;
            float dockHeight = rows == 0 ? 0f : rows * rowStep - tuning.dockSpacing;
            dock.sizeDelta = new Vector2(dock.sizeDelta.x, dockHeight);
            int index = 0;
            foreach (var slot in dockOrder)
            {
                if (!slot.IsVisible) continue;
                int row = index / columns;
                int inRow = Mathf.Min(columns, tiles - row * columns);
                float rowWidth = inRow * tileWidth + (inRow - 1) * tuning.dockSpacing;
                float x = (width - rowWidth) / 2f + (index % columns) * (tileWidth + tuning.dockSpacing);
                // Rows fill from the top; each leaves room above the tile for its count pill.
                float y = (rows - 1 - row) * rowStep;
                slot.SetCompact(compactTiles);
                UIFactory.Place(slot.Button.Root, Vector2.zero, new Vector2(x, y), new Vector2(tileWidth, tileHeight));
                index++;
            }
            bottomBand = tuning.bottomBarMargin + dockHeight + tuning.chipSpacing;
        }

        /// <summary>Displays board occupancy as a percentage. The game layer supplies a normalized 0..1 fraction.</summary>
        public void SetFill(float fraction)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f);
            if (percent == shownFillPercent && fillLabel.gameObject.activeSelf)
            {
                return;
            }
            bool grew = shownFillPercent >= 0 && percent > shownFillPercent;
            shownFillPercent = percent;
            fillLabel.gameObject.SetActive(true);
            fillLabel.text = TmpText.Clean(string.Format(UIStrings.FILL_FORMAT, percent));
            if (grew)
            {
                UIMotion.Pulse(animator, fillKey, fillLabel.rectTransform, tuning.timerPulse, tuning.pulseAmount, tuning.reduceMotion);
            }
            float from = pillFillShown;
            float to = percent / 100f;
            if (tuning.reduceMotion || !root.activeInHierarchy)
            {
                animator.Cancel(pillFillKey);
                DrawPillFill(to);
            }
            else
            {
                animator.Play(pillFillKey, 0f, tuning.timerPulse, t => DrawPillFill(Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t))));
            }
        }

        void DrawPillFill(float share)
        {
            pillFillShown = share;
            float track = levelPill.rect.width - PILL_FILL_INSET * 2f;
            float cap = tuning.hudPillHeight - PILL_FILL_INSET * 2f;
            pillFill.gameObject.SetActive(share > 0f);
            pillFill.sizeDelta = new Vector2(Mathf.Max(cap, track * share), pillFill.sizeDelta.y);
        }

        /// <summary>Height of the dock plus its margin in reference pixels, above the safe-area bottom.</summary>
        public float BottomBandHeight { get { Relayout(); return bottomBand; } }

        /// <summary>Shows or hides the whole HUD.</summary>
        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                animator.Cancel(toastKey);
                animator.Cancel(toastPopKey);
                animator.Cancel(fillKey);
                animator.Cancel(pillFillKey);
                fillLabel.rectTransform.localScale = Vector3.one;
                toast.SetActive(false);
                foreach (var slot in boosterSlots) slot.ResetMotion();
            }
            root.SetActive(visible);
        }

        /// <summary>
        /// Shows or hides the booster dock. It keeps its place in the layout, so the board does not move; result cards
        /// hide it so no half-covered buttons show under them.
        /// </summary>
        public void SetDockVisible(bool visible)
        {
            if (dock.gameObject.activeSelf != visible) dock.gameObject.SetActive(visible);
        }

        /// <summary>Keeps the HUD visible but excludes covered controls from pointer and navigation input.</summary>
        public void SetInteractable(bool enabled)
        {
            interaction.interactable = enabled;
            interaction.blocksRaycasts = enabled;
        }

        /// <summary>Shows a level's number, name and rule chips.</summary>
        public void Bind(LevelInfo info)
        {
            shownFillPercent = -1;
            levelLabel.text = TmpText.Clean(string.Format(UIStrings.LEVEL_PILL_FORMAT, info?.Number ?? 0));
            levelName = info?.Name ?? string.Empty;
            showingItem = false;
            nameLabel.text = TmpText.Clean(levelName);
            nameLabel.color = tuning.ink;
            chipsDirty = true;
            pillFillShown = 0f;
            animator.Cancel(pillFillKey);
            pillFill.gameObject.SetActive(false);
            foreach (var chip in chips)
            {
                chip.SetActive(false);
                UnityEngine.Object.Destroy(chip);
            }
            chips.Clear();
            chipLabels.Clear();
            chipTexts.Clear();
            if (info?.RuleChips != null)
            {
                foreach (string text in info.RuleChips)
                {
                    chips.Add(CreateChip(text));
                    chipTexts.Add(text);
                }
            }
            ShowInfoLine();
        }

        /// <summary>Shows the timer pill and the Freeze slot on timed levels, hides both otherwise (the dock recentres).</summary>
        public void SetTimed(bool timed)
        {
            Timer.SetVisible(timed);
            FitLevelPill(timed);
            boosterSlots[(int)BoosterType.Freeze].SetVisible(timed);
        }

        /// <summary>Updates lock state, count badge and usable look of the three booster slots.</summary>
        /// <param name="progress">Source of unlocks and counts.</param>
        /// <param name="frozen">True while a Freeze is running (the Freeze slot looks unusable).</param>
        public void RefreshBoosters(ProgressService progress, bool frozen)
        {
            for (int i = 0; i < boosterSlots.Length; i++)
            {
                var type = (BoosterType)i;
                var slot = boosterSlots[i];
                bool locked = !progress.IsBoosterUnlocked(type);
                int count = progress.BoosterCount(type);
                slot.SetLocked(locked, progress.Rules.UnlockAfterLevel(type));
                slot.SetCount(count);
                slot.SetUsable(count > 0 && !(type == BoosterType.Freeze && frozen));
            }
        }

        /// <summary>Shakes a booster slot (a refused tap).</summary>
        public void ShakeBooster(BoosterType type) => boosterSlots[(int)type].Shake();

        /// <summary>Bounces a booster slot after it was used.</summary>
        public void BounceBooster(BoosterType type) => boosterSlots[(int)type].PlayUse();

        /// <summary>Shows a short message above the dock; it fades after <see cref="UITuning.toastHold"/> seconds.</summary>
        public void ShowToast(string message)
        {
            toastText.text = TmpText.Clean(message);
            // Never wider than the safe area (small phones, large UI size) and always above the dock, even when the
            // dock has wrapped onto two rows.
            float width = Mathf.Max(1f, Mathf.Min(tuning.toastWidth, safeRect.rect.width - tuning.sideMargin * 2f));
            toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            toastRect.anchoredPosition = new Vector2(0f, BottomBandHeight + tuning.toastGap);
            toast.SetActive(true);
            UIMotion.Pop(animator, toastPopKey, toastRect, 0f, tuning.toastPop, 0.85f, tuning.reduceMotion);
            animator.Play(toastKey, tuning.toastHold, tuning.toastFade, t => toastGroup.alpha = 1f - t,
                () => toast.SetActive(false));
        }

        DockSlot CreateBoosterSlot(BoosterType type, string id) => new DockSlot(factory, dock, tuning, animator, id,
            UIStrings.BoosterArt(type), UIStrings.BoosterName(type), true, () => BoosterClicked?.Invoke(type));

        GameObject CreateChip(string text)
        {
            var chip = factory.Panel("Chip", chipRow, tuning.panel, tuning.chipHeight / 2f);
            var label = factory.Text("Text", chip.transform, text, tuning.chipFontSize, tuning.ink);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = false;
            UIFactory.Stretch(label.rectTransform);
            chipLabels.Add(label);
            return chip.gameObject;
        }
    }
}
