using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct SkillUseRequest
    {
        public SpiritType Spirit { get; }
        public SkillType Skill { get; }
        public int UnitId { get; }
        public int TargetId { get; }
        public HexDirection Direction { get; }
        public IReadOnlyList<HexCoordinate> Path { get; }
        public HexCoordinate? AttackPosition { get; }
        public AssassinLeapPreview? LeapPreview { get; }
        public BattlePathLine? Line { get; }

        public SkillUseRequest(SkillType skill, int unitId, int targetId = 0,
            HexDirection direction = HexDirection.East, IReadOnlyList<HexCoordinate> path = null,
            HexCoordinate? attackPosition = null, AssassinLeapPreview? leapPreview = null, BattlePathLine? line = null, SpiritType spirit = SpiritType.None)
        {
            Skill = skill;
            Spirit = spirit;
            UnitId = unitId;
            TargetId = targetId;
            Direction = direction;
            Path = path;
            AttackPosition = attackPosition;
            LeapPreview = leapPreview;
            Line = line;
        }
    }
}
