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
    public sealed class BattleSpiritArcherPreparationTests
    {
        readonly List<Object> _objects = new List<Object>();
        BattleMapDefinition _map;
        MercenaryDataList _classData;
        ClassDefinition _archer;
        BattleClassViewPrefab[] _prefabs;

        [SetUp]
        public void SetUp()
        {
            _map = CreateAsset<BattleMapDefinition>();
            SetField(_map, "_units", new[]
            {
                new BattleMapUnitPlacement(new HexCoordinate(3, 3), MercenaryClass.Archer,
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

                if (i == (int)MercenaryClass.Archer)
                {
                    _archer = definition;
                    SetField(definition, "_traits", new[] { TraitType.RearGuaranteedCritical });
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
        public void OnlySelectedArcherReceivesSpiritArcherAndItsSpecializationTrait()
        {
            MercenaryClass[] party = { MercenaryClass.Archer, MercenaryClass.Archer };
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(1, MercenarySpecialization.SpiritArcher) };
            Assert.That(Prepare(party, choices, out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);
            Assert.That(inputs[0].Specialization, Is.EqualTo(MercenarySpecialization.None));
            Assert.That(inputs[0].Traits.Has(TraitType.FourElementBasicAttack), Is.False);
            Assert.That(inputs[1].Specialization, Is.EqualTo(MercenarySpecialization.SpiritArcher));
            Assert.That(inputs[1].Traits.Has(TraitType.RearGuaranteedCritical), Is.True);
            Assert.That(inputs[1].Traits.GetCount(TraitType.FourElementBasicAttack), Is.EqualTo(1));
            Trait grant = inputs[1].Traits.Get(TraitType.FourElementBasicAttack, 0);
            Assert.That(grant.Source.Kind, Is.EqualTo(TraitSourceKind.Specialization));
            Assert.That(grant.Source.Id, Is.EqualTo((int)MercenarySpecialization.SpiritArcher));
            Assert.That(inputs[2].Specialization, Is.EqualTo(MercenarySpecialization.None));
            Assert.That(inputs[2].Traits.Has(TraitType.FourElementBasicAttack), Is.False);
            Assert.That(_archer.TraitCount, Is.EqualTo(1));
            Assert.That(_archer.GetTrait(0), Is.EqualTo(TraitType.RearGuaranteedCritical));
        }

        [Test]
        public void ExistingPreparationCallsDefaultEveryParticipantToNoSpecialization()
        {
            Assert.That(BattleMapPreparation.TryPrepare(_map, new[] { MercenaryClass.Archer }, _classData,
                _prefabs, 4, 4, 4, out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.That(inputs[i].Specialization, Is.EqualTo(MercenarySpecialization.None));
                Assert.That(inputs[i].Traits.Has(TraitType.FourElementBasicAttack), Is.False);
            }
        }

        [Test]
        public void MixedSpecializationsGrantOnlyTheirOwnTraits()
        {
            MercenaryClass[] party = { MercenaryClass.Warrior, MercenaryClass.Archer };
            BattlePartySpecialization[] choices =
            {
                new BattlePartySpecialization(0, MercenarySpecialization.Pioneer),
                new BattlePartySpecialization(1, MercenarySpecialization.SpiritArcher)
            };
            Assert.That(Prepare(party, choices, out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);
            Assert.That(inputs[0].Specialization, Is.EqualTo(MercenarySpecialization.Pioneer));
            Assert.That(inputs[0].Traits.Has(TraitType.SkillMovementRecovery), Is.True);
            Assert.That(inputs[0].Traits.Has(TraitType.FourElementBasicAttack), Is.False);
            Assert.That(inputs[1].Specialization, Is.EqualTo(MercenarySpecialization.SpiritArcher));
            Assert.That(inputs[1].Traits.Has(TraitType.FourElementBasicAttack), Is.True);
            Assert.That(inputs[1].Traits.Has(TraitType.SkillMovementRecovery), Is.False);
        }

        [TestCase(-1, MercenarySpecialization.SpiritArcher, MercenaryClass.Archer)]
        [TestCase(1, MercenarySpecialization.SpiritArcher, MercenaryClass.Archer)]
        [TestCase(0, (MercenarySpecialization)99, MercenaryClass.Archer)]
        [TestCase(0, (MercenarySpecialization)(-1), MercenaryClass.Archer)]
        [TestCase(0, MercenarySpecialization.SpiritArcher, MercenaryClass.Warrior)]
        [TestCase(0, MercenarySpecialization.SpiritArcher, MercenaryClass.Rogue)]
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
                new BattlePartySpecialization(0, MercenarySpecialization.SpiritArcher)
            };
            Assert.That(Prepare(new[] { MercenaryClass.Archer }, choices, out _, out _, out _), Is.False);
        }

        [Test]
        public void InvalidSpecializationOnSkippedPartyMemberStillFails()
        {
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(2, MercenarySpecialization.SpiritArcher) };
            Assert.That(Prepare(new[] { MercenaryClass.Archer, MercenaryClass.Archer, MercenaryClass.Warrior },
                choices, out _, out _, out _), Is.False);
        }

        [Test]
        public void SkippedValidSpecializationDoesNotGrantAnotherParticipantItsTrait()
        {
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(2, MercenarySpecialization.SpiritArcher) };
            Assert.That(Prepare(new[] { MercenaryClass.Archer, MercenaryClass.Archer, MercenaryClass.Archer },
                choices, out BattleSpawnInput[] inputs, out int skipped, out string error), Is.True, error);
            Assert.That(skipped, Is.EqualTo(1));

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.That(inputs[i].Specialization, Is.EqualTo(MercenarySpecialization.None));
                Assert.That(inputs[i].Traits.Has(TraitType.FourElementBasicAttack), Is.False);
            }
        }

        [Test]
        public void PreparedAndRestartInputsKeepSpecializationWhenTheSourceSelectionChanges()
        {
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(0, MercenarySpecialization.SpiritArcher) };
            Assert.That(Prepare(new[] { MercenaryClass.Archer }, choices,
                out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);
            GameObject mapObject = new GameObject("Map");
            _objects.Add(mapObject);
            HexMapView mapView = mapObject.AddComponent<HexMapView>();
            BattleRestartSnapshot snapshot = new BattleRestartSnapshot(mapView, inputs, new BattleRules(), 1, 0);
            BattleSpawnInput spiritArcher = inputs[0];
            choices[0] = new BattlePartySpecialization(0, MercenarySpecialization.None);
            inputs[0] = inputs[1];
            Assert.That(snapshot.Inputs[0], Is.SameAs(spiritArcher));
            Assert.That(snapshot.Inputs[0].Specialization, Is.EqualTo(MercenarySpecialization.SpiritArcher));
            Assert.That(snapshot.Inputs[0].Traits.Has(TraitType.FourElementBasicAttack), Is.True);
        }

        [Test]
        public void TraitValidatorsAcceptFourElementBasicAttackAndRetainDistinctSources()
        {
            SetField(_archer, "_traits", new[] { TraitType.FourElementBasicAttack });
            Assert.That(_archer.Validate(out string error), Is.True, error);
            BattlePartySpecialization[] choices = { new BattlePartySpecialization(0, MercenarySpecialization.SpiritArcher) };
            Assert.That(Prepare(new[] { MercenaryClass.Archer }, choices,
                out BattleSpawnInput[] inputs, out _, out error), Is.True, error);
            Assert.That(inputs[0].Traits.GetCount(TraitType.FourElementBasicAttack), Is.EqualTo(2));
            Assert.That(inputs[0].Traits.Get(TraitType.FourElementBasicAttack, 0).Source.Kind, Is.EqualTo(TraitSourceKind.Class));
            Assert.That(inputs[0].Traits.Get(TraitType.FourElementBasicAttack, 1).Source.Kind, Is.EqualTo(TraitSourceKind.Specialization));
            SetField(_archer, "_traits", new[] { TraitType.FourElementBasicAttack, TraitType.FourElementBasicAttack });
            Assert.That(_archer.Validate(out _), Is.False);
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
