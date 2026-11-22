using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public enum BattleAiDecisionKind
    {
        None,
        Move,
        Attack,
        EndTurn
    }

    public readonly struct BattleAiDecision
    {
        public BattleAiDecisionKind Kind { get; }
        public int UnitId { get; }
        public int TargetId { get; }
        public HexCoordinate Destination { get; }

        internal BattleAiDecision(BattleAiDecisionKind kind, int unitId, int targetId, HexCoordinate destination)
        {
            Kind = kind;
            UnitId = unitId;
            TargetId = targetId;
            Destination = destination;
        }
    }
}
