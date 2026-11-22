using System;
using NUnit.Framework;
using RaggedBanner.Combat;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.BattleMaps.Tests
{
    public sealed class BattleMapDefinitionTests
    {
        [Test]
        public void EmptyPlacementsAreValidForAnUnfinishedMap()
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(1, 1,
                Array.Empty<BattleMapUnitPlacement>(), Array.Empty<BattleMapStartPoint>(), out string error), Is.True);
            Assert.That(error, Is.Null);
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void NonpositiveMapDimensionsAreRejected(int width, int height)
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(width, height,
                Array.Empty<BattleMapUnitPlacement>(), Array.Empty<BattleMapStartPoint>(), out _), Is.False);
        }

        [Test]
        public void NullListsAndEntriesAreRejected()
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                null, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                Array.Empty<BattleMapUnitPlacement>(), null, out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                new BattleMapUnitPlacement[] { null }, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                Array.Empty<BattleMapUnitPlacement>(), new BattleMapStartPoint[] { null }, out _), Is.False);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(2, 0)]
        [TestCase(0, 2)]
        public void BothPlacementKindsRejectCoordinatesOutsideMap(int q, int r)
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                new[] { Unit(q, r) }, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                Array.Empty<BattleMapUnitPlacement>(), new[] { Start(q, r, 0) }, out _), Is.False);
        }

        [Test]
        public void UnitsCannotShareAHexWithAnotherUnitOrAStartPoint()
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                new[] { Unit(0, 0), Unit(0, 0) }, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                new[] { Unit(0, 0) }, new[] { Start(0, 0, 0) }, out _), Is.False);
        }

        [Test]
        public void StartPointsCannotShareAHex()
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                Array.Empty<BattleMapUnitPlacement>(), new[] { Start(0, 0, 0), Start(0, 0, 1) }, out _), Is.False);
        }

        [Test]
        public void DuplicateAndNegativeStartOrdersAreRejected()
        {
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                Array.Empty<BattleMapUnitPlacement>(), new[] { Start(0, 0, -1) }, out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2,
                Array.Empty<BattleMapUnitPlacement>(), new[] { Start(0, 0, 5), Start(1, 0, 5) }, out _), Is.False);
        }

        [Test]
        public void ValidationAllowsGapsAndPreservesStartOrderAndInputRecords()
        {
            BattleMapUnitPlacement unit = Unit(1, 1);
            BattleMapStartPoint first = Start(0, 0, 30);
            BattleMapStartPoint second = Start(1, 0, 10);
            BattleMapUnitPlacement[] units = { unit };
            BattleMapStartPoint[] starts = { first, second };

            Assert.That(BattleMapDefinition.ValidatePlacements(2, 2, units, starts, out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(units[0], Is.SameAs(unit));
            Assert.That(starts[0], Is.SameAs(first));
            Assert.That(starts[1], Is.SameAs(second));
            Assert.That(starts[0].Order, Is.EqualTo(30));
            Assert.That(starts[1].Order, Is.EqualTo(10));
        }

        [TestCase(BattleTeam.Player)]
        [TestCase((BattleTeam)99)]
        public void OnlyEnemiesCanBeFixedPlacements(BattleTeam team)
        {
            BattleMapUnitPlacement unit = new BattleMapUnitPlacement(new HexCoordinate(0, 0),
                MercenaryClass.Warrior, team, HexDirection.East);

            Assert.That(BattleMapDefinition.ValidatePlacements(1, 1,
                new[] { unit }, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void UndefinedClassesAreRejected(int value)
        {
            BattleMapUnitPlacement unit = new BattleMapUnitPlacement(new HexCoordinate(0, 0),
                (MercenaryClass)value, BattleTeam.Enemy, HexDirection.East);

            Assert.That(BattleMapDefinition.ValidatePlacements(1, 1,
                new[] { unit }, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
        }

        [TestCase(-1)]
        [TestCase(6)]
        public void BothPlacementKindsRejectUndefinedFacings(int value)
        {
            BattleMapUnitPlacement unit = new BattleMapUnitPlacement(new HexCoordinate(0, 0),
                MercenaryClass.Warrior, BattleTeam.Enemy, (HexDirection)value);
            BattleMapStartPoint start = new BattleMapStartPoint(new HexCoordinate(0, 0), 0, (HexDirection)value);

            Assert.That(BattleMapDefinition.ValidatePlacements(1, 1,
                new[] { unit }, Array.Empty<BattleMapStartPoint>(), out _), Is.False);
            Assert.That(BattleMapDefinition.ValidatePlacements(1, 1,
                Array.Empty<BattleMapUnitPlacement>(), new[] { start }, out _), Is.False);
        }

        [TestCase(MercenaryClass.Warrior, HexDirection.East)]
        [TestCase(MercenaryClass.Rogue, HexDirection.NorthEast)]
        [TestCase(MercenaryClass.Archer, HexDirection.NorthWest)]
        [TestCase(MercenaryClass.Warrior, HexDirection.West)]
        [TestCase(MercenaryClass.Rogue, HexDirection.SouthWest)]
        [TestCase(MercenaryClass.Archer, HexDirection.SouthEast)]
        public void DefinedClassesAndFacingsAreAccepted(MercenaryClass unitClass, HexDirection facing)
        {
            BattleMapUnitPlacement unit = new BattleMapUnitPlacement(new HexCoordinate(0, 0),
                unitClass, BattleTeam.Enemy, facing);
            BattleMapStartPoint start = new BattleMapStartPoint(new HexCoordinate(1, 0), 0, facing);

            Assert.That(BattleMapDefinition.ValidatePlacements(2, 1,
                new[] { unit }, new[] { start }, out _), Is.True);
        }

        static BattleMapUnitPlacement Unit(int q, int r)
        {
            return new BattleMapUnitPlacement(new HexCoordinate(q, r), MercenaryClass.Warrior,
                BattleTeam.Enemy, HexDirection.East);
        }

        static BattleMapStartPoint Start(int q, int r, int order)
        {
            return new BattleMapStartPoint(new HexCoordinate(q, r), order, HexDirection.East);
        }
    }
}
