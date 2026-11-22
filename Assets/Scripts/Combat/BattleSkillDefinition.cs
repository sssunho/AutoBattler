using System;

namespace RaggedBanner.Combat
{
    public sealed class BattleSkillDefinition
    {
        public SkillType Type { get; }
        public SkillTargetType TargetType { get; }
        public SkillInputMode InputMode { get; }
        public SkillRangeSource RangeSource { get; }
        public int MinRange { get; }
        public int MaxRange { get; }
        public int ActionPointCost { get; }
        public int MovementCost { get; }
        public int Damage { get; }
        public AttackAttribute AttackAttribute { get; }
        public int DamageBonus { get; }
        public int FireSpiritDamage { get; }
        public int ElectricSpiritDamage { get; }
        public int CooldownTurns { get; }

        public BattleSkillDefinition(SkillType type, SkillTargetType targetType, SkillRangeSource rangeSource,
            int minRange, int maxRange, int actionPointCost, int movementCost = 0, int damage = 0,
            AttackAttribute attackAttribute = AttackAttribute.Physical, int damageBonus = 0, int fireSpiritDamage = 1, int electricSpiritDamage = 1,
            int cooldownTurns = 0, SkillInputMode? inputMode = null)
        {
            if (cooldownTurns < 0 || type == SkillType.BasicAttack && cooldownTurns != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownTurns), "Cooldown must be nonnegative and basic attack cooldown must be zero.");
            }

            if (!SkillTypeRules.IsValid(type) || (uint)targetType > (uint)SkillTargetType.Direction
                || (uint)rangeSource > (uint)SkillRangeSource.BasicAttack
                || (uint)attackAttribute > (uint)AttackAttribute.Untyped)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (minRange < 0 || maxRange < minRange || actionPointCost < 0 || movementCost < 0
                || damage < 0 || damageBonus < 0 || fireSpiritDamage < 0 || electricSpiritDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minRange));
            }

            SkillTargetType requiredTarget = type == SkillType.Stealth || type == SkillType.Invisibility || type == SkillType.SpiritImbuement
                ? SkillTargetType.Self : type == SkillType.Overwatch ? SkillTargetType.Direction : SkillTargetType.Unit;
            SkillInputMode requiredInput = type == SkillType.SpiritImbuement ? SkillInputMode.Choice
                : requiredTarget == SkillTargetType.Self ? SkillInputMode.Self
                : requiredTarget == SkillTargetType.Direction ? SkillInputMode.Direction : SkillInputMode.Unit;

            if (inputMode.HasValue && inputMode.Value != requiredInput)
            {
                throw new ArgumentException("The input mode is incompatible with this skill.", nameof(inputMode));
            }

            if (targetType != requiredTarget
                || targetType == SkillTargetType.Self && (rangeSource != SkillRangeSource.Own || maxRange != 0)
                || rangeSource == SkillRangeSource.BasicAttack && (minRange != 0 || maxRange != 0)
                || rangeSource == SkillRangeSource.Own && targetType != SkillTargetType.Self && minRange < 1
                || type == SkillType.BasicAttack && (rangeSource != SkillRangeSource.Own || damage < 1 || actionPointCost < 1)
                || type == SkillType.Overwatch && rangeSource != SkillRangeSource.BasicAttack
                || type != SkillType.BasicAttack && damage != 0
                || type != SkillType.Stealth && movementCost != 0
                || type != SkillType.Ambush && damageBonus != 0
                || SkillTypeRules.IsPull(type) && minRange < 2)
            {
                throw new ArgumentException("The skill definition contains incompatible targeting or effect settings.");
            }

            Type = type;
            TargetType = targetType;
            InputMode = requiredInput;
            RangeSource = rangeSource;
            MinRange = minRange;
            MaxRange = maxRange;
            ActionPointCost = actionPointCost;
            MovementCost = movementCost;
            Damage = damage;
            AttackAttribute = attackAttribute;
            DamageBonus = damageBonus;
            FireSpiritDamage = fireSpiritDamage;
            ElectricSpiritDamage = electricSpiritDamage;
            CooldownTurns = cooldownTurns;
        }
    }
}
