using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.BattleMaps;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleMapPreparationTests
    {
        readonly List<UnityEngine.Object> _objects = new List<UnityEngine.Object>();
        readonly ClassDefinition[] _classes = new ClassDefinition[3];
        BattleMapDefinition _map;
        MercenaryDataList _classData;
        BattleClassViewPrefab[] _prefabs;

        [SetUp]
        public void SetUp()
        {
            _map = CreateAsset<BattleMapDefinition>();
            SetField(_map, "_units", new[] { Unit(3, 3, MercenaryClass.Warrior) });
            SetField(_map, "_startPoints", new[] { Start(0, 0, 10, HexDirection.East) });
            _classData = CreateAsset<MercenaryDataList>();
            _prefabs = new BattleClassViewPrefab[3];

            for (int i = 0; i < _classes.Length; i++)
            {
                _classes[i] = CreateAsset<ClassDefinition>();
                SetField(_classes[i], "_basicAttack", BattleSkillTestAssets.CreateBasicAttack(asset => _objects.Add(asset)));
                SetField(_classes[i], "_displayName", ((MercenaryClass)i).ToString());
                SetNestedField(_classes[i], "_baseStats", "_maxHP", 11 + i);
                SetNestedField(_classes[i], "_baseStats", "_movement", 2 + i);
                SetNestedField(_classes[i], "_baseStats", "_initiative", 9 + i);
                SetNestedField(_classes[i], "_baseStats", "_criticalChance", 0.145f);
                SetNestedField(_classes[i], "_baseStats", "_physicalDefense", 1);
                SetNestedField(_classes[i], "_baseStats", "_waterDefense", 2);
                SetNestedField(_classes[i], "_baseStats", "_fireDefense", 3);
                SetNestedField(_classes[i], "_baseStats", "_lightningDefense", 4);
                SetNestedField(_classes[i], "_baseStats", "_poisonDefense", 5);
                SetNestedField(_classes[i], "_basicAttack", "_damage", 7);
                SetNestedField(_classes[i], "_basicAttack", "_maxRange", 3);
                SetNestedField(_classes[i], "_basicAttack", "_attackAttribute", AttackAttribute.Water);
                GameObject viewObject = new GameObject("Class view " + i);
                _objects.Add(viewObject);
                _prefabs[i] = new BattleClassViewPrefab((MercenaryClass)i,
                    viewObject.AddComponent<HexCharacterView>());
            }

            SetClassMappings(3);
            BattleSkillTestAssets.SetCatalog(_classData, asset => _objects.Add(asset));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void PreparationUsesSortedStartsThenEnemyAssetOrderWithoutChangingTheMap()
        {
            BattleMapStartPoint last = Start(0, 0, 30, HexDirection.West);
            BattleMapStartPoint first = Start(1, 0, 10, HexDirection.NorthEast);
            BattleMapStartPoint middle = Start(2, 0, 20, HexDirection.SouthWest);
            BattleMapStartPoint[] starts = { last, first, middle };
            BattleMapUnitPlacement[] enemies =
            {
                Unit(3, 3, MercenaryClass.Archer),
                Unit(2, 3, MercenaryClass.Warrior)
            };
            SetField(_map, "_startPoints", starts);
            SetField(_map, "_units", enemies);
            MercenaryClass[] party = { MercenaryClass.Rogue, MercenaryClass.Archer, MercenaryClass.Rogue };

            Assert.That(TryPrepare(party, out BattleSpawnInput[] inputs, out int skipped, out string error),
                Is.True, error);

            Assert.That(skipped, Is.Zero);
            Assert.That(inputs.Length, Is.EqualTo(5));
            AssertInput(inputs[0], BattleTeam.Player, MercenaryClass.Rogue, first.Coordinate, first.Facing);
            AssertInput(inputs[1], BattleTeam.Player, MercenaryClass.Archer, middle.Coordinate, middle.Facing);
            AssertInput(inputs[2], BattleTeam.Player, MercenaryClass.Rogue, last.Coordinate, last.Facing);
            AssertInput(inputs[3], BattleTeam.Enemy, enemies[0].UnitClass, enemies[0].Coordinate, enemies[0].Facing);
            AssertInput(inputs[4], BattleTeam.Enemy, enemies[1].UnitClass, enemies[1].Coordinate, enemies[1].Facing);
            Assert.That(_map.StartPoints[0], Is.SameAs(last));
            Assert.That(_map.StartPoints[1], Is.SameAs(first));
            Assert.That(_map.StartPoints[2], Is.SameAs(middle));
            Assert.That(_map.Units[0], Is.SameAs(enemies[0]));
            Assert.That(_map.Units[1], Is.SameAs(enemies[1]));
            Assert.That(party, Is.EqualTo(new[] { MercenaryClass.Rogue, MercenaryClass.Archer, MercenaryClass.Rogue }));
            Assert.That(_prefabs[0].Prefab.IsInitialized, Is.False);
        }

        [Test]
        public void StartShortageSkipsTheTailAndDoesNotRequireItsPrefabs()
        {
            _prefabs = new[] { _prefabs[0], _prefabs[1] };
            MercenaryClass[] party = { MercenaryClass.Rogue, MercenaryClass.Archer, MercenaryClass.Archer };

            Assert.That(TryPrepare(party, out BattleSpawnInput[] inputs, out int skipped, out string error),
                Is.True, error);

            Assert.That(inputs.Length, Is.EqualTo(2));
            Assert.That(inputs[0].Stats.UnitClass, Is.EqualTo(MercenaryClass.Rogue));
            Assert.That(skipped, Is.EqualTo(2));
        }

        [Test]
        public void PartyLimitAppliesBeforeStartPointTruncation()
        {
            MercenaryClass[] party =
            {
                MercenaryClass.Warrior, MercenaryClass.Warrior, MercenaryClass.Warrior,
                MercenaryClass.Warrior, MercenaryClass.Warrior
            };

            AssertFailure(party);
        }

        [Test]
        public void MissingEitherTeamIsRejected()
        {
            AssertFailure(Array.Empty<MercenaryClass>());
            SetField(_map, "_startPoints", Array.Empty<BattleMapStartPoint>());
            AssertFailure(new[] { MercenaryClass.Warrior });
            SetField(_map, "_startPoints", new[] { Start(0, 0, 0, HexDirection.East) });
            SetField(_map, "_units", Array.Empty<BattleMapUnitPlacement>());
            AssertFailure(new[] { MercenaryClass.Warrior });
        }

        [Test]
        public void PreparationReusesMapValidationForBoundsOverlapAndDuplicateOrder()
        {
            SetField(_map, "_units", new[] { Unit(4, 0, MercenaryClass.Warrior) });
            AssertFailure(new[] { MercenaryClass.Warrior });
            SetField(_map, "_units", new[] { Unit(0, 0, MercenaryClass.Warrior) });
            AssertFailure(new[] { MercenaryClass.Warrior });
            SetField(_map, "_units", new[] { Unit(3, 3, MercenaryClass.Warrior) });
            SetField(_map, "_startPoints", new[]
            {
                Start(0, 0, 0, HexDirection.East),
                Start(1, 0, 0, HexDirection.East)
            });
            AssertFailure(new[] { MercenaryClass.Warrior });
        }

        [Test]
        public void InvalidSkippedPartyClassesAndPlayerFixedUnitsAreRejected()
        {
            AssertFailure(new[] { MercenaryClass.Warrior, (MercenaryClass)99 });
            SetField(_map, "_units", new[]
            {
                new BattleMapUnitPlacement(new HexCoordinate(3, 3), MercenaryClass.Warrior,
                    BattleTeam.Player, HexDirection.East)
            });
            AssertFailure(new[] { MercenaryClass.Warrior });
        }

        [Test]
        public void RequiredMissingOrNullPrefabsAndDuplicateMappingsAreRejected()
        {
            BattleClassViewPrefab warrior = _prefabs[0];
            _prefabs = new[] { warrior };
            AssertFailure(new[] { MercenaryClass.Rogue });
            _prefabs = new[] { warrior, new BattleClassViewPrefab(MercenaryClass.Rogue, null) };
            AssertFailure(new[] { MercenaryClass.Rogue });
            _prefabs = new[] { warrior, warrior };
            AssertFailure(new[] { MercenaryClass.Warrior });
        }

        [Test]
        public void MissingClassDefinitionsAreRejectedWithoutFallbackStats()
        {
            SetClassMappings(2);
            AssertFailure(new[] { MercenaryClass.Archer });
        }

        [Test]
        public void PreparationCopiesEveryBaseStatAndAttackSettingIntoAnIndependentSnapshot()
        {
            Assert.That(TryPrepare(new[] { MercenaryClass.Rogue }, out BattleSpawnInput[] inputs,
                out _, out string error), Is.True, error);

            BattleUnitStats stats = inputs[0].Stats;
            Assert.That(stats.MaxHealth, Is.EqualTo(12));
            Assert.That(stats.MovementPerTurn, Is.EqualTo(3));
            Assert.That(stats.Initiative, Is.EqualTo(10));
            Assert.That(stats.CriticalChance, Is.EqualTo(15));
            Assert.That(stats.PhysicalDefense, Is.EqualTo(1));
            Assert.That(stats.WaterDefense, Is.EqualTo(2));
            Assert.That(stats.FireDefense, Is.EqualTo(3));
            Assert.That(stats.ElectricityDefense, Is.EqualTo(4));
            Assert.That(stats.PoisonDefense, Is.EqualTo(5));
            Assert.That(inputs[0].Skills.BasicAttack.Damage, Is.EqualTo(7));
            Assert.That(inputs[0].Skills.BasicAttack.MaxRange, Is.EqualTo(3));
            Assert.That(inputs[0].Skills.BasicAttack.AttackAttribute, Is.EqualTo(AttackAttribute.Water));
            Assert.That(inputs[0].Prefab, Is.SameAs(_prefabs[1].Prefab));
            Assert.That(_classes[1].BaseStats.CriticalChance, Is.EqualTo(0.145f));

            SetNestedField(_classes[1], "_baseStats", "_maxHP", 90);
            SetNestedField(_classes[1], "_baseStats", "_initiative", 100);
            Assert.That(stats.MaxHealth, Is.EqualTo(12));
            Assert.That(stats.Initiative, Is.EqualTo(10));
        }

        [TestCase(0f, 0)]
        [TestCase(0.004f, 0)]
        [TestCase(0.005f, 1)]
        [TestCase(0.00499f, 0)]
        [TestCase(0.00501f, 1)]
        [TestCase(0.144f, 14)]
        [TestCase(0.145f, 15)]
        [TestCase(0.14499f, 14)]
        [TestCase(0.14501f, 15)]
        [TestCase(1f, 100)]
        public void CriticalChanceUsesHalfUpPercentageRounding(float chance, int expected)
        {
            SetNestedField(_classes[0], "_baseStats", "_criticalChance", chance);

            Assert.That(TryPrepare(new[] { MercenaryClass.Warrior }, out BattleSpawnInput[] inputs,
                out _, out string error), Is.True, error);
            Assert.That(inputs[0].Stats.CriticalChance, Is.EqualTo(expected));
            Assert.That(_classes[0].BaseStats.CriticalChance, Is.EqualTo(chance));
        }

        [Test]
        public void PreparationCopiesGrantedTraitsAndSettingsWithoutRetainingAuthoringData()
        {
            TraitType[] types =
            {
                TraitType.RearGuaranteedCritical,
                TraitType.DirectionalPhysicalNegation,
                TraitType.OpportunityAttack
            };
            SetField(_classes[1], "_traits", types);
            TraitSettings settings = CreateAsset<TraitSettings>();
            SetNestedField(settings, "_directionalPhysicalNegation", "_frontChance", 0.875f);
            SetNestedField(settings, "_directionalPhysicalNegation", "_sideChance", 0.125f);
            SetNestedField(settings, "_directionalPhysicalNegation", "_rearChance", 0.005f);
            SetField(_classData, "_traitSettings", settings);

            Assert.That(TryPrepare(new[] { MercenaryClass.Rogue }, out BattleSpawnInput[] inputs,
                out _, out string error), Is.True, error);

            TraitCollection traits = inputs[0].Traits;
            Assert.That(traits.Has(TraitType.RearGuaranteedCritical), Is.True);
            Assert.That(traits.Has(TraitType.DirectionalPhysicalNegation), Is.True);
            Assert.That(traits.Has(TraitType.OpportunityAttack), Is.True);
            Assert.That(traits.Get(TraitType.OpportunityAttack, 0).Source,
                Is.EqualTo(new TraitSource(TraitSourceKind.Class, (int)MercenaryClass.Rogue)));
            Trait grant = traits.Get(TraitType.DirectionalPhysicalNegation, 0);
            Assert.That(grant.Source.Kind, Is.EqualTo(TraitSourceKind.Class));
            Assert.That(grant.Source.Id, Is.EqualTo((int)MercenaryClass.Rogue));
            Assert.That(inputs[1].Traits.Has(TraitType.DirectionalPhysicalNegation), Is.False);

            types[0] = TraitType.DirectionalPhysicalNegation;
            SetField(_classes[1], "_traits", Array.Empty<TraitType>());
            SetNestedField(settings, "_directionalPhysicalNegation", "_frontChance", 0f);
            SetNestedField(settings, "_directionalPhysicalNegation", "_sideChance", 1f);
            SetNestedField(settings, "_directionalPhysicalNegation", "_rearChance", 1f);

            Assert.That(traits.GetCount(TraitType.RearGuaranteedCritical), Is.EqualTo(1));
            Assert.That(traits.GetCount(TraitType.DirectionalPhysicalNegation), Is.EqualTo(1));
            Assert.That(traits.GetCount(TraitType.OpportunityAttack), Is.EqualTo(1));
            Assert.That(inputs[0].TraitSettings.FrontNegationChance, Is.EqualTo(0.875f));
            Assert.That(inputs[0].TraitSettings.SideNegationChance, Is.EqualTo(0.125f));
            Assert.That(inputs[0].TraitSettings.RearNegationChance, Is.EqualTo(0.005f));
        }

        [Test]
        public void PreparationUsesTraitsFromDefinitionsInsteadOfImplicitClassAbilities()
        {
            SetField(_classes[2], "_traits", new[] { TraitType.RearGuaranteedCritical });
            SetField(_map, "_startPoints", new[]
            {
                Start(0, 0, 0, HexDirection.East),
                Start(1, 0, 1, HexDirection.East)
            });

            Assert.That(TryPrepare(new[] { MercenaryClass.Rogue, MercenaryClass.Archer },
                out BattleSpawnInput[] inputs, out _, out string error), Is.True, error);

            Assert.That(inputs[0].Traits.Has(TraitType.RearGuaranteedCritical), Is.False);
            Assert.That(inputs[1].Traits.Has(TraitType.RearGuaranteedCritical), Is.True);
            Assert.That(inputs[2].Traits.Has(TraitType.DirectionalPhysicalNegation), Is.False);
        }

        [Test]
        public void DirectionalNegationRequiresValidSettingsBeforePreparingAnyParticipants()
        {
            SetField(_classes[0], "_traits", new[] { TraitType.DirectionalPhysicalNegation });
            AssertFailure(new[] { MercenaryClass.Warrior });

            TraitSettings settings = CreateAsset<TraitSettings>();
            SetNestedField(settings, "_directionalPhysicalNegation", "_frontChance", float.NaN);
            SetField(_classData, "_traitSettings", settings);
            AssertFailure(new[] { MercenaryClass.Warrior });
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidCriticalChanceFailsBeforeConversion(float chance)
        {
            SetNestedField(_classes[0], "_baseStats", "_criticalChance", chance);
            AssertFailure(new[] { MercenaryClass.Warrior });
        }

        [Test]
        public void PreflightChecksConfigurationWithoutNeedingLoadedMapDimensions()
        {
            Assert.That(BattleMapPreparation.TryValidateConfiguration(_map,
                new[] { MercenaryClass.Warrior }, _classData, _prefabs, 4, out string error), Is.True, error);
            _prefabs = Array.Empty<BattleClassViewPrefab>();
            Assert.That(BattleMapPreparation.TryValidateConfiguration(_map,
                new[] { MercenaryClass.Warrior }, _classData, _prefabs, 4, out error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void EquipmentCaptureFreezesDefinitionsAndPreparationDoesNotModifyBaseStats()
        {
            EquipmentDefinition armor = CreateAsset<EquipmentDefinition>();
            SetField(armor, "_type", EquipmentType.Armor);
            SetField(armor, "_displayName", "Armor");
            SetField(armor, "_maxHealthBonus", 2);
            EquipmentDefinition shield = CreateAsset<EquipmentDefinition>();
            SetField(shield, "_type", EquipmentType.Shield);
            SetField(shield, "_displayName", "Shield");
            SetField(shield, "_physicalDefenseBonus", 1);
            EquipmentDefinition[] equipped = { armor, armor, shield };
            Assert.That(BattlePartyEquipment.TryCapture(1, new[] { new BattlePartyEquipment(0, equipped) },
                out BattleEquipmentLoadout[] snapshot, out string error), Is.True, error);
            SetField(armor, "_maxHealthBonus", 100);
            equipped[0] = null;
            Assert.That(BattleMapPreparation.TryPrepare(_map, new[] { MercenaryClass.Warrior }, _classData,
                _prefabs, 4, 4, 4, out BattleSpawnInput[] inputs, out _, out error, equipment: snapshot), Is.True, error);
            Assert.That(inputs[0].Stats.MaxHealth, Is.EqualTo(15));
            Assert.That(inputs[0].Stats.PhysicalDefense, Is.EqualTo(2));
            Assert.That(inputs[0].Equipment.Items.Count, Is.EqualTo(3));
            Assert.That(inputs[1].Stats.MaxHealth, Is.EqualTo(11));
            Assert.That(inputs[1].Equipment.Items, Is.Empty);
            Assert.That(_classes[0].BaseStats.MaxHP, Is.EqualTo(11));
            Assert.That(_classes[0].BaseStats.PhysicalDefense, Is.EqualTo(1));
            Assert.That(BattleMapPreparation.TryPrepare(_map, new[] { MercenaryClass.Warrior }, _classData,
                _prefabs, 4, 4, 4, out BattleSpawnInput[] repeated, out _, out error, equipment: snapshot), Is.True, error);
            Assert.That(repeated[0].Stats.MaxHealth, Is.EqualTo(15));
        }

        [Test]
        public void InvalidEquipmentAssignmentsAreRejectedBeforeMapLoad()
        {
            EquipmentDefinition sword = CreateAsset<EquipmentDefinition>();
            SetField(sword, "_displayName", "Sword");
            SetField(sword, "_physicalAttackBonus", 1);
            Assert.That(BattlePartyEquipment.TryCapture(1,
                new[] { new BattlePartyEquipment(0, new[] { sword, sword, sword, sword }) }, out _, out _), Is.False);
            Assert.That(BattlePartyEquipment.TryCapture(1,
                new[] { new BattlePartyEquipment(0, new EquipmentDefinition[] { null }) }, out _, out _), Is.False);
            Assert.That(BattlePartyEquipment.TryCapture(1,
                new[] { new BattlePartyEquipment(1, new[] { sword }) }, out _, out _), Is.False);
            Assert.That(BattlePartyEquipment.TryCapture(1,
                new[] { new BattlePartyEquipment(0, new[] { sword }), new BattlePartyEquipment(0, Array.Empty<EquipmentDefinition>()) }, out _, out _), Is.False);
            SetField(sword, "_physicalAttackBonus", -1);
            Assert.That(BattlePartyEquipment.TryCapture(1,
                new[] { new BattlePartyEquipment(0, new[] { sword }) }, out _, out _), Is.False);
            SetField(sword, "_physicalAttackBonus", int.MaxValue);
            Assert.That(BattlePartyEquipment.TryCapture(1,
                new[] { new BattlePartyEquipment(0, new[] { sword, sword }) }, out _, out _), Is.False);
            BattleEquipmentLoadout[] excessiveHealth =
            {
                new BattleEquipmentLoadout(new[] { new BattleEquipmentItem(EquipmentType.Armor, maxHealthBonus: int.MaxValue) })
            };
            Assert.That(BattleMapPreparation.TryValidateEquipment(new[] { MercenaryClass.Warrior },
                _classData, excessiveHealth, out _), Is.False);
        }

        [Test]
        public void EmptyEquipmentAssignmentsProduceEmptyPerUnitSnapshots()
        {
            Assert.That(BattlePartyEquipment.TryCapture(2, Array.Empty<BattlePartyEquipment>(),
                out BattleEquipmentLoadout[] equipment, out string error), Is.True, error);
            Assert.That(equipment.Length, Is.EqualTo(2));
            Assert.That(equipment[0].Items, Is.Empty);
            Assert.That(equipment[1].Items, Is.Empty);
        }

        void AssertFailure(IReadOnlyList<MercenaryClass> party)
        {
            Assert.That(TryPrepare(party, out BattleSpawnInput[] inputs, out int skipped, out string error), Is.False);
            Assert.That(inputs, Is.Empty);
            Assert.That(skipped, Is.Zero);
            Assert.That(error, Is.Not.Empty);
        }

        bool TryPrepare(IReadOnlyList<MercenaryClass> party, out BattleSpawnInput[] inputs,
            out int skipped, out string error)
        {
            return BattleMapPreparation.TryPrepare(_map, party, _classData, _prefabs, 4, 4, 4,
                out inputs, out skipped, out error);
        }

        void SetClassMappings(int count)
        {
            SerializedObject serialized = new SerializedObject(_classData);
            SerializedProperty entries = serialized.FindProperty("_classes").FindPropertyRelative("_entries");
            entries.arraySize = count;

            for (int i = 0; i < count; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").intValue = i;
                entry.FindPropertyRelative("_value").objectReferenceValue = _classes[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        static void AssertInput(BattleSpawnInput input, BattleTeam team, MercenaryClass unitClass,
            HexCoordinate coordinate, HexDirection facing)
        {
            Assert.That(input.Team, Is.EqualTo(team));
            Assert.That(input.Stats.UnitClass, Is.EqualTo(unitClass));
            Assert.That(input.Coordinate, Is.EqualTo(coordinate));
            Assert.That(input.Facing, Is.EqualTo(facing));
        }

        static BattleMapUnitPlacement Unit(int q, int r, MercenaryClass unitClass)
        {
            return new BattleMapUnitPlacement(new HexCoordinate(q, r), unitClass,
                BattleTeam.Enemy, HexDirection.NorthWest);
        }

        static BattleMapStartPoint Start(int q, int r, int order, HexDirection facing)
        {
            return new BattleMapStartPoint(new HexCoordinate(q, r), order, facing);
        }

        static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        static void SetNestedField(object target, string group, string name, object value)
        {
            FieldInfo groupField = target.GetType().GetField(group, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(groupField, Is.Not.Null, group);
            object nested = groupField.GetValue(target);
            SetField(nested, name, value);
            groupField.SetValue(target, nested);
        }
    }
}
