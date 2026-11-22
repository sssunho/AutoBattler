using System;

namespace RaggedBanner.Combat
{
    public sealed class SkillSet
    {
        readonly BattleSkillDefinition[] _skills = new BattleSkillDefinition[SkillTypeRules.Capacity];
        readonly BattleSkillDefinition[] _slots = new BattleSkillDefinition[10];
        readonly int[] _skillSlots = new int[SkillTypeRules.Capacity];

        public BattleSkillDefinition BasicAttack => _skills[(int)SkillType.BasicAttack];

        public SkillSet(params BattleSkillDefinition[] skills)
            : this(skills, CreateSequentialSlots(skills))
        {
        }

        public SkillSet(BattleSkillDefinition[] skills, int[] slots)
        {
            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            if (slots == null || slots.Length != skills.Length)
            {
                throw new ArgumentException("Every skill must have a corresponding slot.", nameof(slots));
            }

            for (int i = 0; i < skills.Length; i++)
            {
                BattleSkillDefinition definition = skills[i];

                if (definition == null || (uint)definition.Type >= _skills.Length || _skills[(int)definition.Type] != null)
                {
                    throw new ArgumentException("Skill definitions must be nonnull and unique by type.", nameof(skills));
                }

                int slot = slots[i];

                if (definition.Type == SkillType.BasicAttack ? slot != 0
                    : slot < 1 || slot > 9 || _slots[slot] != null)
                {
                    throw new ArgumentException("Basic attack must use slot zero and other skills must use unique slots from one to nine.", nameof(slots));
                }

                _skills[(int)definition.Type] = definition;
                _skillSlots[(int)definition.Type] = slot;

                if (slot != 0)
                {
                    _slots[slot] = definition;
                }
            }

            if (BasicAttack == null)
            {
                throw new ArgumentException("A skill set requires a basic attack.", nameof(skills));
            }
        }

        public bool TryGetSlot(int slot, out BattleSkillDefinition definition)
        {
            definition = slot >= 1 && slot < _slots.Length ? _slots[slot] : null;
            return definition != null;
        }

        public int GetSlot(SkillType type)
        {
            Get(type);
            return _skillSlots[(int)type];
        }

        public bool Contains(SkillType type)
        {
            return TryGet(type, out _);
        }

        public bool TryGet(SkillType type, out BattleSkillDefinition definition)
        {
            definition = (uint)type < _skills.Length ? _skills[(int)type] : null;
            return definition != null;
        }

        public BattleSkillDefinition Get(SkillType type)
        {
            if (!TryGet(type, out BattleSkillDefinition definition))
            {
                throw new ArgumentException("The unit does not own this skill.", nameof(type));
            }

            return definition;
        }

        public void GetRange(SkillType type, out int minRange, out int maxRange)
        {
            BattleSkillDefinition definition = Get(type);

            if (definition.RangeSource == SkillRangeSource.BasicAttack)
            {
                definition = BasicAttack;
            }

            minRange = definition.MinRange;
            maxRange = definition.MaxRange;
        }

        public bool IsInRange(SkillType type, long distance)
        {
            GetRange(type, out int minRange, out int maxRange);
            return distance >= minRange && distance <= maxRange;
        }

        static int[] CreateSequentialSlots(BattleSkillDefinition[] skills)
        {
            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            int[] slots = new int[skills.Length];
            int nextSlot = 1;

            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null && skills[i].Type != SkillType.BasicAttack)
                {
                    slots[i] = nextSlot++;
                }
            }

            return slots;
        }
    }
}