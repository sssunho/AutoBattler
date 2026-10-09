using System;
using System.Collections.Generic;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattleAttackSystem
    {
        readonly AutoBattlerBattleBoard _board;
        readonly List<AutoBattlerBattleUnit> _units = new List<AutoBattlerBattleUnit>();
        readonly List<AutoBattlerBattleUnit> _hitTargets = new List<AutoBattlerBattleUnit>();
        readonly List<int> _hitDamages = new List<int>();

        public AutoBattlerBattleAttackSystem(AutoBattlerBattleBoard board)
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
                AutoBattlerBattleUnit unit = _units[i];

                if (!unit.IsAlive)
                {
                    continue;
                }

                if (unit.AttackPhase == AutoBattlerBattleAttackPhase.None && !TryStartAttack(unit))
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

        bool TryStartAttack(AutoBattlerBattleUnit unit)
        {
            AutoBattlerBattleUnit target = unit.Target;

            if (!IsValidTarget(unit, target)
                || unit.Coordinate.DistanceTo(target.Coordinate) > unit.AttackRange)
            {
                return false;
            }

            unit.AttackPhase = AutoBattlerBattleAttackPhase.Animation;
            unit.AttackTarget = target;
            unit.AttackElapsed = 0f;
            return true;
        }

        void AdvanceAttack(AutoBattlerBattleUnit unit, float deltaTime)
        {
            float remainingTime = deltaTime;

            if (unit.AttackPhase == AutoBattlerBattleAttackPhase.Animation)
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

                unit.AttackPhase = AutoBattlerBattleAttackPhase.Cooldown;
                unit.AttackElapsed = 0f;

                if (unit.AttackCooldownDuration == 0f)
                {
                    unit.ClearAttackState();
                    return;
                }
            }

            if (unit.AttackPhase != AutoBattlerBattleAttackPhase.Cooldown)
            {
                return;
            }

            unit.AttackElapsed += remainingTime;

            if (unit.AttackElapsed >= unit.AttackCooldownDuration)
            {
                unit.ClearAttackState();
            }
        }

        bool IsValidTarget(AutoBattlerBattleUnit attacker, AutoBattlerBattleUnit target)
        {
            return target != null
                && !ReferenceEquals(attacker, target)
                && attacker.Team != target.Team
                && target.IsAlive
                && IsPlacedUnit(attacker)
                && IsPlacedUnit(target);
        }

        bool IsPlacedUnit(AutoBattlerBattleUnit unit)
        {
            return _board.TryGetUnit(unit.Id, out AutoBattlerBattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit)
                && _board.TryGetUnit(unit.Coordinate, out AutoBattlerBattleUnit occupyingUnit)
                && ReferenceEquals(occupyingUnit, unit);
        }
    }
}
