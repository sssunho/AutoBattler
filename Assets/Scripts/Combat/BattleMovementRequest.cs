using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    enum MovementKind
    {
        Normal,
        Skill,
        Forced
    }

    enum BattleMovementPermission
    {
        Rejected,
        Allowed,
        PendingReaction
    }

    readonly struct BattleMovementRequest
    {
        public int UnitId { get; }
        public HexCoordinate From { get; }
        public HexCoordinate To { get; }
        public MovementKind Kind { get; }

        public BattleMovementRequest(int unitId, HexCoordinate from, HexCoordinate to, MovementKind kind)
        {
            UnitId = unitId;
            From = from;
            To = to;
            Kind = kind;
        }
    }
}
