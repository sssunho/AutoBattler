namespace RaggedBanner.Combat
{
    enum BattleAttackOrigin
    {
        Command,
        Reaction
    }

    enum BattleAttackKind
    {
        Basic,
        Ambush
    }

    readonly struct BattleAttackContext
    {
        public int AttackerId { get; }
        public int TargetId { get; }
        public BattleAttackOrigin Origin { get; }
        public BattleAttackKind Kind { get; }
        public int AdditionalDamage { get; }
        public bool EndsStealth { get; }

        public BattleAttackContext(int attackerId, int targetId, BattleAttackOrigin origin,
            BattleAttackKind kind = BattleAttackKind.Basic, int additionalDamage = 0, bool endsStealth = false)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Origin = origin;
            Kind = kind;
            AdditionalDamage = additionalDamage;
            EndsStealth = endsStealth;
        }
    }
}
