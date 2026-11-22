using System;

namespace RaggedBanner.Combat
{
    public sealed class BattleRules
    {
        public int MaxPlayerUnits { get; }
        public int StartingActionPoints { get; }
        public int MaxActionPoints { get; }
        public int ActionPointsPerTurn { get; }

        public BattleRules(int maxPlayerUnits = 4, int startingActionPoints = 4, int maxActionPoints = 8,
            int actionPointsPerTurn = 4)
        {
            if (maxPlayerUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPlayerUnits));
            }

            if (maxActionPoints <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxActionPoints));
            }

            if (startingActionPoints < 0 || startingActionPoints > maxActionPoints)
            {
                throw new ArgumentOutOfRangeException(nameof(startingActionPoints));
            }

            if (actionPointsPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionPointsPerTurn));
            }

            MaxPlayerUnits = maxPlayerUnits;
            StartingActionPoints = startingActionPoints;
            MaxActionPoints = maxActionPoints;
            ActionPointsPerTurn = actionPointsPerTurn;
        }
    }
}
