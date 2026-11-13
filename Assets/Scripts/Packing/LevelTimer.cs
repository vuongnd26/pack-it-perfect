// Countdown for the timed levels (5, 10, 15). Pure C#: the owner drives it with Tick(unscaled delta).
// Implements: design/game-brief.md "Cơ chế chính" (Hẹn giờ), "Điều kiện thắng / thua" (Thua — chế độ Hẹn giờ).
// Story: production/epics/pack-it-perfect/story-006-timer-mode.md

using System;

namespace PackItPerfect
{
    /// <summary>
    /// Level countdown. States: Stopped → (Start) Delayed → Counting → Expired (stopped); Stop() from any state.
    /// The shown value is whole seconds rounded up, so a 30 s timer shows 30 until 1 s has passed.
    /// </summary>
    public sealed class LevelTimer
    {
        float delay;
        float remaining;
        int shownSeconds;
        bool running;

        /// <summary>Raised with the seconds left each time the shown value changes, once on Start, and with 0 on expiry.</summary>
        public event Action<int> SecondTick;

        /// <summary>
        /// Raised once per second while 1..<see cref="WarningSeconds"/> seconds are left (5, 4, 3, 2, 1 by default).
        /// Audio hook: play the clock tick here (story 009).
        /// </summary>
        public event Action WarningTick;

        /// <summary>Raised once when the countdown reaches 0. The timer is stopped by then.</summary>
        public event Action Expired;

        /// <summary>Creates a stopped timer.</summary>
        /// <param name="warningSeconds">Seconds left at which <see cref="WarningTick"/> starts firing.</param>
        public LevelTimer(int warningSeconds)
        {
            WarningSeconds = warningSeconds;
        }

        /// <summary>Seconds left at which <see cref="WarningTick"/> starts firing.</summary>
        public int WarningSeconds { get; set; }

        /// <summary>True from Start until Stop or expiry (the start delay included).</summary>
        public bool IsRunning => running;

        /// <summary>True while running and past the start delay.</summary>
        public bool IsCounting => running && delay <= 0f;

        /// <summary>Exact seconds left.</summary>
        public float Remaining => remaining;

        /// <summary>Whole seconds left as shown to the player (rounded up).</summary>
        public int SecondsLeft => shownSeconds;

        /// <summary>Starts a full countdown of <paramref name="seconds"/> after <paramref name="startDelay"/> seconds. Seconds &lt;= 0 stops it.</summary>
        public void Start(int seconds, float startDelay)
        {
            if (seconds <= 0)
            {
                Stop();
                return;
            }
            remaining = seconds;
            delay = Math.Max(0f, startDelay);
            shownSeconds = seconds;
            running = true;
            SecondTick?.Invoke(shownSeconds);
        }

        /// <summary>Stops the countdown (e.g. on win); the remaining time is kept for display.</summary>
        public void Stop() => running = false;

        /// <summary>Advances the countdown by <paramref name="deltaTime"/> seconds (pass unscaled time).</summary>
        public void Tick(float deltaTime)
        {
            if (!running || deltaTime <= 0f)
            {
                return;
            }
            if (delay > 0f)
            {
                delay -= deltaTime;
                if (delay > 0f)
                {
                    return;
                }
                deltaTime = -delay;
                delay = 0f;
            }
            remaining = Math.Max(0f, remaining - deltaTime);
            int seconds = (int)Math.Ceiling(remaining);
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                SecondTick?.Invoke(seconds);
                if (seconds > 0 && seconds <= WarningSeconds)
                {
                    WarningTick?.Invoke();
                }
            }
            if (remaining <= 0f)
            {
                running = false;
                Expired?.Invoke();
            }
        }
    }
}
