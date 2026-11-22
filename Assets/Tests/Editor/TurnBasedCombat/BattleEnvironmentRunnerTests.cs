using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleEnvironmentRunnerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        BattleRunner _runner;
        HexMapView _map;
        BattleSimulation _simulation;
        HexCharacter _player;
        HexCharacter _enemy;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = CreateBattle();
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
        }

        [TearDown]
        public void TearDown()
        {
            _runner.ShutdownBattle();

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
        public void EnvironmentReadRemainsAvailableDuringActionsAndEnemyTurns()
        {
            Assert.That(_simulation.TryAttack(_player.Id, _enemy.Id), Is.True);
            Assert.That(_runner.TryGetEnvironmentContext(out BattleSimulation context, out HexMapView map), Is.True);
            Assert.That(context, Is.SameAs(_simulation));
            Assert.That(map, Is.SameAs(_map));
            Assert.That(_runner.TryApplyGroundEffect(default, BattleGroundEffect.Wet, out _), Is.False);
            _simulation.CancelAction();
            Assert.That(_simulation.TryEndTurn(_player.Id), Is.True);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_enemy.Id));
            Assert.That(_runner.TryGetEnvironmentContext(out _, out _), Is.True);
            Assert.That(_runner.TryApplyGroundEffect(default, BattleGroundEffect.Wet, out string error), Is.True, error);
        }

        [Test]
        public void CompletedBattleKeepsEnvironmentReadableButRejectsEdits()
        {
            Assert.That(_runner.TryApplyGroundEffect(default, BattleGroundEffect.Wet, out _), Is.True);
            Assert.That(_simulation.RemoveUnit(_enemy.Id), Is.True);
            Assert.That(_simulation.TryFinalizeResult(out _), Is.True);
            Assert.That(_runner.TryGetEnvironmentContext(out BattleSimulation context, out _), Is.True);
            Assert.That(context.Board.TryGetEnvironment(default, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.Wet));
            Assert.That(_runner.TryApplyAirEffect(default, BattleAirEffect.Smoke, out _), Is.False);
            Assert.That(_runner.TryClearEnvironment(default, true, true, out _), Is.False);
        }

        [Test]
        public void CommandsUseTheCurrentSimulationAfterReplacement()
        {
            Assert.That(_runner.TryApplyGroundEffect(default, BattleGroundEffect.Wet, out _), Is.True);
            BattleSimulation replacement = CreateBattle();
            SetField(_runner, "_simulation", replacement);
            Assert.That(_runner.TryApplyAirEffect(default, BattleAirEffect.Smoke, out _), Is.True);
            Assert.That(replacement.Board.TryGetEnvironment(default, out BattleEnvironmentState current), Is.True);
            Assert.That(current.Ground, Is.EqualTo(BattleGroundEffect.None));
            Assert.That(current.Air, Is.EqualTo(BattleAirEffect.Smoke));
            Assert.That(_simulation.Board.TryGetEnvironment(default, out BattleEnvironmentState previous), Is.True);
            Assert.That(previous.Ground, Is.EqualTo(BattleGroundEffect.Wet));
            Assert.That(previous.Air, Is.EqualTo(BattleAirEffect.None));
        }

        [TestCase("runner")]
        [TestCase("map")]
        [TestCase("initializing")]
        [TestCase("releasing")]
        public void UnavailableBattleCannotBeReadOrEdited(string condition)
        {
            switch (condition)
            {
                case "runner":
                    _runner.enabled = false;
                    break;
                case "map":
                    _map.enabled = false;
                    break;
                case "initializing":
                    SetField(_runner, "_isInitializing", true);
                    break;
                case "releasing":
                    SetField(_runner, "_isReleasing", true);
                    break;
            }

            Assert.That(_runner.TryGetEnvironmentContext(out _, out _), Is.False);
            Assert.That(_runner.TryApplyGroundEffect(default, BattleGroundEffect.Burning, out _), Is.False);
            SetField(_runner, "_isInitializing", false);
            SetField(_runner, "_isReleasing", false);
        }

        [Test]
        public void DebugContextMenuDoesNotModifyAnEditModeBattle()
        {
            BattleEnvironmentDebugController debug = CreateObject("Debug").AddComponent<BattleEnvironmentDebugController>();
            SetField(debug, "_runner", _runner);
            typeof(BattleEnvironmentDebugController).GetMethod("ApplySelectedEffect",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(debug, null);
            Assert.That(_simulation.Board.EnvironmentVersion, Is.Zero);
            string status = (string)typeof(BattleEnvironmentDebugController).GetField("_lastResult",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(debug);
            Assert.That(status, Does.Contain("Play"));
        }

        [Test]
        public void GeneratedDamagePublishesWithoutAViewAndIsNotAnAttack()
        {
            int damageCount = 0;
            int attackCount = 0;
            _runner.AttackCompleted += (result, view, position) => attackCount++;
            _runner.DamageResolved += (damage, position) =>
            {
                damageCount++;
                Assert.That(damage.Origin, Is.EqualTo(BattleDamageOrigin.Environment));
                Assert.That(damage.Result.TargetId, Is.EqualTo(_player.Id));
                Assert.That(damage.Result.Damage, Is.EqualTo(1));
                Assert.That(position, Is.EqualTo(_map.AxialToWorld(_player.Coordinate)));
            };

            Assert.That(_runner.TryApplyGroundEffect(_player.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(_runner.TryApplyGroundEffect(_player.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(damageCount, Is.EqualTo(1));
            Assert.That(attackCount, Is.Zero);
            Assert.That(_simulation.TryDequeueDamage(out _), Is.False);
        }

        [Test]
        public void EndTurnPublishesEnvironmentDamageBeforeAnotherUpdate()
        {
            Assert.That(_runner.TryApplyGroundEffect(_enemy.Coordinate, BattleGroundEffect.Poisoned, out _), Is.True);
            int count = 0;
            _runner.DamageResolved += (damage, position) => count++;
            Assert.That(_runner.TryEndTurn(_player.Id), Is.True);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(_enemy.Health, Is.EqualTo(8));
        }

        [Test]
        public void DamageCallbackShutdownDiscardsRemainingOldResults()
        {
            Assert.That(_simulation.TryApplyGroundEffect(_player.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(_simulation.TryApplyGroundEffect(_enemy.Coordinate, BattleGroundEffect.Poisoned, out _), Is.True);
            int count = 0;
            _runner.DamageResolved += (damage, position) =>
            {
                count++;
                _runner.ShutdownBattle();
            };
            typeof(BattleRunner).GetMethod("DrainDamage", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_runner, new object[] { _simulation, true });
            Assert.That(count, Is.EqualTo(1));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_simulation.TryDequeueDamage(out _), Is.False);
        }

        [Test]
        public void DisableDuringDamageNotificationStillReleasesNextTurnEnvironmentDeath()
        {
            BattleSimulation battle = new BattleSimulation(_map.Width, _map.Height, randomSeed: 8);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleTestUnit(MercenaryClass.Warrior, 1, 1, 3, 1, 0, initiative: 30),
                HexDirection.East, out HexCharacter first), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 1),
                new BattleTestUnit(MercenaryClass.Warrior, 1, 1, 3, 1, 0, initiative: 20),
                HexDirection.West, out HexCharacter second), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 10, 1, 3, 1, 0, initiative: 10),
                HexDirection.East, out _), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", battle);
            HexCharacterView secondView = CreateObject("Second unit").AddComponent<HexCharacterView>();
            SetField(secondView, "_character", second);
            SetField(secondView, "_mapView", _map);
            SetField(secondView, "_model", secondView.transform);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)typeof(BattleRunner)
                .GetField("_views", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_runner);
            views.Add(second.Id, secondView);
            MethodInfo setEnvironment = typeof(BattleBoard).GetMethod("TrySetEnvironment", BindingFlags.Instance | BindingFlags.NonPublic);
            HexCoordinate destination = new HexCoordinate(2, 1);
            setEnvironment.Invoke(battle.Board, new object[] { destination,
                new BattleEnvironmentState(BattleGroundEffect.Burning, BattleAirEffect.None) });
            setEnvironment.Invoke(battle.Board, new object[] { second.Coordinate,
                new BattleEnvironmentState(BattleGroundEffect.Poisoned, BattleAirEffect.None) });
            Assert.That(battle.TryMove(first.Id, destination), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            int notifications = 0;
            _runner.DamageResolved += (damage, position) =>
            {
                notifications++;
                BattlePresentationTestUtility.DisableAndReenable(_runner);
            };
            typeof(BattleRunner).GetMethod("DrainDamage", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_runner, new object[] { battle, true });
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(battle.Board.TryGetUnit(second.Id, out _), Is.False);
            Assert.That(views.ContainsKey(second.Id), Is.False);
            Assert.That(secondView.gameObject.activeSelf, Is.False);
            Assert.That(battle.HasPendingDamage, Is.False);
        }

        BattleSimulation CreateBattle()
        {
            BattleSimulation simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 8);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleTestUnit(MercenaryClass.Archer, 10, 2, 3, 3, 0, initiative: 20),
                HexDirection.East, out _player), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                new BattleTestUnit(MercenaryClass.Warrior, 10, 2, 3, 1, 0),
                HexDirection.West, out _enemy), Is.True);
            Assert.That(simulation.TryStartBattle(), Is.True);
            return simulation;
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
