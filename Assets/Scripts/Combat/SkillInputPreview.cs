using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct SkillInputPreview
    {
        public SkillCheckResult Check { get; }
        public SkillUseRequest Request { get; }
        public HexCoordinate? Landing { get; }
        public bool CanUse => Check.CanUse;
        public string Error => Check.Error;

        internal SkillInputPreview(SkillCheckResult check, SkillUseRequest request = default, HexCoordinate? landing = null)
        {
            Check = check;
            Request = request;
            Landing = landing;
        }
    }
}
