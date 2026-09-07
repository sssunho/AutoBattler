using System;
using NUnit.Framework;

namespace AutoBattler.HexGrid.Tests
{
    public sealed class HexMapTests
    {
        [Test]
        public void IsValid_UsesQAndRAsWidthAndHeight()
        {
            HexMap map = new HexMap(3, 2);

            Assert.That(map.IsValid(new HexCoordinate(0, 0)), Is.True);
            Assert.That(map.IsValid(new HexCoordinate(2, 1)), Is.True);
            Assert.That(map.IsValid(new HexCoordinate(-1, 0)), Is.False);
            Assert.That(map.IsValid(new HexCoordinate(3, 0)), Is.False);
            Assert.That(map.IsValid(new HexCoordinate(0, -1)), Is.False);
            Assert.That(map.IsValid(new HexCoordinate(0, 2)), Is.False);
        }

        [Test]
        public void Indexer_ReturnsInitializedCell()
        {
            HexMap map = new HexMap(2, 2);
            HexCoordinate coordinate = new HexCoordinate(1, 1);

            Assert.That(map[coordinate], Is.Not.Null);
            Assert.That(map[coordinate], Is.SameAs(map[coordinate]));
        }

        [Test]
        public void Indexer_KeepsFlattenedCoordinatesIndependent()
        {
            HexMap map = new HexMap(3, 2);

            map[new HexCoordinate(2, 0)].OccupantUnitId = 10;
            map[new HexCoordinate(0, 1)].OccupantUnitId = 20;

            Assert.That(map[new HexCoordinate(2, 0)].OccupantUnitId, Is.EqualTo(10));
            Assert.That(map[new HexCoordinate(0, 1)].OccupantUnitId, Is.EqualTo(20));
        }

        [Test]
        public void Indexer_ThrowsForInvalidCoordinate()
        {
            HexMap map = new HexMap(2, 2);
            HexCoordinate coordinate = new HexCoordinate(2, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = map[coordinate]);
        }

        [Test]
        public void TryGetCell_ReturnsFalseForInvalidCoordinate()
        {
            HexMap map = new HexMap(2, 2);

            bool found = map.TryGetCell(new HexCoordinate(-1, 0), out HexCell cell);

            Assert.That(found, Is.False);
            Assert.That(cell, Is.Null);
        }

        [Test]
        public void EmptyMap_HasNoValidCoordinates()
        {
            HexMap map = new HexMap(0, 0);

            Assert.That(map.IsValid(new HexCoordinate(0, 0)), Is.False);
        }

        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void Constructor_ThrowsForNegativeSize(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HexMap(width, height));
        }
    }
}
