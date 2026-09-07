using NUnit.Framework;

namespace AutoBattler.HexGrid.Tests
{
    public sealed class HexCoordinateTests
    {
        [Test]
        public void S_IsDerivedFromQAndR()
        {
            HexCoordinate coordinate = new HexCoordinate(2, -5);

            Assert.That(coordinate.S, Is.EqualTo(3));
        }

        [TestCase(HexDirection.East, 1, 0)]
        [TestCase(HexDirection.NorthEast, 1, -1)]
        [TestCase(HexDirection.NorthWest, 0, -1)]
        [TestCase(HexDirection.West, -1, 0)]
        [TestCase(HexDirection.SouthWest, -1, 1)]
        [TestCase(HexDirection.SouthEast, 0, 1)]
        public void GetNeighbor_ReturnsCoordinateInDirection(HexDirection direction, int q, int r)
        {
            HexCoordinate origin = new HexCoordinate(0, 0);

            Assert.That(origin.GetNeighbor(direction), Is.EqualTo(new HexCoordinate(q, r)));
        }

        [Test]
        public void DistanceTo_ReturnsHexDistance()
        {
            HexCoordinate start = new HexCoordinate(1, 2);
            HexCoordinate end = new HexCoordinate(4, -2);

            Assert.That(start.DistanceTo(end), Is.EqualTo(4));
            Assert.That(end.DistanceTo(start), Is.EqualTo(4));
        }

        [Test]
        public void Equality_UsesQAndR()
        {
            HexCoordinate coordinate = new HexCoordinate(3, -1);

            Assert.That(coordinate, Is.EqualTo(new HexCoordinate(3, -1)));
            Assert.That(coordinate, Is.Not.EqualTo(new HexCoordinate(3, 0)));
        }
    }
}
