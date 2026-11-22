using System;
using System.Collections.Generic;
using RaggedBanner.Combat;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.Serialization;

namespace RaggedBanner.MercenaryData
{
    [CreateAssetMenu(menuName = "RaggedBanner/Mercenary Data List", fileName = "MercenaryDataList")]
    public sealed class MercenaryDataList : ScriptableObject
    {
        static readonly MercenaryClass[] _requiredClasses = (MercenaryClass[])Enum.GetValues(typeof(MercenaryClass));

        [SerializeField]
        SerializableDictionary<MercenaryClass, ClassDefinition> _classes = new SerializableDictionary<MercenaryClass, ClassDefinition>();

        [SerializeField, FormerlySerializedAs("_abilitySettings")]
        TraitSettings _traitSettings;

        [SerializeField]
        SkillGrant[] _commonSkills = Array.Empty<SkillGrant>();

        [SerializeField]
        SerializableDictionary<MercenarySpecialization, SpecializationDefinition> _specializations =
            new SerializableDictionary<MercenarySpecialization, SpecializationDefinition>();

        [NonSerialized]
        bool _isValid;

        public TraitSettings TraitSettings => _traitSettings;

        public bool Validate(out string error)
        {
            Invalidate();

            if (_classes == null)
            {
                return FailValidation("Classes must not be null.", out error);
            }

            if (!_classes.Validate(out string dictionaryError))
            {
                return FailValidation($"Classes: {dictionaryError}", out error);
            }

            if (_traitSettings != null && !_traitSettings.Validate(out string settingsError))
            {
                return FailValidation(settingsError, out error);
            }

            HashSet<ClassDefinition> definitions = new HashSet<ClassDefinition>();

            for (int i = 0; i < _classes.EntryCount; i++)
            {
                MercenaryClass unitClass = _classes.GetKey(i);

                if (!Enum.IsDefined(typeof(MercenaryClass), unitClass))
                {
                    return FailValidation($"Classes[{i}].Key contains undefined value {(int)unitClass}.", out error);
                }

                ClassDefinition definition = _classes.GetValue(i);

                if (definition == null)
                {
                    return FailValidation($"Classes[{i}] ({unitClass}) must reference a ClassDefinition asset.", out error);
                }

                if (!definitions.Add(definition))
                {
                    return FailValidation($"Classes[{i}] ({unitClass}) reuses ClassDefinition '{definition.name}' for multiple classes.", out error);
                }

                if (!definition.Validate(out string definitionError))
                {
                    return FailValidation($"Classes[{i}] ({unitClass}): {definitionError}", out error);
                }

                for (int traitIndex = 0; traitIndex < definition.TraitCount; traitIndex++)
                {
                    if (definition.GetTrait(traitIndex) == TraitType.DirectionalPhysicalNegation
                        && _traitSettings == null)
                    {
                        return FailValidation($"Classes[{i}] ({unitClass}), ClassDefinition '{definition.name}': "
                            + $"Traits[{traitIndex}] requires a TraitSettings asset for DirectionalPhysicalNegation.", out error);
                    }
                }
            }

            for (int i = 0; i < _requiredClasses.Length; i++)
            {
                if (!_classes.TryGetValue(_requiredClasses[i], out _))
                {
                    return FailValidation($"Classes is missing a definition for {_requiredClasses[i]}.", out error);
                }
            }

            if (!SkillGrant.ValidateList(_commonSkills, out string commonError))
            {
                return FailValidation($"CommonSkills: {commonError}", out error);
            }

            if (_specializations == null)
            {
                return FailValidation("Specializations must not be null.", out error);
            }

            if (!_specializations.Validate(out string specializationDictionaryError))
            {
                return FailValidation($"Specializations: {specializationDictionaryError}", out error);
            }

            for (int i = 0; i < _requiredClasses.Length; i++)
            {
                if (!TryComposeSkillSet(_requiredClasses[i], MercenarySpecialization.None, out _, out error))
                {
                    Invalidate();
                    return false;
                }
            }

            for (int i = 0; i < _specializations.EntryCount; i++)
            {
                MercenarySpecialization specialization = _specializations.GetKey(i);

                if (!MercenarySpecializationRules.TryGetClass(specialization, out MercenaryClass unitClass))
                {
                    return FailValidation($"Specializations[{i}] must use a supported specialization other than None.", out error);
                }

                if (!TryComposeSkillSet(unitClass, specialization, out _, out error))
                {
                    Invalidate();
                    return false;
                }

                _specializations.TryGetValue(specialization, out SpecializationDefinition specializationDefinition);

                for (int traitIndex = 0; traitIndex < specializationDefinition.TraitCount; traitIndex++)
                {
                    if (specializationDefinition.GetTrait(traitIndex) == TraitType.DirectionalPhysicalNegation
                        && _traitSettings == null)
                    {
                        return FailValidation($"Specializations[{i}] ({specialization}), Traits[{traitIndex}] requires a TraitSettings asset for DirectionalPhysicalNegation.", out error);
                    }
                }
            }

            _isValid = true;
            error = null;
            return true;
        }

