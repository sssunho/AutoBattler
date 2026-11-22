namespace RaggedBanner.Combat
{
    readonly struct BattleResolvedDamage
    {
        public AttackAttribute? Attribute { get; }
        public BattleDamageBreakdown Breakdown { get; }
        public bool IsCritical { get; }
        public bool WasNegated { get; }

        public BattleResolvedDamage(AttackAttribute? attribute, BattleDamageBreakdown breakdown,
            bool isCritical, bool wasNegated)
        {
            Attribute = attribute;
            Breakdown = breakdown;
            IsCritical = isCritical;
            WasNegated = wasNegated;
        }
    }
}
