using System;
using RaggedBanner.Combat;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.Serialization;

namespace RaggedBanner.MercenaryData
{
    [CreateAssetMenu(menuName = "RaggedBanner/Class Definition", fileName = "ClassDefinition")]
    public sealed class ClassDefinition : ScriptableObject
    {
        [SerializeField]
        string _displayName;

        [SerializeField]
        ClassBaseStats _baseStats;

        [SerializeField]
        SkillDefinition _basicAttack;

        [SerializeField]
        SkillGrant[] _grantedSkills = Array.Empty<SkillGrant>();

        [SerializeField, FormerlySerializedAs("_abilities")]
        TraitType[] _traits = Array.Empty<TraitType>();

        public string DisplayName => _displayName;
        public ClassBaseStats BaseStats => _baseStats;
        public SkillDefinition BasicAttack => _basicAttack;
        public int GrantedSkillCount => _grantedSkills != null ? _grantedSkills.Length : 0;
        public int TraitCount => _traits != null ? _traits.Length : 0;

        public SkillGrant GetGrantedSkill(int index)
        {
            return _grantedSkills[index];
        }

        public TraitType GetTrait(int index)
        {
            return _traits[index];
        }

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                return FailValidation("DisplayName must not be empty or whitespace.", out error);
            }

            if (!_baseStats.Validate(out string statsError))
            {
                return FailValidation(statsError, out error);
            }

            if (_basicAttack == null || _basicAttack.Type != SkillType.BasicAttack)
            {
                return FailValidation("BasicAttack must reference a BasicAttack skill definition.", out error);
            }

            if (!_basicAttack.Validate(out string attackError))
            {
                return FailValidation(attackError, out error);
            }

            if (!SkillGrant.ValidateList(_grantedSkills, out string grantError))
            {
                return FailValidation(grantError, out error);
            }

            if (_traits == null)
            {
                return FailValidation("Traits must not be null. Use an empty array for no traits.", out error);
            }

            for (int i = 0; i < _traits.Length; i++)
            {
                if (!TraitTypeRules.IsValid(_traits[i]))
                {
                    return FailValidation($"Traits[{i}] contains undefined value {(int)_traits[i]}.", out error);
                }

                for (int j = 0; j < i; j++)
                {
                    if (_traits[j] == _traits[i])
                    {
                        return FailValidation($"Traits[{i}] duplicates {_traits[i]}.", out error);
                    }
                }
            }

            error = null;
            return true;
        }

        bool FailValidation(string reason, out string error)
        {
            error = $"ClassDefinition '{name}': {reason}";
            return false;
        }
    }
}
