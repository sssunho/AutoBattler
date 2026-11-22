using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattlePathfinder
    {
        static readonly HexDirection[] _directions =
        {
            HexDirection.East,
            HexDirection.NorthEast,
            HexDirection.NorthWest,
            HexDirection.West,
            HexDirection.SouthWest,
            HexDirection.SouthEast
        };

        readonly Queue<HexCoordinate> _frontier = new Queue<HexCoordinate>();
        readonly Dictionary<HexCoordinate, int> _distances = new Dictionary<HexCoordinate, int>();
        readonly List<HexCoordinate> _destinations = new List<HexCoordinate>();
        readonly List<AutoBattlerBattleUnit> _validTargets = new List<AutoBattlerBattleUnit>();

        public bool TryFindPathToAttackPosition(
            AutoBattlerBattleBoard board,
            AutoBattlerBattleUnit mover,
            IReadOnlyList<AutoBattlerBattleUnit> targets,
            List<HexCoordinate> result,
            out AutoBattlerBattleUnit target)
        {
            target = null;

            if (result == null)
            {
                return false;
            }

            result.Clear();

            _frontier.Clear();
            _distances.Clear();
            _destinations.Clear();
            _validTargets.Clear();

            if (board == null || mover == null || targets == null
                || !mover.IsAlive || !IsPlacedUnit(board, mover))
            {
                return false;
            }

            HexCoordinate start = mover.Coordinate;
            int nearestDistance = int.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                AutoBattlerBattleUnit candidate = targets[i];

                if (!CanSearch(board, mover, candidate))
                {
                    continue;
                }

                _validTargets.Add(candidate);
                int distance = start.DistanceTo(candidate.Coordinate);

                if (distance <= mover.AttackRange
                    && (target == null || distance < nearestDistance
                        || (distance == nearestDistance && candidate.Id > target.Id)))
                {
                    target = candidate;
                    nearestDistance = distance;
                }
            }

            if (target != null)
            {
                return true;
            }

            if (_validTargets.Count == 0 || mover.AttackRange == 0)
            {
                return false;
            }

            _frontier.Enqueue(start);
            _distances.Add(start, 0);

            int minDist = int.MaxValue;

            while (_frontier.Count > 0)
            {
                HexCoordinate current = _frontier.Dequeue();
                int dist = _distances[current] + 1;

                if (dist > minDist)
                {
                    break;
                }

                for (int i = 0; i < _directions.Length; i++)
                {
                    HexCoordinate neighbor = current.GetNeighbor(_directions[i]);

                    if (!board.IsValid(neighbor)
                        || board.IsOccupied(neighbor)
                        || _distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    _distances.Add(neighbor, dist);

                    AutoBattlerBattleUnit candidate = FindTargetInRange(neighbor, mover.AttackRange);

                    if (candidate != null)
                    {
                        minDist = dist;

                        if (target == null || candidate.Id > target.Id)
                        {
                            target = candidate;
                            _destinations.Clear();
                        }

                        if (ReferenceEquals(candidate, target))
                        {
                            _destinations.Add(neighbor);
                        }

                        continue;
                    }

                    _frontier.Enqueue(neighbor);
                }
            }

            if (_destinations.Count > 0)
            {
                BuildPath(start, target.Coordinate, result);
                return true;
            }

            return false;
        }

        AutoBattlerBattleUnit FindTargetInRange(HexCoordinate coordinate, int attackRange)
        {
            AutoBattlerBattleUnit target = null;

            for (int i = 0; i < _validTargets.Count; i++)
            {
                AutoBattlerBattleUnit candidate = _validTargets[i];

                if (coordinate.DistanceTo(candidate.Coordinate) <= attackRange
                    && (target == null || candidate.Id > target.Id))
                {
                    target = candidate;
                }
            }

            return target;
        }

        static bool CanSearch(AutoBattlerBattleBoard board, AutoBattlerBattleUnit mover, AutoBattlerBattleUnit target)
        {
            if (board == null
                || mover == null
                || target == null
                || ReferenceEquals(mover, target)
                || !mover.IsAlive
                || !target.IsAlive
                || mover.Team == target.Team)
            {
                return false;
            }

            return IsPlacedUnit(board, mover) && IsPlacedUnit(board, target);
        }

        static bool IsPlacedUnit(AutoBattlerBattleBoard board, AutoBattlerBattleUnit unit)
        {
            return board.TryGetUnit(unit.Id, out AutoBattlerBattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit)
                && board.TryGetUnit(unit.Coordinate, out AutoBattlerBattleUnit occupyingUnit)
                && ReferenceEquals(occupyingUnit, unit);
        }

        void BuildPath(
            HexCoordinate start,
            HexCoordinate target,
            List<HexCoordinate> result)
        {
            HexCoordinate dest = SelectDestination(start, target);
            int curDist = _distances[dest];

            while (curDist > 0)
            {
                result.Add(dest);

                if (curDist == 1)
                {
                    break;
                }

                _destinations.Clear();

                for (int i = 0; i < _directions.Length; i++)
                {
                    HexCoordinate neighbor = dest.GetNeighbor(_directions[i]);
                    if (_distances.TryGetValue(neighbor, out int dist))
                    {
                        if (dist == curDist - 1)
                        {
                            _destinations.Add(neighbor);
                        }
                    }
                }

                dest = SelectDestination(start, target);
                curDist--;
            }

            result.Reverse();
        }

        HexCoordinate SelectDestination(HexCoordinate start, HexCoordinate target)
        {
            if (_destinations.Count == 0)
            {
                throw new System.Exception("have no destinaion");
            }

            HexCoordinate dir = target - start;
            HexCoordinate res = _destinations[0];
            double best = DirectionSimilarity(dir, res - start);

            for (int i = 1; i < _destinations.Count; i++)
            {
                double similarity = DirectionSimilarity(dir, _destinations[i] - start);

                if (similarity > best)
                {
                    res = _destinations[i];
                    best = similarity;
                }
            }

            return res;
        }

        static double DirectionSimilarity(
            HexCoordinate left,
            HexCoordinate right)
        {
            double leftLengthSquared = HexMath.Dot(left, left);
            double rightLengthSquared = HexMath.Dot(right, right);

            if (leftLengthSquared == 0d || rightLengthSquared == 0d)
            {
                return double.NegativeInfinity;
            }

            return HexMath.Dot(left, right) / System.Math.Sqrt(leftLengthSquared * rightLengthSquared);
        }
    }
}