        public bool TryGetDefinition(MercenaryClass unitClass, out ClassDefinition definition)
        {
            definition = null;

            if (!_isValid || _classes == null || !_classes.TryGetValue(unitClass, out definition)
                || definition == null)
            {
                definition = null;
                return false;
            }

            return true;
        }

        public bool TryCreateSkillSet(MercenaryClass unitClass, MercenarySpecialization specialization,
            out SkillSet skills, out string error)
        {
            skills = null;

            if (!_isValid)
            {
                error = $"MercenaryDataList '{name}' must be validated before creating skills.";
                return false;
            }

            return TryComposeSkillSet(unitClass, specialization, out skills, out error);
        }

        public bool TryCreateTraits(MercenaryClass unitClass, MercenarySpecialization specialization,
            out TraitCollection traits, out string error)
        {
            traits = null;

            if (!_isValid || !TryGetDefinition(unitClass, out ClassDefinition definition))
            {
                error = $"MercenaryDataList '{name}' must be validated and contain a definition for {unitClass}.";
                return false;
            }

            if (!MercenarySpecializationRules.IsCompatible(unitClass, specialization))
            {
                error = $"MercenaryDataList '{name}': {specialization} is not valid for {unitClass}.";
                return false;
            }

            if (!definition.Validate(out error))
            {
                return false;
            }

            SpecializationDefinition specializationDefinition = null;

            if (specialization != MercenarySpecialization.None)
            {
                if (_specializations == null || !_specializations.TryGetValue(specialization, out specializationDefinition)
                    || specializationDefinition == null)
                {
                    error = $"MercenaryDataList '{name}': No definition exists for {specialization}.";
                    return false;
                }

                if (!specializationDefinition.Validate(out error))
                {
                    return false;
                }
            }

            int specializationCount = specializationDefinition != null ? specializationDefinition.TraitCount : 0;
            Trait[] grants = new Trait[definition.TraitCount + specializationCount];
            TraitSource classSource = new TraitSource(TraitSourceKind.Class, (int)unitClass);
            TraitSource specializationSource = new TraitSource(TraitSourceKind.Specialization, (int)specialization);
            bool requiresSettings = false;

            for (int i = 0; i < definition.TraitCount; i++)
            {
                TraitType type = definition.GetTrait(i);
                grants[i] = new Trait(type, classSource);
                requiresSettings |= type == TraitType.DirectionalPhysicalNegation;
            }

            for (int i = 0; i < specializationCount; i++)
            {
                TraitType type = specializationDefinition.GetTrait(i);
                grants[definition.TraitCount + i] = new Trait(type, specializationSource);
                requiresSettings |= type == TraitType.DirectionalPhysicalNegation;
            }

            if (requiresSettings)
            {
                if (_traitSettings == null)
                {
                    error = $"MercenaryDataList '{name}': DirectionalPhysicalNegation requires a TraitSettings asset.";
                    return false;
                }

                if (!_traitSettings.Validate(out error))
                {
                    return false;
                }
            }

            traits = grants.Length == 0 ? TraitCollection.Empty : new TraitCollection(grants);
            error = null;
            return true;
        }

