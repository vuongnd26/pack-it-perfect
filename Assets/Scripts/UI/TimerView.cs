// HUD clock pill: "⏱ 30s", red background from 10 s left.
// Implements: design/game-brief.md "Cơ chế chính" (Hẹn giờ: "Đồng hồ chuyển đỏ khi còn 10 giây").
// Story: production/epics/pack-it-perfect/story-006-timer-mode.md

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Displays the seconds left. Shown only on timed levels.</summary>
    public sealed class TimerView
    {
        readonly UITuning tuning;
        readonly Image pill;
        readonly TextMeshProUGUI label;
        readonly bool illustrated;

        /// <summary>Builds the pill under <paramref name="parent"/>; the caller places <see cref="Root"/>.</summary>
        public TimerView(UIFactory factory, Transform parent, UITuning tuning)
        {
            this.tuning = tuning;
            pill = factory.Panel("Timer", parent, tuning.panel, tuning.hudRowHeight / 2f);
            label = factory.Text("Seconds", pill.transform, string.Empty, tuning.buttonFontSize, tuning.ink, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var clock = factory.Art("Clock", pill.transform, "ui_clock");
            illustrated = clock != null;
            UIFactory.Stretch(label.rectTransform, illustrated ? 64f : 0f, 0f, 8f, 0f);
            if (clock != null)
                UIFactory.Place(clock.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(64f, 76f));
        }

        /// <summary>Root rect of the pill.</summary>
        public RectTransform Root => pill.rectTransform;

        /// <summary>True when the last shown value was at or below the red threshold.</summary>
        public bool IsDanger { get; private set; }

        /// <summary>Shows or hides the pill.</summary>
        public void SetVisible(bool visible) => pill.gameObject.SetActive(visible);

        /// <summary>Shows <paramref name="seconds"/> left; red background at or below <see cref="UITuning.timerDangerSeconds"/>.</summary>
        public void SetSeconds(int seconds)
        {
            label.text = TmpText.Clean(string.Format(illustrated ? UIStrings.SECONDS_FORMAT : UIStrings.TIMER_FORMAT, seconds));
            IsDanger = seconds <= tuning.timerDangerSeconds;
            pill.color = IsDanger ? tuning.danger : tuning.panel;
            label.color = IsDanger ? Color.white : tuning.ink;
        }
    }
}
