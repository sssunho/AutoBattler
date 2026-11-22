namespace RaggedBanner.Combat
{
    public readonly struct BattleAttackResult
    {
        public int TargetId { get; }
        public int Damage { get; }
        public int RemainingHealth { get; }
        public bool TargetDied { get; }
        public AttackAttribute? AttackAttribute { get; }
        public BattleDamageBreakdown DamageByAttribute { get; }
        public bool IsCritical { get; }
        public bool WasNegated { get; }

        internal BattleAttackResult(int targetId, int damage, int remainingHealth,
            AttackAttribute attackAttribute, bool isCritical, bool wasNegated)
            : this(targetId, damage, remainingHealth, attackAttribute,
                BattleDamageBreakdown.Single(attackAttribute, wasNegated ? 0 : damage), isCritical, wasNegated)
        {
        }

        internal BattleAttackResult(int targetId, int damage, int remainingHealth,
            AttackAttribute? attackAttribute, BattleDamageBreakdown damageByAttribute, bool isCritical, bool wasNegated)
        {
            TargetId = targetId;
            Damage = damage;
            RemainingHealth = remainingHealth;
            TargetDied = remainingHealth == 0;
            AttackAttribute = attackAttribute;
            DamageByAttribute = damageByAttribute;
            IsCritical = isCritical;
            WasNegated = wasNegated;
        }
    }
}
