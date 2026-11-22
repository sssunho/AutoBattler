using System;

namespace RaggedBanner.Combat
{
    public sealed class BattleTraitSettings
    {
        public static BattleTraitSettings None { get; } = new BattleTraitSettings(0f, 0f, 0f);

        public float FrontNegationChance { get; }
        public float SideNegationChance { get; }
        public float RearNegationChance { get; }

        public BattleTraitSettings(float frontNegationChance, float sideNegationChance, float rearNegationChance)
        {
            ValidateChance(frontNegationChance, nameof(frontNegationChance));
            ValidateChance(sideNegationChance, nameof(sideNegationChance));
            ValidateChance(rearNegationChance, nameof(rearNegationChance));
            FrontNegationChance = frontNegationChance;
            SideNegationChance = sideNegationChance;
            RearNegationChance = rearNegationChance;
        }

        static void ValidateChance(float chance, string parameterName)
        {
            if (float.IsNaN(chance) || float.IsInfinity(chance) || chance < 0f || chance > 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
