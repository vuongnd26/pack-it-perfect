// Main screen (the app opens here): title, tagline, total stars + levels passed, Play, Levels.
// Implements: design/game-brief.md "Progression & Meta" ("Màn hình chính hiện tổng số sao và số màn đã qua").
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

using System;
using PackItPerfect.Meta;
using TMPro;
using UnityEngine;

namespace PackItPerfect.UI
{
    /// <summary>Opaque full-screen main menu.</summary>
    public sealed class MainScreen
    {
        const float CONTENT_WIDTH = 900f;
        const float LOGO_Y = 535f;
        const float LOGO_HEIGHT = 640f;
        const float TITLE_HEIGHT = 300f;
        const float TAGLINE_Y = 130f;
        const float TAGLINE_HEIGHT = 120f;
        const float STATS_Y = -10f;
        const float STATS_HEIGHT = 130f;
        const float PLAY_Y = -210f;
        const float PLAY_HEIGHT = 190f;
        const float LEVELS_Y = -410f;
        const float LEVELS_HEIGHT = 150f;
        const float PREFS_Y = -590f;

        readonly GameObject root;
        readonly TextMeshProUGUI stats;
        readonly UIButton sound;
        readonly UIButton motion;

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
            var centre = new Vector2(0.5f, 0.5f);

            // Illustrated logo (suitcase + lettering); plain title text if the art is missing.
            var logo = factory.Art("Logo", safe, "logo");
            if (logo != null)
            {
                UIFactory.Place(logo.rectTransform, centre, new Vector2(0f, LOGO_Y), new Vector2(CONTENT_WIDTH, LOGO_HEIGHT));
            }
            else
            {
                var title = factory.Text("Title", safe, UIStrings.TITLE, tuning.titleFontSize, tuning.ink, FontStyles.Bold);
                UIFactory.Place(title.rectTransform, centre, new Vector2(0f, LOGO_Y), new Vector2(CONTENT_WIDTH, TITLE_HEIGHT));
            }

            var tagline = factory.Text("Tagline", safe, UIStrings.TAGLINE, tuning.bodyFontSize, tuning.ink);
            UIFactory.Place(tagline.rectTransform, centre, new Vector2(0f, TAGLINE_Y), new Vector2(CONTENT_WIDTH, TAGLINE_HEIGHT));

            var statsCard = factory.Panel("Stats", safe, tuning.panel, STATS_HEIGHT / 2f);
            UIFactory.Place(statsCard.rectTransform, centre, new Vector2(0f, STATS_Y), new Vector2(CONTENT_WIDTH, STATS_HEIGHT));
            stats = factory.Text("Stats Text", statsCard.transform, string.Empty, tuning.bodyFontSize + 4f, tuning.ink, FontStyles.Bold);
            stats.textWrappingMode = TextWrappingModes.NoWrap;
            stats.enableAutoSizing = true;
            stats.fontSizeMax = tuning.bodyFontSize + 4f;
            stats.fontSizeMin = tuning.smallFontSize;
            UIFactory.Stretch(stats.rectTransform, tuning.sideMargin, 0f, tuning.sideMargin, 0f);

            PlayButton = factory.Button("Main Play", safe, UIStrings.PLAY, ButtonStyle.Primary, tuning.buttonFontSize + 12f,
                () => PlayClicked?.Invoke());
            UIFactory.Place(PlayButton.Root, centre, new Vector2(0f, PLAY_Y), new Vector2(CONTENT_WIDTH, PLAY_HEIGHT));

            var levels = factory.Button("Main Levels", safe, UIStrings.LEVELS, ButtonStyle.Secondary, tuning.buttonFontSize,
                () => LevelsClicked?.Invoke());
            UIFactory.Place(levels.Root, centre, new Vector2(0f, LEVELS_Y), new Vector2(CONTENT_WIDTH, LEVELS_HEIGHT));

            sound = factory.Button("Main Sound", safe, UIStrings.SOUND_ON, ButtonStyle.Secondary, tuning.smallFontSize,
                () => SoundClicked?.Invoke());
            UIFactory.Place(sound.Root, centre, new Vector2(-230f, PREFS_Y), new Vector2(440f, 120f));
            motion = factory.Button("Main Motion", safe, UIStrings.MOTION_ON, ButtonStyle.Secondary, tuning.smallFontSize,
                () => MotionClicked?.Invoke());
            UIFactory.Place(motion.Root, centre, new Vector2(230f, PREFS_Y), new Vector2(440f, 120f));

            root.SetActive(false);
        }

        /// <summary>The Play button (first focus for gamepads).</summary>
        public UIButton PlayButton { get; }

        /// <summary>Shows or hides the screen.</summary>
        public void SetVisible(bool visible) => root.SetActive(visible);

        /// <summary>Updates the stats line from <paramref name="progress"/>.</summary>
        public void Refresh(ProgressService progress)
        {
            sound.SetLabel(progress.Muted ? UIStrings.SOUND_OFF : UIStrings.SOUND_ON);
            motion.SetLabel(progress.ReduceMotion ? UIStrings.MOTION_REDUCED : UIStrings.MOTION_ON);
            stats.text = TmpText.Clean(string.Format(UIStrings.STATS_FORMAT,
                progress.TotalStars, progress.LevelsPassed, progress.LevelCount));
        }
    }
}
