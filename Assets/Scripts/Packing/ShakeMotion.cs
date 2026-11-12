// Decaying oscillation used for the invalid-drop shake, the container shake (pairs not touching),
// the upright wobble and the hint wobble.
// Stories: production/epics/pack-it-perfect/story-001-core-packing-loop.md, story-003-obstacle-rules.md, story-005-hint-and-stars.md
// Pure C#: no MonoBehaviour dependency.

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>A sine oscillation whose amplitude decays linearly to 0 over its duration.</summary>
    public sealed class ShakeMotion
    {
        float duration;
        float amplitude;
        float cycles;
        float timeLeft;

        /// <summary>True while the oscillation runs.</summary>
        public bool Active => timeLeft > 0f;

        /// <summary>Starts (or restarts) the oscillation.</summary>
        /// <param name="seconds">Duration.</param>
        /// <param name="peak">Peak value at the start (cells for a shake, degrees for a wobble).</param>
        /// <param name="fullCycles">Full back-and-forth swings over the duration.</param>
        public void Start(float seconds, float peak, float fullCycles)
        {
            duration = Mathf.Max(0f, seconds);
            amplitude = peak;
            cycles = fullCycles;
            timeLeft = duration;
        }

        /// <summary>Stops immediately.</summary>
        public void Stop() => timeLeft = 0f;

        /// <summary>Advances by <paramref name="deltaTime"/> and returns the current value (0 once finished).</summary>
        public float Tick(float deltaTime)
        {
            if (timeLeft <= 0f || duration <= 0f)
            {
                timeLeft = 0f;
                return 0f;
            }
            timeLeft = Mathf.Max(0f, timeLeft - deltaTime);
            float elapsed = duration - timeLeft;
            float decay = timeLeft / duration;
            return amplitude * decay * Mathf.Sin(elapsed / duration * cycles * 2f * Mathf.PI);
        }
    }
}
