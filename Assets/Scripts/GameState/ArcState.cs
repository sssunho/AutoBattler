using System;

namespace RaggedBanner.GameState
{
    public readonly struct ArcState : IEquatable<ArcState>
    {
        public string Stage { get; }
        public ArcStatus Status { get; }

        public ArcState(string stage, ArcStatus status)
        {
            if (string.IsNullOrWhiteSpace(stage))
            {
                throw new ArgumentException("An arc stage ID is required.", nameof(stage));
            }

            if ((uint)status > (uint)ArcStatus.Completed)
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            Stage = stage;
            Status = status;
        }

        public bool Equals(ArcState other)
        {
            return Status == other.Status && string.Equals(Stage, other.Stage, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ArcState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Stage == null ? 0 : StringComparer.Ordinal.GetHashCode(Stage)) * 397) ^ (int)Status;
            }
        }
    }
}
