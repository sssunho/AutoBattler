using System;
using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.MercenaryData.Tests
{
    public sealed class TraitCollectionTests
    {
        [Test]
        public void EmptyCollectionHasNoTraits()
        {
            Assert.That(TraitCollection.Empty.Has(TraitType.RearGuaranteedCritical), Is.False);
            Assert.That(TraitCollection.Empty.GetCount(TraitType.RearGuaranteedCritical), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => TraitCollection.Empty.Get(TraitType.RearGuaranteedCritical, 0));
        }

        [Test]
        public void DifferentSourcesRemainSeparateAndKeepTheirInputOrder()
        {
            TraitSource first = new TraitSource(TraitSourceKind.Equipment, 102);
            TraitSource second = new TraitSource(TraitSourceKind.Equipment, 101);
            TraitSource third = new TraitSource(TraitSourceKind.Specialization, 102);
            TraitCollection collection = new TraitCollection(new[]
            {
                new Trait(TraitType.DirectionalPhysicalNegation, first),
                new Trait(TraitType.RearGuaranteedCritical, first),
                new Trait(TraitType.DirectionalPhysicalNegation, second),
                new Trait(TraitType.DirectionalPhysicalNegation, third)
            });

            Assert.That(collection.Has(TraitType.DirectionalPhysicalNegation), Is.True);
            Assert.That(collection.GetCount(TraitType.DirectionalPhysicalNegation), Is.EqualTo(3));
            Assert.That(collection.GetCount(TraitType.RearGuaranteedCritical), Is.EqualTo(1));
            Assert.That(collection.Get(TraitType.DirectionalPhysicalNegation, 0).Source, Is.EqualTo(first));
            Assert.That(collection.Get(TraitType.DirectionalPhysicalNegation, 1).Source, Is.EqualTo(second));
            Assert.That(collection.Get(TraitType.DirectionalPhysicalNegation, 2).Source, Is.EqualTo(third));
        }

        [TestCase(TraitType.RearGuaranteedCritical)]
        [TestCase(TraitType.DirectionalPhysicalNegation)]
        [TestCase(TraitType.OpportunityAttack)]
        public void SameSourceCannotGrantTheSameTypeTwice(TraitType type)
        {
            Trait first = new Trait(type, new TraitSource(TraitSourceKind.Class, 1));
            Trait duplicate = new Trait(type, new TraitSource(TraitSourceKind.Class, 1));

            Assert.Throws<ArgumentException>(() => new TraitCollection(new[] { first, duplicate }));
        }

        [Test]
        public void ReplacingAndClearingTheInputDoesNotChangeTheCollection()
        {
            Trait original = new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Class, 1));
            List<Trait> input = new List<Trait> { original };
            TraitCollection collection = new TraitCollection(input);
            input[0] = new Trait(TraitType.DirectionalPhysicalNegation, new TraitSource(TraitSourceKind.Equipment, 99));
            input.Clear();

            Assert.That(collection.Has(TraitType.RearGuaranteedCritical), Is.True);
            Assert.That(collection.Has(TraitType.DirectionalPhysicalNegation), Is.False);
            Assert.That(collection.GetCount(TraitType.RearGuaranteedCritical), Is.EqualTo(1));
            Assert.That(collection.Get(TraitType.RearGuaranteedCritical, 0).Source, Is.EqualTo(original.Source));
        }

        [Test]
        public void NullInputAndDefaultTraitAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new TraitCollection(null));
            Assert.Throws<ArgumentException>(() => new TraitCollection(new[] { default(Trait) }));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(999)]
        public void UndefinedTraitTypesAreRejected(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Trait((TraitType)value, default));
        }

        [TestCase(-1, 0)]
        [TestCase(4, 0)]
        [TestCase(0, -1)]
        public void InvalidSourcesAreRejected(int kind, int id)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitSource((TraitSourceKind)kind, id));
        }

        [TestCase(TraitSourceKind.Class)]
        [TestCase(TraitSourceKind.Specialization)]
        [TestCase(TraitSourceKind.Equipment)]
        [TestCase(TraitSourceKind.Selection)]
        public void SourceIdsAcceptZeroAndTheMaximumValue(TraitSourceKind kind)
        {
            Assert.That(new TraitSource(kind, 0).Id, Is.Zero);
            Assert.That(new TraitSource(kind, int.MaxValue).Id, Is.EqualTo(int.MaxValue));
        }

        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(int.MaxValue)]
        public void GetRejectsIndicesOutsideTheRequestedType(int index)
        {
            TraitCollection collection = new TraitCollection(new[]
            {
                new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Class, 1))
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Get(TraitType.RearGuaranteedCritical, index));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Get(TraitType.DirectionalPhysicalNegation, 0));
        }
    }
}
