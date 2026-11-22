using System;
using System.Collections.Generic;
using RaggedBanner.Combat;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [Serializable]
    public struct SkillGrant
    {
        [SerializeField]
        SkillDefinition _skill;

        [SerializeField, Range(1, 9)]
        int _slot;

        public SkillDefinition Skill => _skill;
        public int Slot => _slot;

        internal bool TryAppendTo(List<BattleSkillDefinition> skills, List<int> slots, out string error)
        {
            if (_skill == null || _skill.Type == SkillType.BasicAttack)
            {
                error = "Skill must reference a non-basic skill definition.";
                return false;
            }

            if (_slot < 1 || _slot > 9)
            {
                error = "Slot must be between 1 and 9.";
                return false;
            }

            if (!_skill.TryCreateRuntime(out BattleSkillDefinition definition, out error))
            {
                return false;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].Type == definition.Type)
                {
                    error = $"Skill type {definition.Type} is granted more than once.";
                    return false;
                }

                if (slots[i] == _slot)
                {
                    error = $"Slot {_slot} is assigned more than once.";
                    return false;
                }
            }

            skills.Add(definition);
            slots.Add(_slot);
            error = null;
            return true;
        }

        internal static bool ValidateList(SkillGrant[] grants, out string error)
        {
            if (grants == null)
            {
                error = "GrantedSkills must not be null. Use an empty array for no skills.";
                return false;
            }

            List<BattleSkillDefinition> skills = new List<BattleSkillDefinition>(grants.Length);
            List<int> slots = new List<int>(grants.Length);

            for (int i = 0; i < grants.Length; i++)
            {
                if (!grants[i].TryAppendTo(skills, slots, out string grantError))
                {
                    error = $"GrantedSkills[{i}]: {grantError}";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
