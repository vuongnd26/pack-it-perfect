// Level select: every level with its number, best stars or a lock; locked levels cannot be clicked.
// Implements: design/game-brief.md "Progression & Meta" (bảng chọn lại màn), "Quyết định đã chốt" (Mở khóa tuần tự).
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

using System;
using System.Text;
using PackItPerfect.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Opaque full-screen level grid.</summary>
    public sealed class LevelSelectScreen
    {
        const float HEADER_TOP = 24f;
        const float HEADER_HEIGHT = 120f;
        const float HOME_WIDTH = 300f;
        const float TITLE_WIDTH = 500f;
        const float GRID_TOP = 200f;
        const float NUMBER_BOTTOM_SHARE = 0.38f;
        const float STARS_HEIGHT_SHARE = 0.36f;

        readonly UITuning tuning;
        readonly GameObject root;
        readonly UIButton[] cells;
        readonly TextMeshProUGUI[] starLabels;
        readonly Image[,] starIcons;
        readonly Image[] lockIcons;
        readonly StringBuilder builder = new StringBuilder();

        /// <summary>An unlocked level was clicked (1-based number).</summary>
        public event Action<int> LevelChosen;
        /// <summary>Home clicked.</summary>
        public event Action HomeClicked;

        /// <summary>Builds the (hidden) screen with <paramref name="levelCount"/> cells under <paramref name="parent"/>.</summary>
        public LevelSelectScreen(UIFactory factory, Transform parent, UITuning tuning, int levelCount)
        {
            this.tuning = tuning;
            var background = factory.Panel("Level Select", parent, tuning.menuBackground, 0f, raycast: true);
            UIFactory.Stretch(background.rectTransform);
            root = background.gameObject;
            factory.Backdrop(background.transform, "bg_menu");
            var safe = factory.SafeContent(background.transform);

            var home = factory.Button("Select Home", safe, UIStrings.HOME, ButtonStyle.Secondary, tuning.bodyFontSize,
                () => HomeClicked?.Invoke());
            UIFactory.Place(home.Root, new Vector2(0f, 1f), new Vector2(tuning.sideMargin, -HEADER_TOP),
                new Vector2(HOME_WIDTH, HEADER_HEIGHT));
            HomeButton = home;

            var title = factory.Text("Title", safe, UIStrings.LEVELS, tuning.headingFontSize, tuning.ink, FontStyles.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(1f, 1f), new Vector2(-tuning.sideMargin, -HEADER_TOP),
                new Vector2(TITLE_WIDTH, HEADER_HEIGHT));
            title.alignment = TextAlignmentOptions.Right;

            int columns = Mathf.Max(1, tuning.levelSelectColumns);
            int rows = Mathf.CeilToInt(levelCount / (float)columns);
            var gridRect = UIFactory.CreateRect("Grid", safe);
            var gridSize = new Vector2(
                columns * tuning.levelCellSize.x + (columns - 1) * tuning.levelCellSpacing.x,
                rows * tuning.levelCellSize.y + Mathf.Max(0, rows - 1) * tuning.levelCellSpacing.y);
            UIFactory.Place(gridRect, new Vector2(0.5f, 1f), new Vector2(0f, -GRID_TOP), gridSize);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = tuning.levelCellSize;
            grid.spacing = tuning.levelCellSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            cells = new UIButton[levelCount];
            starLabels = new TextMeshProUGUI[levelCount];
            starIcons = new Image[levelCount, ProgressService.MAX_STARS];
            lockIcons = new Image[levelCount];
            float cellHeight = tuning.levelCellSize.y - tuning.buttonShadowDepth;
            for (int i = 0; i < levelCount; i++)
            {
                int level = i + 1;
                var cell = factory.Button($"Level {level}", gridRect, level.ToString(), ButtonStyle.Secondary,
                    tuning.headingFontSize, () => LevelChosen?.Invoke(level));
                UIFactory.Stretch(cell.Label.rectTransform, 8f, cellHeight * NUMBER_BOTTOM_SHARE, 8f, 0f);
                var stars = factory.Text("Stars", cell.Face.transform, string.Empty, tuning.smallFontSize + 4f, Color.white);
                stars.textWrappingMode = TextWrappingModes.NoWrap;
                var starsRect = stars.rectTransform;
                starsRect.anchorMin = new Vector2(0f, 0f);
                starsRect.anchorMax = new Vector2(1f, 0f);
                starsRect.pivot = new Vector2(0.5f, 0f);
                starsRect.anchoredPosition = new Vector2(0f, cellHeight * 0.08f);
                starsRect.sizeDelta = new Vector2(0f, cellHeight * STARS_HEIGHT_SHARE);
                for (int s = 0; s < ProgressService.MAX_STARS; s++)
                {
                    var icon = factory.Art($"Star {s + 1}", cell.Face.transform, "ui_star");
                    if (icon != null)
                        UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0f), new Vector2((s - 1) * 64f, 24f), new Vector2(62f, 62f));
                    starIcons[i, s] = icon;
                }
                var lockIcon = factory.Art("Lock", cell.Face.transform, "ui_lock");
                if (lockIcon != null)
                    UIFactory.Place(lockIcon.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(70f, 70f));
                lockIcons[i] = lockIcon;
                cells[i] = cell;
                starLabels[i] = stars;
            }
            root.SetActive(false);
        }

        /// <summary>The Home button.</summary>
        public UIButton HomeButton { get; }

        /// <summary>Shows or hides the screen.</summary>
        public void SetVisible(bool visible) => root.SetActive(visible);

        /// <summary>First cell of <paramref name="level"/> (for gamepad focus), or null.</summary>
        public UIButton Cell(int level) => level >= 1 && level <= cells.Length ? cells[level - 1] : null;

        /// <summary>Updates lock state and best stars of every cell from <paramref name="progress"/>.</summary>
        public void Refresh(ProgressService progress)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int level = i + 1;
                bool unlocked = progress.IsUnlocked(level);
                cells[i].Interactable = unlocked;
                cells[i].Label.color = unlocked ? tuning.ink : tuning.muted;
                bool illustrated = starIcons[i, 0] != null && lockIcons[i] != null;
                starLabels[i].text = illustrated ? string.Empty : TmpText.Clean(unlocked ? Stars(progress.BestStars(level)) : UIStrings.LOCK);
                for (int s = 0; s < ProgressService.MAX_STARS; s++)
                {
                    var icon = starIcons[i, s];
                    if (icon == null) continue;
                    icon.gameObject.SetActive(unlocked);
                    icon.color = new Color(1f, 1f, 1f, s < progress.BestStars(level) ? 1f : tuning.emptyStarAlpha);
                }
                if (lockIcons[i] != null) lockIcons[i].gameObject.SetActive(!unlocked);
                cells[i].Face.color = !unlocked ? new Color32(0xEE, 0xE2, 0xCC, 0xFF)
                    : level == progress.NextLevelToPlay ? new Color32(0xD1, 0xF1, 0xDF, 0xFF) : tuning.panel;
            }
        }

        string Stars(int count)
        {
            builder.Clear();
            for (int i = 0; i < count; i++)
            {
                builder.Append(UIStrings.STAR);
            }
            return builder.ToString();
        }
    }
}
