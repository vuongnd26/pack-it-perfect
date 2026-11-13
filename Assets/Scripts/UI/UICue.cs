// Interface moments the audio layer may answer with a sound. The UI only names the moment; it never picks a clip.

namespace PackItPerfect.UI
{
    /// <summary>A presentation moment raised by <see cref="GameUI.Cue"/>.</summary>
    public enum UICue
    {
        /// <summary>A star pops in on the win card; the step is the star's index (0-based).</summary>
        Star,
        /// <summary>Rewards or a new booster appear on the win card.</summary>
        Reward,
        /// <summary>A booster tap was refused (locked, empty or not usable now).</summary>
        Refused,
        /// <summary>A banner card slides in.</summary>
        Banner,
        /// <summary>A Freeze started holding the clock.</summary>
        Freeze,
        /// <summary>The timer ran out.</summary>
        Lost,
        /// <summary>A booster was bought in the shop.</summary>
        Purchase,
    }
}
