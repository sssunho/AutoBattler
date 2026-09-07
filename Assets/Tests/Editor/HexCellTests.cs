using NUnit.Framework;

namespace AutoBattler.HexGrid.Tests
{
    public sealed class HexCellTests
    {
        [Test]
        public void NewCell_IsUnoccupied()
        {
            HexCell cell = new HexCell();

            Assert.That(cell.OccupantUnitId, Is.Null);
            Assert.That(cell.IsOccupied, Is.False);
        }

        [Test]
        public void OccupantUnitId_MakesCellOccupied()
        {
            HexCell cell = new HexCell
            {
                OccupantUnitId = 3
            };

            Assert.That(cell.IsOccupied, Is.True);
        }
    }
}
