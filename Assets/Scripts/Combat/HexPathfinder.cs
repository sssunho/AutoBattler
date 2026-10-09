using System.Collections.Generic;
using AutoBattler.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed class HexPathfinder
    {
        readonly Queue<HexCoordinate> _frontier = new Queue<HexCoordinate>();
        readonly Dictionary<HexCoordinate, HexCoordinate> _parents = new Dictionary<HexCoordinate, HexCoordinate>();

        public bool TryFindPath(BattleBoard board, HexCoordinate start, HexCoordinate destination, List<HexCoordinate> result)
        {
            if (result == null)
            {
                return false;
            }

            result.Clear();
            _frontier.Clear();
            _parents.Clear();

            if (board == null || !board.IsValid(start) || !board.IsValid(destination)
                || (destination != start && board.IsOccupied(destination)))
            {
                return false;
            }

            _frontier.Enqueue(start);
            _parents.Add(start, start);

            while (_frontier.Count > 0)
            {
                HexCoordinate current = _frontier.Dequeue();

                if (current == destination)
                {
                    while (current != start)
                    {
                        result.Add(current);
                        current = _parents[current];
                    }

                    result.Reverse();
                    return true;
                }

                for (int i = 0; i < 6; i++)
                {
                    HexCoordinate neighbor = current.GetNeighbor((HexDirection)i);

                    if (!board.IsValid(neighbor) || board.IsOccupied(neighbor) || _parents.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    _parents.Add(neighbor, current);
                    _frontier.Enqueue(neighbor);
                }
            }

            return false;
        }
    }
}
