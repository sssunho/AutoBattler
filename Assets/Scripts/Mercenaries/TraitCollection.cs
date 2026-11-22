using System;
using System.Collections.Generic;

namespace RaggedBanner.Mercenaries
{
    public sealed class TraitCollection
    {
        static readonly TraitCollection _empty = new TraitCollection(Array.Empty<Trait>());

        readonly Dictionary<TraitType, Trait[]> _traits;

        public static TraitCollection Empty => _empty;

        public TraitCollection(IReadOnlyList<Trait> traits)
        {
            if (traits == null)
            {
                throw new ArgumentNullException(nameof(traits));
            }

            Dictionary<TraitType, List<Trait>> grouped = new Dictionary<TraitType, List<Trait>>();
            HashSet<(TraitType, TraitSource)> grants = new HashSet<(TraitType, TraitSource)>();

            for (int i = 0; i < traits.Count; i++)
            {
                Trait trait = traits[i];

                if (!TraitTypeRules.IsValid(trait.Type)
                    || (uint)trait.Source.Kind > (uint)TraitSourceKind.Selection || trait.Source.Id < 0)
                {
                    throw new ArgumentException("Traits must contain valid types and sources.", nameof(traits));
                }

                if (!grants.Add((trait.Type, trait.Source)))
                {
                    throw new ArgumentException("The same source cannot grant the same trait more than once.", nameof(traits));
                }

                if (!grouped.TryGetValue(trait.Type, out List<Trait> entries))
                {
                    entries = new List<Trait>();
                    grouped.Add(trait.Type, entries);
                }

                entries.Add(trait);
            }

            _traits = new Dictionary<TraitType, Trait[]>(grouped.Count);

            foreach (KeyValuePair<TraitType, List<Trait>> group in grouped)
            {
                _traits.Add(group.Key, group.Value.ToArray());
            }
        }

        public bool Has(TraitType type)
        {
            return _traits.ContainsKey(type);
        }

        public int GetCount(TraitType type)
        {
            return _traits.TryGetValue(type, out Trait[] traits) ? traits.Length : 0;
        }

        public Trait Get(TraitType type, int index)
        {
            if (!_traits.TryGetValue(type, out Trait[] traits) || (uint)index >= (uint)traits.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return traits[index];
        }
    }
}
