namespace RaggedBanner.Combat
{
    enum BattleReactionType
    {
        OpportunityAttack,
        Overwatch
    }

    readonly struct BattleReactionRequest
    {
        public BattleReactionType Type { get; }
        public int ReactorId { get; }
        public int TargetId { get; }
        public int TurnOrderIndex { get; }

        public BattleReactionRequest(BattleReactionType type, int reactorId, int targetId, int turnOrderIndex)
        {
            Type = type;
            ReactorId = reactorId;
            TargetId = targetId;
            TurnOrderIndex = turnOrderIndex;
        }
    }
}
