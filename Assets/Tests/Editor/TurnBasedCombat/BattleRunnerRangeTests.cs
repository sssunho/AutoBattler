using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRunnerRangeTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        readonly List<HexCoordinate> _movement = new List<HexCoordinate>();
        readonly List<BattleAttackTarget> _targets = new List<BattleAttackTarget>();
        BattleRunner _runner;
        BattleSimulation _simulation;
        HexMapView _map;
        HexCharacterView _playerView;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 11);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter player), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                BattleTestUtility.CreateStats(), HexDirection.West, out _), Is.True);
            BattleTestUtility.StartTurn(_simulation, player.Id);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            SetField(_runner, "_simulation", _simulation);
            _playerView = BattleTestViews.CreateCharacterView("Player", _objects, _animations);
            Assert.That(_playerView.TryBind(player, _map), Is.True);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(player.Id, _playerView);
            Assert.That(_playerView.IsReady, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
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
        public void ContextReturnsTheActualSessionAndReadyMovementCapability()
        {
            Assert.That(_runner.TryGetRangeContext(out BattleSimulation session, out HexCharacter unit, out bool canMove), Is.True);
            Assert.That(session, Is.SameAs(_simulation));
            Assert.That(unit.Id, Is.EqualTo(1));
            Assert.That(canMove, Is.True);
            Assert.That(_runner.TryCopyActionRange(_movement, _targets), Is.True);
            Assert.That(_movement, Is.Not.Empty);
            Assert.That(_targets.Count, Is.EqualTo(1));
            Assert.That(_targets[0].RequiresMovement, Is.True);
        }

        [Test]
        public void UnreadyMovementViewDoesNotSuppressValidAttackCommands()
        {
            SetField(_playerView, "_moveSpeed", 0f);
            Assert.That(_runner.TryGetRangeContext(out _, out _, out bool canMove), Is.True);
            Assert.That(canMove, Is.False);
            Assert.That(_runner.TryCopyActionRange(_movement, _targets), Is.True);
            Assert.That(_movement, Is.Empty);
            Assert.That(_targets.Count, Is.EqualTo(1));
            Assert.That(_runner.TryMove(1, new HexCoordinate(2, 1)), Is.False);
            Assert.That(_runner.TryAttack(1, _targets[0].UnitId), Is.True);
        }

        [TestCase("busy")]
        [TestCase("enemy-turn")]
        [TestCase("disabled")]
        [TestCase("result")]
        [TestCase("shutdown")]
        [TestCase("invalid-map")]
        public void UnavailableContextClearsPreviouslyDisplayedRanges(string state)
        {
            Assert.That(_runner.TryCopyActionRange(_movement, _targets), Is.True);

            switch (state)
            {
                case "busy":
                    Assert.That(_runner.TryMove(1, new HexCoordinate(2, 1)), Is.True);
                    break;
                case "enemy-turn":
                    Assert.That(_simulation.TryEndTurn(1), Is.True);
                    BattleTestUtility.AdvanceToTurn(_simulation, 2);
                    break;
                case "disabled":
                    _runner.enabled = false;
                    break;
                case "result":
                    Assert.That(_simulation.RemoveUnit(2), Is.True);
                    Assert.That(_simulation.TryFinalizeResult(out _), Is.True);
                    break;
                case "shutdown":
                    _runner.ShutdownBattle();
                    break;
                default:
                    SetField(_map, "_width", _map.Width + 1);
                    break;
            }

            Assert.That(_runner.TryGetRangeContext(out BattleSimulation session, out HexCharacter unit, out bool canMove), Is.False);
            Assert.That(session, Is.Null);
            Assert.That(unit, Is.Null);
            Assert.That(canMove, Is.False);
            Assert.That(_runner.TryCopyActionRange(_movement, _targets), Is.False);
            Assert.That(_movement, Is.Empty);
            Assert.That(_targets, Is.Empty);
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
    }
}
