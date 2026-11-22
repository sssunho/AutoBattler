using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat;
using RaggedBanner.Mercenaries;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.MercenaryData.Tests
{
    public sealed class MercenaryDataTests
    {
        const string _assetDirectory = "Assets/GameData/Mercenaries/";

        const string _legacyFolderPrefix = "Assets/__MercenaryDataLegacyTests_";

        readonly List<ScriptableObject> _objects = new List<ScriptableObject>();
        string _legacyFolder;

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_legacyFolder) && _legacyFolder.StartsWith(_legacyFolderPrefix, StringComparison.Ordinal)
                && AssetDatabase.IsValidFolder(_legacyFolder))
            {
                AssetDatabase.DeleteAsset(_legacyFolder);
            }

            _legacyFolder = null;

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t")]
        public void MissingDisplayNamesAreRejected(string displayName)
        {
            ClassDefinition definition = CreateClass();
            SetField(definition, "_displayName", displayName);

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("DisplayName"));
        }

        [TestCase("_baseStats", "_maxHP", 0)]
        [TestCase("_baseStats", "_maxHP", -1)]
        [TestCase("_baseStats", "_movement", -1)]
        [TestCase("_baseStats", "_physicalDefense", -1)]
        [TestCase("_baseStats", "_waterDefense", -1)]
        [TestCase("_baseStats", "_fireDefense", -1)]
        [TestCase("_baseStats", "_lightningDefense", -1)]
        [TestCase("_baseStats", "_poisonDefense", -1)]
        [TestCase("_basicAttack", "_damage", 0)]
        [TestCase("_basicAttack", "_damage", -1)]
        [TestCase("_basicAttack", "_maxRange", 0)]
        [TestCase("_basicAttack", "_maxRange", -1)]
        [TestCase("_basicAttack", "_actionPointCost", 0)]
        [TestCase("_basicAttack", "_actionPointCost", -1)]
        public void InvalidIntegerValuesAreRejectedWithoutClamping(string group, string field, int value)
        {
            ClassDefinition definition = CreateClass();
            SetNestedField(definition, group, field, value);

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(GetField(GetField(definition, group), field), Is.EqualTo(value));
        }

        [Test]
        public void MinimumAndMaximumIntegerBoundariesAreAccepted()
        {
            ClassDefinition definition = CreateClass();

            Assert.That(definition.Validate(out string error), Is.True, error);
            Assert.That(error, Is.Null);

            string[] statFields =
            {
                "_maxHP", "_movement", "_physicalDefense", "_waterDefense", "_fireDefense",
                "_lightningDefense", "_poisonDefense"
            };

            for (int i = 0; i < statFields.Length; i++)
            {
                SetNestedField(definition, "_baseStats", statFields[i], int.MaxValue);
            }

            SetNestedField(definition, "_basicAttack", "_damage", int.MaxValue);
            SetNestedField(definition, "_basicAttack", "_maxRange", int.MaxValue);

            Assert.That(definition.Validate(out error), Is.True, error);
            Assert.That(error, Is.Null);
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidCriticalChancesAreRejectedWithoutClamping(float chance)
        {
            ClassDefinition definition = CreateClass();
            SetNestedField(definition, "_baseStats", "_criticalChance", chance);

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("CriticalChance"));
            Assert.That(definition.BaseStats.CriticalChance, Is.EqualTo(chance));
        }

        [TestCase(0f)]
        [TestCase(0.12345f)]
        [TestCase(1f)]
        public void ValidCriticalChancesKeepTheirPrecision(float chance)
        {
            ClassDefinition definition = CreateClass();
            SetNestedField(definition, "_baseStats", "_criticalChance", chance);

            Assert.That(definition.Validate(out string error), Is.True, error);
            Assert.That(definition.BaseStats.CriticalChance, Is.EqualTo(chance));
        }

        [TestCase(-1)]
        [TestCase(999)]
        public void UndefinedAttackAttributesAreRejected(int value)
        {
            ClassDefinition definition = CreateClass();
            SetNestedField(definition, "_basicAttack", "_attackAttribute", (AttackAttribute)value);

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [TestCase(AttackAttribute.Physical)]
        [TestCase(AttackAttribute.Water)]
        [TestCase(AttackAttribute.Fire)]
        [TestCase(AttackAttribute.Electricity)]
        [TestCase(AttackAttribute.Poison)]
        [TestCase(AttackAttribute.Untyped)]
        public void DefinedAttackAttributesAreAccepted(AttackAttribute attribute)
        {
            ClassDefinition definition = CreateClass();
            SetNestedField(definition, "_basicAttack", "_attackAttribute", attribute);

            Assert.That(definition.Validate(out string error), Is.True, error);
            Assert.That(definition.BasicAttack.AttackAttribute, Is.EqualTo(attribute));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(999)]
        public void UndefinedTraitsAreRejected(int value)
        {
            ClassDefinition definition = CreateClass(traits: new[] { (TraitType)value });

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("Traits"));
        }

        [TestCase(TraitType.DirectionalPhysicalNegation)]
        [TestCase(TraitType.RearGuaranteedCritical)]
        [TestCase(TraitType.OpportunityAttack)]
        public void DuplicateTraitsAreRejected(TraitType trait)
        {
            ClassDefinition definition = CreateClass(traits: new[] { trait, trait });

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("Traits"));
        }

        [Test]
        public void EmptyTraitsAreValidButNullTraitsAreRejected()
        {
            ClassDefinition definition = CreateClass();

            Assert.That(definition.TraitCount, Is.Zero);
            Assert.That(definition.Validate(out string error), Is.True, error);

            SetField(definition, "_traits", null);

            Assert.That(definition.Validate(out error), Is.False);
            Assert.That(error, Does.Contain("Traits"));
        }

        [Test]
        public void DistinctTraitsPreserveTheirOrder()
        {
            ClassDefinition definition = CreateClass(traits: new[]
            {
                TraitType.RearGuaranteedCritical, TraitType.DirectionalPhysicalNegation
            });

            Assert.That(definition.Validate(out string error), Is.True, error);
            Assert.That(definition.TraitCount, Is.EqualTo(2));
            Assert.That(definition.GetTrait(0), Is.EqualTo(TraitType.RearGuaranteedCritical));
            Assert.That(definition.GetTrait(1), Is.EqualTo(TraitType.DirectionalPhysicalNegation));
        }

        [Test]
        public void LegacyAbilityFieldDeserializesAsTraits()
        {
            ClassDefinition original = CreateClass(new[] { TraitType.RearGuaranteedCritical });

            ClassDefinition restored = ReloadWithLegacyField(original, "_traits", "_abilities");

            Assert.That(restored.Validate(out string error), Is.True, error);
            Assert.That(restored.TraitCount, Is.EqualTo(1));
            Assert.That(restored.GetTrait(0), Is.EqualTo(TraitType.RearGuaranteedCritical));
        }

        [Test]
        public void LegacySettingsFieldKeepsTheReferencedAsset()
        {
            TraitSettings settings = CreateObject<TraitSettings>();
            MercenaryDataList original = CreateList();
            SetField(original, "_traitSettings", settings);

            MercenaryDataList restored = ReloadWithLegacyField(original, "_traitSettings", "_abilitySettings");

            Assert.That(restored.Validate(out string error), Is.True, error);
            Assert.That(restored.TraitSettings, Is.SameAs(settings));
        }

        [Test]
        public void InvalidDirectionalChancesAreRejectedWithoutClamping(
            [Values("_frontChance", "_sideChance", "_rearChance")] string field,
            [Values(-0.01f, 1.01f, float.NaN, float.PositiveInfinity, float.NegativeInfinity)] float chance)
        {
            TraitSettings settings = CreateObject<TraitSettings>();
            SetNestedField(settings, "_directionalPhysicalNegation", field, chance);

            Assert.That(settings.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain(field.Substring(1)).IgnoreCase);
            Assert.That(GetField(GetField(settings, "_directionalPhysicalNegation"), field), Is.EqualTo(chance));
        }

        [TestCase(0f)]
        [TestCase(0.12345f)]
        [TestCase(1f)]
        public void DirectionalChancesAreIndependentAndDoNotNeedToSumToOne(float chance)
        {
            TraitSettings settings = CreateObject<TraitSettings>();
            SetNestedField(settings, "_directionalPhysicalNegation", "_frontChance", chance);
            SetNestedField(settings, "_directionalPhysicalNegation", "_sideChance", chance);
            SetNestedField(settings, "_directionalPhysicalNegation", "_rearChance", chance);

            Assert.That(settings.Validate(out string error), Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(settings.DirectionalPhysicalNegation.FrontChance, Is.EqualTo(chance));
            Assert.That(settings.DirectionalPhysicalNegation.SideChance, Is.EqualTo(chance));
            Assert.That(settings.DirectionalPhysicalNegation.RearChance, Is.EqualTo(chance));
        }

        [Test]
        public void ClassLookupRequiresSuccessfulValidation()
        {
            ClassDefinition[] definitions = { CreateClass(), CreateClass(), CreateClass() };
            MercenaryDataList list = CreateList(definitions);

            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out ClassDefinition result), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(error, Is.Null);

            for (int i = 0; i < definitions.Length; i++)
            {
                Assert.That(list.TryGetDefinition((MercenaryClass)i, out result), Is.True);
                Assert.That(result, Is.SameAs(definitions[i]));
            }

            Assert.That(list.TryGetDefinition((MercenaryClass)999, out result), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void NullAndEmptyClassDictionariesAreRejected()
        {
            MercenaryDataList list = CreateList();
            SetField(list, "_classes", null);

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

            SetField(list, "_classes", SerializableDictionaryTestUtility.Create<MercenaryClass, ClassDefinition>());

            Assert.That(list.Validate(out error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
        }

        [TestCase(MercenaryClass.Warrior)]
        [TestCase(MercenaryClass.Rogue)]
        [TestCase(MercenaryClass.Archer)]
        public void EveryDefinedClassIsRequired(MercenaryClass omitted)
        {
            MercenaryDataList list = CreateList();
            List<KeyValuePair<MercenaryClass, ClassDefinition>> entries = new List<KeyValuePair<MercenaryClass, ClassDefinition>>();

            for (int i = 0; i < 3; i++)
            {
                if ((MercenaryClass)i != omitted)
                {
                    entries.Add(new KeyValuePair<MercenaryClass, ClassDefinition>((MercenaryClass)i, CreateClass()));
                }
            }

            SetField(list, "_classes", SerializableDictionaryTestUtility.Create(entries.ToArray()));

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain(omitted.ToString()));
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
        }

        [TestCase(-1)]
        [TestCase(999)]
        public void UndefinedClassKeysAreRejected(int key)
        {
            MercenaryDataList list = CreateList();
            SerializableDictionary<MercenaryClass, ClassDefinition> classes = GetClasses(list);
            SerializableDictionaryTestUtility.AddEntry(classes, (MercenaryClass)key, CreateClass());

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
        }

        [Test]
        public void DuplicateKeysAreRejectedWithoutRemovingSourceEntries()
        {
            MercenaryDataList list = CreateList();
            SerializableDictionary<MercenaryClass, ClassDefinition> classes = GetClasses(list);
            ClassDefinition duplicate = CreateClass();
            SerializableDictionaryTestUtility.AddEntry(classes, MercenaryClass.Warrior, duplicate);

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(classes.EntryCount, Is.EqualTo(4));
            Assert.That(classes.GetKey(3), Is.EqualTo(MercenaryClass.Warrior));
            Assert.That(classes.GetValue(3), Is.SameAs(duplicate));
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
        }

        [Test]
        public void NullDefinitionsAndARepeatedDefinitionAssetAreRejected()
        {
            ClassDefinition warrior = CreateClass();
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), null });

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

            SetField(list, "_classes", SerializableDictionaryTestUtility.Create(
                new KeyValuePair<MercenaryClass, ClassDefinition>(MercenaryClass.Warrior, warrior),
                new KeyValuePair<MercenaryClass, ClassDefinition>(MercenaryClass.Rogue, CreateClass()),
                new KeyValuePair<MercenaryClass, ClassDefinition>(MercenaryClass.Archer, warrior)));

            Assert.That(list.Validate(out error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
        }

        [Test]
        public void FailedRevalidationPreventsPreviousClassLookups()
        {
            ClassDefinition warrior = CreateClass();
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), CreateClass() });

            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.True);

            SetNestedField(warrior, "_baseStats", "_maxHP", 0);

            Assert.That(list.Validate(out error), Is.False);
            Assert.That(error, Does.Contain("MaxHP"));
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out ClassDefinition result), Is.False);
            Assert.That(result, Is.Null);

            SetNestedField(warrior, "_baseStats", "_maxHP", 1);

            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
            Assert.That(list.Validate(out error), Is.True, error);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out result), Is.True);
            Assert.That(result, Is.SameAs(warrior));
        }

        [TestCase("OnValidate")]
        [TestCase("OnEnable")]
        public void UnityCallbacksInvalidateLookupUntilExplicitValidation(string callback)
        {
            MercenaryDataList list = CreateList();
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.True);

            MethodInfo method = typeof(MercenaryDataList).GetMethod(callback, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, callback);
            method.Invoke(list, null);

            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out ClassDefinition result), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(list.Validate(out error), Is.True, error);
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.True);
        }

        [Test]
        public void DirectionalNegationRequiresSettingsAndAcceptsAValidSharedAsset()
        {
            ClassDefinition definition = CreateClass(traits: new[] { TraitType.DirectionalPhysicalNegation });
            TraitSettings settings = CreateObject<TraitSettings>();
            MercenaryDataList list = CreateList(new[]
            {
                definition, CreateClass(new[] { TraitType.DirectionalPhysicalNegation }), CreateClass()
            });

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("TraitSettings"));

            SetField(list, "_traitSettings", settings);

            Assert.That(list.Validate(out error), Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(list.TraitSettings, Is.SameAs(settings));
        }

        [Test]
        public void EmptyTraitsAndRearCriticalDoNotRequireSettings()
        {
            ClassDefinition rearCritical = CreateClass(new[] { TraitType.RearGuaranteedCritical });
            MercenaryDataList list = CreateList(new[] { CreateClass(), rearCritical, CreateClass() });

            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(list.TraitSettings, Is.Null);
        }

        [Test]
        public void SuppliedSettingsAreValidatedEvenWhenNoClassUsesThem()
        {
            TraitSettings settings = CreateObject<TraitSettings>();
            SetNestedField(settings, "_directionalPhysicalNegation", "_frontChance", float.NaN);
            MercenaryDataList list = CreateList();
            SetField(list, "_traitSettings", settings);

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("FrontChance"));
            Assert.That(list.TryGetDefinition(MercenaryClass.Warrior, out _), Is.False);
        }

        [TestCase("Warrior", "전사", 10, 2, 0.05f, 1, 1, TraitType.OpportunityAttack)]
        [TestCase("Rogue", "도적", 8, 4, 0.15f, 0, 1, TraitType.RearGuaranteedCritical)]
        [TestCase("Archer", "궁수", 8, 2, 0.10f, 0, 8, (TraitType)0)]
        [Category("ProjectContent")]
        public void ClassAssetsKeepBaseStatsAndUseExplicitTraits(string assetName, string displayName,
            int maxHP, int movement, float criticalChance, int physicalDefense, int range, TraitType trait)
        {
            ClassDefinition definition = AssetDatabase.LoadAssetAtPath<ClassDefinition>(_assetDirectory + assetName + ".asset");

            Assert.That(definition, Is.Not.Null, assetName);
            Assert.That(definition.Validate(out string error), Is.True, error);
            Assert.That(definition.DisplayName, Is.EqualTo(displayName));
            Assert.That(definition.BaseStats.MaxHP, Is.EqualTo(maxHP));
            Assert.That(definition.BaseStats.Movement, Is.EqualTo(movement));
            Assert.That(definition.BaseStats.CriticalChance, Is.EqualTo(criticalChance));
            Assert.That(definition.BaseStats.PhysicalDefense, Is.EqualTo(physicalDefense));
            Assert.That(definition.BaseStats.WaterDefense, Is.Zero);
            Assert.That(definition.BaseStats.FireDefense, Is.Zero);
            Assert.That(definition.BaseStats.LightningDefense, Is.Zero);
            Assert.That(definition.BaseStats.PoisonDefense, Is.Zero);
            Assert.That(definition.BasicAttack.Damage, Is.EqualTo(3));
            Assert.That(definition.BasicAttack.MaxRange, Is.EqualTo(range));
            Assert.That(definition.BasicAttack.AttackAttribute, Is.EqualTo(AttackAttribute.Physical));
            Assert.That(definition.BaseStats.Initiative, Is.Zero);

            if (assetName == "Warrior")
            {
                Assert.That(definition.TraitCount, Is.EqualTo(2));
                Assert.That(definition.GetTrait(0), Is.EqualTo(TraitType.DirectionalPhysicalNegation));
                Assert.That(definition.GetTrait(1), Is.EqualTo(trait));
                return;
            }

            Assert.That(definition.TraitCount, Is.EqualTo(trait == 0 ? 0 : 1));

            if (trait != 0)
            {
                Assert.That(definition.GetTrait(0), Is.EqualTo(trait));
            }
        }

        [Test]
        [Category("ProjectContent")]
        public void PrototypeListResolvesAllClassAssetsAndTheDocumentedSettings()
        {
            ClassDefinition[] definitions =
            {
                AssetDatabase.LoadAssetAtPath<ClassDefinition>(_assetDirectory + "Warrior.asset"),
                AssetDatabase.LoadAssetAtPath<ClassDefinition>(_assetDirectory + "Rogue.asset"),
                AssetDatabase.LoadAssetAtPath<ClassDefinition>(_assetDirectory + "Archer.asset")
            };
            TraitSettings settings = AssetDatabase.LoadAssetAtPath<TraitSettings>(_assetDirectory + "TraitSettings.asset");
            MercenaryDataList list = AssetDatabase.LoadAssetAtPath<MercenaryDataList>(_assetDirectory + "MercenaryDataList.asset");

            Assert.That(settings, Is.Not.Null);
            Assert.That(list, Is.Not.Null);
            Assert.That(list.TraitSettings, Is.SameAs(settings));
            Assert.That(settings.DirectionalPhysicalNegation.FrontChance, Is.EqualTo(0.50f));
            Assert.That(settings.DirectionalPhysicalNegation.SideChance, Is.EqualTo(0.10f));
            Assert.That(settings.DirectionalPhysicalNegation.RearChance, Is.Zero);
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(error, Is.Null);

            for (int i = 0; i < definitions.Length; i++)
            {
                Assert.That(definitions[i], Is.Not.Null);
                Assert.That(list.TryGetDefinition((MercenaryClass)i, out ClassDefinition definition), Is.True);
                Assert.That(definition, Is.SameAs(definitions[i]));
            }
        }

        [Test]
        public void ClassRequiresABasicAttackWithAnOwnedRange()
        {
            ClassDefinition definition = CreateClass();
            SkillDefinition original = definition.BasicAttack;
            SetField(definition, "_basicAttack", null);
            Assert.That(definition.Validate(out _), Is.False);
            SetField(definition, "_basicAttack", CreateSkill(SkillType.Stealth, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 2, movement: 2));
            Assert.That(definition.Validate(out _), Is.False);
            SetField(definition, "_basicAttack", original);
            SetField(original, "_rangeSource", SkillRangeSource.BasicAttack);
            SetField(original, "_minRange", 0);
            SetField(original, "_maxRange", 0);
            Assert.That(definition.Validate(out _), Is.False);
        }

        [TestCase("null")]
        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("basic")]
        [TestCase("slot0")]
        [TestCase("slot10")]
        [TestCase("invalidDefinition")]
        public void InvalidCommonGrantsAreRejectedBeforeSnapshotCreation(string invalid)
        {
            MercenaryDataList list = CreateList();
            SkillGrant[] skills = (SkillGrant[])GetField(list, "_commonSkills");

            switch (invalid)
            {
                case "null":
                    SetField(list, "_commonSkills", null);
                    break;
                case "missing":
                    skills[0] = CreateGrant(null, 2);
                    break;
                case "duplicate":
                    SetField(list, "_commonSkills", new[] { skills[0], CreateGrant(skills[0].Skill, 3) });
                    break;
                case "basic":
                    skills[0] = CreateGrant(CreateClass().BasicAttack, 2);
                    break;
                case "slot0":
                    skills[0] = CreateGrant(skills[0].Skill, 0);
                    break;
                case "slot10":
                    skills[0] = CreateGrant(skills[0].Skill, 10);
                    break;
                case "invalidDefinition":
                    SetField(skills[0].Skill, "_actionPointCost", -1);
                    break;
            }

            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.None,
                out _, out _), Is.False);
        }

        [Test]
        public void SkillSnapshotPreservesClassAttackAndActiveSettingsAfterAssetEdits()
        {
            ClassDefinition warrior = CreateClass();
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), CreateClass() });
            SetField(warrior.BasicAttack, "_maxRange", 8);
            SetField(warrior.BasicAttack, "_damage", 7);
            SetField(warrior.BasicAttack, "_attackAttribute", AttackAttribute.Fire);
            SetField(warrior.BasicAttack, "_actionPointCost", 3);
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.Pioneer,
                out SkillSet snapshot, out error), Is.True, error);
            SerializableDictionary<MercenarySpecialization, SpecializationDefinition> specializations =
                (SerializableDictionary<MercenarySpecialization, SpecializationDefinition>)GetField(list, "_specializations");
            Assert.That(specializations.TryGetValue(MercenarySpecialization.Pioneer, out SpecializationDefinition pioneer), Is.True);
            SkillGrant[] grants = (SkillGrant[])GetField(pioneer, "_grantedSkills");
            SetField(warrior.BasicAttack, "_maxRange", 1);
            SetField(warrior.BasicAttack, "_damage", 2);
            SetField(warrior.BasicAttack, "_actionPointCost", 1);
            SetField(grants[0].Skill, "_actionPointCost", 7);
            grants[0] = CreateGrant(grants[0].Skill, 9);
            Assert.That(snapshot.BasicAttack.MaxRange, Is.EqualTo(8));
            Assert.That(snapshot.BasicAttack.Damage, Is.EqualTo(7));
            Assert.That(snapshot.BasicAttack.AttackAttribute, Is.EqualTo(AttackAttribute.Fire));
            Assert.That(snapshot.BasicAttack.ActionPointCost, Is.EqualTo(3));
            Assert.That(snapshot.Get(SkillType.PullSelf).ActionPointCost, Is.EqualTo(2));
            Assert.That(snapshot.GetSlot(SkillType.PullSelf), Is.EqualTo(1));
            Assert.That(snapshot.TryGetSlot(9, out _), Is.False);
            Assert.That(snapshot.Contains(SkillType.PullTarget), Is.True);
            Assert.That(snapshot.Contains(SkillType.Invisibility), Is.False);
            Assert.That(list.Validate(out error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.Pioneer,
                out SkillSet nextSnapshot, out error), Is.True, error);
            Assert.That(nextSnapshot.GetSlot(SkillType.PullSelf), Is.EqualTo(9));
            Assert.That(nextSnapshot.Get(SkillType.PullSelf).ActionPointCost, Is.EqualTo(7));
        }

        [Test]
        public void EmptyGrantsAllowBasicAttackOnly()
        {
            MercenaryDataList list = CreateList();
            SetField(list, "_commonSkills", Array.Empty<SkillGrant>());
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.None,
                out SkillSet skills, out error), Is.True, error);
            Assert.That(skills.BasicAttack, Is.Not.Null);
            Assert.That(skills.Contains(SkillType.Stealth), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DuplicateTypesFromDifferentAssetsAndDuplicateSlotsAcrossSourcesAreRejected(bool duplicateSlot)
        {
            ClassDefinition warrior = CreateClass();
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), CreateClass() });
            SkillDefinition grant = duplicateSlot
                ? CreateSkill(SkillType.Invisibility, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2)
                : CreateSkill(SkillType.Stealth, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2, movement: 2);
            SetField(warrior, "_grantedSkills", new[] { CreateGrant(grant, duplicateSlot ? 2 : 8) });
            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain(duplicateSlot ? "Slot" : "Skill type"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SpecializationGrantsCannotConflictWithClassGrants(bool duplicateSlot)
        {
            ClassDefinition warrior = CreateClass();
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), CreateClass() });
            SkillDefinition grant = duplicateSlot
                ? CreateSkill(SkillType.Invisibility, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2)
                : CreateSkill(SkillType.PullSelf, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2);
            SetField(warrior, "_grantedSkills", new[] { CreateGrant(grant, duplicateSlot ? 1 : 8) });
            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain(duplicateSlot ? "Slot" : "Skill type"));
        }

        [Test]
        public void SkillsCanBeGrantedToAnotherClassWithoutGrantingTraits()
        {
            ClassDefinition warrior = CreateClass();
            SkillDefinition ambush = CreateSkill(SkillType.Ambush, SkillTargetType.Unit,
                SkillRangeSource.BasicAttack, 0, 0, 2, bonus: 2);
            SetField(warrior, "_grantedSkills", new[] { CreateGrant(ambush, 8) });
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), CreateClass() });
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.None,
                out SkillSet skills, out error), Is.True, error);
            Assert.That(skills.GetSlot(SkillType.Ambush), Is.EqualTo(8));
            Assert.That(warrior.TraitCount, Is.Zero);
        }

        [TestCase(MercenaryClass.Archer, MercenarySpecialization.Pioneer)]
        [TestCase(MercenaryClass.Warrior, MercenarySpecialization.Assassin)]
        [TestCase(MercenaryClass.Rogue, MercenarySpecialization.SpiritArcher)]
        [TestCase(MercenaryClass.Warrior, (MercenarySpecialization)999)]
        public void InvalidClassSpecializationPairsAreRejected(MercenaryClass unitClass, MercenarySpecialization specialization)
        {
            MercenaryDataList list = CreateList();
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(unitClass, specialization, out SkillSet skills, out error), Is.False);
            Assert.That(skills, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [TestCase(MercenarySpecialization.None)]
        [TestCase((MercenarySpecialization)999)]
        public void InvalidSpecializationDictionaryKeysAreRejected(MercenarySpecialization specialization)
        {
            MercenaryDataList list = CreateList();
            SetField(list, "_specializations", SerializableDictionaryTestUtility.Create(
                new KeyValuePair<MercenarySpecialization, SpecializationDefinition>(specialization,
                    CreateObject<SpecializationDefinition>())));
            Assert.That(list.Validate(out _), Is.False);
        }

        [Test]
        public void NullClassAndSpecializationGrantArraysAreRejected()
        {
            ClassDefinition definition = CreateClass();
            SetField(definition, "_grantedSkills", null);
            Assert.That(definition.Validate(out _), Is.False);
            SpecializationDefinition specialization = CreateObject<SpecializationDefinition>();
            SetField(specialization, "_grantedSkills", null);
            Assert.That(specialization.Validate(out _), Is.False);
        }

        [Test]
        public void MissingSpecializationReferencesFailWithoutFallingBackToCodeGrants()
        {
            MercenaryDataList list = CreateList();
            SetField(list, "_specializations", SerializableDictionaryTestUtility.Create<MercenarySpecialization, SpecializationDefinition>());
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.Pioneer,
                out _, out _), Is.False);
            SetField(list, "_specializations", SerializableDictionaryTestUtility.Create(
                new KeyValuePair<MercenarySpecialization, SpecializationDefinition>(MercenarySpecialization.Pioneer, null)));
            Assert.That(list.Validate(out _), Is.False);
            SetField(list, "_specializations", null);
            Assert.That(list.Validate(out _), Is.False);
        }

        [TestCase(MercenaryClass.Warrior, MercenarySpecialization.Pioneer, SkillType.PullSelf, 1)]
        [TestCase(MercenaryClass.Warrior, MercenarySpecialization.Pioneer, SkillType.PullTarget, 3)]
        [TestCase(MercenaryClass.Rogue, MercenarySpecialization.Assassin, SkillType.Ambush, 1)]
        [TestCase(MercenaryClass.Rogue, MercenarySpecialization.Assassin, SkillType.AssassinLeap, 3)]
        [TestCase(MercenaryClass.Rogue, MercenarySpecialization.Assassin, SkillType.Invisibility, 4)]
        [TestCase(MercenaryClass.Archer, MercenarySpecialization.SpiritArcher, SkillType.Overwatch, 1)]
        [TestCase(MercenaryClass.Archer, MercenarySpecialization.SpiritArcher, SkillType.SpiritImbuement, 3)]
        [Category("ProjectContent")]
        public void MigratedAssetsPreserveGrantedSkillsAndSlots(MercenaryClass unitClass,
            MercenarySpecialization specialization, SkillType type, int slot)
        {
            MercenaryDataList list = AssetDatabase.LoadAssetAtPath<MercenaryDataList>(_assetDirectory + "MercenaryDataList.asset");
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateSkillSet(unitClass, specialization, out SkillSet skills, out error), Is.True, error);
            Assert.That(skills.GetSlot(type), Is.EqualTo(slot));
            Assert.That(skills.GetSlot(SkillType.Stealth), Is.EqualTo(2));
        }

        [Test]
        public void SpiritImbuementSnapshotsZeroCostAndConfiguredDamage()
        {
            SkillDefinition definition = CreateSkill(SkillType.SpiritImbuement, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 0);
            SetField(definition, "_fireSpiritDamage", 3);
            SetField(definition, "_electricSpiritDamage", 2);
            Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition snapshot, out string error), Is.True, error);
            SetField(definition, "_fireSpiritDamage", 7);
            SetField(definition, "_electricSpiritDamage", 8);
            Assert.That(snapshot.ActionPointCost, Is.Zero);
            Assert.That(snapshot.FireSpiritDamage, Is.EqualTo(3));
            Assert.That(snapshot.ElectricSpiritDamage, Is.EqualTo(2));
        }

        [TestCase("_fireSpiritDamage")]
        [TestCase("_electricSpiritDamage")]
        public void SpiritImbuementRejectsNegativeDamage(string field)
        {
            SkillDefinition definition = CreateSkill(SkillType.SpiritImbuement, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 0);
            SetField(definition, field, -1);
            Assert.That(definition.TryCreateRuntime(out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        [Category("ProjectContent")]
        public void ShippedSpiritImbuementUsesZeroApAndOneAdditionalDamage()
        {
            SkillDefinition definition = AssetDatabase.LoadAssetAtPath<SkillDefinition>(
                _assetDirectory + "Skills/SpiritImbuement.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition runtime, out string error), Is.True, error);
            Assert.That(runtime.Type, Is.EqualTo(SkillType.SpiritImbuement));
            Assert.That(runtime.TargetType, Is.EqualTo(SkillTargetType.Self));
            Assert.That(runtime.ActionPointCost, Is.Zero);
            Assert.That(runtime.FireSpiritDamage, Is.EqualTo(1));
            Assert.That(runtime.ElectricSpiritDamage, Is.EqualTo(1));
        }

        [Test]
        public void ExplicitInputModeIsValidatedAndCapturedInTheRuntimeSnapshot()
        {
            SkillDefinition definition = CreateSkill(SkillType.SpiritImbuement, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 0);
            Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition snapshot, out string error), Is.True, error);
            Assert.That(snapshot.InputMode, Is.EqualTo(SkillInputMode.Choice));
            SetField(definition, "_inputMode", SkillInputMode.Self);
            Assert.That(definition.TryCreateRuntime(out _, out error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(snapshot.InputMode, Is.EqualTo(SkillInputMode.Choice));
        }

        [TestCase(SkillType.BasicAttack, -1)]
        [TestCase(SkillType.BasicAttack, 1)]
        [TestCase(SkillType.BasicAttack, int.MaxValue)]
        [TestCase(SkillType.SpiritImbuement, -1)]
        public void InvalidCooldownIsRejectedWithoutChangingTheAsset(SkillType type, int cooldown)
        {
            SkillDefinition definition = type == SkillType.BasicAttack
                ? CreateSkill(type, SkillTargetType.Unit, SkillRangeSource.Own, 1, 1, 2, damage: 3)
                : CreateSkill(type, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0);
            SetField(definition, "_cooldownTurns", cooldown);
            Assert.That(definition.Validate(out string validationError), Is.False);
            Assert.That(validationError, Is.Not.Empty);
            Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition snapshot, out string creationError), Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(creationError, Is.Not.Empty);
            Assert.That(definition.CooldownTurns, Is.EqualTo(cooldown));
        }

        [Test]
        public void CooldownSnapshotDoesNotChangeWhenTheAssetIsEdited()
        {
            SkillDefinition definition = CreateSkill(SkillType.SpiritImbuement, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 0);
            SetField(definition, "_cooldownTurns", 2);
            Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition snapshot, out string error), Is.True, error);
            SetField(definition, "_cooldownTurns", 4);
            Assert.That(snapshot.CooldownTurns, Is.EqualTo(2));
            Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition updated, out error), Is.True, error);
            Assert.That(updated.CooldownTurns, Is.EqualTo(4));
        }

        [Test]
        public void InvalidBasicAttackCooldownRejectsTheCatalogBeforeBattlePreparation()
        {
            ClassDefinition warrior = CreateClass();
            MercenaryDataList list = CreateList(new[] { warrior, CreateClass(), CreateClass() });
            SetField(warrior.BasicAttack, "_cooldownTurns", 1);
            Assert.That(list.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(list.TryCreateSkillSet(MercenaryClass.Warrior, MercenarySpecialization.None,
                out _, out _), Is.False);
        }

        [Test]
        [Category("ProjectContent")]
        public void ShippedSkillsHaveThreeTurnCooldownExceptBasicAttacks()
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillDefinition", new[] { _assetDirectory + "Skills" });
            Assert.That(guids, Is.Not.Empty);
            for (int i = 0; i < guids.Length; i++)
            {
                SkillDefinition definition = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                int expected = definition.Type == SkillType.BasicAttack ? 0 : 3;
                Assert.That(definition.CooldownTurns, Is.EqualTo(expected), definition.name);
                Assert.That(definition.TryCreateRuntime(out BattleSkillDefinition runtime, out string error), Is.True, error);
                Assert.That(runtime.CooldownTurns, Is.EqualTo(expected), definition.name);
            }
        }

        [Test]
        public void SpecializationTraitsAreComposedFromDataAndKeepDistinctSources()
        {
            MercenaryDataList list = CreateList(new[]
            {
                CreateClass(new[] { TraitType.OpportunityAttack }), CreateClass(), CreateClass()
            });
            SerializableDictionary<MercenarySpecialization, SpecializationDefinition> definitions =
                (SerializableDictionary<MercenarySpecialization, SpecializationDefinition>)GetField(list, "_specializations");
            Assert.That(definitions.Validate(out _), Is.True);
            Assert.That(definitions.TryGetValue(MercenarySpecialization.Pioneer, out SpecializationDefinition pioneer), Is.True);
            SetField(pioneer, "_traits", new[] { TraitType.OpportunityAttack });
            Assert.That(list.Validate(out string error), Is.True, error);
            Assert.That(list.TryCreateTraits(MercenaryClass.Warrior, MercenarySpecialization.Pioneer,
                out TraitCollection traits, out error), Is.True, error);
            Assert.That(traits.GetCount(TraitType.OpportunityAttack), Is.EqualTo(2));
            Assert.That(traits.Get(TraitType.OpportunityAttack, 0).Source.Kind, Is.EqualTo(TraitSourceKind.Class));
            Assert.That(traits.Get(TraitType.OpportunityAttack, 1).Source.Kind, Is.EqualTo(TraitSourceKind.Specialization));
            Assert.That(traits.Has(TraitType.SkillMovementRecovery), Is.False);

            SetField(pioneer, "_traits", Array.Empty<TraitType>());
            Assert.That(list.TryCreateTraits(MercenaryClass.Warrior, MercenarySpecialization.Pioneer,
                out TraitCollection updated, out error), Is.True, error);
            Assert.That(updated.GetCount(TraitType.OpportunityAttack), Is.EqualTo(1));
            Assert.That(traits.GetCount(TraitType.OpportunityAttack), Is.EqualTo(2));
        }

        [Test]
        public void SpecializationTraitsRejectDuplicateInvalidAndNullLists()
        {
            SpecializationDefinition definition = CreateObject<SpecializationDefinition>();
            SetField(definition, "_traits", new[] { TraitType.OpportunityAttack, TraitType.OpportunityAttack });
            Assert.That(definition.Validate(out _), Is.False);
            SetField(definition, "_traits", new[] { (TraitType)0 });
            Assert.That(definition.Validate(out _), Is.False);
            SetField(definition, "_traits", null);
            Assert.That(definition.Validate(out _), Is.False);
        }

        [Test]
        public void SpecializationNegationRequiresSettingsDuringValidationAndComposition()
        {
            MercenaryDataList list = CreateList();
            Assert.That(list.Validate(out string error), Is.True, error);
            SerializableDictionary<MercenarySpecialization, SpecializationDefinition> definitions =
                (SerializableDictionary<MercenarySpecialization, SpecializationDefinition>)GetField(list, "_specializations");
            Assert.That(definitions.TryGetValue(MercenarySpecialization.Pioneer, out SpecializationDefinition pioneer), Is.True);
            SetField(pioneer, "_traits", new[] { TraitType.DirectionalPhysicalNegation });
            Assert.That(list.TryCreateTraits(MercenaryClass.Warrior, MercenarySpecialization.Pioneer, out _, out error), Is.False);
            Assert.That(error, Does.Contain("TraitSettings"));
            Assert.That(list.Validate(out error), Is.False);
            Assert.That(error, Does.Contain("TraitSettings"));
        }

        [TestCase(MercenaryClass.Warrior, MercenarySpecialization.None, true)]
        [TestCase(MercenaryClass.Warrior, MercenarySpecialization.Pioneer, true)]
        [TestCase(MercenaryClass.Rogue, MercenarySpecialization.Pioneer, false)]
        [TestCase(MercenaryClass.Archer, MercenarySpecialization.Assassin, false)]
        [TestCase((MercenaryClass)(-1), MercenarySpecialization.None, false)]
        [TestCase(MercenaryClass.Warrior, (MercenarySpecialization)99, false)]
        public void SpecializationCompatibilityRejectsUndefinedAndMismatchedValues(
            MercenaryClass unitClass, MercenarySpecialization specialization, bool expected)
        {
            Assert.That(MercenarySpecializationRules.IsCompatible(unitClass, specialization), Is.EqualTo(expected));
        }

        [TestCase("Pioneer", TraitType.SkillMovementRecovery)]
        [TestCase("Assassin", TraitType.BattleStartInvisibility)]
        [TestCase("SpiritArcher", TraitType.FourElementBasicAttack)]
        [Category("ProjectContent")]
        public void ShippedSpecializationsKeepTheirExistingTrait(string name, TraitType expected)
        {
            SpecializationDefinition definition = AssetDatabase.LoadAssetAtPath<SpecializationDefinition>(_assetDirectory + name + ".asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Validate(out string error), Is.True, error);
            Assert.That(definition.TraitCount, Is.EqualTo(1));
            Assert.That(definition.GetTrait(0), Is.EqualTo(expected));
        }

        MercenaryDataList CreateList(ClassDefinition[] definitions = null)
        {
            if (definitions == null)
            {
                definitions = new[] { CreateClass(), CreateClass(), CreateClass() };
            }

            MercenaryDataList list = CreateObject<MercenaryDataList>();
            SetField(list, "_commonSkills", new[]
            {
                CreateGrant(CreateSkill(SkillType.Stealth, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2, movement: 2), 2)
            });
            SpecializationDefinition pioneer = CreateObject<SpecializationDefinition>();
            SetField(pioneer, "_grantedSkills", new[]
            {
                CreateGrant(CreateSkill(SkillType.PullSelf, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2), 1),
                CreateGrant(CreateSkill(SkillType.PullTarget, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2), 3)
            });
            SetField(list, "_specializations", SerializableDictionaryTestUtility.Create(
                new KeyValuePair<MercenarySpecialization, SpecializationDefinition>(MercenarySpecialization.Pioneer, pioneer)));
            SetField(list, "_classes", SerializableDictionaryTestUtility.Create(
                new KeyValuePair<MercenaryClass, ClassDefinition>(MercenaryClass.Warrior, definitions[0]),
                new KeyValuePair<MercenaryClass, ClassDefinition>(MercenaryClass.Rogue, definitions[1]),
                new KeyValuePair<MercenaryClass, ClassDefinition>(MercenaryClass.Archer, definitions[2])));
            return list;
        }

        static SkillGrant CreateGrant(SkillDefinition definition, int slot)
        {
            object grant = default(SkillGrant);
            SetField(grant, "_skill", definition);
            SetField(grant, "_slot", slot);
            return (SkillGrant)grant;
        }

        static SerializableDictionary<MercenaryClass, ClassDefinition> GetClasses(MercenaryDataList list)
        {
            return (SerializableDictionary<MercenaryClass, ClassDefinition>)GetField(list, "_classes");
        }

        ClassDefinition CreateClass(TraitType[] traits = null)
        {
            ClassDefinition definition = CreateObject<ClassDefinition>();
            SetField(definition, "_displayName", "Test class");
            SetField(definition, "_baseStats", default(ClassBaseStats));
            SetNestedField(definition, "_baseStats", "_maxHP", 1);
            SetField(definition, "_basicAttack", CreateSkill(SkillType.BasicAttack, SkillTargetType.Unit, SkillRangeSource.Own, 1, 1, 2, damage: 1));
            SetNestedField(definition, "_basicAttack", "_damage", 1);
            SetNestedField(definition, "_basicAttack", "_maxRange", 1);
            SetField(definition, "_traits", traits ?? Array.Empty<TraitType>());
            return definition;
        }

        T ReloadWithLegacyField<T>(T main, string field, string legacyField) where T : ScriptableObject
        {
            _legacyFolder = _legacyFolderPrefix + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", _legacyFolder.Substring("Assets/".Length));
            string path = _legacyFolder + "/Legacy.asset";
            AssetDatabase.CreateAsset(main, path);

            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null && _objects[i] != main)
                {
                    AssetDatabase.AddObjectToAsset(_objects[i], main);
                }
            }

            AssetDatabase.SaveAssets();
            string fullPath = Path.GetFullPath(path);
            string text = File.ReadAllText(fullPath).Replace("\r\n", "\n");
            string current = "\n  " + field + ":";
            Assert.That(text.Contains(current), Is.True);
            File.WriteAllText(fullPath, text.Replace(current, "\n  " + legacyField + ":"));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            T restored = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(restored, Is.Not.Null);
            return restored;
        }

        T CreateObject<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            instance.name = typeof(T).Name + " test";
            _objects.Add(instance);
            return instance;
        }

        SkillDefinition CreateSkill(SkillType type, SkillTargetType target, SkillRangeSource source,
            int min, int max, int cost, int damage = 0, int movement = 0, int bonus = 0)
        {
            SkillDefinition definition = CreateObject<SkillDefinition>();
            SetField(definition, "_type", type);
            SetField(definition, "_targetType", target);
            SetField(definition, "_inputMode", type == SkillType.SpiritImbuement ? SkillInputMode.Choice
                : target == SkillTargetType.Self ? SkillInputMode.Self
                : target == SkillTargetType.Direction ? SkillInputMode.Direction : SkillInputMode.Unit);
            SetField(definition, "_rangeSource", source);
            SetField(definition, "_minRange", min);
            SetField(definition, "_maxRange", max);
            SetField(definition, "_actionPointCost", cost);
            SetField(definition, "_movementCost", movement);
            SetField(definition, "_damage", damage);
            SetField(definition, "_damageBonus", bonus);
            return definition;
        }

        static void SetNestedField(object target, string group, string field, object value)
        {
            object nested = GetField(target, group);
            SetField(nested, field, value);
            SetField(target, group, nested);
        }

        static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
