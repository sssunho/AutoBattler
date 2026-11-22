namespace RaggedBanner.Combat
{
    public readonly struct SkillCheckResult
    {
        public bool CanUse { get; }
        public string Error { get; }

        internal SkillCheckResult(bool canUse, string error = null)
        {
            CanUse = canUse;
            Error = error;
        }
    }
}
