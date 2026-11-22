using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat
{
    public sealed class BattleEnemyAI
    {
        readonly HexPathfinder _pathfinder = new HexPathfinder();
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();
        readonly List<HexPathfinder.ReachablePosition> _reachablePositions = new List<HexPathfinder.ReachablePosition>();

        public BattleAiDecision Decide(BattleSimulation simulation)
        {
            if (simulation == null || simulation.State != BattleState.Running || simulation.IsBusy
                || !simulation.CurrentUnitId.HasValue
                || !simulation.Board.TryGetUnit(simulation.CurrentUnitId.Value, out HexCharacter unit)
                || unit.Team != BattleTeam.Enemy)
            {
                return default;
            }

            bool prefersRear;
            switch (unit.Stats.UnitClass)
            {
                case MercenaryClass.Warrior:
                    return ChooseApproach(simulation, unit);
                case MercenaryClass.Rogue:
                    prefersRear = true;
                    break;
                case MercenaryClass.Archer:
                    prefersRear = false;
                    break;
                default:
                    return default;
            }

            if (unit.ActionPoints >= unit.Skills.BasicAttack.ActionPointCost
                && TryChooseAttackPosition(simulation, unit, prefersRear, out BattleAiDecision attack))
            {
                return attack;
            }

            return ChooseApproach(simulation, unit);
        }

        bool TryChooseAttackPosition(BattleSimulation simulation, HexCharacter unit, bool prefersRear,
            out BattleAiDecision decision)
        {
            decision = default;
            int targetId = 0;

            if (!prefersRear)
            {
                foreach (HexCharacter target in simulation.Board.Units)
                {
                    if (target.Team != unit.Team
                        && simulation.IsVisibleTo(unit.Team, target.Id)
                        && unit.Skills.IsInRange(SkillType.BasicAttack, unit.Coordinate.DistanceTo(target.Coordinate))
                        && simulation.HasSkillLineOfSight(unit, SkillType.BasicAttack, unit.Coordinate, target.Coordinate)
                        && (targetId == 0 || target.Id < targetId))
                    {
                        targetId = target.Id;
                    }
                }

                if (targetId != 0)
                {
                    decision = new BattleAiDecision(BattleAiDecisionKind.Attack, unit.Id, targetId, unit.Coordinate);
                    return true;
                }
            }

            _pathfinder.FindReachablePositions(simulation.Board, unit.Coordinate, unit.MovementRemaining, _reachablePositions);
            bool bestIsRear = false;
            int bestAttackDistance = 0;
            int shortestDistance = int.MaxValue;
            HexCoordinate destination = default;

            foreach (HexCharacter target in simulation.Board.Units)
            {
                if (target.Team == unit.Team || !simulation.IsVisibleTo(unit.Team, target.Id))
                {
                    continue;
                }

                for (int i = 0; i < _reachablePositions.Count; i++)
                {
                    HexPathfinder.ReachablePosition position = _reachablePositions[i];
                    int attackDistance = position.Coordinate.DistanceTo(target.Coordinate);

                    if (!unit.Skills.IsInRange(SkillType.BasicAttack, attackDistance)
                        || !simulation.HasSkillLineOfSight(unit, SkillType.BasicAttack, position.Coordinate, target.Coordinate))
                    {
                        continue;
                    }

                    bool isRear = prefersRear && BattleFacing.Classify(target.Facing, target.Coordinate,
                        position.Coordinate) == BattleRelativeDirection.Rear;
                    bool betterPriority = prefersRear ? isRear && !bestIsRear : attackDistance > bestAttackDistance;
                    bool equalPriority = prefersRear ? isRear == bestIsRear : attackDistance == bestAttackDistance;

                    if (targetId == 0 || betterPriority
                        || (equalPriority && (position.Distance < shortestDistance
                            || (position.Distance == shortestDistance && target.Id < targetId))))
                    {
                        targetId = target.Id;
                        bestIsRear = isRear;
                        bestAttackDistance = attackDistance;
                        shortestDistance = position.Distance;
                        destination = position.Coordinate;
                    }
                }
            }

            if (targetId == 0)
            {
                return false;
            }

            decision = new BattleAiDecision(BattleAiDecisionKind.Attack, unit.Id, targetId, destination);
            return true;
        }

        BattleAiDecision ChooseApproach(BattleSimulation simulation, HexCharacter unit)
        {
            int targetId = 0;
            int shortestDistance = int.MaxValue;
            HexCoordinate destination = default;

            foreach (HexCharacter target in simulation.Board.Units)
            {
                if (target.Team != BattleTeam.Player || !simulation.IsVisibleTo(unit.Team, target.Id))
                {
                    continue;
                }

                int distance = 0;
                HexCoordinate candidateDestination = default;

                if (!unit.Skills.IsInRange(SkillType.BasicAttack, unit.Coordinate.DistanceTo(target.Coordinate))
                    || !simulation.HasSkillLineOfSight(unit, SkillType.BasicAttack, unit.Coordinate, target.Coordinate))
                {
                    if (unit.MovementRemaining == 0
                        || !_pathfinder.TryFindPathToAttackPosition(simulation.Board, unit.Coordinate, target.Coordinate, unit.Skills.BasicAttack.MaxRange, _path, unit.Skills.BasicAttack.MinRange))
                    {
                        continue;
                    }

                    distance = _path.Count;
                    candidateDestination = _path[Math.Min(distance, unit.MovementRemaining) - 1];
                }

                if (distance < shortestDistance || (distance == shortestDistance && target.Id < targetId))
                {
                    shortestDistance = distance;
                    targetId = target.Id;
                    destination = candidateDestination;
                }
            }

            if (targetId != 0)
            {
                if (shortestDistance > 0)
                {
                    return new BattleAiDecision(BattleAiDecisionKind.Move, unit.Id, targetId, destination);
                }

                if (unit.ActionPoints >= unit.Skills.BasicAttack.ActionPointCost)
                {
                    return new BattleAiDecision(BattleAiDecisionKind.Attack, unit.Id, targetId, unit.Coordinate);
                }
            }

            return new BattleAiDecision(BattleAiDecisionKind.EndTurn, unit.Id, 0, default);
        }
    }
}
