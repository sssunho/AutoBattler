using System;
using System.Collections.Generic;

namespace AutoBattler.Combat
{
    public sealed class BattleAttackSystem
    {
        readonly BattleBoard _board;
        readonly List<BattleUnit> _units = new List<BattleUnit>();
        readonly List<BattleUnit> _hitTargets = new List<BattleUnit>();
        readonly List<int> _hitDamages = new List<int>();

        public BattleAttackSystem(BattleBoard board)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            _board.CopyUnitsTo(_units);
            _hitTargets.Clear();
            _hitDamages.Clear();

            for (int i = 0; i < _units.Count; i++)
            {
                BattleUnit unit = _units[i];

                if (!unit.IsAlive)
                {
                    continue;
                }

                if (unit.AttackPhase == BattleAttackPhase.None && !TryStartAttack(unit))
                {
                    continue;
                }

                AdvanceAttack(unit, deltaTime);
            }

            for (int i = 0; i < _hitTargets.Count; i++)
            {
                _board.TryApplyDamage(_hitTargets[i], _hitDamages[i]);
            }
        }

        bool TryStartAttack(BattleUnit unit)
        {
            BattleUnit target = unit.Target;

            if (!IsValidTarget(unit, target)
                || unit.Coordinate.DistanceTo(target.Coordinate) > unit.AttackRange)
            {
                return false;
            }

            unit.AttackPhase = BattleAttackPhase.Animation;
            unit.AttackTarget = target;
            unit.AttackElapsed = 0f;
            return true;
        }

        void AdvanceAttack(BattleUnit unit, float deltaTime)
        {
            float remainingTime = deltaTime;

            if (unit.AttackPhase == BattleAttackPhase.Animation)
            {
                float previousElapsed = unit.AttackElapsed;
                float animationRemaining = unit.AttackAnimationDuration - previousElapsed;
                float animationAdvance = Math.Min(remainingTime, animationRemaining);
                float nextElapsed = previousElapsed + animationAdvance;
                float hitTime = unit.AttackAnimationDuration * unit.AttackHitNormalizedTime;

                if (previousElapsed < hitTime && nextElapsed >= hitTime)
                {
                    _hitTargets.Add(unit.AttackTarget);
                    _hitDamages.Add(unit.AttackDamage);
                }

                unit.AttackElapsed = nextElapsed;
                remainingTime -= animationAdvance;

                if (unit.AttackElapsed < unit.AttackAnimationDuration)
                {
                    return;
                }

                unit.AttackPhase = BattleAttackPhase.Cooldown;
                unit.AttackElapsed = 0f;

                if (unit.AttackCooldownDuration == 0f)
                {
                    unit.ClearAttackState();
                    return;
                }
            }

            if (unit.AttackPhase != BattleAttackPhase.Cooldown)
            {
                return;
            }

            unit.AttackElapsed += remainingTime;

            if (unit.AttackElapsed >= unit.AttackCooldownDuration)
            {
                unit.ClearAttackState();
            }
        }

        bool IsValidTarget(BattleUnit attacker, BattleUnit target)
        {
            return target != null
                && !ReferenceEquals(attacker, target)
                && attacker.Team != target.Team
                && target.IsAlive
                && IsPlacedUnit(attacker)
                && IsPlacedUnit(target);
        }

        bool IsPlacedUnit(BattleUnit unit)
        {
            return _board.TryGetUnit(unit.Id, out BattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit)
                && _board.TryGetUnit(unit.Coordinate, out BattleUnit occupyingUnit)
                && ReferenceEquals(occupyingUnit, unit);
        }
    }
}
