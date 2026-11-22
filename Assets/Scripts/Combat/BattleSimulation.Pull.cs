using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        public bool IsPullMovement => _skillExecution != null && SkillTypeRules.IsPull(_skillExecution.Definition.Type);
        public bool HasStartedPullMovement => IsPullMovement && _skillExecution.HasStartedMovement;

        public bool CanSelectPullSkill(int unitId)
        {
            return CanSelectSkill(unitId, SkillType.PullSelf).CanUse
                || CanSelectSkill(unitId, SkillType.PullTarget).CanUse;
        }

        public bool CanUsePullSkill(int actorId, int targetId, SkillType skill)
        {
            return TryCopyPullPath(actorId, targetId, skill, null);
        }

        public bool TryCopyPullPath(int actorId, int targetId, SkillType skill,
            List<HexCoordinate> destination)
        {
            return TryCopyPullPathCore(actorId, targetId, skill, null, destination);
        }

        public bool TryCopyPullPath(int actorId, int targetId, SkillType skill,
            BattlePathLine line, List<HexCoordinate> destination)
        {
            return TryCopyPullPathCore(actorId, targetId, skill, line, destination);
        }

        bool TryCopyPullPathCore(int actorId, int targetId, SkillType skill,
            BattlePathLine? line, List<HexCoordinate> destination)
        {
            destination?.Clear();
            return SkillTypeRules.IsPull(skill) && CanSelectSkill(actorId, skill).CanUse
                && TryGetOpponents(actorId, targetId, out HexCharacter actor, out HexCharacter target)
                && IsVisibleTo(actor.Team, targetId)
                && TryBuildPullPath(actor, target, skill, line ?? GetPullCenterLine(actor, target), destination);
        }

        bool ExecutePullSkill(int actorId, int targetId, SkillType skill,
            IReadOnlyList<HexCoordinate> path)
        {
            if (!SkillTypeRules.IsPull(skill) || !CanSelectSkill(actorId, skill).CanUse
                || !TryGetOpponents(actorId, targetId, out HexCharacter actor, out HexCharacter target)
                || !IsVisibleTo(actor.Team, targetId) || !IsValidPullPath(actor, target, skill, path))
            {
                return false;
            }

            _actingUnitId = actorId;
            _targetUnitId = targetId;
            _movement.Begin(path, skill == SkillType.PullSelf ? actorId : targetId,
                skill == SkillType.PullSelf ? MovementKind.Skill : MovementKind.Forced,
                actor.Coordinate, target.Coordinate);
            _phase = BattleActionPhase.Moving;
            return true;
        }

        bool CanUsePull(HexCharacter actor, SkillType skill)
        {
            return actor.Health > 0 && HasSkillResources(actor, skill);
        }

        bool TryBuildPullPath(HexCharacter actor, HexCharacter target, SkillType skill,
            BattlePathLine line, List<HexCoordinate> destination)
        {
            destination?.Clear();
            return actor.Skills.IsInRange(skill, GetHexDistance(actor.Coordinate, target.Coordinate))
                && HasSkillLineOfSight(actor, skill, actor.Coordinate, target.Coordinate)
                && _pullPathfinder.TryFindPath(_board, actor.Coordinate, target.Coordinate, skill, line, destination);
        }

        static BattlePathLine GetPullCenterLine(HexCharacter actor, HexCharacter target)
        {
            return new BattlePathLine((long)target.Coordinate.R - actor.Coordinate.R,
                (long)actor.Coordinate.Q - target.Coordinate.Q);
        }

        bool IsValidPullPath(HexCharacter actor, HexCharacter target, SkillType skill,
            IReadOnlyList<HexCoordinate> path)
        {
            long distance = GetHexDistance(actor.Coordinate, target.Coordinate);

            if (path == null
                || !actor.Skills.IsInRange(skill, distance) || path.Count != distance - 1
                || !HasSkillLineOfSight(actor, skill, actor.Coordinate, target.Coordinate))
            {
                return false;
            }

            HexCoordinate current = skill == SkillType.PullSelf ? actor.Coordinate : target.Coordinate;
            HexCoordinate anchor = skill == SkillType.PullSelf ? target.Coordinate : actor.Coordinate;

            for (int i = 0; i < path.Count; i++)
            {
                HexCoordinate next = path[i];

                if (!_board.IsValid(next) || _board.IsOccupied(next)
                    || GetHexDistance(current, next) != 1
                    || GetHexDistance(next, anchor) != distance - i - 1)
                {
                    return false;
                }

                current = next;
            }

            return true;
        }

        bool TryBeginPullStep(out HexMovementStep step)
        {
            step = default;

            if (_state != BattleState.Running || _movement.StopRequested
                || !TryGetOpponents(_actingUnitId, _targetUnitId, out HexCharacter actor, out HexCharacter target)
                || !_board.TryGetUnit(_movement.MoverId, out HexCharacter mover))
            {
                StopPullMovement();
                return false;
            }

            if (!HasStartedPullMovement)
            {
                if (CurrentUnitId != actor.Id || !CanUsePull(actor, _skillExecution.Definition.Type) || !IsVisibleTo(actor.Team, target.Id)
                    || actor.Coordinate != _movement.ActorOrigin || target.Coordinate != _movement.TargetOrigin
                    || !IsValidPullPath(actor, target, _skillExecution.Definition.Type, _movement.Path))
                {
                    StopPullMovement();
                    return false;
                }
            }

            HexCoordinate expectedMover = _movement.Index == 0
                ? _skillExecution.Definition.Type == SkillType.PullSelf ? _movement.ActorOrigin : _movement.TargetOrigin
                : _movement.Path[_movement.Index - 1];
            HexCoordinate anchor = _skillExecution.Definition.Type == SkillType.PullSelf ? target.Coordinate : actor.Coordinate;
            HexCoordinate expectedAnchor = _skillExecution.Definition.Type == SkillType.PullSelf
                ? _movement.TargetOrigin : _movement.ActorOrigin;
            HexCoordinate origin = mover.Coordinate;
            HexCoordinate destination = _movement.Path[_movement.Index];
            MovementKind kind = _movement.Kind;

            if (origin != expectedMover || anchor != expectedAnchor
                || EvaluateMovement(new BattleMovementRequest(mover.Id, origin, destination, kind))
                    != BattleMovementPermission.Allowed)
            {
                StopPullMovement();
                return false;
            }

            if (!HasStartedPullMovement)
            {
                SpendSkillCost(actor);
                actor.SetFacing(BattleFacing.GetDirection(actor.Coordinate, target.Coordinate));
                RefreshDetection();
                _skillExecution.HasStartedMovement = true;
            }

            if (!_board.TryMove(mover.Id, destination))
            {
                StopPullMovement();
                return false;
            }

            if (_skillExecution.Definition.Type == SkillType.PullSelf)
            {
                mover.SetFacing(BattleFacing.GetDirection(origin, destination));
            }

            if (_movement.Index == 0)
            {
                actor.ApplySkillMovementRecovery();
            }

            step = new HexMovementStep(mover.Id, origin, destination);
            _movement.MarkStepIssued();
            return true;
        }

        void StopPullMovement()
        {
            _movement.RequestStop();

            if (!_movement.WaitingForCompletion)
            {
                FinishSkillMovement();
            }
        }
    }
}
