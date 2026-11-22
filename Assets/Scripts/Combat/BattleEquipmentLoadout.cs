using System;
using System.Collections.Generic;

namespace RaggedBanner.Combat
{
    public enum EquipmentType
    {
        Longsword,
        Dagger,
        Shield,
        Armor
    }

    public readonly struct BattleEquipmentItem
    {
        public EquipmentType Type { get; }
        public int PhysicalAttackBonus { get; }
        public int CriticalDamageBonus { get; }
        public int PhysicalDefenseBonus { get; }
        public int MaxHealthBonus { get; }

        public BattleEquipmentItem(EquipmentType type, int physicalAttackBonus = 0,
            int criticalDamageBonus = 0, int physicalDefenseBonus = 0, int maxHealthBonus = 0)
        {
            if ((uint)type > (uint)EquipmentType.Armor || physicalAttackBonus < 0
                || criticalDamageBonus < 0 || physicalDefenseBonus < 0 || maxHealthBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            Type = type;
            PhysicalAttackBonus = physicalAttackBonus;
            CriticalDamageBonus = criticalDamageBonus;
            PhysicalDefenseBonus = physicalDefenseBonus;
            MaxHealthBonus = maxHealthBonus;
        }
    }

    public sealed class BattleEquipmentLoadout
    {
        public static BattleEquipmentLoadout Empty { get; } = new BattleEquipmentLoadout(Array.Empty<BattleEquipmentItem>());
        public IReadOnlyList<BattleEquipmentItem> Items { get; }
        public int PhysicalAttackBonus { get; }
        public int CriticalDamageBonus { get; }
        public int PhysicalDefenseBonus { get; }
        public int MaxHealthBonus { get; }

        public BattleEquipmentLoadout(IReadOnlyList<BattleEquipmentItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count > 3)
            {
                throw new ArgumentException("A unit can equip at most three items.", nameof(items));
            }

            BattleEquipmentItem[] snapshot = new BattleEquipmentItem[items.Count];
            checked
            {
                for (int i = 0; i < snapshot.Length; i++)
                {
                    BattleEquipmentItem item = items[i];
                    snapshot[i] = item;
                    PhysicalAttackBonus += item.PhysicalAttackBonus;
                    CriticalDamageBonus += item.CriticalDamageBonus;
                    PhysicalDefenseBonus += item.PhysicalDefenseBonus;
                    MaxHealthBonus += item.MaxHealthBonus;
                }
            }

            Items = Array.AsReadOnly(snapshot);
        }

        public BattleUnitStats ApplyTo(BattleUnitStats baseStats)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            return new BattleUnitStats(baseStats.UnitClass, checked(baseStats.MaxHealth + MaxHealthBonus),
                baseStats.MovementPerTurn, baseStats.CriticalChance,
                checked(baseStats.PhysicalDefense + PhysicalDefenseBonus), baseStats.WaterDefense,
                baseStats.FireDefense, baseStats.ElectricityDefense, baseStats.PoisonDefense, baseStats.Initiative);
        }
    }
}
