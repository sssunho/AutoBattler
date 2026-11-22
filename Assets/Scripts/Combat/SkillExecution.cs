namespace RaggedBanner.Combat
{
    sealed class SkillExecution
    {
        public int UnitId { get; }
        public BattleSkillDefinition Definition { get; }
        public bool CostCommitted { get; set; }
        public bool HasStartedMovement { get; set; }

        public SkillExecution(int unitId, BattleSkillDefinition definition)
        {
            UnitId = unitId;
            Definition = definition;
        }
    }
}
