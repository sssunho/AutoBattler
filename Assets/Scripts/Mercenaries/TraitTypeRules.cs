namespace RaggedBanner.Mercenaries
{
    public static class TraitTypeRules
    {
        public static bool IsValid(TraitType type)
        {
            return (uint)type >= (uint)TraitType.DirectionalPhysicalNegation && (uint)type <= (uint)TraitType.FourElementBasicAttack;
        }
    }
}
