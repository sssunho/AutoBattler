using System;

namespace RaggedBanner.Mercenaries
{
    public readonly struct Trait
    {
        public TraitType Type { get; }
        public TraitSource Source { get; }

        public Trait(TraitType type, TraitSource source)
        {
            if (!TraitTypeRules.IsValid(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if ((uint)source.Kind > (uint)TraitSourceKind.Selection || source.Id < 0)
            {
                throw new ArgumentException("A trait requires a valid source.", nameof(source));
            }

            Type = type;
            Source = source;
        }
    }
}
