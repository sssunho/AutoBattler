using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct SkillInputQuery
    {
        public SkillType Skill { get; }
        public int UnitId { get; }
        public int TargetId { get; }
        public HexDirection Direction { get; }
        public int OptionId { get; }
        public BattlePathLine? Line { get; }

        public SkillInputQuery(SkillType skill, int unitId, int targetId = 0,
            HexDirection direction = HexDirection.East, int optionId = 0, BattlePathLine? line = null)
        {
            Skill = skill;
            UnitId = unitId;
            TargetId = targetId;
            Direction = direction;
            OptionId = optionId;
            Line = line;
        }
    }
}
