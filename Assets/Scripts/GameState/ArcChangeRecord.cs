using System;

namespace RaggedBanner.GameState
{
    public readonly struct ArcChangeRecord
    {
        public string ArcId { get; }
        public ArcState Before { get; }
        public ArcState After { get; }

        public ArcChangeRecord(string arcId, ArcState before, ArcState after)
        {
            if (string.IsNullOrWhiteSpace(arcId))
            {
                throw new ArgumentException("An arc ID is required.", nameof(arcId));
            }

            if (string.IsNullOrWhiteSpace(before.Stage) || string.IsNullOrWhiteSpace(after.Stage))
            {
                throw new ArgumentException("Arc states must have stage IDs.");
            }

            if (before.Equals(after))
            {
                throw new ArgumentException("An arc change must contain different states.");
            }

            ArcId = arcId;
            Before = before;
            After = after;
        }
    }
}
