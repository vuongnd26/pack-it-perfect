using NUnit.Framework;
using PackItPerfect.Meta;

namespace PackItPerfect.Tests
{
    /// <summary>Star shop: balance, prices, unlock gating and persistence, with an isolated in-memory save.</summary>
    [TestFixture, Category("Unit")]
    public sealed class ShopTests
    {
        const int LEVEL_COUNT = 8;
        const int HINT_PRICE = 2;
        const int FREEZE_PRICE = 3;
        const int AUTO_PACK_PRICE = 5;

        static ProgressionRules Rules() => new ProgressionRules(
            new BoosterBundle(0, 0, 0), new[] { 1, 6, 4 }, new BoosterBundle(0, 0, 0), PackingFixtures.FreezeSeconds,
            new int[0], new BoosterBundle[0], new[] { HINT_PRICE, AUTO_PACK_PRICE, FREEZE_PRICE });

        static ProgressService Service(MemorySaveStore store, int passedLevels, int starsEach = 3)
        {
            var progress = new ProgressService(store, LEVEL_COUNT, Rules());
            for (int level = 1; level <= passedLevels; level++) progress.RecordWin(level, starsEach);
            return progress;
        }

        /// <summary>The balance starts as every earned star; buying takes the price and adds one booster.</summary>
        [Test]
        public void Buy_UnlockedBoosterWithEnoughStars_SpendsPriceAndAddsOne()
        {
            var progress = Service(new MemorySaveStore(), passedLevels: 2); // 6 stars, Hint unlocked after level 1
            int before = progress.BoosterCount(BoosterType.Hint);

            var result = progress.TryBuy(BoosterType.Hint);

            Assert.That(result, Is.EqualTo(ProgressService.PurchaseResult.Bought));
            Assert.That(progress.StarBalance, Is.EqualTo(6 - HINT_PRICE));
            Assert.That(progress.StarsSpent, Is.EqualTo(HINT_PRICE));
            Assert.That(progress.BoosterCount(BoosterType.Hint), Is.EqualTo(before + 1));
        }

        /// <summary>Spending stars never lowers a level's best stars or the lifetime total.</summary>
        [Test]
        public void Buy_AfterPurchase_LeavesLevelStarsAndTotalUntouched()
        {
            var progress = Service(new MemorySaveStore(), passedLevels: 3);

            progress.TryBuy(BoosterType.Hint);

            Assert.That(progress.TotalStars, Is.EqualTo(9));
            for (int level = 1; level <= 3; level++) Assert.That(progress.BestStars(level), Is.EqualTo(3));
            Assert.That(progress.IsUnlocked(4), Is.True, "Level unlocks depend on passing, not on the balance.");
        }

        /// <summary>A purchase that costs more than the balance is refused and changes nothing.</summary>
        [Test]
        public void Buy_NotEnoughStars_IsRefusedAndChangesNothing()
        {
            var store = new MemorySaveStore();
            var progress = Service(store, passedLevels: 1, starsEach: 1); // 1 star, Hint costs 2
            int saves = store.SaveCount;

            var result = progress.TryBuy(BoosterType.Hint);

            Assert.That(result, Is.EqualTo(ProgressService.PurchaseResult.NotEnoughStars));
            Assert.That(progress.StarBalance, Is.EqualTo(1));
            Assert.That(progress.BoosterCount(BoosterType.Hint), Is.Zero);
            Assert.That(store.SaveCount, Is.EqualTo(saves), "A refused purchase writes nothing.");
        }

        /// <summary>A locked booster cannot be bought even with a full balance.</summary>
        [Test]
        public void Buy_LockedBooster_IsRefusedEvenWithEnoughStars()
        {
            var progress = Service(new MemorySaveStore(), passedLevels: 3); // Freeze opens after level 4

            var result = progress.TryBuy(BoosterType.Freeze);

            Assert.That(result, Is.EqualTo(ProgressService.PurchaseResult.Locked));
            Assert.That(progress.StarBalance, Is.EqualTo(9));
            Assert.That(progress.BoosterCount(BoosterType.Freeze), Is.Zero);
        }

        /// <summary>A booster with no price is not for sale.</summary>
        [Test]
        public void Buy_BoosterWithoutPrice_IsNotForSale()
        {
            var noShop = new ProgressService(new MemorySaveStore(), LEVEL_COUNT, PackingFixtures.Progression());
            noShop.RecordWin(1, 3);

            Assert.That(noShop.TryBuy(BoosterType.Hint), Is.EqualTo(ProgressService.PurchaseResult.NotForSale));
            Assert.That(noShop.Rules.HasShop, Is.False);
            Assert.That(Rules().HasShop, Is.True);
        }

