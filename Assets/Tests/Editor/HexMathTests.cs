using NUnit.Framework;

namespace AutoBattler.HexGrid.Tests
{
    public sealed class HexMathTests
    {
        [TestCase(1, 0, 1, 0, 2d)]
        [TestCase(1, 0, 0, 1, 1d)]
        [TestCase(1, 0, -1, 0, -2d)]
        [TestCase(2, -1, -3, 4, -9d)]
        public void Dot_ReturnsCubeCoordinateDotProduct(
            int leftQ,
            int leftR,
            int rightQ,
            int rightR,
            double expected)
        {
            HexCoordinate left = new HexCoordinate(leftQ, leftR);
            HexCoordinate right = new HexCoordinate(rightQ, rightR);

            double result = HexMath.Dot(left, right);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Dot_IsSymmetric()
        {
            HexCoordinate left = new HexCoordinate(3, -2);
            HexCoordinate right = new HexCoordinate(-4, 1);

            Assert.That(HexMath.Dot(left, right), Is.EqualTo(HexMath.Dot(right, left)));
        }
    }
}
