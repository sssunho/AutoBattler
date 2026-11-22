namespace RaggedBanner.Combat
{
    public sealed class BattleResult
    {
        public BattleOutcome Outcome { get; }
        public int RoundNumber { get; }
        public int InitialPlayerCount { get; }
        public int InitialEnemyCount { get; }
        public int RemainingPlayerCount { get; }
        public int RemainingEnemyCount { get; }

        internal BattleResult(BattleOutcome outcome, int roundNumber, int initialPlayerCount,
            int initialEnemyCount, int remainingPlayerCount, int remainingEnemyCount)
        {
            Outcome = outcome;
            RoundNumber = roundNumber;
            InitialPlayerCount = initialPlayerCount;
            InitialEnemyCount = initialEnemyCount;
            RemainingPlayerCount = remainingPlayerCount;
            RemainingEnemyCount = remainingEnemyCount;
        }
    }
}
