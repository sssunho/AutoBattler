using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.TestTools;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRestartTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        BattleStartController _controller;
        BattleRunner _runner;
        HexMapView _map;
        HexCharacterView _prefab;
        BattleInputController _input;
        GameObject _hud;
        Camera _camera;
        BattleSpawnInput[] _inputs;
        BattleRules _rules;
        BattleRestartSnapshot _snapshot;
        int _firstUnitId;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _prefab = BattleTestViews.CreateCharacterView("Template", _objects, _animations);
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(_runner, "_initializationMode", BattleInitializationMode.External);
            BattleTestUnit stats = new BattleTestUnit(MercenaryClass.Warrior, 12, 30, 3, 3, 0, initiative: 17);
            _inputs = new[]
            {
                BattleTestUnitExtensions.CreateSpawnInput(_prefab, BattleTeam.Player, stats, new HexCoordinate(1, 1), HexDirection.East),
                BattleTestUnitExtensions.CreateSpawnInput(_prefab, BattleTeam.Player, stats, new HexCoordinate(0, 2), HexDirection.SouthEast),
                BattleTestUnitExtensions.CreateSpawnInput(_prefab, BattleTeam.Enemy, stats, new HexCoordinate(2, 1), HexDirection.West)
            };
            _rules = new BattleRules(4, 5, 9, 3);
            Assert.That(_runner.TryInitializeBattle(_map, _inputs, _rules, 719, out string error), Is.True, error);
            _firstUnitId = _runner.CurrentUnitId.Value;
            _snapshot = new BattleRestartSnapshot(_map, _inputs, _rules, 719, 1);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            _hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
            _objects.Add(_hud);
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 4.75f;
            _camera.transform.SetPositionAndRotation(new Vector3(8f, 14f, -7f), Quaternion.Euler(42f, 20f, 0f));
            _controller = CreateObject("Controller").AddComponent<BattleStartController>();
            SetField(_controller, "_runner", _runner);
            SetField(_controller, "_input", _input);
            SetField(_controller, "_battleHud", _hud);
            SetField(_controller, "_battleCamera", _camera);
            SetField(_controller, "_snapshot", _snapshot);
            SetField(_controller, "_skippedPlayerCount", 1);
            SetField(_controller, "_phase", BattleStartPhase.Running);
            SetField(_controller, "_ownsBattle", true);
            Invoke(_controller, "BindRunner");
        }

        [TearDown]
        public void TearDown()
        {
            BattleStartupCancellationProbe.Target = null;

            if (_controller != null)
            {
                Invoke(_controller, "Shutdown");
            }

            if (_runner != null)
            {
                _runner.ShutdownBattle();
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            BattleTestViews.DisposeFixtures(_animations);
        }

        [Test]
        public void ResultKeepsTheWorldVisibleAndBlocksOnlyBattleInput()
        {
            Assert.That(_runner.RemoveCharacter(3), Is.True);
            Invoke(_runner, "Update");
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Result));
            Assert.That(_controller.Result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(_controller.Result.InitialPlayerCount, Is.EqualTo(2));
            Assert.That(_controller.SkippedPlayerCount, Is.EqualTo(1));
            Assert.That(_input.enabled, Is.False);
            Assert.That(_hud.activeSelf, Is.True);
            Assert.That(_camera.isActiveAndEnabled, Is.True);
            Assert.That(_controller.CanRestart, Is.True);
        }

        [Test]
        public void RestartWaitsForNextFrameAndRestoresSnapshotRulesStatsSeedAndCamera()
        {
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            SetField(original, "_health", 1);
            SetField(_runner, "_maxPlayerUnits", 1);
            SetField(_runner, "_startingActionPoints", 0);
            SetField(_controller, "_randomSeed", 10001);
            SetField(_controller, "_party", new[] { MercenaryClass.Archer });
            _inputs[0] = BattleTestUnitExtensions.CreateSpawnInput(_prefab, BattleTeam.Enemy,
                BattleTestUnit.ForClass(MercenaryClass.Archer), new HexCoordinate(7, 7), HexDirection.West);
            Vector3 position = _camera.transform.position;
            Quaternion rotation = _camera.transform.rotation;
            float zoom = _camera.orthographicSize;

            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Restarting));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.transform.childCount, Is.Zero);
            Assert.That(_input.enabled, Is.False);
            Assert.That(_controller.TryRequestRestart(out _), Is.False);
            Invoke(_controller, "Update");
            Assert.That(_runner.IsInitialized, Is.False);

            AdvanceRestart();

            Assert.That(_controller.IsRunning, Is.True, _controller.Status);
            Assert.That(_runner.Rules, Is.SameAs(_rules));
            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_firstUnitId));
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter replacement), Is.True);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Health, Is.EqualTo(12));
            Assert.That(replacement.ActionPoints, Is.EqualTo(5));
            Assert.That(replacement.Coordinate, Is.EqualTo(new HexCoordinate(1, 1)));
            Assert.That(replacement.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(replacement.Stats, Is.SameAs(original.Stats));
            Assert.That(replacement.Skills, Is.SameAs(original.Skills));
            Assert.That(replacement.Skills.BasicAttack.MaxRange, Is.EqualTo(3));
            Assert.That(replacement.Skills.BasicAttack.Damage, Is.EqualTo(30));
            Assert.That(replacement.Skills.GetSlot(SkillType.Stealth), Is.EqualTo(2));
            Assert.That(replacement.Skills.TryGetSlot(2, out BattleSkillDefinition restartedSkill), Is.True);
            Assert.That(restartedSkill.Type, Is.EqualTo(SkillType.Stealth));
            Assert.That(replacement.Stats.Initiative, Is.EqualTo(17));
            Assert.That(_camera.transform.position, Is.EqualTo(position));
            Assert.That(_camera.transform.rotation, Is.EqualTo(rotation));
            Assert.That(_camera.orthographicSize, Is.EqualTo(zoom));
            Assert.That(_map.isActiveAndEnabled, Is.True);
        }

        [Test]
        public void RestartPreservesTraitSourcesAndSettingsWhileReplacingTheBattleCharacter()
        {
            Trait[] grants =
            {
                new Trait(TraitType.RearGuaranteedCritical, new TraitSource(TraitSourceKind.Selection, 7)),
                new Trait(TraitType.DirectionalPhysicalNegation, new TraitSource(TraitSourceKind.Specialization, 2)),
                new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Equipment, 4))
            };
            TraitCollection traits = new TraitCollection(grants);
            BattleTraitSettings settings = new BattleTraitSettings(0.8f, 0.25f, 0.005f);
            BattleSpawnInput previous = _inputs[0];
            _inputs[0] = BattleTestUnitExtensions.CreateSpawnInput(previous.Prefab, previous.Team, previous.Stats,
                previous.Coordinate, previous.Facing, traits, settings, skills: previous.Skills);
            _runner.ShutdownBattle();
            Assert.That(_runner.TryInitializeBattle(_map, _inputs, _rules, 719, out string error), Is.True, error);
            _snapshot = new BattleRestartSnapshot(_map, _inputs, _rules, 719, 1);
            SetField(_controller, "_snapshot", _snapshot);
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            SetField(original, "_health", 1);
            grants[0] = default;
            grants[1] = default;
            grants[2] = default;
            _inputs[0] = previous;

            Assert.That(_controller.TryRequestRestart(out error), Is.True, error);
            AdvanceRestart();

            Assert.That(_runner.TryGetCharacter(1, out HexCharacter replacement), Is.True);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Health, Is.EqualTo(replacement.MaxHealth));
            Assert.That(replacement.Traits.Has(TraitType.RearGuaranteedCritical), Is.True);
            Assert.That(replacement.Traits.Get(TraitType.RearGuaranteedCritical, 0).Source,
                Is.EqualTo(new TraitSource(TraitSourceKind.Selection, 7)));
            Assert.That(replacement.Traits.Get(TraitType.DirectionalPhysicalNegation, 0).Source,
                Is.EqualTo(new TraitSource(TraitSourceKind.Specialization, 2)));
            Assert.That(replacement.Traits.Get(TraitType.OpportunityAttack, 0).Source,
                Is.EqualTo(new TraitSource(TraitSourceKind.Equipment, 4)));
            Assert.That(replacement.TraitSettings.FrontNegationChance, Is.EqualTo(0.8f));
            Assert.That(replacement.TraitSettings.SideNegationChance, Is.EqualTo(0.25f));
            Assert.That(replacement.TraitSettings.RearNegationChance, Is.EqualTo(0.005f));
        }

        [Test]
        public void RestartPreservesEquipmentWithoutApplyingStatBonusesAgain()
        {
            BattleEquipmentItem[] items =
            {
                new BattleEquipmentItem(EquipmentType.Armor, maxHealthBonus: 2),
                new BattleEquipmentItem(EquipmentType.Armor, maxHealthBonus: 2),
                new BattleEquipmentItem(EquipmentType.Shield, physicalDefenseBonus: 1)
            };
            BattleEquipmentLoadout equipment = new BattleEquipmentLoadout(items);
            BattleSpawnInput previous = _inputs[0];
            BattleUnitStats adjusted = equipment.ApplyTo(previous.Stats);
            _inputs[0] = new BattleSpawnInput(previous.Prefab, previous.Team, adjusted,
                previous.Coordinate, previous.Facing, previous.Traits, previous.TraitSettings,
                previous.Specialization, previous.Skills, equipment);
            _runner.ShutdownBattle();
            Assert.That(_runner.TryInitializeBattle(_map, _inputs, _rules, 719, out string error), Is.True, error);
            _snapshot = new BattleRestartSnapshot(_map, _inputs, _rules, 719, 1);
            SetField(_controller, "_snapshot", _snapshot);
            _inputs[0] = previous;
            items[0] = default;

            for (int i = 0; i < 2; i++)
            {
                Assert.That(_runner.TryGetCharacter(1, out HexCharacter original), Is.True);
                SetField(original, "_health", 1);
                Assert.That(_controller.TryRequestRestart(out error), Is.True, error);
                AdvanceRestart();
                Assert.That(_runner.TryGetCharacter(1, out HexCharacter replacement), Is.True);
                Assert.That(replacement, Is.Not.SameAs(original));
                Assert.That(replacement.Health, Is.EqualTo(previous.Stats.MaxHealth + 4));
                Assert.That(replacement.Stats.PhysicalDefense, Is.EqualTo(previous.Stats.PhysicalDefense + 1));
                Assert.That(replacement.Equipment.Items.Count, Is.EqualTo(3));
                Assert.That(replacement.Equipment.Items[0].MaxHealthBonus, Is.EqualTo(2));
            }
        }

        [TestCase("position")]
        [TestCase("rotation")]
        [TestCase("scale")]
        [TestCase("width")]
        [TestCase("hex")]
        public void ChangedMapRejectsTheRequestBeforeDestroyingTheCurrentAttempt(string change)
        {
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter original), Is.True);

            switch (change)
            {
                case "position":
                    _map.transform.position += Vector3.right;
                    break;
                case "rotation":
                    _map.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                    break;
                case "scale":
                    _map.transform.localScale = Vector3.one * 2f;
                    break;
                case "width":
                    SetField(_map, "_width", _map.Width + 1);
                    break;
                default:
                    SetField(_map, "_hexSize", _map.HexSize + 0.25f);
                    break;
            }

            Assert.That(_controller.TryRequestRestart(out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Running));
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(original));
            Assert.That(_runner.transform.childCount, Is.EqualTo(3));
        }

        [Test]
        public void InvalidPrefabRequestPreservesCompletedResultAndCanBeRetriedAfterCorrection()
        {
            Assert.That(_runner.RemoveCharacter(3), Is.True);
            Invoke(_runner, "Update");
            BattleResult result = _controller.Result;
            _prefab.enabled = false;
            Assert.That(_controller.TryRequestRestart(out _), Is.False);
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Result));
            Assert.That(_controller.Result, Is.SameAs(result));
            Assert.That(_runner.Result, Is.SameAs(result));
            _prefab.enabled = true;
            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            AdvanceRestart();
            Assert.That(_controller.IsRunning, Is.True, _controller.Status);
            Invoke(_controller, "OnBattleCompleted", result);
            Assert.That(_controller.Result, Is.Null);
            Assert.That(_controller.IsRunning, Is.True);
        }

        [Test]
        public void FailureAfterOldAttemptWasDiscardedLeavesAnErrorAndNoPartialBattle()
        {
            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            SetField(_prefab, "_animationProfile", null);
            LogAssert.Expect(LogType.Error, "HexCharacterView requires a model, Animator, animation profile, and a positive finite move speed.");
            AdvanceRestart();
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Error));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.transform.childCount, Is.Zero);
            Assert.That(_controller.Result, Is.Null);
            Assert.That(_controller.CanRestart, Is.False);
            Assert.That(_input.enabled, Is.False);
            Assert.That(_hud.activeSelf, Is.True);
            Assert.That(_map.isActiveAndEnabled, Is.True);
        }

        [Test]
        public void PrefabLossBetweenCleanupAndNextFrameFailsWithoutLeavingParticipants()
        {
            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            Object.DestroyImmediate(_prefab.gameObject);
            AdvanceRestart();
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Error));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.transform.childCount, Is.Zero);
            Assert.That(_map.isActiveAndEnabled, Is.True);
        }

        [Test]
        public void ShutdownDuringThePendingFrameCancelsRecreation()
        {
            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            Invoke(_controller, "Shutdown");
            AdvanceRestart();
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Closing));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.transform.childCount, Is.Zero);
            Assert.That(_hud.activeSelf, Is.False);
        }

        [Test]
        public void DisableDuringNewParticipantCreationDoesNotReviveTheAttempt()
        {
            _prefab.gameObject.AddComponent<BattleStartupCancellationProbe>();
            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            BattleStartupCancellationProbe.Target = _controller;
            AdvanceRestart();
            Assert.That(BattleStartupCancellationProbe.Target, Is.Null);
            Assert.That(_controller.enabled, Is.False);
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.transform.childCount, Is.Zero);
            Invoke(_controller, "OnDisable");
            Assert.That(_controller.Phase, Is.EqualTo(BattleStartPhase.Closing));
            Assert.That(_input.enabled, Is.False);
            Assert.That(_hud.activeSelf, Is.False);
        }

        [Test]
        public void RestartBeforeLethalAttackHitPreservesHealthAndDoesNotPublishTheAbandonedOutcome()
        {
            BattleSimulation simulation = (BattleSimulation)GetField(_runner, "_simulation");
            Assert.That(_runner.TryGetCharacter(simulation.CurrentUnitId.Value, out HexCharacter attacker), Is.True);

            if (attacker.Team != BattleTeam.Player)
            {
                Assert.That(simulation.TryEndTurn(attacker.Id), Is.True);
                Assert.That(_runner.TryGetCharacter(simulation.CurrentUnitId.Value, out attacker), Is.True);
            }

            Assert.That(attacker.Team, Is.EqualTo(BattleTeam.Player));
            Assert.That(_runner.TryGetCharacter(3, out HexCharacter previousEnemy), Is.True);
            SetField(previousEnemy, "_facing", HexDirection.East);
            Assert.That(BattleFacing.Classify(previousEnemy.Facing, previousEnemy.Coordinate, attacker.Coordinate),
                Is.EqualTo(BattleRelativeDirection.Rear));
            int healthBeforeRestart = previousEnemy.Health;
            Assert.That(simulation.TryAttack(attacker.Id, 3), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            int outcomes = 0;
            _runner.BattleCompleted += result => outcomes++;
            Assert.That(_controller.TryRequestRestart(out string error), Is.True, error);
            Assert.That(previousEnemy.Health, Is.EqualTo(healthBeforeRestart));
            Assert.That(simulation.Result, Is.Null);
            Assert.That(outcomes, Is.Zero);
            AdvanceRestart();
            Assert.That(outcomes, Is.Zero);
            Assert.That(_controller.Result, Is.Null);
            Assert.That(_runner.TryGetCharacter(3, out HexCharacter enemy), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(enemy.MaxHealth));
        }

        [Test]
        public void RestartRestoresSpiritArcherTraitAndItsCompositeAttack()
        {
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.FourElementBasicAttack,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.SpiritArcher))
            });
            BattleTestUnit stats = new BattleTestUnit(MercenaryClass.Archer, 12, 30, 3, 8, 0, initiative: 100);
            _inputs[0] = BattleTestUnitExtensions.CreateSpawnInput(_prefab, BattleTeam.Player, stats,
                new HexCoordinate(1, 1), HexDirection.East, traits,
                specialization: MercenarySpecialization.SpiritArcher);
            _runner.ShutdownBattle();
            Assert.That(_runner.TryInitializeBattle(_map, _inputs, _rules, 719, out string error), Is.True, error);
            _snapshot = new BattleRestartSnapshot(_map, _inputs, _rules, 719, 1);
            SetField(_controller, "_snapshot", _snapshot);
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            SetField(original, "_health", 1);
            _inputs[0] = BattleTestUnitExtensions.CreateSpawnInput(_prefab, BattleTeam.Player, stats,
                new HexCoordinate(7, 7), HexDirection.West);

            Assert.That(_controller.TryRequestRestart(out error), Is.True, error);
            AdvanceRestart();

            Assert.That(_controller.IsRunning, Is.True, _controller.Status);
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter restored), Is.True);
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.Specialization, Is.EqualTo(MercenarySpecialization.SpiritArcher));
            Assert.That(restored.Traits.Has(TraitType.FourElementBasicAttack), Is.True);
            Assert.That(restored.Health, Is.EqualTo(12));
            Assert.That(restored.Coordinate, Is.EqualTo(new HexCoordinate(1, 1)));
            BattleSimulation simulation = (BattleSimulation)GetField(_runner, "_simulation");
            Assert.That(simulation.TryAttack(restored.Id, 3), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(4));
            Assert.That(result.DamageByAttribute.Water, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Fire, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Electricity, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Poison, Is.EqualTo(1));
        }

        [Test]
        public void RestartClearsRuntimeEnvironment()
        {
            HexCoordinate coordinate = new HexCoordinate(0, 0);
            Assert.That(_runner.TryApplyGroundEffect(coordinate, BattleGroundEffect.Wet, out string error), Is.True, error);
            Assert.That(_runner.TryApplyAirEffect(coordinate, BattleAirEffect.Smoke, out error), Is.True, error);
            Assert.That(_runner.TryGetEnvironmentContext(out BattleSimulation before, out _), Is.True);
            Assert.That(_controller.TryRequestRestart(out error), Is.True, error);
            AdvanceRestart();
            Assert.That(_controller.IsRunning, Is.True, _controller.Status);
            Assert.That(_runner.TryGetEnvironmentContext(out BattleSimulation after, out _), Is.True);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Board.TryGetEnvironment(coordinate, out BattleEnvironmentState state), Is.True);
            Assert.That(state.IsEmpty, Is.True);
            Assert.That(after.Board.EnvironmentVersion, Is.Zero);
        }

        void AdvanceRestart()
        {
            SetField(_controller, "_restartRequestedFrame", Time.frameCount - 1);
            Invoke(_controller, "Update");
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Invoke(object target, string name, params object[] arguments)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }
    }
}
