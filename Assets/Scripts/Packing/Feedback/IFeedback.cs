// Player feedback seam: the packing screen reports rule texts and words through this interface so the
// screen-space UI wave can replace the built-in world-space toast (PackingGame.Feedback).
// Stories: production/epics/pack-it-perfect/story-003-obstacle-rules.md (violation toasts, "Pair! 💞").

using UnityEngine;

namespace PackItPerfect
{
    /// <summary>Shows short texts to the player. Strings arrive verbatim (may contain U+FE0F; see <see cref="TmpText.Clean"/>).</summary>
    public interface IFeedback
    {
        /// <summary>Shows a toast; the newest replaces any toast still showing.</summary>
        void Toast(string message);

        /// <summary>Shows a short floating word at a world position (e.g. "Pair! 💞" at a piece).</summary>
        void Word(Vector3 worldPosition, string word);

        /// <summary>Removes everything showing (called when a level loads).</summary>
        void Clear();
    }
}
