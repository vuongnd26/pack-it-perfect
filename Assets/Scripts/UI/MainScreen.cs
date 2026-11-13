// Main screen (the app opens here): title, tagline, total stars + levels passed, Play, Levels.
// Implements: design/game-brief.md "Progression & Meta" ("Màn hình chính hiện tổng số sao và số màn đã qua").
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

using System;
using PackItPerfect.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Opaque full-screen main menu.</summary>
    public sealed class MainScreen
    {
        const float CONTENT_WIDTH = 900f;
        const float LOGO_HEIGHT = 640f;
        const float TITLE_HEIGHT = 300f;
        const float TAGLINE_HEIGHT = 120f;
        const float STATS_HEIGHT = 130f;
        const float PLAY_HEIGHT = 190f;
        const float LEVELS_HEIGHT = 150f;
        const float CHIP_SPACING = 20f;
        const float CHIP_ICON_WIDTH = 84f;
        const float CHIP_ICON_HEIGHT = 64f;
        const float CHIP_PADDING = 18f;
        const float CHIP_ICON_FONT = 24f;

        readonly GameObject root;
        readonly TextMeshProUGUI stats;
        readonly UIButton sound;
        readonly UIButton motion;
        readonly UIButton uiSize;
        /// <summary>Requests the next readable interface size.</summary>
        public event Action UISizeClicked;
        readonly GameObject[] boosterChips = new GameObject[BoosterBundle.TYPE_COUNT];
        readonly TextMeshProUGUI[] boosterCounts = new TextMeshProUGUI[BoosterBundle.TYPE_COUNT];

        /// <summary>Requests an audio preference change.</summary>
        public event Action SoundClicked;
        /// <summary>Requests a motion preference change.</summary>
        public event Action MotionClicked;

        /// <summary>"▶ Play" clicked.</summary>
        public event Action PlayClicked;
        /// <summary>"Levels" clicked.</summary>
        public event Action LevelsClicked;

        /// <summary>Builds the (hidden) screen under <paramref name="parent"/>.</summary>
        public MainScreen(UIFactory factory, Transform parent, UITuning tuning)
        {
            var background = factory.Panel("Main Screen", parent, tuning.menuBackground, 0f, raycast: true);
            UIFactory.Stretch(background.rectTransform);
            root = background.gameObject;
            factory.Backdrop(background.transform, "bg_menu");
            var safe = factory.SafeContent(background.transform);
            var content = UIFactory.CreateRect("Main Content", safe);
            UIFactory.StackVertically(content, 16, CHIP_SPACING);

            // Keep the existing presentation, but let the stack and safe viewport own placement.
            var logo = factory.Art("Logo", content, "logo");
            if (logo != null)
            {
                UIFactory.FixedSize(logo.rectTransform, LOGO_HEIGHT);
            }
            else
            {
                var title = factory.Text("Title", content, UIStrings.TITLE, tuning.titleFontSize, tuning.ink, FontStyles.Bold);
                UIFactory.FixedSize(title.rectTransform, TITLE_HEIGHT);
            }

            var tagline = factory.Text("Tagline", content, UIStrings.TAGLINE, tuning.bodyFontSize, tuning.ink);
            UIFactory.FixedSize(tagline.rectTransform, TAGLINE_HEIGHT);

            var statsCard = factory.Panel("Stats", content, tuning.panel, STATS_HEIGHT / 2f);
            UIFactory.FixedSize(statsCard.rectTransform, STATS_HEIGHT);
            stats = factory.Text("Stats Text", statsCard.transform, string.Empty, tuning.bodyFontSize + 4f, tuning.ink, FontStyles.Bold);
            stats.textWrappingMode = TextWrappingModes.NoWrap;
            stats.enableAutoSizing = true;
            stats.fontSizeMax = tuning.bodyFontSize + 4f;
            stats.fontSizeMin = tuning.smallFontSize;
            UIFactory.Stretch(stats.rectTransform, tuning.sideMargin, 0f, tuning.sideMargin, 0f);

            PlayButton = factory.Button("Main Play", content, UIStrings.PLAY, ButtonStyle.Primary, tuning.buttonFontSize + 12f,
                () => PlayClicked?.Invoke());
            UIFactory.FixedSize(PlayButton.Root, PLAY_HEIGHT);

            var levels = factory.Button("Main Levels", content, UIStrings.LEVELS, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => LevelsClicked?.Invoke());
            UIFactory.FixedSize(levels.Root, LEVELS_HEIGHT);

            sound = factory.Button("Main Sound", content, UIStrings.SOUND_ON, ButtonStyle.Secondary, tuning.smallFontSize,
                () => SoundClicked?.Invoke());
            UIFactory.FixedSize(sound.Root, 120f);
            motion = factory.Button("Main Motion", content, UIStrings.MOTION_ON, ButtonStyle.Secondary, tuning.smallFontSize,
                () => MotionClicked?.Invoke());
            UIFactory.FixedSize(motion.Root, 120f);
            uiSize = factory.Button("Main UI Size", content, string.Format(UIStrings.UI_SIZE_FORMAT, 100),
                ButtonStyle.Secondary, tuning.smallFontSize, () => UISizeClicked?.Invoke());
            UIFactory.FixedSize(uiSize.Root, 120f);

            var boosterRow = UIFactory.CreateRect("Boosters", content);
            UIFactory.FixedSize(boosterRow, tuning.mainBoosterChipSize.y);
            boosterRow.gameObject.AddComponent<ResponsiveUILayout>().ConfigureFlow(CHIP_SPACING, tuning.mainBoosterChipSize.y, 0f, false);
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                BuildBoosterChip(factory, tuning, boosterRow, (BoosterType)i);
            }

            UIFactory.ScrollWithinSafeArea(content, safe, CONTENT_WIDTH, 1f, tuning.sideMargin);
            root.SetActive(false);
        }

        void BuildBoosterChip(UIFactory factory, UITuning tuning, Transform row, BoosterType type)
        {
            var chip = factory.Panel($"Booster {type}", row, tuning.panel, tuning.mainBoosterChipSize.y / 2f);
            chip.rectTransform.sizeDelta = tuning.mainBoosterChipSize;
            var icon = factory.Icon("Icon", chip.transform, UIStrings.BoosterArt(type), UIStrings.BoosterName(type),
                CHIP_ICON_FONT, tuning.ink);
            UIFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(CHIP_PADDING, 0f),
                new Vector2(CHIP_ICON_WIDTH, CHIP_ICON_HEIGHT));
            var count = factory.Text("Count", chip.transform, string.Empty, tuning.bodyFontSize, tuning.ink, FontStyles.Bold);
            count.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Stretch(count.rectTransform, CHIP_PADDING + CHIP_ICON_WIDTH, 0f, CHIP_PADDING, 0f);
            boosterChips[(int)type] = chip.gameObject;
            boosterCounts[(int)type] = count;
        }

        /// <summary>The Play button (first focus for gamepads).</summary>
        public UIButton PlayButton { get; }

        /// <summary>Shows or hides the screen.</summary>
        public void SetVisible(bool visible) => root.SetActive(visible);

        /// <summary>Updates the stats line from <paramref name="progress"/>.</summary>
        public void Refresh(ProgressService progress)
        {
            uiSize.SetLabel(string.Format(UIStrings.UI_SIZE_FORMAT, progress.UIScalePercent));
            sound.SetLabel(progress.Muted ? UIStrings.SOUND_OFF : UIStrings.SOUND_ON);
            motion.SetLabel(progress.ReduceMotion ? UIStrings.MOTION_REDUCED : UIStrings.MOTION_ON);
            stats.text = TmpText.Clean(string.Format(UIStrings.STATS_FORMAT,
                progress.TotalStars, progress.LevelsPassed, progress.LevelCount));
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
            {
                var type = (BoosterType)i;
                boosterChips[i].SetActive(progress.IsBoosterUnlocked(type));
                boosterCounts[i].text = TmpText.Clean(progress.BoosterCount(type).ToString());
            }
        }
    }
}
