using AutoBattler.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct HexMovementStep
    {
        public long Id { get; }
        public int UnitId { get; }
        public HexCoordinate From { get; }
        public HexCoordinate To { get; }

        internal HexMovementStep(long id, int unitId, HexCoordinate from, HexCoordinate to)
        {
            Id = id;
            UnitId = unitId;
            From = from;
            To = to;
        }
    }
}
