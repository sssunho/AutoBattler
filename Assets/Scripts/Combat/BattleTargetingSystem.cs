using System;
using System.Collections.Generic;
using AutoBattler.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class BattleTargetingSystem
    {
        readonly BattleBoard _board;
        readonly BattlePathfinder _pathfinder;
        readonly List<BattleUnit> _candidates = new List<BattleUnit>();
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();

        public BattleTargetingSystem(BattleBoard board, BattlePathfinder pathfinder)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        }

        public void ValidateCurrentTarget(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (!IsValidTarget(unit, unit.Target)
                || unit.Coordinate.DistanceTo(unit.Target.Coordinate) > unit.AttackRange)
            {
                unit.Target = null;
            }
        }

        public bool TryAcquireTargetInRange(BattleUnit unit)
        {
            if (unit == null)
            {
                return false;
            }

            unit.Target = null;
            CollectCandidates(unit);
            int bestDistance = int.MaxValue;

            for (int i = 0; i < _candidates.Count; i++)
            {
                BattleUnit candidate = _candidates[i];
                int distance = unit.Coordinate.DistanceTo(candidate.Coordinate);

                if (distance > unit.AttackRange)
                {
                    continue;
                }

                if (unit.Target == null || distance < bestDistance
                    || (distance == bestDistance && candidate.Id > unit.Target.Id))
                {
                    unit.Target = candidate;
                    bestDistance = distance;
                }
            }

            return unit.Target != null;
        }

        public bool TryAcquireReachableTarget(BattleUnit unit, out HexCoordinate nextStep)
        {
            nextStep = default;

            if (unit == null)
            {
                return false;
            }

            unit.Target = null;
            CollectCandidates(unit);

            if (!_pathfinder.TryFindPathToAttackPosition(_board, unit, _candidates, _path, out BattleUnit target)
                || _path.Count == 0)
            {
                return false;
            }

            unit.Target = target;
            nextStep = _path[0];
            return true;
        }

        void CollectCandidates(BattleUnit unit)
        {
            _board.CopyUnitsTo(_candidates);
            int count = 0;

            for (int i = 0; i < _candidates.Count; i++)
            {
                BattleUnit candidate = _candidates[i];

                if (IsValidTarget(unit, candidate))
                {
                    _candidates[count] = candidate;
                    count++;
                }
            }

            _candidates.RemoveRange(count, _candidates.Count - count);
        }

        bool IsValidTarget(BattleUnit unit, BattleUnit target)
        {
            return unit != null
                && target != null
                && unit.IsAlive
                && target.IsAlive
                && unit.Team != target.Team
                && IsPlacedUnit(unit)
                && IsPlacedUnit(target);
        }

        bool IsPlacedUnit(BattleUnit unit)
        {
            return _board.TryGetUnit(unit.Id, out BattleUnit registeredUnit)
                && ReferenceEquals(unit, registeredUnit)
                && _board.TryGetUnit(unit.Coordinate, out BattleUnit occupyingUnit)
                && ReferenceEquals(unit, occupyingUnit);
        }
    }
}
