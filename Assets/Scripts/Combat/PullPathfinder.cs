using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    sealed class PullPathfinder
    {
        readonly Queue<HexCoordinate> _frontier = new Queue<HexCoordinate>();
        readonly Dictionary<HexCoordinate, HexCoordinate> _parents = new Dictionary<HexCoordinate, HexCoordinate>();
        readonly Dictionary<HexCoordinate, double> _scores = new Dictionary<HexCoordinate, double>();

        public bool TryFindPath(BattleBoard board, HexCoordinate actorPosition, HexCoordinate targetPosition,
            SkillType skill, BattlePathLine line, List<HexCoordinate> result)
        {
            result?.Clear();
            _frontier.Clear();
            _parents.Clear();
            _scores.Clear();

            if ((skill != SkillType.PullSelf && skill != SkillType.PullTarget) || !line.IsValid)
            {
                return false;
            }

            _frontier.Enqueue(actorPosition);
            _parents.Add(actorPosition, actorPosition);
            _scores.Add(actorPosition, 0d);

            while (_frontier.Count > 0)
            {
                HexCoordinate current = _frontier.Dequeue();

                if (current == targetPosition)
                {
                    current = _parents[current];

                    while (current != actorPosition)
                    {
                        result?.Add(current);
                        current = _parents[current];
                    }

                    if (skill == SkillType.PullSelf)
                    {
                        result?.Reverse();
                    }

                    return true;
                }

                long currentDistance = GetDistance(current, targetPosition);
                double currentScore = _scores[current];

                for (int direction = 0; direction < 6; direction++)
                {
                    HexCoordinate neighbor = current.GetNeighbor((HexDirection)direction);

                    if (!board.IsValid(neighbor)
                        || GetDistance(neighbor, targetPosition) != currentDistance - 1
                        || neighbor != targetPosition && board.IsOccupied(neighbor))
                    {
                        continue;
                    }

                    double offset = ((long)neighbor.Q - actorPosition.Q) * line.QWeight
                        + ((long)neighbor.R - actorPosition.R) * line.RWeight;
                    double score = currentScore + (neighbor == targetPosition ? 0d : offset * offset);

                    if (_scores.TryGetValue(neighbor, out double previousScore))
                    {
                        if (score < previousScore)
                        {
                            _scores[neighbor] = score;
                            _parents[neighbor] = current;
                        }

                        continue;
                    }

                    _parents.Add(neighbor, current);
                    _scores.Add(neighbor, score);
                    _frontier.Enqueue(neighbor);
                }
            }

            return false;
        }

        static long GetDistance(HexCoordinate from, HexCoordinate to)
        {
            long q = (long)to.Q - from.Q;
            long r = (long)to.R - from.R;
            return Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(q + r)));
        }
    }
}
