using System;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [CreateAssetMenu(menuName = "RaggedBanner/Specialization Definition", fileName = "SpecializationDefinition")]
    public sealed class SpecializationDefinition : ScriptableObject
    {
        [SerializeField]
        SkillGrant[] _grantedSkills = Array.Empty<SkillGrant>();

        [SerializeField]
        TraitType[] _traits = Array.Empty<TraitType>();

        public int TraitCount => _traits != null ? _traits.Length : 0;

        public TraitType GetTrait(int index)
        {
            return _traits[index];
        }

        public int GrantedSkillCount => _grantedSkills != null ? _grantedSkills.Length : 0;

        public SkillGrant GetGrantedSkill(int index)
        {
            return _grantedSkills[index];
        }

        public bool Validate(out string error)
        {
            if (!SkillGrant.ValidateList(_grantedSkills, out string grantError))
            {
                error = $"SpecializationDefinition '{name}': {grantError}";
                return false;
            }

            if (_traits == null)
            {
                error = $"SpecializationDefinition '{name}': Traits must not be null. Use an empty array for no traits.";
                return false;
            }

            for (int i = 0; i < _traits.Length; i++)
            {
                if (!TraitTypeRules.IsValid(_traits[i]))
                {
                    error = $"SpecializationDefinition '{name}': Traits[{i}] contains undefined value {(int)_traits[i]}.";
                    return false;
                }

                for (int j = 0; j < i; j++)
                {
                    if (_traits[j] == _traits[i])
                    {
                        error = $"SpecializationDefinition '{name}': Traits[{i}] duplicates {_traits[i]}.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
