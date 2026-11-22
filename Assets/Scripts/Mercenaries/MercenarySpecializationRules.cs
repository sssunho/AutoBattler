namespace RaggedBanner.Mercenaries
{
    public static class MercenarySpecializationRules
    {
        public static bool IsCompatible(MercenaryClass unitClass, MercenarySpecialization specialization)
        {
            if ((uint)unitClass > (uint)MercenaryClass.Archer)
            {
                return false;
            }

            return specialization == MercenarySpecialization.None
                || TryGetClass(specialization, out MercenaryClass requiredClass) && requiredClass == unitClass;
        }

        public static bool TryGetClass(MercenarySpecialization specialization, out MercenaryClass unitClass)
        {
            switch (specialization)
            {
                case MercenarySpecialization.Pioneer:
                    unitClass = MercenaryClass.Warrior;
                    return true;
                case MercenarySpecialization.Assassin:
                    unitClass = MercenaryClass.Rogue;
                    return true;
                case MercenarySpecialization.SpiritArcher:
                    unitClass = MercenaryClass.Archer;
                    return true;
                default:
                    unitClass = default;
                    return false;
            }
        }
    }
}
