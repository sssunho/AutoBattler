using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        readonly List<BattleAttackResult> _singleAttackResults = new List<BattleAttackResult>();

        void ApplySpiritAt(HexCoordinate coordinate)
        {
            if (!_board.IsValid(coordinate))
            {
                return;
            }
            _board.TryGetEnvironment(coordinate, out BattleEnvironmentState state);
            BattleEnvironmentState previous = state;
            BattleGroundEffect ground;
            switch (_attackExecution.Spirit)
            {
                case SpiritType.Fire:
                    if (!BattleEnvironmentRules.TryApplyAir(state, BattleAirEffect.Smoke, out state))
                    {
                        return;
                    }
                    ground = BattleGroundEffect.Burning;
                    break;
                case SpiritType.Water:
                    ground = BattleGroundEffect.Wet;
                    break;
                case SpiritType.Poison:
                    ground = BattleGroundEffect.Poisoned;
                    break;
                default:
                    return;
            }
            if (BattleEnvironmentRules.TryApplyGround(state, ground, out BattleEnvironmentState result))
            {
                ApplyEnvironmentChange(coordinate, previous, result, true);
            }
        }

        public bool TryCompleteAttack(out BattleAttackResult result)
        {
            int primaryId = _attackExecution.Context.TargetId;
            bool completed = TryCompleteAttackBatch(_singleAttackResults);
            result = default;
            for (int i = 0; i < _singleAttackResults.Count; i++)
            {
                if (_singleAttackResults[i].TargetId == primaryId)
                {
                    result = _singleAttackResults[i];
                    return completed;
                }
            }
            return false;
        }

        public bool TryCompleteAttackBatch(List<BattleAttackResult> results)
        {
            bool resolved = TryResolveAttackDamage(results, true);
            TryFinishAttackPresentation();
            return resolved;
        }

        public bool IsAttackDamageResolved => _phase == BattleActionPhase.Attacking && _attackExecution.DamageResolved;

        public bool HasAttackParticipants => _phase == BattleActionPhase.Attacking
            && TryGetOpponents(_attackExecution.Context.AttackerId, _attackExecution.Context.TargetId, out _, out _);

        public bool TryFinishAttackPresentation()
        {
            if (_phase != BattleActionPhase.Attacking || !_attackExecution.DamageResolved)
            {
                return false;
            }

            FinishAttack();
            return true;
        }

        public bool TryAbortAttack()
        {
            if (_phase != BattleActionPhase.Attacking)
            {
                return false;
            }

            FinishAttack();
            return true;
        }

        public bool TryVisitAttackCell(HexCoordinate coordinate)
        {
            if (!HasAttackParticipants || _attackExecution.DamageResolved
                || _attackExecution.SpiritActor == null || coordinate == _attackExecution.SpiritActor.Coordinate
                || !_board.IsValid(coordinate) || !_attackExecution.Visit(coordinate))
            {
                return false;
            }

            ApplySpiritAt(coordinate);
            if (_attackExecution.Spirit == SpiritType.Electricity
                && _board.TryGetUnit(coordinate, out HexCharacter target)
                && target.Health > 0 && target.Team != _attackExecution.SpiritActor.Team
                && !_attackExecution.Targets.Contains(target))
            {
                _attackExecution.Targets.Add(target);
            }

            for (int i = 0; i < _environmentDeaths.Count; i++)
            {
                RemoveUnit(_environmentDeaths[i]);
            }

            _environmentDeaths.Clear();
            return true;
        }

        public bool TryResolveAttackDamage(List<BattleAttackResult> results, bool applyWholePath)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }
            results.Clear();
            if (_phase != BattleActionPhase.Attacking || _attackExecution.DamageResolved)
            {
                return false;
            }
            bool hasSpirit = _attackExecution.SpiritActor != null;
            if (applyWholePath)
            {
                for (int i = 0; i < _attackExecution.Path.Count && HasAttackParticipants; i++)
                {
                    TryVisitAttackCell(_attackExecution.Path[i]);
                }
            }
            _attackExecution.DamageResolved = true;
            bool hasPrimary = TryGetOpponents(_attackExecution.Context.AttackerId, _attackExecution.Context.TargetId, out HexCharacter attacker, out HexCharacter target)
                && attacker.Skills.IsInRange(_attackExecution.Context.Kind == BattleAttackKind.Ambush ? SkillType.Ambush : SkillType.BasicAttack,
                    attacker.Coordinate.DistanceTo(target.Coordinate));
            if (hasPrimary)
            {
                bool electric = _attackExecution.Targets.Contains(target);
                results.Add(ApplyAttackDamage(attacker, target, true,
                    _attackExecution.Spirit == SpiritType.Fire ? _attackExecution.FireDamage : 0,
                    electric ? _attackExecution.ElectricDamage : 0));
            }
            for (int i = 0; hasPrimary && i < _attackExecution.Targets.Count; i++)
            {
                HexCharacter secondary = _attackExecution.Targets[i];
                if (secondary.Health > 0 && (!hasPrimary || secondary.Id != target.Id))
                {
                    results.Add(ApplyAttackDamage(_attackExecution.SpiritActor, secondary, false, 0, _attackExecution.ElectricDamage));
                }
            }
            for (int i = 0; i < _environmentDeaths.Count; i++)
            {
                RemoveUnit(_environmentDeaths[i]);
            }
            _environmentDeaths.Clear();
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].TargetDied)
                {
                    RemoveUnit(results[i].TargetId);
                }
            }
            return hasPrimary || hasSpirit;
        }

        BattleAttackResult ApplyAttackDamage(HexCharacter attacker, HexCharacter target, bool primary, int fireBonus, int electricBonus)
        {
            BattleResolvedDamage resolved = BattleDamageResolver.Resolve(attacker, target, _attackExecution.Context,
                primary, fireBonus, electricBonus, _combatRandom, _criticalDamageBonus);
            bool stealthed = target.IsStealthed;
            int damage = target.ApplyDamage(resolved.Breakdown.Total);
            if (stealthed && !target.IsStealthed)
            {
                InvalidateVisibility();
            }
            BattleAttackResult result = new BattleAttackResult(target.Id, damage, target.Health, resolved.Attribute, resolved.Breakdown,
                resolved.IsCritical, resolved.WasNegated);
            _pendingDamage.Enqueue(new BattleDamageEvent(BattleDamageOrigin.Attack, target.Coordinate, result));
            return result;
        }
    }
}