        bool TryComposeSkillSet(MercenaryClass unitClass, MercenarySpecialization specialization,
            out SkillSet skills, out string error)
        {
            skills = null;

            if (_classes == null || !_classes.TryGetValue(unitClass, out ClassDefinition definition)
                || definition == null
                || definition.BasicAttack == null || definition.BasicAttack.Type != SkillType.BasicAttack)
            {
                error = $"MercenaryDataList '{name}': No validated basic attack exists for {unitClass}.";
                return false;
            }

            if (!definition.Validate(out error)
                || !definition.BasicAttack.TryCreateRuntime(out BattleSkillDefinition basicAttack, out error))
            {
                return false;
            }

            SpecializationDefinition specializationDefinition = null;

            if (specialization != MercenarySpecialization.None)
            {
                if (!MercenarySpecializationRules.TryGetClass(specialization, out MercenaryClass requiredClass)
                    || requiredClass != unitClass)
                {
                    error = $"MercenaryDataList '{name}': {specialization} is not valid for {unitClass}.";
                    return false;
                }

                if (_specializations == null || !_specializations.TryGetValue(specialization, out specializationDefinition)
                    || specializationDefinition == null)
                {
                    error = $"MercenaryDataList '{name}': No definition exists for {specialization}.";
                    return false;
                }

                if (!specializationDefinition.Validate(out error))
                {
                    return false;
                }
            }

            List<BattleSkillDefinition> definitions = new List<BattleSkillDefinition>(8) { basicAttack };
            List<int> slots = new List<int>(8) { 0 };

            if (_commonSkills == null)
            {
                error = $"MercenaryDataList '{name}': CommonSkills must not be null.";
                return false;
            }

            for (int i = 0; i < _commonSkills.Length; i++)
            {
                if (!_commonSkills[i].TryAppendTo(definitions, slots, out string grantError))
                {
                    error = $"MercenaryDataList '{name}', {unitClass}/{specialization}, CommonSkills[{i}]: {grantError}";
                    return false;
                }
            }

            for (int i = 0; i < definition.GrantedSkillCount; i++)
            {
                if (!definition.GetGrantedSkill(i).TryAppendTo(definitions, slots, out string grantError))
                {
                    error = $"MercenaryDataList '{name}', {unitClass}/{specialization}, Class GrantedSkills[{i}]: {grantError}";
                    return false;
                }
            }

            if (specializationDefinition != null)
            {
                for (int i = 0; i < specializationDefinition.GrantedSkillCount; i++)
                {
                    if (!specializationDefinition.GetGrantedSkill(i).TryAppendTo(definitions, slots, out string grantError))
                    {
                        error = $"MercenaryDataList '{name}', {unitClass}/{specialization}, Specialization GrantedSkills[{i}]: {grantError}";
                        return false;
                    }
                }
            }

            try
            {
                skills = new SkillSet(definitions.ToArray(), slots.ToArray());
                error = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                error = $"MercenaryDataList '{name}': {exception.Message}";
                return false;
            }
        }

        void OnEnable()
        {
            Invalidate();
        }

        void OnValidate()
        {
            Invalidate();
        }

        void Invalidate()
        {
            _isValid = false;

            if (_classes != null)
            {
                _classes.Invalidate();
            }

            if (_specializations != null)
            {
                _specializations.Invalidate();
            }
        }

        bool FailValidation(string reason, out string error)
        {
            Invalidate();
            error = $"MercenaryDataList '{name}': {reason}";
            return false;
        }
    }
}
