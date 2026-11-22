using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct HexMovementStep
    {
        public int UnitId { get; }
        public HexCoordinate From { get; }
        public HexCoordinate To { get; }

        internal HexMovementStep(int unitId, HexCoordinate from, HexCoordinate to)
        {
            UnitId = unitId;
            From = from;
            To = to;
        }
    }
}
