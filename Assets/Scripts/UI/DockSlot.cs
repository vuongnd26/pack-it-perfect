// One round slot of the booster dock: icon (art or text fallback), count badge, lock overlay with "Lv N", disabled look,
// plus bounce / badge-pop / shake feedback. Presentation only: it raises a click callback and never touches charges.
// Implements: design/game-brief.md "Nâng cấp Cozy Travel" (booster dock).

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>A round dock button with an optional count badge and a lock overlay.</summary>
    public sealed class DockSlot
    {
        const float ICON_INSET = 24f;
        const float LOCK_ICON_OFFSET = 16f;
        const float LOCK_LABEL_OFFSET = -26f;
        const float LOCK_LABEL_HEIGHT = 36f;
        const float SHAKE_CYCLES = 2f;
        const float DISABLED_ICON_ALPHA_SHARE = 1f;
        static readonly Color GreyBadge = new Color(0.7f, 0.7f, 0.7f, 1f);

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly RectTransform visual;
        readonly Graphic icon;
        readonly Color iconColor;
        readonly Color faceColor;
        readonly RectTransform badge;
        readonly Image badgeImage;
        readonly bool badgeIsArt;
        readonly TextMeshProUGUI badgeText;
        readonly GameObject lockVeil;
        readonly TextMeshProUGUI lockLabel;
        readonly object bounceKey = new object();
        readonly object shakeKey = new object();
        readonly object badgeKey = new object();
        int shownCount = -1;
        bool locked;

        /// <summary>Builds the slot under <paramref name="parent"/>; a layout group places <see cref="Button"/>'s root.</summary>
        /// <param name="id">Button id for the click-sound hook.</param>
        /// <param name="artName">Resources/Art icon name.</param>
        /// <param name="fallbackText">Text shown when the art is missing.</param>
        /// <param name="hasBadge">False for Undo (unlimited, no count).</param>
        public DockSlot(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator, string id,
            string artName, string fallbackText, bool hasBadge, Action onClick)
        {
            this.tuning = tuning;
            this.animator = animator;
            float size = tuning.dockSlotSize;
            Button = factory.RoundButton(id, parent, fallbackText, ButtonStyle.Secondary, tuning.dockLabelFontSize, size, onClick);
            Button.Root.sizeDelta = new Vector2(size, size);
            Button.Label.gameObject.SetActive(false);
            visual = (RectTransform)Button.Face.transform.parent;
            faceColor = Button.Face.color;
            var face = Button.Face.transform;

            icon = factory.Icon("Icon", face, artName, fallbackText, tuning.dockLabelFontSize, tuning.ink);
            bool illustrated = icon is Image;
            UIFactory.Stretch(icon.rectTransform, ICON_INSET, illustrated ? tuning.dockLabelFontSize + 12f : ICON_INSET,
                ICON_INSET, ICON_INSET);
            iconColor = icon.color;
            // Names stay visible even when the icon exists; fallback text already supplies the name otherwise.
            if (illustrated)
            {
                Button.Label.gameObject.SetActive(true);
                UIFactory.TopStrip(Button.Label.rectTransform, size - tuning.buttonShadowDepth - tuning.dockLabelFontSize - 8f,
                    tuning.dockLabelFontSize + 4f, 4f);
                Button.Label.fontSizeMin = tuning.dockLabelFontSize;
                Button.Label.fontSizeMax = tuning.dockLabelFontSize;
            }

            float radius = Mathf.Max(1f, (size - tuning.buttonShadowDepth) / 2f);
            if (hasBadge)
            {
                var art = factory.Art("Badge", face, "ui_badge");
                badgeIsArt = art != null;
                badgeImage = art != null ? art : factory.Panel("Badge", face, tuning.badgeColor, tuning.badgeSize / 2f);
                badge = badgeImage.rectTransform;
                UIFactory.Place(badge, Vector2.one, tuning.badgeOffset, new Vector2(tuning.badgeSize, tuning.badgeSize));
                badge.pivot = new Vector2(0.5f, 0.5f);
                badge.anchoredPosition = new Vector2(tuning.badgeOffset.x - tuning.badgeSize / 2f,
                    tuning.badgeOffset.y - tuning.badgeSize / 2f);
                badgeText = factory.Text("Count", badge, string.Empty, tuning.badgeFontSize,
                    badgeIsArt ? tuning.ink : Color.white, FontStyles.Bold);
                badgeText.textWrappingMode = TextWrappingModes.NoWrap;
                badgeText.enableAutoSizing = true;
                badgeText.fontSizeMax = tuning.badgeFontSize;
                badgeText.fontSizeMin = tuning.badgeFontSize * 0.6f;
                UIFactory.Stretch(badgeText.rectTransform, 4f, 0f, 4f, 0f);
            }

            var veil = factory.Panel("Lock Veil", face, tuning.dockLockVeil, radius);
            UIFactory.Stretch(veil.rectTransform);
            lockVeil = veil.gameObject;
            var lockArt = factory.Art("Lock Icon", veil.transform, "ui_lock");
            if (lockArt != null)
            {
                UIFactory.Place(lockArt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, LOCK_ICON_OFFSET),
                    Vector2.one * tuning.lockIconSize);
                lockArt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            lockLabel = factory.Text("Lock Level", veil.transform, string.Empty, tuning.lockLabelFontSize, Color.white, FontStyles.Bold);
            lockLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Place(lockLabel.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(0f, lockArt != null ? LOCK_LABEL_OFFSET : 0f), new Vector2(size, LOCK_LABEL_HEIGHT));
            lockLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lockVeil.SetActive(false);
        }

        /// <summary>The slot's button (focus target).</summary>
        public UIButton Button { get; }

        /// <summary>Shows or hides the slot (a layout group recentres the rest).</summary>
        public void SetVisible(bool visible) => Button.Root.gameObject.SetActive(visible);

        /// <summary>True while the slot is shown.</summary>
        public bool IsVisible => Button.Root.gameObject.activeSelf;

        /// <summary>Shows the lock overlay with "Lv N" (the level whose first clear opens it), or removes it.</summary>
        public void SetLocked(bool isLocked, int unlockLevel)
        {
            locked = isLocked;
            // The lock explanation replaces the action label instead of drawing both on top of each other.
            Button.Label.gameObject.SetActive(!isLocked && icon is Image);
            lockVeil.SetActive(isLocked);
            if (isLocked)
            {
                lockLabel.text = TmpText.Clean(string.Format(UIStrings.LOCK_LEVEL_FORMAT, unlockLevel));
            }
            if (badge != null)
            {
                badge.gameObject.SetActive(!isLocked);
            }
        }

        /// <summary>Updates the count badge; pops it when the number changes after the first set.</summary>
        public void SetCount(int count)
        {
            if (badge == null || count == shownCount)
            {
                return;
            }
            bool animate = shownCount >= 0 && !tuning.reduceMotion;
            shownCount = count;
            badgeText.text = TmpText.Clean(count > 99 ? UIStrings.BADGE_OVERFLOW : count.ToString());
            badgeImage.color = count > 0 ? (badgeIsArt ? Color.white : tuning.badgeColor)
                : (badgeIsArt ? GreyBadge : tuning.badgeEmptyColor);
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

        /// <summary>Normal look when usable, dimmed when not (the button still accepts taps to explain why).</summary>
        public void SetUsable(bool usable)
        {
            bool dim = !usable && !locked;
            Button.Face.color = dim ? Multiply(faceColor, tuning.dockDisabledTint) : faceColor;
            icon.color = dim ? Multiply(iconColor, tuning.dockDisabledTint) : iconColor;
        }

        /// <summary>Quick scale bounce after the slot's action ran. Skipped under reduced motion.</summary>
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

        static Color Multiply(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a * DISABLED_ICON_ALPHA_SHARE);
    }
}
