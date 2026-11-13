using System.Collections.Generic;
using NUnit.Framework;
using PackItPerfect.Meta;

namespace PackItPerfect.Tests
{
    /// <summary>Manual-clock timer tests and isolated in-memory progression/save contracts.</summary>
    [TestFixture, Category("Unit")]
    public sealed class TimerProgressionTests
    {
        /// <summary>Time beyond the start delay reaches the countdown instead of being discarded.</summary>
        [Test]
        public void StartDelay_ConsumesOnlyDelayThenCountsOverflow()
        {
            var timer = Timer();
            timer.Start(PackingFixtures.CountdownSeconds, PackingFixtures.StartDelay);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.False);

            timer.Tick(PackingFixtures.StartDelay + 0.5f);

            Assert.That(timer.IsCounting, Is.True);
            Assert.That(timer.Remaining, Is.EqualTo(PackingFixtures.CountdownSeconds - 0.5f).Within(PackingFixtures.Tolerance));
            Assert.That(timer.SecondsLeft, Is.EqualTo(PackingFixtures.CountdownSeconds));
        }

        /// <summary>Freeze absorbs elapsed time first and forwards only its overflow to the clock.</summary>
        [Test]
        public void FreezeOverflow_ResumesCountdownAndEmitsOneEndEvent()
        {
            var timer = Timer();
            var changes = new List<bool>();
            timer.FreezeChanged += changes.Add;
            timer.Start(PackingFixtures.CountdownSeconds, 0f);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.True);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.False, "Freeze never stacks.");
            timer.Tick(4f);
            Assert.That(timer.FrozenRemaining, Is.EqualTo(6f));
            Assert.That(timer.Remaining, Is.EqualTo(PackingFixtures.CountdownSeconds));

            timer.Tick(7f);

            Assert.That(timer.IsFrozen, Is.False);
            Assert.That(timer.FrozenRemaining, Is.Zero);
            Assert.That(timer.Remaining, Is.EqualTo(PackingFixtures.CountdownSeconds - 1f));
            Assert.That(changes, Is.EqualTo(new[] { true, false }));
        }

        /// <summary>Stopped, delayed, expired and untimed clocks reject Freeze; positive counting clocks accept it.</summary>
        [Test]
        public void FreezeAvailability_RejectsStoppedDelayedExpiredAndUntimedClocks()
        {
            var timer = Timer();
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.False);
            timer.Start(PackingFixtures.CountdownSeconds, PackingFixtures.StartDelay);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.False);
            timer.Tick(PackingFixtures.StartDelay);
            Assert.That(timer.Freeze(0f), Is.False);
            Assert.That(timer.Freeze(-1f), Is.False);
            timer.Tick(PackingFixtures.CountdownSeconds);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.False);
            timer.Start(0, 0f);

            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.False);
        }

        /// <summary>Real elapsed active time is not arbitrarily clamped and expiry fires exactly once.</summary>
        [Test]
        public void LargeActiveDelta_ExpiresExactlyOnceWithoutClampingTime()
        {
            var timer = Timer();
            int expired = 0;
            timer.Expired += () => expired++;
            timer.Start(3, 0f);

            timer.Tick(5f);
            timer.Tick(5f);

            Assert.That(timer.Remaining, Is.Zero);
            Assert.That(timer.SecondsLeft, Is.Zero);
            Assert.That(timer.IsRunning, Is.False);
            Assert.That(expired, Is.EqualTo(1));
        }

        /// <summary>Win-stop before the frame's next tick keeps the displayed remainder and cannot expire.</summary>
        [Test]
        public void StopBeforeExpiryTick_KeepsRemainingTimeAndSuppressesExpiry()
        {
            var timer = Timer();
            int expired = 0;
            timer.Expired += () => expired++;
            timer.Start(1, 0f);

            timer.Stop();
            timer.Tick(1f);

            Assert.That(timer.IsRunning, Is.False);
            Assert.That(timer.Remaining, Is.EqualTo(1f));
            Assert.That(expired, Is.Zero);
        }

        /// <summary>No Tick during an owner-managed pause preserves both countdown and active Freeze.</summary>
        [Test]
        public void OwnerWithholdsTicksDuringPause_ResumeContinuesFrozenRemainder()
        {
            var timer = Timer();
            timer.Start(PackingFixtures.CountdownSeconds, 0f);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.True);
            timer.Tick(4f);
            float remaining = timer.Remaining;
            float frozen = timer.FrozenRemaining;
            // Pause is owned by PackingGame: no wall clock, sleeps or synthetic Tick during that interval.
            Assert.That(timer.Remaining, Is.EqualTo(remaining));
            Assert.That(timer.FrozenRemaining, Is.EqualTo(frozen));

            timer.Tick(frozen + 1f);

            Assert.That(timer.Remaining, Is.EqualTo(remaining - 1f));
            Assert.That(timer.IsFrozen, Is.False);
        }

        /// <summary>Nonpositive delta must never advance delay, Freeze or countdown.</summary>
        [TestCase(0f), TestCase(-1f)]
        public void NonpositiveTick_DoesNotAdvanceClock(float delta)
        {
            var timer = Timer();
            timer.Start(PackingFixtures.CountdownSeconds, 0f);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.True);

            timer.Tick(delta);

            Assert.That(timer.Remaining, Is.EqualTo(PackingFixtures.CountdownSeconds));
            Assert.That(timer.FrozenRemaining, Is.EqualTo(PackingFixtures.FreezeSeconds));
        }

        /// <summary>A freeze silences warning ticks; restart and stop each cancel the active freeze once.</summary>
        [Test]
        public void FrozenClock_SuppressesWarningsAndRestartClearsFreeze()
        {
            var timer = Timer();
            int warnings = 0;
            var changes = new List<bool>();
            timer.WarningTick += () => warnings++;
            timer.FreezeChanged += changes.Add;
            timer.Start(PackingFixtures.WarningSeconds + 1, 0f);
            timer.Tick(1.5f);
            Assert.That(warnings, Is.EqualTo(1));
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.True);
            timer.Tick(9f);
            Assert.That(warnings, Is.EqualTo(1));

            timer.Start(PackingFixtures.CountdownSeconds, 0f);
            Assert.That(timer.IsFrozen, Is.False);
            Assert.That(timer.Freeze(PackingFixtures.FreezeSeconds), Is.True);
            timer.Stop();
            timer.Stop();

            Assert.That(timer.IsFrozen, Is.False);
            Assert.That(changes, Is.EqualTo(new[] { true, false, true, false }));
        }

        /// <summary>Assistance lowers the award; no assistance preserves three stars.</summary>
        [TestCase(false, 3), TestCase(true, 2)]
        public void AssistanceFlag_SelectsExpectedStarAward(bool assisted, int expected)
        {
            Assert.That(StarCalculator.StarsFor(assisted), Is.EqualTo(expected));
        }

        /// <summary>A fresh save exposes only level one and has neither unlocked nor spendable boosters.</summary>
        [Test]
        public void FreshProgress_UnlocksOnlyFirstLevelAndCannotSpendEmptyInventory()
        {
            var store = new MemorySaveStore();
            var progress = Progress(store);

            Assert.That(progress.HighestUnlocked, Is.EqualTo(1));
            Assert.That(progress.NextLevelToPlay, Is.EqualTo(1));
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(progress.TotalStars, Is.Zero);
            Assert.That(progress.IsBoosterUnlocked(BoosterType.Hint), Is.False);
            Assert.That(progress.TryConsume(BoosterType.Hint), Is.False);
            Assert.That(store.SaveCount, Is.Zero);
        }

        /// <summary>First clear combines milestone and three-star rewards; replay cannot pay either twice.</summary>
        [Test]
        public void FirstThreeStarClear_GrantsMilestoneBonusAndUnlockExactlyOnce()
        {
            var store = new MemorySaveStore();
            var progress = Progress(store);

            var first = progress.CompleteLevel(1, 3);
            var replay = progress.CompleteLevel(1, 3);

            Assert.That(first.FirstClear, Is.True);
            Assert.That(first.Improved, Is.True);
            Assert.That(first.Boosters.hint, Is.EqualTo(4));
            Assert.That(first.Unlocked, Is.EqualTo(new[] { BoosterType.Hint }));
            Assert.That(replay.FirstClear, Is.False);
            Assert.That(replay.Improved, Is.False);
            Assert.That(replay.Boosters.IsEmpty, Is.True);
            Assert.That(replay.Unlocked, Is.Empty);
            Assert.That(progress.BoosterCount(BoosterType.Hint), Is.EqualTo(4));
            Assert.That(store.Saved.boosters[0], Is.EqualTo(4));
            Assert.That(progress.IsUnlocked(2), Is.True);
            Assert.That(progress.IsUnlocked(3), Is.False);
        }

        /// <summary>Improving from two to three stars grants only the new bonus and never reduces best stars.</summary>
        [Test]
        public void TwoToThreeStarUpgrade_PaysBonusOnceAndLowerReplayKeepsBest()
        {
            var progress = Progress(new MemorySaveStore());
            var first = progress.CompleteLevel(1, 2);

            var upgrade = progress.CompleteLevel(1, 3);
            var replay = progress.CompleteLevel(1, 2);
            var repeated = progress.CompleteLevel(1, 3);

            Assert.That(first.Boosters.hint, Is.EqualTo(3));
            Assert.That(upgrade.FirstClear, Is.False);
            Assert.That(upgrade.Improved, Is.True);
            Assert.That(upgrade.Boosters.hint, Is.EqualTo(1));
            Assert.That(replay.Boosters.IsEmpty, Is.True);
            Assert.That(repeated.Boosters.IsEmpty, Is.True);
            Assert.That(progress.BestStars(1), Is.EqualTo(3));
            Assert.That(progress.TotalStars, Is.EqualTo(3));
        }

        /// <summary>Later milestones unlock the matching booster and preserve inventory on reload.</summary>
        [Test]
        public void MilestoneClears_UnlockFreezeAndAutoPackAndPersistRewards()
        {
            var store = new MemorySaveStore();
            var progress = Progress(store);
            for (int level = 1; level <= 3; level++) progress.CompleteLevel(level, 2);

            var freeze = progress.CompleteLevel(4, 2);
            progress.CompleteLevel(5, 2);
            var autoPack = progress.CompleteLevel(6, 2);
            var reloaded = Progress(store);

            Assert.That(freeze.Unlocked, Is.EqualTo(new[] { BoosterType.Freeze }));
            Assert.That(freeze.Boosters.freeze, Is.EqualTo(2));
            Assert.That(autoPack.Unlocked, Is.EqualTo(new[] { BoosterType.AutoPack }));
            Assert.That(autoPack.Boosters.autoPack, Is.EqualTo(2));
            Assert.That(reloaded.BoosterCount(BoosterType.Hint), Is.EqualTo(3));
            Assert.That(reloaded.BoosterCount(BoosterType.Freeze), Is.EqualTo(2));
            Assert.That(reloaded.BoosterCount(BoosterType.AutoPack), Is.EqualTo(2));
            Assert.That(reloaded.HighestUnlocked, Is.EqualTo(7));
            Assert.That(reloaded.CompleteLevel(6, 2).Boosters.IsEmpty, Is.True);
        }

        /// <summary>Spending writes exactly one charge; spending an empty type leaves saves and events alone.</summary>
        [Test]
        public void BoosterConsumption_PersistsOneChargeAndRejectsEmptyInventory()
        {
            var store = new MemorySaveStore();
            var progress = Progress(store);
            progress.CompleteLevel(1, 2);
            int events = 0;
            progress.BoostersChanged += () => events++;
            int saves = store.SaveCount;

            Assert.That(progress.TryConsume(BoosterType.Hint), Is.True);
            Assert.That(progress.TryConsume(BoosterType.Freeze), Is.False);

            Assert.That(progress.BoosterCount(BoosterType.Hint), Is.EqualTo(2));
            Assert.That(store.Saved.boosters[0], Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(saves + 1));
            Assert.That(events, Is.EqualTo(1));
        }

        /// <summary>A version-one save keeps stars and receives passed milestones once after persistence.</summary>
        [Test]
        public void LegacyMigration_PreservesStarsAndDoesNotRegrantAfterSaveReload()
        {
            var store = new MemorySaveStore(new ProgressData
            {
                version = 1, bestStars = new[] { 3, 3, 3, 3, 2, 3 }, boosters = null,
            });

            var progress = Progress(store);
            progress.SetPreferences(true, true); // Persist normalization through the public save boundary.
            var reloaded = Progress(store);

            Assert.That(reloaded.TotalStars, Is.EqualTo(17));
            Assert.That(reloaded.BoosterCount(BoosterType.Hint), Is.EqualTo(3));
            Assert.That(reloaded.BoosterCount(BoosterType.Freeze), Is.EqualTo(2));
            Assert.That(reloaded.BoosterCount(BoosterType.AutoPack), Is.EqualTo(2));
            Assert.That(store.Saved.version, Is.EqualTo(ProgressData.CURRENT_VERSION));
            Assert.That(store.Saved.startingBoostersGranted, Is.True);
            Assert.That(reloaded.Muted, Is.True);
            Assert.That(reloaded.ReduceMotion, Is.True);
        }

        /// <summary>Malformed in-memory arrays are fitted and clamped without file access or resetting valid values.</summary>
        [Test]
        public void InvalidStoredValues_NormalizeCountsAndPreserveValidProgress()
        {
            var store = new MemorySaveStore(new ProgressData
            {
                bestStars = new[] { -1, 9, 2, 3 }, boosters = new[] { -4, 2 }, startingBoostersGranted = true,
            });

            var progress = new ProgressService(store, 3, PackingFixtures.Progression());

            Assert.That(progress.BestStars(1), Is.Zero);
            Assert.That(progress.BestStars(2), Is.EqualTo(3));
            Assert.That(progress.BestStars(3), Is.EqualTo(2));
            Assert.That(progress.TotalStars, Is.EqualTo(5));
            Assert.That(progress.BoosterCount(BoosterType.Hint), Is.Zero);
            Assert.That(progress.BoosterCount(BoosterType.AutoPack), Is.EqualTo(2));
            Assert.That(progress.BoosterCount(BoosterType.Freeze), Is.Zero);
        }

        /// <summary>A missing or unreadable save (the store returns null) yields fresh isolated progress.</summary>
        [Test]
        public void MissingSave_FallsBackToFreshProgressWithoutWritingOnLoad()
        {
            var store = new MemorySaveStore();

            var progress = Progress(store);

            Assert.That(progress.LevelsPassed, Is.Zero);
            Assert.That(progress.TotalStars, Is.Zero);
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(store.SaveCount, Is.Zero);
        }

        /// <summary>Invalid completions do not grant inventory, write saves or change progression.</summary>
        [TestCase(0, 3), TestCase(PackingFixtures.LevelCount + 1, 3), TestCase(1, 0)]
        public void InvalidCompletion_DoesNotRewardOrWriteSave(int level, int stars)
        {
            var store = new MemorySaveStore();
            var progress = Progress(store);

            var reward = progress.CompleteLevel(level, stars);

            Assert.That(reward.Boosters.IsEmpty, Is.True);
            Assert.That(reward.FirstClear, Is.False);
            Assert.That(progress.TotalStars, Is.Zero);
            Assert.That(store.SaveCount, Is.Zero);
        }

        static LevelTimer Timer() => new LevelTimer(PackingFixtures.WarningSeconds);

        /// <summary>Legacy sizes normalize safely; readable UI preferences do not alter stars or charges.</summary>
        [TestCase(0, 100)]
        [TestCase(100, 100)]
        [TestCase(125, 125)]
        [TestCase(150, 150)]
        [TestCase(999, 100)]
        public void UISize_LoadAndSave_PreservesProgress(int stored, int expected)
        {
            var store = new MemorySaveStore(new ProgressData { uiScalePercent = stored });
            var progress = Progress(store);
            Assert.That(progress.UIScalePercent, Is.EqualTo(expected));
            progress.CompleteLevel(1, 3);
            int stars = progress.TotalStars;
            int charges = progress.BoosterCount(BoosterType.Hint);
            progress.SetUIScalePercent(150);
            var reloaded = Progress(store);
            Assert.That(reloaded.UIScalePercent, Is.EqualTo(150));
            Assert.That(reloaded.TotalStars, Is.EqualTo(stars));
            Assert.That(reloaded.BoosterCount(BoosterType.Hint), Is.EqualTo(charges));
        }

        /// <summary>Expanded touch reach never overrides exact slots or selects a placed piece.</summary>
        [Test]
        public void TrayTouchPadding_ExactHitsWinAndEmptySlotsAreIgnored()
        {
            var shape = PackingFixtures.Single();
            var tray = new TrayView(new[] { shape, shape }, 0f, 4f, new PackingTuning());
            var top = tray.TopLeftFor(0, shape);
            var inside = (UnityEngine.Vector2)top + new UnityEngine.Vector2(0.3f, -0.3f);
            Assert.That(tray.HitTestExpanded(inside, i => true, 0.2f), Is.EqualTo(0));
            var outside = (UnityEngine.Vector2)top + new UnityEngine.Vector2(-0.1f, -0.3f);
            Assert.That(tray.HitTest(outside, i => true), Is.EqualTo(-1));
            Assert.That(tray.HitTestExpanded(outside, i => true, 0.2f), Is.EqualTo(0));
            Assert.That(tray.HitTestExpanded(outside, i => i != 0, 0.2f), Is.EqualTo(-1));
            Assert.That(tray.HitTestExpanded(new UnityEngine.Vector2(100,100), i => true, 0.2f), Is.EqualTo(-1));
        }

        static ProgressService Progress(MemorySaveStore store) =>
            new ProgressService(store, PackingFixtures.LevelCount, PackingFixtures.Progression());
    }
}
