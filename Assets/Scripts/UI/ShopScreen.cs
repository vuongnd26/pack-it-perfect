// Shop: spend stars on boosters. A full-screen overlay opened from the main, win and pause screens; closing it
// returns to whatever was underneath. Presentation only: it asks ProgressService to buy and shows the answer.
// Implements: design/game-brief.md "Booster" (cửa hàng sao, quyết định 2026-11-13).

using System;
using PackItPerfect.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackItPerfect.UI
{
    /// <summary>Opaque overlay with the star balance and one card per booster: icon, name, description, stock and a Buy button.</summary>
    public sealed class ShopScreen
    {
        const float CONTENT_WIDTH = 900f;
        const float HEADER_HEIGHT = 120f;
        const float BALANCE_HEIGHT = 110f;
        const float HINT_HEIGHT = 56f;
        const float CARD_HEIGHT = 350f;
        const float CARD_SPACING = 24f;
        const float CLOSE_HEIGHT = 130f;
        const float ICON_SIZE = 120f;
        const float ICON_LEFT = 54f;
        const float ICON_TOP = 48f;
        const float BUY_WIDTH = 260f;
        const float BUY_HEIGHT = 104f;
        const float BUY_RIGHT = 54f;
        const float BUY_BOTTOM = 56f;
        const float TEXT_GAP = 20f;
        const float TEXT_RIGHT = 54f;
        const float MIN_FONT_SHARE = 0.6f;

        sealed class Card
        {
            public RectTransform root;
            public UIButton buy;
            public TextMeshProUGUI owned;
            public TextMeshProUGUI description;
            public TextMeshProUGUI locked;
            public readonly object popKey = new object();
        }

        readonly UITuning tuning;
        readonly UIAnimator animator;
        readonly GameObject root;
        readonly TextMeshProUGUI balance;
        readonly Card[] cards = new Card[BoosterBundle.TYPE_COUNT];
        readonly object balanceKey = new object();
        readonly object messageKey = new object();
        readonly RectTransform balancePill;
        readonly TextMeshProUGUI message;

        /// <summary>A Buy button was clicked (the type); the owner buys and calls <see cref="Refresh"/>.</summary>
        public event Action<BoosterType> BuyClicked;
        /// <summary>The Back button was clicked.</summary>
        public event Action CloseClicked;

        /// <summary>Canvas group of the whole overlay (fade-in).</summary>
        public CanvasGroup Group { get; }

        /// <summary>The Back button (first focus for gamepads).</summary>
        public UIButton CloseButton { get; }

        /// <summary>
        /// The control that gets the first gamepad/keyboard focus: the first Buy button. Focusing Back instead made the
        /// scroll area jump to the bottom and hide the title and the star balance.
        /// </summary>
        public UIButton FirstFocus => cards[0].buy;

        /// <summary>True while the shop is on screen.</summary>
        public bool IsVisible => root.activeSelf;

        /// <summary>Builds the (hidden) shop under <paramref name="parent"/>, above every other screen.</summary>
        public ShopScreen(UIFactory factory, Transform parent, UITuning tuning, UIAnimator animator)
        {
            this.tuning = tuning;
            this.animator = animator;
            var background = factory.Panel("Shop Screen", parent, tuning.menuBackground, 0f, raycast: true);
            UIFactory.Stretch(background.rectTransform);
            root = background.gameObject;
            UIFactory.Isolate(root);
            Group = UIFactory.Group(root);
            factory.Backdrop(background.transform, "bg_menu");
            var safe = factory.SafeContent(background.transform);
            var content = UIFactory.CreateRect("Shop Content", safe);
            UIFactory.StackVertically(content, 16, CARD_SPACING);

            var title = factory.Text("Title", content, UIStrings.SHOP_TITLE, tuning.headingFontSize, tuning.ink, FontStyles.Bold);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FixedSize(title.rectTransform, HEADER_HEIGHT);

            var pill = factory.Panel("Balance", content, tuning.panel, BALANCE_HEIGHT / 2f);
            balancePill = pill.rectTransform;
            UIFactory.FixedSize(balancePill, BALANCE_HEIGHT);
            balance = factory.Text("Balance Text", pill.transform, string.Empty, tuning.headingFontSize * 0.8f, tuning.ink, FontStyles.Bold);
            balance.textWrappingMode = TextWrappingModes.NoWrap;
            balance.enableAutoSizing = true;
            balance.fontSizeMax = tuning.headingFontSize * 0.8f;
            balance.fontSizeMin = tuning.smallFontSize;
            UIFactory.Stretch(balance.rectTransform, tuning.sideMargin, 0f, tuning.sideMargin, 0f);

            var hint = factory.Text("Hint", content, UIStrings.SHOP_HINT, tuning.smallFontSize, tuning.muted);
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            hint.enableAutoSizing = true;
            hint.fontSizeMax = tuning.smallFontSize;
            hint.fontSizeMin = tuning.smallFontSize * MIN_FONT_SHARE;
            UIFactory.FixedSize(hint.rectTransform, HINT_HEIGHT);

            // Purchase results appear here, inside the shop (the HUD toast is hidden behind this screen).
            message = factory.Text("Message", content, string.Empty, tuning.smallFontSize, tuning.primaryShadow, FontStyles.Bold);
            message.textWrappingMode = TextWrappingModes.NoWrap;
            message.enableAutoSizing = true;
            message.fontSizeMax = tuning.smallFontSize;
            message.fontSizeMin = tuning.smallFontSize * MIN_FONT_SHARE;
            UIFactory.FixedSize(message.rectTransform, HINT_HEIGHT);

            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = BuildCard(factory, content, (BoosterType)i);
            }

            CloseButton = factory.Button("Shop Close", content, UIStrings.SHOP_CLOSE, ButtonStyle.Primary, tuning.buttonFontSize,
                () => CloseClicked?.Invoke());
            UIFactory.FixedSize(CloseButton.Root, CLOSE_HEIGHT);

            UIFactory.ScrollWithinSafeArea(content, safe, CONTENT_WIDTH, 1f, tuning.sideMargin);
            root.SetActive(false);
        }

        Card BuildCard(UIFactory factory, Transform parent, BoosterType type)
        {
            var card = new Card();
            var panel = factory.Panel($"Shop {type}", parent, tuning.panel, tuning.cardRadius * 0.6f);
            factory.StyleCard(panel);
            card.root = panel.rectTransform;
            UIFactory.FixedSize(card.root, CARD_HEIGHT);

            // Two rows so the words get the full card width on any screen: icon, name and description on top,
            // stock on the left and the Buy button on the right below.
            var icon = factory.Icon("Icon", card.root, UIStrings.BoosterArt(type), UIStrings.BoosterName(type),
                tuning.smallFontSize, tuning.ink);
            UIFactory.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(ICON_LEFT, -ICON_TOP), Vector2.one * ICON_SIZE);

            float textLeft = ICON_LEFT + ICON_SIZE + TEXT_GAP;
            var name = Label(factory, card.root, "Name", UIStrings.BoosterName(type), tuning.bodyFontSize, tuning.ink,
                FontStyles.Bold, textLeft, TEXT_RIGHT, 0.72f, 0.95f, false);
            card.description = Label(factory, card.root, "Description", UIStrings.BoosterDescription(type, 10f), tuning.smallFontSize - 4f,
                tuning.muted, FontStyles.Normal, textLeft, TEXT_RIGHT, 0.43f, 0.73f, true);
            float stockRight = BUY_WIDTH + BUY_RIGHT + TEXT_GAP;
            card.owned = Label(factory, card.root, "Owned", string.Empty, tuning.smallFontSize - 4f, tuning.primaryShadow,
                FontStyles.Bold, ICON_LEFT, stockRight, 0.03f, 0.36f, false);
            // While locked this replaces the stock line: the card says why it cannot be bought.
            card.locked = Label(factory, card.root, "Locked", string.Empty, tuning.smallFontSize - 4f, tuning.danger,
                FontStyles.Bold, ICON_LEFT, stockRight, 0.03f, 0.36f, false);
            name.text = TmpText.Clean(UIStrings.BoosterName(type));

            card.buy = factory.Button($"Shop Buy {type}", card.root, string.Empty, ButtonStyle.Accent, tuning.buttonFontSize - 8f,
                () => BuyClicked?.Invoke(type));
            UIFactory.Place(card.buy.Root, new Vector2(1f, 0f), new Vector2(-BUY_RIGHT, BUY_BOTTOM), new Vector2(BUY_WIDTH, BUY_HEIGHT));
            return card;
        }

        TextMeshProUGUI Label(UIFactory factory, RectTransform parent, string name, string text, float size, Color color,
            FontStyles style, float left, float right, float minY, float maxY, bool wrap)
        {
            var label = factory.Text(name, parent, text, size, color, style, TextAlignmentOptions.Left);
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = size * MIN_FONT_SHARE;
            label.overflowMode = TextOverflowModes.Truncate;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, minY);
            rect.anchorMax = new Vector2(1f, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
            return label;
        }

        /// <summary>Shows the shop (fade-in is played by the owner).</summary>
        public void Show(ProgressService progress)
        {
            root.SetActive(true);
            Refresh(progress);
        }

        /// <summary>Hides the shop.</summary>
        public void Hide()
        {
            animator.Cancel(balanceKey);
            animator.Cancel(messageKey);
            message.text = string.Empty;
            balancePill.localScale = Vector3.one;
            foreach (var card in cards)
            {
                animator.Cancel(card.popKey);
                animator.Cancel(card.root);
                card.root.localScale = Vector3.one;
                card.buy.Root.anchoredPosition = new Vector2(-BUY_RIGHT, BUY_BOTTOM);
            }
            root.SetActive(false);
        }

        /// <summary>Updates the balance, stock, prices and which Buy buttons can be pressed.</summary>
        public void Refresh(ProgressService progress)
        {
            balance.text = TmpText.Clean(string.Format(UIStrings.SHOP_BALANCE_FORMAT, progress.StarBalance));
            for (int i = 0; i < cards.Length; i++)
            {
                var type = (BoosterType)i;
                var card = cards[i];
                int price = progress.ShopPrice(type);
                bool forSale = price > 0;
                bool unlocked = progress.IsBoosterUnlocked(type);
                bool affordable = progress.StarBalance >= price;
                card.root.gameObject.SetActive(forSale);
                card.description.text = TmpText.Clean(UIStrings.BoosterDescription(type, progress.Rules.FreezeSeconds));
                card.owned.text = TmpText.Clean(string.Format(UIStrings.SHOP_OWNED_FORMAT, progress.BoosterCount(type)));
                card.owned.gameObject.SetActive(unlocked);
                card.locked.gameObject.SetActive(!unlocked);
                card.locked.text = TmpText.Clean(string.Format(UIStrings.SHOP_LOCKED_FORMAT, progress.Rules.UnlockAfterLevel(type)));
                card.buy.SetLabel(string.Format(UIStrings.SHOP_PRICE_FORMAT, price));
                // A button that cannot be pressed still looks pressable-but-dim and still answers a tap with a reason.
                card.buy.Face.color = unlocked && affordable ? tuning.accent : new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }

        /// <summary>Shows a short result line (bought, or why not) that fades after <see cref="UITuning.toastHold"/> seconds.</summary>
        public void ShowMessage(string text, bool good)
        {
            message.text = TmpText.Clean(text);
            message.color = good ? tuning.primaryShadow : tuning.danger;
            animator.Play(messageKey, tuning.toastHold, tuning.toastFade, t => message.alpha = 1f - t,
                () => message.text = string.Empty);
        }

        /// <summary>Pops the card of a booster that was just bought and bumps the balance.</summary>
        public void PlayBought(BoosterType type)
        {
            if (tuning.reduceMotion) return;
            UIMotion.Pulse(animator, cards[(int)type].popKey, cards[(int)type].root, tuning.timerPulse * 1.4f, tuning.pulseAmount * 0.5f, false);
            UIMotion.Pulse(animator, balanceKey, balancePill, tuning.timerPulse, tuning.pulseAmount, false);
        }

        /// <summary>Shakes a card whose purchase was refused.</summary>
        public void PlayRefused(BoosterType type)
        {
            if (tuning.reduceMotion) return;
            var rect = cards[(int)type].buy.Root;
            var key = cards[(int)type].root;
            animator.Play(key, 0f, tuning.shakeDuration, t =>
            {
                float x = t >= 1f ? 0f : Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * tuning.shakeDistance;
                rect.anchoredPosition = new Vector2(-BUY_RIGHT + x, BUY_BOTTOM);
            });
        }
    }
}
