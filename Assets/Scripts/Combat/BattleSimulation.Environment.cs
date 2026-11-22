using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        readonly Queue<BattleDamageEvent> _pendingDamage = new Queue<BattleDamageEvent>();
        readonly List<int> _environmentDeaths = new List<int>();

        public bool HasPendingDamage => _pendingDamage.Count > 0;

        public bool TryDequeueDamage(out BattleDamageEvent damage)
        {
            if (_pendingDamage.Count == 0)
            {
                damage = default;
                return false;
            }

            damage = _pendingDamage.Dequeue();
            return true;
        }

        public void DiscardPendingDamage()
        {
            _pendingDamage.Clear();
        }

        public bool TryApplyGroundEffect(HexCoordinate coordinate, BattleGroundEffect effect, out string error)
        {
            if (!CanChangeEnvironment(coordinate, out error))
            {
                return false;
            }

            if (effect == BattleGroundEffect.None || (uint)effect > (uint)BattleGroundEffect.Electrified)
            {
                error = "A ground effect must be a defined nonempty effect.";
                return false;
            }

            _board.TryGetEnvironment(coordinate, out BattleEnvironmentState current);

            if (!BattleEnvironmentRules.TryApplyGround(current, effect, out BattleEnvironmentState result))
            {
                error = "The environment reaction did not reach a stable state.";
                return false;
            }

            return ApplyEnvironmentChange(coordinate, current, result);
        }

        public bool TryApplyAirEffect(HexCoordinate coordinate, BattleAirEffect effect, out string error)
        {
            if (!CanChangeEnvironment(coordinate, out error))
            {
                return false;
            }

            if (effect == BattleAirEffect.None || (uint)effect > (uint)BattleAirEffect.ElectricCloud)
            {
                error = "An air effect must be a defined nonempty effect.";
                return false;
            }

            _board.TryGetEnvironment(coordinate, out BattleEnvironmentState current);

            if (!BattleEnvironmentRules.TryApplyAir(current, effect, out BattleEnvironmentState result))
            {
                error = "The environment reaction did not reach a stable state.";
                return false;
            }

            return ApplyEnvironmentChange(coordinate, current, result);
        }

        public bool TryClearEnvironment(HexCoordinate coordinate, bool clearGround, bool clearAir, out string error)
        {
            if (!CanChangeEnvironment(coordinate, out error))
            {
                return false;
            }

            if (!clearGround && !clearAir)
            {
                error = "At least one environment slot must be selected for removal.";
                return false;
            }

            _board.TryGetEnvironment(coordinate, out BattleEnvironmentState current);
            return ApplyEnvironmentChange(coordinate, current, new BattleEnvironmentState(
                clearGround ? BattleGroundEffect.None : current.Ground,
                clearAir ? BattleAirEffect.None : current.Air));
        }

        bool CanChangeEnvironment(HexCoordinate coordinate, out string error)
        {
            error = null;

            if (_state != BattleState.Running || IsBusy)
            {
                error = "Environment changes require a running battle with no action in progress.";
                return false;
            }

            if (!_board.IsValid(coordinate))
            {
                error = "The environment coordinate is outside the battle map.";
                return false;
            }

            return true;
        }

        bool ApplyEnvironmentChange(HexCoordinate coordinate, BattleEnvironmentState previous,
            BattleEnvironmentState current, bool deferDeath = false)
        {
            if (!_board.TrySetEnvironment(coordinate, current))
            {
                return false;
            }

            if (previous.Ground == current.Ground && previous.Air == current.Air)
            {
                return true;
            }

            if (_board.TryGetUnit(coordinate, out HexCharacter unit))
            {
                ApplyEnvironmentDamage(unit,
                    previous.Ground != current.Ground ? current.Ground : BattleGroundEffect.None,
                    previous.Air != current.Air ? current.Air : BattleAirEffect.None, deferDeath);
            }

            return true;
        }

        void ApplyEnvironmentAt(HexCharacter unit)
        {
            if (_board.TryGetEnvironment(unit.Coordinate, out BattleEnvironmentState environment))
            {
                ApplyEnvironmentDamage(unit, environment.Ground, environment.Air, false);
            }
        }

        void ApplyEnvironmentDamage(HexCharacter unit, BattleGroundEffect ground, BattleAirEffect air, bool deferDeath)
        {
            if (unit.Health <= 0)
            {
                return;
            }

            long fire = ground == BattleGroundEffect.Burning ? 1 : 0;
            long poison = (ground == BattleGroundEffect.Poisoned ? 1 : 0) + (air == BattleAirEffect.PoisonCloud ? 1 : 0);
            long electricity = (ground == BattleGroundEffect.Electrified ? 1 : 0) + (air == BattleAirEffect.ElectricCloud ? 1 : 0);

            if (fire == 0 && poison == 0 && electricity == 0)
            {
                return;
            }

            AttackAttribute? attribute = fire > 0 && poison == 0 && electricity == 0 ? AttackAttribute.Fire
                : poison > 0 && fire == 0 && electricity == 0 ? AttackAttribute.Poison
                : electricity > 0 && fire == 0 && poison == 0 ? AttackAttribute.Electricity : (AttackAttribute?)null;
            BattleDamageBreakdown breakdown = new BattleDamageBreakdown(
                fire: Math.Max(0L, fire - unit.Stats.FireDefense),
                electricity: Math.Max(0L, electricity - unit.Stats.ElectricityDefense),
                poison: Math.Max(0L, poison - unit.Stats.PoisonDefense));
            bool stealthed = unit.IsStealthed;
            int damage = unit.ApplyDamage(breakdown.Total);

            if (stealthed && !unit.IsStealthed)
            {
                InvalidateVisibility();
            }

            BattleAttackResult result = new BattleAttackResult(unit.Id, damage, unit.Health,
                attribute, breakdown, false, false);
            _pendingDamage.Enqueue(new BattleDamageEvent(BattleDamageOrigin.Environment, unit.Coordinate, result));

            if (unit.Health == 0)
            {
                if (deferDeath)
                {
                    _environmentDeaths.Add(unit.Id);
                }
                else
                {
                    RemoveUnit(unit.Id);
                }
            }
        }
    }
}
