namespace RaggedBanner.Combat
{
    public static class SkillTypeRules
    {
        public const int Capacity = (int)SkillType.SpiritImbuement + 1;

        public static bool IsValid(SkillType type)
        {
            return (uint)type < Capacity;
        }

        public static bool IsPull(SkillType type)
        {
            return type == SkillType.PullSelf || type == SkillType.PullTarget;
        }
    }
}
