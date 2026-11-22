using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        readonly List<HexCoordinate> _skillQueryPath = new List<HexCoordinate>();
        SkillExecution _skillExecution;

        public SkillCheckResult CanSelectSkill(int unitId, SkillType skill)
        {
            if (_state != BattleState.Running || IsBusy || CurrentUnitId != unitId
                || !_board.TryGetUnit(unitId, out HexCharacter unit) || unit.Health <= 0)
            {
                return new SkillCheckResult(false, "현재 행동할 수 있는 유닛이 아닙니다.");
            }

            if (!unit.Skills.TryGet(skill, out BattleSkillDefinition definition))
            {
                return new SkillCheckResult(false, "보유하지 않은 스킬입니다.");
            }

            if (unit.ActionPoints < definition.ActionPointCost
                || unit.MovementRemaining < definition.MovementCost)
            {
                return new SkillCheckResult(false, "행동력 또는 이동력이 부족합니다.");
            }

            if (unit.GetRemainingCooldown(skill) > 0)
            {
                return new SkillCheckResult(false, "스킬의 쿨타임이 남아 있습니다.");
            }

            bool allowed;

            switch (skill)
            {
                case SkillType.SpiritImbuement:
                case SkillType.BasicAttack:
                    allowed = true;
                    break;
                case SkillType.Stealth:
                    allowed = !unit.IsStealthed && !IsDetected(unit);
                    break;
                case SkillType.Ambush:
                    allowed = unit.IsStealthed && !unit.HasUsedAmbush;
                    break;
                case SkillType.Overwatch:
                    allowed = unit.Team == BattleTeam.Player;
                    break;
                case SkillType.PullSelf:
                case SkillType.PullTarget:
                    allowed = true;
                    break;
                case SkillType.Invisibility:
                    allowed = !unit.IsInvisible;
                    break;
                case SkillType.AssassinLeap:
                    allowed = true;
                    break;
                default:
                    allowed = false;
                    break;
            }

            return new SkillCheckResult(allowed, allowed ? null : "스킬의 사용 조건을 충족하지 않습니다.");
        }

        public SkillCheckResult CanUseSkill(SkillUseRequest request)
        {
            SkillCheckResult selection = CanSelectSkill(request.UnitId, request.Skill);

            if (!selection.CanUse)
            {
                return selection;
            }

            _board.TryGetUnit(request.UnitId, out HexCharacter actor);
            BattleSkillDefinition definition = actor.Skills.Get(request.Skill);

            if (request.Skill != SkillType.SpiritImbuement && request.Spirit != SpiritType.None
                || definition.TargetType != SkillTargetType.Unit && request.TargetId != 0
                || definition.TargetType != SkillTargetType.Direction && request.Direction != HexDirection.East
                || request.Skill != SkillType.BasicAttack && request.AttackPosition.HasValue
                || request.Skill != SkillType.AssassinLeap && request.LeapPreview.HasValue
                || request.Skill != SkillType.BasicAttack && request.Skill != SkillType.PullSelf
                    && request.Skill != SkillType.PullTarget && (request.Path != null || request.Line.HasValue)
                || request.Path != null && (request.AttackPosition.HasValue || request.Line.HasValue)
                || request.AttackPosition.HasValue && request.Line.HasValue)
            {
                return new SkillCheckResult(false, "스킬에 맞지 않는 대상 또는 경로 입력입니다.");
            }

            bool allowed;

            switch (request.Skill)
            {
                case SkillType.SpiritImbuement:
                    allowed = request.Spirit > SpiritType.None && request.Spirit <= SpiritType.Poison;
                    break;
                case SkillType.BasicAttack:
                    allowed = CanRequestBasicAttack(request, actor);
                    break;
                case SkillType.Stealth:
                case SkillType.Invisibility:
                    allowed = true;
                    break;
                case SkillType.Overwatch:
                    allowed = (uint)request.Direction < 6;
                    break;
                case SkillType.Ambush:
                    allowed = TryGetOpponents(actor.Id, request.TargetId, out _, out HexCharacter ambushTarget)
                        && IsAmbushTarget(actor, ambushTarget);
                    break;
                case SkillType.PullSelf:
                case SkillType.PullTarget:
                    allowed = TryGetOpponents(actor.Id, request.TargetId, out _, out HexCharacter pullTarget)
                        && IsVisibleTo(actor.Team, request.TargetId)
                        && (request.Path != null ? IsValidPullPath(actor, pullTarget, request.Skill, request.Path)
                            : TryBuildPullPath(actor, pullTarget, request.Skill,
                                request.Line ?? GetPullCenterLine(actor, pullTarget), _skillQueryPath));
                    break;
                case SkillType.AssassinLeap:
                    allowed = request.LeapPreview.HasValue
                        && TryBuildAssassinLeapPreview(actor.Id, request.TargetId, out AssassinLeapPreview leap)
                        && SameAssassinLeapPreview(request.LeapPreview.Value, leap);
                    break;
                default:
                    allowed = false;
                    break;
            }

            return new SkillCheckResult(allowed, allowed ? null : "대상, 사거리 또는 이동 경로가 유효하지 않습니다.");
        }

        bool CanRequestBasicAttack(SkillUseRequest request, HexCharacter actor)
        {
            if (!TryGetOpponents(actor.Id, request.TargetId, out _, out HexCharacter target)
                || !IsVisibleTo(actor.Team, target.Id))
            {
                return false;
            }

            if (request.Path != null)
            {
                HexCoordinate position = request.Path.Count == 0 ? actor.Coordinate : request.Path[request.Path.Count - 1];
                return IsValidMovementPath(actor, request.Path)
                    && actor.Skills.IsInRange(SkillType.BasicAttack, position.DistanceTo(target.Coordinate))
                    && HasSkillLineOfSight(actor, SkillType.BasicAttack, position, target.Coordinate);
            }

            BattlePathLine line = request.Line ?? BattlePathLine.Between(actor.Coordinate, target.Coordinate);

            if (request.AttackPosition.HasValue)
            {
                HexCoordinate position = request.AttackPosition.Value;
                return _board.IsValid(position)
                    && actor.Skills.IsInRange(SkillType.BasicAttack, position.DistanceTo(target.Coordinate))
                    && HasSkillLineOfSight(actor, SkillType.BasicAttack, position, target.Coordinate)
                    && _pathfinder.TryFindPath(_board, actor.Coordinate, position, actor.MovementRemaining, line, _skillQueryPath);
            }

            return _pathfinder.TryFindPathToAttackPosition(_board, actor.Coordinate, target.Coordinate,
                actor.Skills.BasicAttack.MaxRange, actor.MovementRemaining, line, _skillQueryPath,
                actor.Skills.BasicAttack.MinRange);
        }

        public bool TryUseSkill(SkillUseRequest request)
        {
            if (!CanUseSkill(request).CanUse)
            {
                return false;
            }

            _board.TryGetUnit(request.UnitId, out HexCharacter actor);
            _skillExecution = new SkillExecution(actor.Id, actor.Skills.Get(request.Skill));
            bool accepted;

            switch (request.Skill)
            {
                case SkillType.SpiritImbuement:
                    SpendSkillCost(actor);
                    actor.PrepareSpirit(request.Spirit);
                    accepted = true;
                    break;
                case SkillType.BasicAttack:
                    accepted = ExecuteAttack(actor.Id, request.TargetId, request.Path ?? _skillQueryPath);
                    break;
                case SkillType.Stealth:
                    accepted = ExecuteEnterStealth(actor.Id);
                    break;
                case SkillType.Invisibility:
                    accepted = ExecuteEnterInvisibility(actor.Id);
                    break;
                case SkillType.Ambush:
                    accepted = ExecuteAmbush(actor.Id, request.TargetId);
                    break;
                case SkillType.Overwatch:
                    accepted = ExecutePrepareOverwatch(actor.Id, request.Direction);
                    break;
                case SkillType.PullSelf:
                case SkillType.PullTarget:
                    accepted = ExecutePullSkill(actor.Id, request.TargetId, request.Skill, request.Path ?? _skillQueryPath);
                    break;
                case SkillType.AssassinLeap:
                    accepted = ExecuteAssassinLeap(actor.Id, request.TargetId, request.LeapPreview.Value);
                    break;
                default:
                    accepted = false;
                    break;
            }

            if (!accepted || !IsBusy)
            {
                _skillExecution = null;
            }

            return accepted;
        }

        void SpendSkillCost(HexCharacter unit)
        {
            if (_skillExecution == null || _skillExecution.UnitId != unit.Id || _skillExecution.CostCommitted)
            {
                return;
            }

            unit.SpendActionPoints(_skillExecution.Definition.ActionPointCost);
            unit.SpendMovement(_skillExecution.Definition.MovementCost);
            unit.StartCooldown(_skillExecution.Definition.Type);
            _skillExecution.CostCommitted = true;
        }

        bool HasSkillResources(HexCharacter unit, SkillType skill)
        {
            return unit.Skills.TryGet(skill, out BattleSkillDefinition definition)
                && unit.GetRemainingCooldown(skill) == 0
                && unit.ActionPoints >= definition.ActionPointCost
                && unit.MovementRemaining >= definition.MovementCost;
        }

        public bool TryAttack(int attackerId, int targetId)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, attackerId, targetId));
        }

        public bool TryAttack(int attackerId, int targetId, HexCoordinate attackPosition)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, attackerId, targetId, attackPosition: attackPosition));
        }

        public bool TryAttack(int attackerId, int targetId, IReadOnlyList<HexCoordinate> path)
        {
            return path != null && TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, attackerId, targetId, path: path));
        }

        public bool TryEnterStealth(int unitId)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.Stealth, unitId));
        }

        public bool TryEnterInvisibility(int unitId)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.Invisibility, unitId));
        }

        public bool TryAmbush(int unitId, int targetId)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.Ambush, unitId, targetId));
        }

        public bool TryPrepareOverwatch(int unitId, HexDirection direction)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.Overwatch, unitId, direction: direction));
        }

        public bool TryAssassinLeap(int unitId, int targetId, AssassinLeapPreview preview)
        {
            return TryUseSkill(new SkillUseRequest(SkillType.AssassinLeap, unitId, targetId, leapPreview: preview));
        }

        public bool TryPullSkill(int unitId, int targetId, SkillType skill)
        {
            return SkillTypeRules.IsPull(skill)
                && TryUseSkill(new SkillUseRequest(skill, unitId, targetId));
        }

        public bool TryPullSkill(int unitId, int targetId, SkillType skill, BattlePathLine line)
        {
            return SkillTypeRules.IsPull(skill)
                && TryUseSkill(new SkillUseRequest(skill, unitId, targetId, line: line));
        }

        public bool TryPullSkill(int unitId, int targetId, SkillType skill, IReadOnlyList<HexCoordinate> path)
        {
            return SkillTypeRules.IsPull(skill) && path != null
                && TryUseSkill(new SkillUseRequest(skill, unitId, targetId, path: path));
        }
    }
}
