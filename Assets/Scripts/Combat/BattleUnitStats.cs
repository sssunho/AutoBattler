using System;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat
{
    public sealed class BattleUnitStats
    {
        public MercenaryClass UnitClass { get; }
        public int MaxHealth { get; }
        public int MovementPerTurn { get; }
        public int Initiative { get; }
        public int CriticalChance { get; }
        public int PhysicalDefense { get; }
        public int WaterDefense { get; }
        public int FireDefense { get; }
        public int ElectricityDefense { get; }
        public int PoisonDefense { get; }

        public BattleUnitStats(MercenaryClass unitClass, int maxHealth, int movementPerTurn, int criticalChance,
            int physicalDefense = 0,
            int waterDefense = 0, int fireDefense = 0, int electricityDefense = 0, int poisonDefense = 0,
            int initiative = 0)
        {
            if ((uint)unitClass > (uint)MercenaryClass.Archer)
            {
                throw new ArgumentOutOfRangeException(nameof(unitClass));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (movementPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(movementPerTurn));
            }

            if (initiative < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initiative));
            }

            if (criticalChance < 0 || criticalChance > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(criticalChance));
            }

            if (physicalDefense < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            }

            if (waterDefense < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(waterDefense));
            }

            if (fireDefense < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fireDefense));
            }

            if (electricityDefense < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(electricityDefense));
            }

            if (poisonDefense < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(poisonDefense));
            }

            UnitClass = unitClass;
            MaxHealth = maxHealth;
            MovementPerTurn = movementPerTurn;
            Initiative = initiative;
            CriticalChance = criticalChance;
            PhysicalDefense = physicalDefense;
            WaterDefense = waterDefense;
            FireDefense = fireDefense;
            ElectricityDefense = electricityDefense;
            PoisonDefense = poisonDefense;
        }

        public static BattleUnitStats ForClass(MercenaryClass unitClass)
        {
            switch (unitClass)
            {
                case MercenaryClass.Warrior:
                    return new BattleUnitStats(unitClass, 10, 2, 5, physicalDefense: 1);
                case MercenaryClass.Rogue:
                    return new BattleUnitStats(unitClass, 8, 4, 15);
                case MercenaryClass.Archer:
                    return new BattleUnitStats(unitClass, 8, 2, 10);
                default:
                    throw new ArgumentOutOfRangeException(nameof(unitClass));
            }
        }

        public int GetDefense(AttackAttribute attackAttribute)
        {
            switch (attackAttribute)
            {
                case AttackAttribute.Physical:
                    return PhysicalDefense;
                case AttackAttribute.Water:
                    return WaterDefense;
                case AttackAttribute.Fire:
                    return FireDefense;
                case AttackAttribute.Electricity:
                    return ElectricityDefense;
                case AttackAttribute.Poison:
                    return PoisonDefense;
                case AttackAttribute.Untyped:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attackAttribute));
            }
        }
    }
}
