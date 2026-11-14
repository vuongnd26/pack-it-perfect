// One tile of the booster dock: large icon on the left, the action's name on the right at a fixed readable size, and a
// plain high-contrast count pill on the top edge. Presentation only: it raises a click callback and never touches charges.
// Implements: design/game-brief.md "Nâng cấp Cozy Travel" (booster dock), "Readability B" (dock tiles).

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>A dock tile with an optional count pill and a locked look that keeps its name readable.</summary>
    public sealed class DockSlot
    {
        const float PADDING = 8f;
        const float LABEL_GAP = 6f;
        const float COMPACT_MIN_FONT_SHARE = 0.8f;
        const float BADGE_PADDING = 22f;
        const float BADGE_RIGHT = 6f;
        const float LOCK_SIZE = 44f;
        const float SHAKE_CYCLES = 2f;
        const float UNUSABLE_ICON_ALPHA = 0.4f;

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly RectTransform visual;
        readonly Graphic icon;
        readonly Color iconColor;
        readonly RectTransform badge;
        readonly Image badgeImage;
        readonly TextMeshProUGUI badgeText;
        readonly GameObject lockIcon;
        readonly RectTransform lockRect;
        bool laidOut;
        readonly object bounceKey = new object();
        readonly object shakeKey = new object();
        readonly object badgeKey = new object();
        int shownCount = -1;
        bool locked;
        bool usable = true;
        bool compact;
        readonly bool hasBadge;

        /// <summary>Builds the tile under <paramref name="parent"/>; the owner places and sizes <see cref="Button"/>'s root.</summary>
        /// <param name="id">Button id for the click-sound hook.</param>
        /// <param name="artName">Resources/Art icon name.</param>
        /// <param name="name">Action name shown beside the icon.</param>
        /// <param name="hasBadge">False for Undo (unlimited, no count).</param>
        public DockSlot(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator, string id,
            string artName, string name, bool hasBadge, Action onClick)
        {
            this.tuning = tuning;
            this.animator = animator;
            this.hasBadge = hasBadge;
            Button = factory.Button(id, parent, name, ButtonStyle.Secondary, tuning.dockLabelFontSize, onClick,
                tuning.cornerRadius * 0.8f);
            visual = (RectTransform)Button.Face.transform.parent;
            var face = Button.Face.transform;

            icon = factory.Icon("Icon", face, artName, string.Empty, tuning.dockLabelFontSize, tuning.ink);
            iconColor = icon.color;

            // The name never auto-shrinks: it wraps to a second line ("Auto Pack") instead.
            var label = Button.Label;
            label.enableAutoSizing = false;
            label.fontSize = tuning.dockLabelFontSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.lineSpacing = -26f;

            var lockArt = factory.Art("Lock", face, "ui_lock");
            if (lockArt != null)
            {
                lockRect = lockArt.rectTransform;
                lockIcon = lockArt.gameObject;
                lockIcon.SetActive(false);
            }

            if (hasBadge)
            {
                // Sits on the root, not the face, so the pressed tint and the unusable look never dim the number.
                badgeImage = factory.Panel("Badge", Button.Root, tuning.ink, tuning.badgeSize / 2f);
                badge = badgeImage.rectTransform;
                badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
                badge.pivot = new Vector2(1f, 1f);
                badge.anchoredPosition = new Vector2(BADGE_RIGHT, tuning.badgeRise);
                badgeText = factory.Text("Count", badge, string.Empty, tuning.badgeFontSize, Color.white, FontStyles.Bold);
                badgeText.textWrappingMode = TextWrappingModes.NoWrap;
                UIFactory.Stretch(badgeText.rectTransform);
            }
            SetCompact(false);
        }

        /// <summary>
        /// Wide tiles put the name beside the icon; compact tiles (four narrow columns at a large UI size) put the icon
        /// above the name, so the dock stays one row instead of doubling its height.
        /// </summary>
        public void SetCompact(bool isCompact)
        {
            if (laidOut && compact == isCompact) return;
            laidOut = true;
            compact = isCompact;
            var label = Button.Label;
            float badgeClearance = hasBadge ? Mathf.Max(0f, tuning.badgeSize - tuning.badgeRise - 30f) : 0f;
            if (compact)
            {
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -PADDING), Vector2.one * tuning.dockIconSize);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMax = tuning.dockLabelFontSize;
                label.fontSizeMin = tuning.dockLabelFontSize * COMPACT_MIN_FONT_SHARE;
                UIFactory.Stretch(label.rectTransform, 4f, 0f, 4f, PADDING + tuning.dockIconSize);
                if (lockRect != null)
                    UIFactory.Place(lockRect, new Vector2(0f, 1f), new Vector2(PADDING, -PADDING), Vector2.one * LOCK_SIZE);
            }
            else
            {
                UIFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(PADDING, 0f), Vector2.one * tuning.dockIconSize);
                label.alignment = TextAlignmentOptions.Left;
                // Full size whenever the words fit on one or two lines; a slightly narrower tile (tall phones) may
                // shrink a long single word a little rather than break it mid-word.
                label.enableAutoSizing = true;
                label.fontSizeMax = tuning.dockLabelFontSize;
                label.fontSizeMin = tuning.dockLabelFontSize * COMPACT_MIN_FONT_SHARE;
                UIFactory.Stretch(label.rectTransform, PADDING + tuning.dockIconSize + LABEL_GAP, 0f, PADDING, badgeClearance);
                if (lockRect != null)
                    UIFactory.Place(lockRect, new Vector2(0f, 0f), new Vector2(2f, 2f), Vector2.one * LOCK_SIZE);
            }
        }

        /// <summary>The tile's button (focus target).</summary>
        public UIButton Button { get; }

        /// <summary>Shows or hides the tile (the dock lays the rest out again).</summary>
        public void SetVisible(bool visible) => Button.Root.gameObject.SetActive(visible);

        /// <summary>True while the tile is shown.</summary>
        public bool IsVisible => Button.Root.gameObject.activeSelf;

        /// <summary>Locked: a padlock on the icon and "Lv N" in the pill (the level whose first clear opens it). The name stays.</summary>
        public void SetLocked(bool isLocked, int unlockLevel)
        {
            locked = isLocked;
            if (lockIcon != null) lockIcon.SetActive(isLocked);
            if (isLocked && badge != null)
            {
                shownCount = -1;
                SetBadge(string.Format(UIStrings.LOCK_LEVEL_FORMAT, unlockLevel), tuning.badgeEmptyColor);
            }
            ApplyLook();
        }

        /// <summary>Updates the count pill; pops it when the number changes after the first set.</summary>
        public void SetCount(int count)
        {
            if (badge == null || locked || count == shownCount)
            {
                return;
            }
            bool animate = shownCount >= 0 && !tuning.reduceMotion;
            shownCount = count;
            SetBadge(count > 99 ? UIStrings.BADGE_OVERFLOW : count.ToString(), count > 0 ? tuning.ink : tuning.badgeEmptyColor);
            if (animate)
            {
                UIMotion.Pop(animator, badgeKey, badge, 0f, tuning.badgePopDuration, tuning.badgePopStart, false);
            }
            else
            {
                animator.Cancel(badgeKey);
                badge.localScale = Vector3.one;
            }
        }

        void SetBadge(string text, Color color)
        {
            badgeText.text = TmpText.Clean(text);
            badgeImage.color = color;
            // The pill grows with its text; the number itself never shrinks.
            float width = Mathf.Max(tuning.badgeSize, badgeText.GetPreferredValues(badgeText.text).x + BADGE_PADDING * 2f);
            badge.sizeDelta = new Vector2(width, tuning.badgeSize);
        }

        /// <summary>Normal look when usable; a faded icon when not. Name and count stay at full contrast either way.</summary>
        public void SetUsable(bool isUsable)
        {
            usable = isUsable;
            ApplyLook();
        }

        void ApplyLook()
        {
            bool faded = locked || !usable;
            icon.color = faded ? new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * UNUSABLE_ICON_ALPHA) : iconColor;
        }

        /// <summary>Quick scale bounce after the tile's action ran. Skipped under reduced motion.</summary>
        public void PlayUse()
        {
            if (tuning.reduceMotion)
            {
                return;
            }
            var root = Button.Root;
            animator.Play(bounceKey, 0f, tuning.dockBounceDuration, t =>
            {
                float scale = t >= 1f ? 1f : 1f + tuning.dockBounceAmount * Mathf.Sin(t * Mathf.PI);
                root.localScale = new Vector3(scale, scale, 1f);
            });
        }

        /// <summary>Side-to-side shake for a refused tap. Skipped under reduced motion (the toast still explains).</summary>
        public void Shake()
        {
            if (tuning.reduceMotion)
            {
                return;
            }
            animator.Play(shakeKey, 0f, tuning.shakeDuration, t =>
            {
                float x = t >= 1f ? 0f : Mathf.Sin(t * Mathf.PI * 2f * SHAKE_CYCLES) * (1f - t) * tuning.shakeDistance;
                visual.anchoredPosition = new Vector2(x, 0f);
            });
        }

        /// <summary>Stops running tweens and resets scale and offset (call when the HUD hides).</summary>
        public void ResetMotion()
        {
            animator.Cancel(bounceKey);
            animator.Cancel(shakeKey);
            animator.Cancel(badgeKey);
            Button.Root.localScale = Vector3.one;
            visual.anchoredPosition = Vector2.zero;
            if (badge != null) badge.localScale = Vector3.one;
        }
    }
}
