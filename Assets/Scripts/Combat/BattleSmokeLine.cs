using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    sealed class BattleSmokeLine
    {
        readonly List<HexCoordinate> _cells = new List<HexCoordinate>();

        public bool IsClear(BattleBoard board, HexCoordinate origin, HexCoordinate target, int maximumRange)
        {
            if (board == null || !board.IsValid(origin) || !board.IsValid(target))
            {
                return false;
            }

            if (maximumRange < 2)
            {
                return true;
            }

            BattleSpiritPath.Copy(origin, target, _cells);
            for (int i = 0; i < _cells.Count; i++)
            {
                if (board.TryGetEnvironment(_cells[i], out BattleEnvironmentState environment)
                    && environment.Air == BattleAirEffect.Smoke)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
