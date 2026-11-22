using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct BattleDamageEvent
    {
        public BattleDamageOrigin Origin { get; }
        public HexCoordinate Coordinate { get; }
        public BattleAttackResult Result { get; }

        internal BattleDamageEvent(BattleDamageOrigin origin, HexCoordinate coordinate, BattleAttackResult result)
        {
            Origin = origin;
            Coordinate = coordinate;
            Result = result;
        }
    }
}
