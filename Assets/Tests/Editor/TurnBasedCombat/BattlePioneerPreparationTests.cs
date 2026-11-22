using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.BattleMaps;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattlePioneerPreparationTests
    {
        readonly List<Object> _objects = new List<Object>();
        BattleMapDefinition _map;
        MercenaryDataList _classData;
        ClassDefinition _warrior;
        BattleClassViewPrefab[] _prefabs;

        [SetUp]
        public void SetUp()
        {
            _map = CreateAsset<BattleMapDefinition>();
            SetField(_map, "_units", new[]
            {
                new BattleMapUnitPlacement(new HexCoordinate(3, 3), MercenaryClass.Warrior,
                    BattleTeam.Enemy, HexDirection.East)
            });
            SetField(_map, "_startPoints", new[]
            {
                new BattleMapStartPoint(new HexCoordinate(0, 0), 1, HexDirection.East),
                new BattleMapStartPoint(new HexCoordinate(1, 0), 2, HexDirection.East)
            });
            _classData = CreateAsset<MercenaryDataList>();
            _prefabs = new BattleClassViewPrefab[3];
            SerializedObject serialized = new SerializedObject(_classData);
            SerializedProperty entries = serialized.FindProperty("_classes").FindPropertyRelative("_entries");
            entries.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                ClassDefinition definition = CreateAsset<ClassDefinition>();
                SetField(definition, "_basicAttack", BattleSkillTestAssets.CreateBasicAttack(asset => _objects.Add(asset)));
                SetField(definition, "_displayName", ((MercenaryClass)i).ToString());
                SetNestedField(definition, "_baseStats", "_maxHP", 10);
                SetNestedField(definition, "_baseStats", "_movement", 2);
                SetNestedField(definition, "_basicAttack", "_damage", 3);
                SetNestedField(definition, "_basicAttack", "_maxRange", 1);

                if (i == 0)
                {
                    _warrior = definition;
                    SetField(definition, "_traits", new[] { TraitType.OpportunityAttack });
                }

                GameObject prefab = new GameObject("Class " + i);
                _objects.Add(prefab);
                _prefabs[i] = new BattleClassViewPrefab((MercenaryClass)i, prefab.AddComponent<HexCharacterView>());
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").intValue = i;
                entry.FindPropertyRelative("_value").objectReferenceValue = definition;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            BattleSkillTestAssets.SetCatalog(_classData, asset => _objects.Add(asset));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void OnlySelectedWarriorReceivesPioneerAndItsSpecializationTrait()
        {
            MercenaryClass[] party = { MercenaryClass.Warrior, MercenaryClass.Warrior };
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(1, MercenarySpecialization.Pioneer) };
            Assert.That(Prepare(party, choices, out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);
            Assert.That(inputs[0].Specialization, Is.EqualTo(MercenarySpecialization.None));
            Assert.That(inputs[0].Traits.Has(TraitType.SkillMovementRecovery), Is.False);
            Assert.That(inputs[1].Specialization, Is.EqualTo(MercenarySpecialization.Pioneer));
            Assert.That(inputs[1].Traits.Has(TraitType.OpportunityAttack), Is.True);
            Assert.That(inputs[1].Traits.GetCount(TraitType.SkillMovementRecovery), Is.EqualTo(1));
            Trait grant = inputs[1].Traits.Get(TraitType.SkillMovementRecovery, 0);
            Assert.That(grant.Source.Kind, Is.EqualTo(TraitSourceKind.Specialization));
            Assert.That(grant.Source.Id, Is.EqualTo((int)MercenarySpecialization.Pioneer));
            Assert.That(inputs[2].Specialization, Is.EqualTo(MercenarySpecialization.None));
            Assert.That(inputs[2].Traits.Has(TraitType.SkillMovementRecovery), Is.False);
            Assert.That(_warrior.TraitCount, Is.EqualTo(1));
            Assert.That(_warrior.GetTrait(0), Is.EqualTo(TraitType.OpportunityAttack));
        }

        [Test]
        public void ExistingPreparationCallsDefaultEveryParticipantToNoSpecialization()
        {
            Assert.That(BattleMapPreparation.TryPrepare(_map, new[] { MercenaryClass.Warrior }, _classData,
                _prefabs, 4, 4, 4, out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.That(inputs[i].Specialization, Is.EqualTo(MercenarySpecialization.None));
                Assert.That(inputs[i].Traits.Has(TraitType.SkillMovementRecovery), Is.False);
            }
        }

        [TestCase(-1, MercenarySpecialization.Pioneer, MercenaryClass.Warrior)]
        [TestCase(1, MercenarySpecialization.Pioneer, MercenaryClass.Warrior)]
        [TestCase(0, (MercenarySpecialization)99, MercenaryClass.Warrior)]
        [TestCase(0, (MercenarySpecialization)(-1), MercenaryClass.Warrior)]
        [TestCase(0, MercenarySpecialization.Pioneer, MercenaryClass.Rogue)]
        [TestCase(0, MercenarySpecialization.Pioneer, MercenaryClass.Archer)]
        public void InvalidSpecializationConfigurationsFailBeforePreparation(int index,
            MercenarySpecialization specialization, MercenaryClass unitClass)
        {
            MercenaryClass[] party = { unitClass };
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(index, specialization) };
            Assert.That(BattleMapPreparation.TryValidateConfiguration(_map, party, _classData, _prefabs,
                4, out string error, choices), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(Prepare(party, choices, out BattleSpawnInput[] inputs, out int skipped, out error), Is.False);
            Assert.That(inputs, Is.Empty);
            Assert.That(skipped, Is.Zero);
        }

        [Test]
        public void DuplicateIndicesAreRejectedEvenWhenOneEntryIsNone()
        {
            BattlePartySpecialization[] choices =
            {
                new BattlePartySpecialization(0, MercenarySpecialization.None),
                new BattlePartySpecialization(0, MercenarySpecialization.Pioneer)
            };
            Assert.That(Prepare(new[] { MercenaryClass.Warrior }, choices, out _, out _, out _), Is.False);
        }

        [Test]
        public void InvalidSpecializationOnSkippedPartyMemberStillFails()
        {
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(2, MercenarySpecialization.Pioneer) };
            Assert.That(Prepare(new[] { MercenaryClass.Warrior, MercenaryClass.Warrior, MercenaryClass.Rogue },
                choices, out _, out _, out _), Is.False);
        }

        [Test]
        public void SkippedValidSpecializationDoesNotGrantAnotherParticipantItsTrait()
        {
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(2, MercenarySpecialization.Pioneer) };
            Assert.That(Prepare(new[] { MercenaryClass.Warrior, MercenaryClass.Warrior, MercenaryClass.Warrior },
                choices, out BattleSpawnInput[] inputs, out int skipped, out string error), Is.True, error);
            Assert.That(skipped, Is.EqualTo(1));

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.That(inputs[i].Specialization, Is.EqualTo(MercenarySpecialization.None));
                Assert.That(inputs[i].Traits.Has(TraitType.SkillMovementRecovery), Is.False);
            }
        }

        [Test]
        public void PreparedAndRestartInputsKeepSpecializationWhenTheSourceSelectionChanges()
        {
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(0, MercenarySpecialization.Pioneer) };
            Assert.That(Prepare(new[] { MercenaryClass.Warrior }, choices,
                out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);
            GameObject mapObject = new GameObject("Map");
            _objects.Add(mapObject);
            HexMapView mapView = mapObject.AddComponent<HexMapView>();
            BattleRestartSnapshot snapshot = new BattleRestartSnapshot(mapView, inputs, new BattleRules(), 1, 0);
            BattleSpawnInput pioneer = inputs[0];
            choices[0] = new BattlePartySpecialization(0, MercenarySpecialization.None);
            inputs[0] = inputs[1];
            Assert.That(snapshot.Inputs[0], Is.SameAs(pioneer));
            Assert.That(snapshot.Inputs[0].Specialization, Is.EqualTo(MercenarySpecialization.Pioneer));
            Assert.That(snapshot.Inputs[0].Traits.Has(TraitType.SkillMovementRecovery), Is.True);
        }

        [Test]
        public void TraitValidatorsAcceptRecoveryAndRetainDistinctSources()
        {
            SetField(_warrior, "_traits", new[] { TraitType.SkillMovementRecovery });
            Assert.That(_warrior.Validate(out string error), Is.True, error);
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(0, MercenarySpecialization.Pioneer) };
            Assert.That(Prepare(new[] { MercenaryClass.Warrior }, choices,
                out BattleSpawnInput[] inputs, out _, out error), Is.True, error);
            Assert.That(inputs[0].Traits.GetCount(TraitType.SkillMovementRecovery), Is.EqualTo(2));
            Assert.That(inputs[0].Traits.Get(TraitType.SkillMovementRecovery, 0).Source.Kind, Is.EqualTo(TraitSourceKind.Class));
            Assert.That(inputs[0].Traits.Get(TraitType.SkillMovementRecovery, 1).Source.Kind, Is.EqualTo(TraitSourceKind.Specialization));
            SetField(_warrior, "_traits", new[] { TraitType.SkillMovementRecovery, TraitType.SkillMovementRecovery });
            Assert.That(_warrior.Validate(out _), Is.False);
        }

        bool Prepare(IReadOnlyList<MercenaryClass> party, IReadOnlyList<BattlePartySpecialization> specializations,
            out BattleSpawnInput[] inputs, out int skipped, out string error)
        {
            return BattleMapPreparation.TryPrepare(_map, party, _classData, _prefabs, 4, 4, 4,
                out inputs, out skipped, out error, specializations);
        }

        T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void SetNestedField(object target, string group, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(group, BindingFlags.Instance | BindingFlags.NonPublic);
            object nested = field.GetValue(target);
            SetField(nested, name, value);
            field.SetValue(target, nested);
        }
    }
}
