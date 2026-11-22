using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct HexAttackStep
    {
        public int AttackerId { get; }
        public int TargetId { get; }
        public HexCoordinate TargetCoordinate { get; }

        internal HexAttackStep(int attackerId, int targetId, HexCoordinate targetCoordinate)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            TargetCoordinate = targetCoordinate;
        }
    }
}
