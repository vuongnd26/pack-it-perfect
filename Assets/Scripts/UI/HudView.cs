// In-level HUD: top band (round pause button, level pill with the level name, timer pill, compact rule chips) and the
// bottom booster dock (Undo, Hint, Auto Pack, Freeze) with a toast line above it.
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
        const float NAME_GAP = 4f;
        const float TOAST_RADIUS = 32f;
        const float TOAST_PADDING = 28f;
        const float CHIP_MIN_FONT_SHARE = 0.6f;

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
        readonly List<GameObject> chips = new List<GameObject>();
        readonly DockSlot undoSlot;
        readonly DockSlot[] boosterSlots = new DockSlot[BoosterBundle.TYPE_COUNT];
        readonly GameObject toast;
        readonly CanvasGroup toastGroup;
        readonly TextMeshProUGUI toastText;
        readonly object toastKey = new object();
        readonly object toastPopKey = new object();
        readonly object fillKey = new object();
        readonly RectTransform toastRect;
        readonly RectTransform safeRect;
        bool undoUsable = true;
        int shownFillPercent = -1;

        /// <summary>Pause button clicked.</summary>
        public event Action PauseClicked;
        /// <summary>Undo slot clicked.</summary>
        public event Action UndoClicked;
        /// <summary>A booster slot (Hint, Auto Pack, Freeze) clicked, locked or not.</summary>
        public event Action<BoosterType> BoosterClicked;

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

            var top = UIFactory.CreateRect("Top Band", safe);
            UIFactory.TopStrip(top, 0f, tuning.hudHeight, tuning.sideMargin);
            float rowTop = tuning.hudTopMargin;

            PauseButton = factory.RoundButton("HUD Pause", top, UIStrings.PAUSE_ICON, ButtonStyle.Secondary,
                tuning.buttonFontSize, tuning.hudButtonSize, () => PauseClicked?.Invoke());
            UIFactory.Place(PauseButton.Root, new Vector2(0f, 1f), new Vector2(0f, -rowTop), Vector2.one * tuning.hudButtonSize);
            factory.ButtonIcon(PauseButton, "ui_pause", iconOnly: true);

            var levelPill = factory.Panel("Level Pill", top, tuning.ink, tuning.hudPillHeight / 2f);
            UIFactory.TopStrip(levelPill.rectTransform, rowTop, tuning.hudPillHeight, 0f);
            levelPill.rectTransform.offsetMin = new Vector2(tuning.hudButtonSize + tuning.chipSpacing, levelPill.rectTransform.offsetMin.y);
            levelPill.rectTransform.offsetMax = new Vector2(-tuning.timerPillWidth - tuning.chipSpacing, levelPill.rectTransform.offsetMax.y);
            levelLabel = factory.Text("Level", levelPill.transform, string.Empty, tuning.bodyFontSize, Color.white, FontStyles.Bold);
            levelLabel.textWrappingMode = TextWrappingModes.NoWrap;
            levelLabel.enableAutoSizing = true;
            levelLabel.fontSizeMax = tuning.bodyFontSize;
            levelLabel.fontSizeMin = tuning.hudNameFontSize;
            UIFactory.Stretch(levelLabel.rectTransform);

            nameLabel = factory.Text("Level Name", top, string.Empty, tuning.hudNameFontSize, tuning.ink, FontStyles.Bold);
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMax = tuning.hudNameFontSize;
            nameLabel.fontSizeMin = tuning.hudNameFontSize * CHIP_MIN_FONT_SHARE;
            UIFactory.TopStrip(nameLabel.rectTransform, rowTop + tuning.hudPillHeight + NAME_GAP, tuning.hudNameHeight, 0f);
            nameLabel.rectTransform.offsetMin = new Vector2(tuning.hudButtonSize + tuning.chipSpacing, nameLabel.rectTransform.offsetMin.y);
            nameLabel.rectTransform.offsetMax = new Vector2(-tuning.timerPillWidth - tuning.chipSpacing, nameLabel.rectTransform.offsetMax.y);

            Timer = new TimerView(factory, top, tuning);
            UIFactory.Place(Timer.Root, new Vector2(1f, 1f), new Vector2(0f, -rowTop),
                new Vector2(tuning.timerPillWidth, tuning.hudPillHeight));
            Timer.SetVisible(false);

            chipRow = UIFactory.CreateRect("Rule Chips", top);
            UIFactory.TopStrip(chipRow, tuning.chipRowTop, tuning.chipHeight, 0f);
            chipRow.gameObject.AddComponent<ResponsiveUILayout>().ConfigureFlow(tuning.chipSpacing,
                tuning.chipHeight, tuning.chipPaddingX);

            fillLabel = factory.Text("Fill", top, string.Empty, tuning.hudNameFontSize, tuning.ink, FontStyles.Bold);
            UIFactory.Place(fillLabel.rectTransform, new Vector2(1f, 1f),
                new Vector2(0f, -(rowTop + tuning.hudPillHeight + NAME_GAP)),
                new Vector2(tuning.timerPillWidth, tuning.hudNameHeight));
            fillLabel.gameObject.SetActive(false);

            dock = UIFactory.CreateRect("Booster Dock", safe);
            dock.anchorMin = new Vector2(0f, 0f);
            dock.anchorMax = new Vector2(1f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);
            dock.anchoredPosition = new Vector2(0f, tuning.bottomBarMargin);
            dock.sizeDelta = new Vector2(-2f * tuning.sideMargin, tuning.bottomBarHeight);
            dock.gameObject.AddComponent<ResponsiveUILayout>().ConfigureFlow(tuning.dockSpacing,
                tuning.dockSlotSize, 0f, false);

            undoSlot = new DockSlot(factory, dock, tuning, animator, "HUD Undo", "ui_undo", UIStrings.UNDO, false,
                () => UndoClicked?.Invoke());
            boosterSlots[(int)BoosterType.Hint] = CreateBoosterSlot(BoosterType.Hint, "HUD Hint");
            boosterSlots[(int)BoosterType.AutoPack] = CreateBoosterSlot(BoosterType.AutoPack, "HUD AutoPack");
            boosterSlots[(int)BoosterType.Freeze] = CreateBoosterSlot(BoosterType.Freeze, "HUD Freeze");

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
        public float TopBandHeight => Mathf.Max(tuning.hudHeight, tuning.chipRowTop + chipRow.rect.height + tuning.chipSpacing);

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
        }

        /// <summary>Height of the dock plus its margin in reference pixels, above the safe-area bottom.</summary>
        public float BottomBandHeight => Mathf.Max(tuning.bottomBarHeight, dock.rect.height) + tuning.bottomBarMargin;

        /// <summary>Shows or hides the whole HUD.</summary>
        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                animator.Cancel(toastKey);
                animator.Cancel(toastPopKey);
                animator.Cancel(fillKey);
                fillLabel.rectTransform.localScale = Vector3.one;
                toast.SetActive(false);
                undoSlot.ResetMotion();
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
            nameLabel.text = TmpText.Clean(info?.Name);
            foreach (var chip in chips)
            {
                chip.SetActive(false);
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

        /// <summary>Shows the timer pill and the Freeze slot on timed levels, hides both otherwise (the dock recentres).</summary>
        public void SetTimed(bool timed)
        {
            Timer.SetVisible(timed);
            boosterSlots[(int)BoosterType.Freeze].SetVisible(timed);
            LayoutRebuilder.MarkLayoutForRebuild(dock);
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

        /// <summary>Dims the Undo slot when there is nothing to undo. Cheap to call every frame.</summary>
        public void SetUndoUsable(bool usable)
        {
            if (usable == undoUsable)
            {
                return;
            }
            undoUsable = usable;
            undoSlot.SetUsable(usable);
        }

        /// <summary>Shakes a booster slot (a refused tap).</summary>
        public void ShakeBooster(BoosterType type) => boosterSlots[(int)type].Shake();

        /// <summary>Shakes the Undo slot.</summary>
        public void ShakeUndo() => undoSlot.Shake();

        /// <summary>Bounces a booster slot after it was used.</summary>
        public void BounceBooster(BoosterType type) => boosterSlots[(int)type].PlayUse();

        /// <summary>Bounces the Undo slot.</summary>
        public void BounceUndo() => undoSlot.PlayUse();

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
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = false;
            UIFactory.Stretch(label.rectTransform, tuning.chipPaddingX, 0f, tuning.chipPaddingX, 0f);
            return chip.gameObject;
        }
    }
}
