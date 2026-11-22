using System;
using System.Collections.Generic;

namespace RaggedBanner.GameState
{
    public sealed class AppliedEventEffects
    {
        public static AppliedEventEffects Empty { get; } = new AppliedEventEffects(
            Array.Empty<string>(), Array.Empty<string>(), 0, 0, 0, Array.Empty<string>(), null);

        public IReadOnlyList<string> AddedTags { get; }
        public IReadOnlyList<string> RemovedTags { get; }
        public int FameChange { get; }
        public int ReputationChange { get; }
        public int MoneyChange { get; }
        public IReadOnlyList<string> RevealedGoalIds { get; }
        public ArcChangeRecord? ArcChange { get; }
        public bool IsEmpty => AddedTags.Count == 0 && RemovedTags.Count == 0
            && FameChange == 0 && ReputationChange == 0 && MoneyChange == 0
            && RevealedGoalIds.Count == 0 && !ArcChange.HasValue;

        public AppliedEventEffects(IEnumerable<string> addedTags, IEnumerable<string> removedTags,
            int fameChange, int reputationChange, int moneyChange,
            IEnumerable<string> revealedGoalIds, ArcChangeRecord? arcChange)
        {
            if (arcChange.HasValue && string.IsNullOrWhiteSpace(arcChange.Value.ArcId))
            {
                throw new ArgumentException("An arc change must have an arc ID.", nameof(arcChange));
            }

            AddedTags = CopyIds(addedTags, nameof(addedTags));
            RemovedTags = CopyIds(removedTags, nameof(removedTags));
            RevealedGoalIds = CopyIds(revealedGoalIds, nameof(revealedGoalIds));
            FameChange = fameChange;
            ReputationChange = reputationChange;
            MoneyChange = moneyChange;
            ArcChange = arcChange;
        }

        static IReadOnlyList<string> CopyIds(IEnumerable<string> ids, string parameterName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            List<string> copy = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    throw new ArgumentException("IDs must be nonempty and unique.", parameterName);
                }

                copy.Add(id);
            }

            return copy.AsReadOnly();
        }
    }
}
