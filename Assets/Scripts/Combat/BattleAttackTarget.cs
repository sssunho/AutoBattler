namespace RaggedBanner.Combat
{
    public readonly struct BattleAttackTarget
    {
        public int UnitId { get; }
        public bool RequiresMovement { get; }

        public BattleAttackTarget(int unitId, bool requiresMovement)
        {
            UnitId = unitId;
            RequiresMovement = requiresMovement;
        }
    }
}
