using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        public bool IsAssassinLeap => _skillExecution != null && _skillExecution.Definition.Type == SkillType.AssassinLeap;
        public bool HasStartedAssassinLeap => IsAssassinLeap && _skillExecution.HasStartedMovement;

        public bool CanEnterInvisibility(int unitId)
        {
            return CanSelectSkill(unitId, SkillType.Invisibility).CanUse;
        }

        bool ExecuteEnterInvisibility(int unitId)
        {
            if (!CanEnterInvisibility(unitId))
            {
                return false;
            }

            _board.TryGetUnit(unitId, out HexCharacter unit);
            SpendSkillCost(unit);
            unit.EnterInvisibility();
            InvalidateVisibility();
            return true;
        }

        public bool CanSelectAssassinLeap(int unitId)
        {
            return CanSelectSkill(unitId, SkillType.AssassinLeap).CanUse;
        }

        public bool TryGetAssassinLeapPreview(int actorId, int targetId, out AssassinLeapPreview preview)
        {
            preview = default;
            return CanSelectAssassinLeap(actorId) && TryBuildAssassinLeapPreview(actorId, targetId, out preview);
        }

        bool ExecuteAssassinLeap(int actorId, int targetId, AssassinLeapPreview preview)
        {
            if (!TryGetAssassinLeapPreview(actorId, targetId, out AssassinLeapPreview current)
                || !SameAssassinLeapPreview(preview, current))
            {
                return false;
            }

            _movement.BeginLeap(current);
            _actingUnitId = actorId;
            _targetUnitId = targetId;
            _phase = BattleActionPhase.Moving;
            return true;
        }

        bool TryBuildAssassinLeapPreview(int actorId, int targetId, out AssassinLeapPreview preview)
        {
            preview = default;

            if (!TryGetOpponents(actorId, targetId, out HexCharacter actor, out HexCharacter target)
                || actor.Health <= 0 || !HasSkillResources(actor, SkillType.AssassinLeap)
                || !IsVisibleTo(actor.Team, targetId))
            {
                return false;
            }

            long distance = GetHexDistance(actor.Coordinate, target.Coordinate);
            HexCoordinate landing = target.Coordinate.GetNeighbor((HexDirection)(((int)target.Facing + 3) % 6));

            if (!actor.Skills.IsInRange(SkillType.AssassinLeap, distance) || !_board.IsValid(landing) || _board.IsOccupied(landing)
                || !HasSkillLineOfSight(actor, SkillType.AssassinLeap, actor.Coordinate, target.Coordinate)
                || !actor.Skills.IsInRange(SkillType.BasicAttack, landing.DistanceTo(target.Coordinate))
                || !HasSkillLineOfSight(actor, SkillType.BasicAttack, landing, target.Coordinate))
            {
                return false;
            }

            preview = new AssassinLeapPreview(actorId, targetId, actor.Coordinate,
                target.Coordinate, target.Facing, landing);
            return true;
        }

        static bool SameAssassinLeapPreview(AssassinLeapPreview left, AssassinLeapPreview right)
        {
            return left.ActorId == right.ActorId && left.TargetId == right.TargetId
                && left.ActorOrigin == right.ActorOrigin && left.TargetOrigin == right.TargetOrigin
                && left.TargetFacing == right.TargetFacing && left.Landing == right.Landing;
        }

        bool TryBeginAssassinLeap(out HexMovementStep step)
        {
            step = default;

            if (_state != BattleState.Running || _movement.StopRequested || HasStartedAssassinLeap
                || CurrentUnitId != _actingUnitId
                || !TryBuildAssassinLeapPreview(_actingUnitId, _targetUnitId, out AssassinLeapPreview current)
                || !SameAssassinLeapPreview(_movement.LeapPreview, current)
                || !_board.TryRelocate(_actingUnitId, current.Landing))
            {
                StopAssassinLeap();
                return false;
            }

            _board.TryGetUnit(_actingUnitId, out HexCharacter actor);
            SpendSkillCost(actor);
            _skillExecution.HasStartedMovement = true;
            _movement.MarkStepIssued();
            step = new HexMovementStep(actor.Id, current.ActorOrigin, current.Landing);
            return true;
        }

        void CompleteAssassinLeap()
        {
            if (_board.TryGetUnit(_actingUnitId, out HexCharacter actor)
                && _board.TryGetUnit(_targetUnitId, out HexCharacter target)
                && actor.Coordinate != target.Coordinate)
            {
                actor.SetFacing(BattleFacing.GetDirection(actor.Coordinate, target.Coordinate));
            }

            RefreshDetection();

            if (_movement.StopRequested || _state != BattleState.Running
                || !_board.TryGetUnit(_actingUnitId, out actor)
                || actor.Coordinate != _movement.LeapPreview.Landing || !IsTargetInRange())
            {
                FinishAssassinLeap();
                return;
            }

            ClearMovement();
            _phase = BattleActionPhase.ReadyToAttack;
        }

        void StopAssassinLeap()
        {
            _movement.RequestStop();

            if (_phase == BattleActionPhase.Attacking)
            {
                _stopAfterAttack = true;
                return;
            }

            if (!_movement.WaitingForCompletion)
            {
                FinishAssassinLeap();
            }
        }

        void FinishAssassinLeap()
        {
            if (HasStartedAssassinLeap && _board.TryGetUnit(_actingUnitId, out HexCharacter actor))
            {
                Reveal(actor);
            }

            ClearMovement();
            _phase = BattleActionPhase.Settling;
        }
    }
}
