// Storage seam for progress, so ProgressService stays pure C# and testable.
// Story: production/epics/pack-it-perfect/story-007-progression-meta.md

namespace PackItPerfect.Meta
{
    /// <summary>Loads and saves <see cref="ProgressData"/>. UI code never calls a store directly; it goes through <see cref="ProgressService"/>.</summary>
    public interface ISaveStore
    {
        /// <summary>Returns the saved progress, or null when nothing is saved yet or the save cannot be read.</summary>
        ProgressData Load();

        /// <summary>Persists <paramref name="data"/>. Failures are logged, never thrown.</summary>
        void Save(ProgressData data);
    }
}
