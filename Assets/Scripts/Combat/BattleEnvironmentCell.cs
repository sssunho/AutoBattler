using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct BattleEnvironmentCell
    {
        public HexCoordinate Coordinate { get; }
        public BattleEnvironmentState State { get; }

        public BattleEnvironmentCell(HexCoordinate coordinate, BattleEnvironmentState state)
        {
            Coordinate = coordinate;
            State = state;
        }
    }
}
