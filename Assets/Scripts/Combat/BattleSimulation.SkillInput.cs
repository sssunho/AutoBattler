using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        public SkillInputPreview PreviewSkill(SkillInputQuery query, List<HexCoordinate> path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path.Clear();
            SkillCheckResult selection = CanSelectSkill(query.UnitId, query.Skill);
            if (!selection.CanUse)
            {
                return new SkillInputPreview(selection);
            }

            _board.TryGetUnit(query.UnitId, out HexCharacter actor);
            BattleSkillDefinition definition = actor.Skills.Get(query.Skill);
            if (definition.InputMode != SkillInputMode.Choice && query.OptionId != 0
                || definition.InputMode != SkillInputMode.Direction && query.Direction != HexDirection.East
                || definition.InputMode == SkillInputMode.Self && query.TargetId != 0 && query.TargetId != actor.Id
                || (definition.InputMode == SkillInputMode.Choice || definition.InputMode == SkillInputMode.Direction) && query.TargetId != 0)
            {
                return InvalidSkillPreview();
            }

            SkillUseRequest request;
            HexCoordinate? landing = null;
            switch (query.Skill)
            {
                case SkillType.BasicAttack:
                    if (!TryGetOpponents(actor.Id, query.TargetId, out _, out HexCharacter attackTarget)
                        || !IsVisibleTo(actor.Team, attackTarget.Id)
                        || !_pathfinder.TryFindPathToAttackPosition(_board, actor.Coordinate, attackTarget.Coordinate,
                            definition.MaxRange, actor.MovementRemaining,
                            query.Line ?? BattlePathLine.Between(actor.Coordinate, attackTarget.Coordinate), path, definition.MinRange))
                    {
                        path.Clear();
                        return InvalidSkillPreview();
                    }

                    request = new SkillUseRequest(query.Skill, actor.Id, query.TargetId, path: path);
                    break;
                case SkillType.PullSelf:
                case SkillType.PullTarget:
                    bool found = query.Line.HasValue
                        ? TryCopyPullPath(actor.Id, query.TargetId, query.Skill, query.Line.Value, path)
                        : TryCopyPullPath(actor.Id, query.TargetId, query.Skill, path);
                    if (!found)
                    {
                        path.Clear();
                        return InvalidSkillPreview();
                    }

                    request = new SkillUseRequest(query.Skill, actor.Id, query.TargetId, path: path);
                    break;
                case SkillType.AssassinLeap:
                    if (!TryBuildAssassinLeapPreview(actor.Id, query.TargetId, out AssassinLeapPreview leap))
                    {
                        return InvalidSkillPreview();
                    }

                    landing = leap.Landing;
                    request = new SkillUseRequest(query.Skill, actor.Id, query.TargetId, leapPreview: leap);
                    break;
                case SkillType.SpiritImbuement:
                    if (!BattleSkillChoices.TryResolveSpirit(query.Skill, query.OptionId, out SpiritType spirit))
                    {
                        return InvalidSkillPreview();
                    }

                    request = new SkillUseRequest(query.Skill, actor.Id, spirit: spirit);
                    break;
                default:
                    request = new SkillUseRequest(query.Skill, actor.Id,
                        definition.TargetType == SkillTargetType.Unit ? query.TargetId : 0, query.Direction);
                    break;
            }

            SkillCheckResult check = CanUseSkill(request);
            if (!check.CanUse)
            {
                path.Clear();
                return new SkillInputPreview(check);
            }

            return new SkillInputPreview(check, request, landing);
        }

        static SkillInputPreview InvalidSkillPreview()
        {
            return new SkillInputPreview(new SkillCheckResult(false, "대상, 방향, 선택 항목 또는 이동 경로가 유효하지 않습니다."));
        }
    }
}
