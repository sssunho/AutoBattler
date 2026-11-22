using System;

namespace RaggedBanner.Mercenaries
{
    public readonly struct TraitSource : IEquatable<TraitSource>
    {
        public TraitSourceKind Kind { get; }
        public int Id { get; }

        public TraitSource(TraitSourceKind kind, int id)
        {
            if ((uint)kind > (uint)TraitSourceKind.Selection)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            Kind = kind;
            Id = id;
        }

        public bool Equals(TraitSource other)
        {
            return Kind == other.Kind && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is TraitSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Kind * 397 ^ Id);
        }
    }
}
