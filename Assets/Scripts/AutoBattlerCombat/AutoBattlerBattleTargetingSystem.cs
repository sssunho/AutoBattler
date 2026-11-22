using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattleTargetingSystem
    {
        readonly AutoBattlerBattleBoard _board;
        readonly AutoBattlerBattlePathfinder _pathfinder;
        readonly List<AutoBattlerBattleUnit> _candidates = new List<AutoBattlerBattleUnit>();
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();

        public AutoBattlerBattleTargetingSystem(AutoBattlerBattleBoard board, AutoBattlerBattlePathfinder pathfinder)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        }

        public void ValidateCurrentTarget(AutoBattlerBattleUnit unit)
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

        public bool TryAcquireTargetInRange(AutoBattlerBattleUnit unit)
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
                AutoBattlerBattleUnit candidate = _candidates[i];
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

        public bool TryAcquireReachableTarget(AutoBattlerBattleUnit unit, out HexCoordinate nextStep)
        {
            nextStep = default;

            if (unit == null)
            {
                return false;
            }

            unit.Target = null;
            CollectCandidates(unit);

            if (!_pathfinder.TryFindPathToAttackPosition(_board, unit, _candidates, _path, out AutoBattlerBattleUnit target)
                || _path.Count == 0)
            {
                return false;
            }

            unit.Target = target;
            nextStep = _path[0];
            return true;
        }

        void CollectCandidates(AutoBattlerBattleUnit unit)
        {
            _board.CopyUnitsTo(_candidates);
            int count = 0;

            for (int i = 0; i < _candidates.Count; i++)
            {
                AutoBattlerBattleUnit candidate = _candidates[i];

                if (IsValidTarget(unit, candidate))
                {
                    _candidates[count] = candidate;
                    count++;
                }
            }

            _candidates.RemoveRange(count, _candidates.Count - count);
        }

        bool IsValidTarget(AutoBattlerBattleUnit unit, AutoBattlerBattleUnit target)
        {
            return unit != null
                && target != null
                && unit.IsAlive
                && target.IsAlive
                && unit.Team != target.Team
                && IsPlacedUnit(unit)
                && IsPlacedUnit(target);
        }

        bool IsPlacedUnit(AutoBattlerBattleUnit unit)
        {
            return _board.TryGetUnit(unit.Id, out AutoBattlerBattleUnit registeredUnit)
                && ReferenceEquals(unit, registeredUnit)
                && _board.TryGetUnit(unit.Coordinate, out AutoBattlerBattleUnit occupyingUnit)
                && ReferenceEquals(unit, occupyingUnit);
        }
    }
}
