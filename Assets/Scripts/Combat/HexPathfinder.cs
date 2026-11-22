using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed class HexPathfinder
    {
        internal readonly struct ReachablePosition
        {
            public HexCoordinate Coordinate { get; }
            public int Distance { get; }

            public ReachablePosition(HexCoordinate coordinate, int distance)
            {
                Coordinate = coordinate;
                Distance = distance;
            }
        }

        readonly Queue<HexCoordinate> _frontier = new Queue<HexCoordinate>();
        readonly Dictionary<HexCoordinate, HexCoordinate> _parents = new Dictionary<HexCoordinate, HexCoordinate>();

        readonly Dictionary<HexCoordinate, int> _distances = new Dictionary<HexCoordinate, int>();
        readonly Dictionary<HexCoordinate, double> _scores = new Dictionary<HexCoordinate, double>();
        readonly BattleSmokeLine _smokeLine = new BattleSmokeLine();

        internal void FindReachablePositions(BattleBoard board, HexCoordinate start, int maxDistance,
            List<ReachablePosition> result)
        {
            if (result == null)
            {
                return;
            }

            result.Clear();
            _parents.Clear();

            if (board == null || !board.IsValid(start) || maxDistance < 0)
            {
                return;
            }

            result.Add(new ReachablePosition(start, 0));
            _parents.Add(start, start);

            for (int index = 0; index < result.Count; index++)
            {
                ReachablePosition current = result[index];

                if (current.Distance >= maxDistance)
                {
                    continue;
                }

                for (int direction = 0; direction < 6; direction++)
                {
                    HexCoordinate neighbor = current.Coordinate.GetNeighbor((HexDirection)direction);

                    if (!board.IsValid(neighbor) || board.IsOccupied(neighbor) || _parents.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    _parents.Add(neighbor, current.Coordinate);
                    result.Add(new ReachablePosition(neighbor, current.Distance + 1));
                }
            }
        }

        public bool TryFindPath(BattleBoard board, HexCoordinate start, HexCoordinate destination, List<HexCoordinate> result)
        {
            return TryFindPath(board, start, destination, int.MaxValue, BattlePathLine.Between(start, destination), result);
        }

        public bool TryFindPath(BattleBoard board, HexCoordinate start, HexCoordinate destination,
            int maxDistance, BattlePathLine line, List<HexCoordinate> result)
        {
            return TryFindPath(board, start, destination, 0, 0, maxDistance, line, result);
        }

        public bool TryFindPathToAttackPosition(BattleBoard board, HexCoordinate start, HexCoordinate target,
            int attackRange, List<HexCoordinate> result, int minRange = 1)
        {
            return TryFindPathToAttackPosition(board, start, target, attackRange, int.MaxValue,
                BattlePathLine.Between(start, target), result, minRange);
        }

        public bool TryFindPathToAttackPosition(BattleBoard board, HexCoordinate start, HexCoordinate target,
            int attackRange, int maxDistance, BattlePathLine line, List<HexCoordinate> result, int minRange = 1)
        {
            if (minRange < 1 || attackRange < minRange)
            {
                result?.Clear();
                return false;
            }

            return TryFindPath(board, start, target, minRange, attackRange, maxDistance, line, result);
        }

        bool TryFindPath(BattleBoard board, HexCoordinate start, HexCoordinate destination, int minimumDistance, int stoppingDistance,
            int maxDistance, BattlePathLine line, List<HexCoordinate> result)
        {
            if (result == null)
            {
                return false;
            }

            result.Clear();
            _frontier.Clear();
            _parents.Clear();
            _distances.Clear();
            _scores.Clear();

            if (board == null || !board.IsValid(start) || !board.IsValid(destination) || maxDistance < 0
                || (stoppingDistance == 0 && destination != start && board.IsOccupied(destination)))
            {
                return false;
            }

            if (start.DistanceTo(destination) >= minimumDistance && start.DistanceTo(destination) <= stoppingDistance
                && _smokeLine.IsClear(board, start, destination, stoppingDistance))
            {
                return true;
            }

            if (!line.IsValid)
            {
                return false;
            }

            _frontier.Enqueue(start);
            _parents.Add(start, start);
            _distances.Add(start, 0);
            _scores.Add(start, 0d);
            int bestDistance = int.MaxValue;
            double bestScore = double.PositiveInfinity;
            HexCoordinate bestGoal = default;
            bool hasGoal = false;

            while (_frontier.Count > 0)
            {
                HexCoordinate current = _frontier.Dequeue();
                int distance = _distances[current];
                double score = _scores[current];

                if (distance > bestDistance)
                {
                    break;
                }

                if (current.DistanceTo(destination) >= minimumDistance && current.DistanceTo(destination) <= stoppingDistance
                    && _smokeLine.IsClear(board, current, destination, stoppingDistance))
                {
                    if (!hasGoal || score < bestScore)
                    {
                        hasGoal = true;
                        bestDistance = distance;
                        bestScore = score;
                        bestGoal = current;
                    }

                    continue;
                }

                if (distance >= maxDistance || distance >= bestDistance)
                {
                    continue;
                }

                for (int i = 0; i < 6; i++)
                {
                    HexCoordinate neighbor = current.GetNeighbor((HexDirection)i);

                    if (!board.IsValid(neighbor) || board.IsOccupied(neighbor))
                    {
                        continue;
                    }

                    int nextDistance = distance + 1;
                    double offset = ((long)neighbor.Q - start.Q) * line.QWeight
                        + ((long)neighbor.R - start.R) * line.RWeight;
                    double nextScore = score + offset * offset;

                    if (_distances.TryGetValue(neighbor, out int previousDistance))
                    {
                        if (previousDistance == nextDistance && nextScore < _scores[neighbor])
                        {
                            _scores[neighbor] = nextScore;
                            _parents[neighbor] = current;
                        }

                        continue;
                    }

                    _parents.Add(neighbor, current);
                    _distances.Add(neighbor, nextDistance);
                    _scores.Add(neighbor, nextScore);
                    _frontier.Enqueue(neighbor);
                }
            }

            if (!hasGoal)
            {
                return false;
            }

            while (bestGoal != start)
            {
                result.Add(bestGoal);
                bestGoal = _parents[bestGoal];
            }

            result.Reverse();
            return true;
        }
    }
}