        /// <summary>Several purchases can drain the balance exactly to zero but never below.</summary>
        [Test]
        public void Buy_RepeatedUntilEmpty_StopsAtZeroBalance()
        {
            var progress = Service(new MemorySaveStore(), passedLevels: 2, starsEach: 2); // 4 stars: two Hints

            Assert.That(progress.TryBuy(BoosterType.Hint), Is.EqualTo(ProgressService.PurchaseResult.Bought));
            Assert.That(progress.TryBuy(BoosterType.Hint), Is.EqualTo(ProgressService.PurchaseResult.Bought));
            Assert.That(progress.TryBuy(BoosterType.Hint), Is.EqualTo(ProgressService.PurchaseResult.NotEnoughStars));

            Assert.That(progress.StarBalance, Is.Zero);
            Assert.That(progress.StarsSpent, Is.EqualTo(4));
        }

        /// <summary>Balance, stock and spent stars survive a restart.</summary>
        [Test]
        public void Purchase_Reloaded_KeepsBalanceAndBoosters()
        {
            var store = new MemorySaveStore();
            var first = Service(store, passedLevels: 4);
            first.TryBuy(BoosterType.Freeze);
            first.TryBuy(BoosterType.Hint);

            var reloaded = new ProgressService(store, LEVEL_COUNT, Rules());

            Assert.That(reloaded.StarsSpent, Is.EqualTo(FREEZE_PRICE + HINT_PRICE));
            Assert.That(reloaded.StarBalance, Is.EqualTo(12 - FREEZE_PRICE - HINT_PRICE));
            Assert.That(reloaded.BoosterCount(BoosterType.Freeze), Is.EqualTo(1));
            Assert.That(reloaded.BoosterCount(BoosterType.Hint), Is.EqualTo(1));
        }

        /// <summary>An old save without the field behaves as if nothing was ever spent.</summary>
        [Test]
        public void OldSaveWithoutSpentField_StartsWithFullBalance()
        {
            var old = new ProgressData { bestStars = new[] { 3, 3, 0, 0, 0, 0, 0, 0 }, startingBoostersGranted = true };
            old.boosters = new int[BoosterBundle.TYPE_COUNT];

            var progress = new ProgressService(new MemorySaveStore(old), LEVEL_COUNT, Rules());

            Assert.That(progress.StarsSpent, Is.Zero);
            Assert.That(progress.StarBalance, Is.EqualTo(6));
        }

        /// <summary>A damaged save cannot owe more stars than were earned: the debt is clamped, never negative.</summary>
        [Test]
        public void DamagedSaveWithHugeSpent_ClampsToEarnedStars()
        {
            var damaged = new ProgressData { bestStars = new[] { 3, 0, 0, 0, 0, 0, 0, 0 }, starsSpent = 999,
                startingBoostersGranted = true, boosters = new int[BoosterBundle.TYPE_COUNT] };

            var progress = new ProgressService(new MemorySaveStore(damaged), LEVEL_COUNT, Rules());

            Assert.That(progress.StarsSpent, Is.EqualTo(3));
            Assert.That(progress.StarBalance, Is.Zero);
        }

        /// <summary>A new best on a replayed level raises the balance by the difference, and a purchase raises one event.</summary>
        [Test]
        public void StarsChanged_FiresOnNewBestAndOnPurchase()
        {
            var progress = Service(new MemorySaveStore(), passedLevels: 1, starsEach: 2);
            int events = 0;
            progress.StarsChanged += () => events++;

            progress.RecordWin(1, 3);
            progress.RecordWin(1, 3); // no improvement: no event
            progress.TryBuy(BoosterType.Hint);

            Assert.That(events, Is.EqualTo(2));
            Assert.That(progress.StarBalance, Is.EqualTo(3 - HINT_PRICE));
        }

        /// <summary>The shipped prices parse from progression.json and every price is positive.</summary>
        [Test]
        public void ShippedProgressionJson_DefinesPositivePricesForEveryBooster()
        {
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(ProgressionRules.RESOURCE);
            Assert.That(asset, Is.Not.Null);

            var rules = ProgressionRules.FromJson(asset.text, out string error);

            Assert.That(error, Is.Null);
            Assert.That(rules.HasShop, Is.True);
            for (int i = 0; i < BoosterBundle.TYPE_COUNT; i++)
                Assert.That(rules.ShopPrice((BoosterType)i), Is.GreaterThan(0), $"Booster {(BoosterType)i} has no price.");
        }
    }
}
