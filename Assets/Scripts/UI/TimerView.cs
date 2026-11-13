// HUD clock pill: "30s", red background from 10 s left; icy blue with a snowflake (or the word "Frozen") while a Freeze holds it.
// Implements: design/game-brief.md "Cơ chế chính" (Hẹn giờ: "Đồng hồ chuyển đỏ khi còn 10 giây"), "Nâng cấp Cozy Travel" (Freeze).
// Story: production/epics/pack-it-perfect/story-006-timer-mode.md

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Displays the seconds left. Shown only on timed levels.</summary>
    public sealed class TimerView
    {
        const float ICON_WIDTH = 56f;
        const float ICON_HEIGHT = 64f;
        const float ICON_INSET = 12f;
        const float LABEL_INSET = 8f;

        readonly UITuning tuning;
        readonly Image pill;
        readonly TextMeshProUGUI label;
        readonly GameObject clockIcon;
        readonly GameObject freezeIcon;
        readonly bool illustrated;
        int lastSeconds;
        int lastFrozenSeconds = -1;

        /// <summary>Builds the pill under <paramref name="parent"/>; the caller places <see cref="Root"/>.</summary>
        public TimerView(UIFactory factory, Transform parent, UITuning tuning)
        {
            this.tuning = tuning;
            pill = factory.Panel("Timer", parent, tuning.panel, tuning.hudPillHeight / 2f);
            label = factory.Text("Seconds", pill.transform, string.Empty, tuning.buttonFontSize - 8f, tuning.ink, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMax = tuning.buttonFontSize - 8f;
            label.fontSizeMin = tuning.smallFontSize * 0.7f;
            var clock = factory.Art("Clock", pill.transform, "ui_clock");
            var freeze = factory.Art("Freeze Icon", pill.transform, "ui_freeze");
            illustrated = clock != null;
            UIFactory.Stretch(label.rectTransform, illustrated ? ICON_WIDTH + ICON_INSET : 0f, 0f, LABEL_INSET, 0f);
            if (clock != null)
            {
                UIFactory.Place(clock.rectTransform, new Vector2(0f, 0.5f), new Vector2(ICON_INSET, 0f),
                    new Vector2(ICON_WIDTH, ICON_HEIGHT));
                clockIcon = clock.gameObject;
            }
            if (freeze != null)
            {
                UIFactory.Place(freeze.rectTransform, new Vector2(0f, 0.5f), new Vector2(ICON_INSET, 0f),
                    new Vector2(ICON_WIDTH, ICON_HEIGHT));
                freezeIcon = freeze.gameObject;
                freezeIcon.SetActive(false);
                if (!illustrated)
                {
                    UIFactory.Stretch(label.rectTransform, 0f, 0f, LABEL_INSET, 0f);
                }
            }
        }

        /// <summary>Root rect of the pill.</summary>
        public RectTransform Root => pill.rectTransform;

        /// <summary>True when the last shown value was at or below the red threshold.</summary>
        public bool IsDanger { get; private set; }

        /// <summary>True while the pill shows a Freeze.</summary>
        public bool IsFrozen { get; private set; }

        /// <summary>Shows or hides the pill.</summary>
        public void SetVisible(bool visible) => pill.gameObject.SetActive(visible);

        /// <summary>Shows <paramref name="seconds"/> left; red background at or below <see cref="UITuning.timerDangerSeconds"/>.</summary>
        public void SetSeconds(int seconds)
        {
            lastSeconds = seconds;
            IsDanger = seconds <= tuning.timerDangerSeconds;
            if (IsFrozen)
            {
                return;
            }
            Paint();
        }

        /// <summary>
        /// Switches the frozen look (ice-blue pill, snowflake or the word "Frozen", remaining freeze seconds) on or off.
        /// Call again with the new <paramref name="freezeSecondsLeft"/> as it counts down; unchanged values are cheap.
        /// </summary>
        public void SetFrozen(bool frozen, int freezeSecondsLeft = 0)
        {
            if (frozen == IsFrozen && (!frozen || freezeSecondsLeft == lastFrozenSeconds))
            {
                return;
            }
            IsFrozen = frozen;
            lastFrozenSeconds = frozen ? freezeSecondsLeft : -1;
            if (freezeIcon != null)
            {
                freezeIcon.SetActive(frozen);
                if (clockIcon != null)
                {
                    clockIcon.SetActive(!frozen);
                }
            }
            if (frozen)
            {
                string format = freezeIcon != null ? UIStrings.SECONDS_FORMAT : UIStrings.FROZEN_FORMAT;
                label.text = TmpText.Clean(string.Format(format, freezeSecondsLeft));
                pill.color = tuning.freezeTint;
                label.color = tuning.freezeText;
            }
            else
            {
                Paint();
            }
        }

        void Paint()
        {
            label.text = TmpText.Clean(string.Format(illustrated ? UIStrings.SECONDS_FORMAT : UIStrings.TIMER_FORMAT, lastSeconds));
            pill.color = IsDanger ? tuning.danger : tuning.panel;
            label.color = IsDanger ? Color.white : tuning.ink;
        }
    }
}
