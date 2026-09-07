using System;
using System.Collections.Generic;
using AutoBattler.HexGrid;

using Unity.Profiling;

namespace AutoBattler.Combat
{
    public sealed class BattleMovementSystem
    {
        const float _maximumMoveGauge = 2f;

        static readonly Comparison<BattleUnit> _movementPriority = CompareMovementPriority;

        readonly BattleBoard _board;
        readonly BattleTargetingSystem _targetingSystem;
        readonly List<BattleUnit> _units = new List<BattleUnit>();

        static readonly ProfilerMarker _bfsMarker = new ProfilerMarker("BFS");

        public BattleMovementSystem(BattleBoard board, BattleTargetingSystem targetingSystem)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _targetingSystem = targetingSystem ?? throw new ArgumentNullException(nameof(targetingSystem));
        }

        public void Tick(float tickDuration)
        {
            if (tickDuration <= 0f || float.IsNaN(tickDuration) || float.IsInfinity(tickDuration))
            {
                return;
            }

            _board.CopyUnitsTo(_units);

            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (!_units[i].IsAlive)
                {
                    _units.RemoveAt(i);
                }
            }

            _units.Sort(_movementPriority);

            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];

                _targetingSystem.ValidateCurrentTarget(unit);

                if (unit.IsAttacking || unit.Target != null)
                {
                    continue;
                }

                if (_targetingSystem.TryAcquireTargetInRange(unit))
                {
                    continue;
                }

                unit.MoveGauge = Math.Min(
                    unit.MoveGauge + unit.MoveSpeed * tickDuration,
                    _maximumMoveGauge);

                HexCoordinate nextStep;

                using (_bfsMarker.Auto())
                {
                    if (!_targetingSystem.TryAcquireReachableTarget(unit, out nextStep))
                    {
                        continue;
                    }
                }

                if (unit.MoveGauge < 1f)
                {
                    continue;
                }

                if (!_board.TryMove(unit, nextStep))
                {
                    continue;
                }

                unit.MoveGauge -= 1f;
            }
        }

        static int CompareMovementPriority(BattleUnit left, BattleUnit right)
        {
            int speedComparison = right.MoveSpeed.CompareTo(left.MoveSpeed);

            if (speedComparison != 0)
            {
                return speedComparison;
            }

            return right.Id.CompareTo(left.Id);
        }
    }
}
