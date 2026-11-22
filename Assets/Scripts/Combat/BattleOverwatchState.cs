using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct BattleOverwatchState
    {
        public int UnitId { get; }
        public HexCoordinate Coordinate { get; }
        public HexDirection Direction { get; }
        public int Range { get; }
        public int MinRange { get; }

        public BattleOverwatchState(int unitId, HexCoordinate coordinate, HexDirection direction, int range, int minRange = 1)
        {
            UnitId = unitId;
            Coordinate = coordinate;
            Direction = direction;
            Range = range;
            MinRange = minRange;
        }
    }
}
