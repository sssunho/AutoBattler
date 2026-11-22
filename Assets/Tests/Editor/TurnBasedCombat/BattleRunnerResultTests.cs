using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRunnerResultTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _fixtures = new List<BattleAnimationFixture>();
        BattleRunner _runner;
        BattleSimulation _simulation;
        HexMapView _map;
        HexCharacter _player;
        HexCharacter _enemy;
        HexCharacterView _playerView;
        HexCharacterView _enemyView;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 3);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 0),
                BattleTestUtility.CreateStats(attackDamage: 10), HexDirection.East, out _player), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(), HexDirection.West, out _enemy), Is.True);
            BattleTestUtility.StartTurn(_simulation, _player.Id);
            _playerView = CreateView(_player);
            _enemyView = CreateView(_enemy);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            SetField(_runner, "_simulation", _simulation);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(_player.Id, _playerView);
            views.Add(_enemy.Id, _enemyView);
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
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();

            for (int i = 0; i < _fixtures.Count; i++)
            {
                _fixtures[i].Dispose();
            }

            _fixtures.Clear();
        }

        [Test]
        public void AllDestroyedViewsAreRemovedBeforeDrawIsPublishedOnce()
        {
            int notifications = 0;
            _runner.BattleCompleted += result =>
            {
                notifications++;
                Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Draw));
                Assert.That(_runner.State, Is.EqualTo(BattleState.Completed));
                Assert.That(_runner.IsBusy, Is.False);
            };
            UnityEngine.Object.DestroyImmediate(_playerView.gameObject);
            UnityEngine.Object.DestroyImmediate(_enemyView.gameObject);

            Tick();
            Tick();

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(_runner.Result.RemainingPlayerCount, Is.Zero);
            Assert.That(_runner.Result.RemainingEnemyCount, Is.Zero);
        }

        [Test]
        public void LethalAttackPresentationIsReportedBeforeBattleCompletion()
        {
            List<string> events = new List<string>();
            _runner.AttackCompleted += (result, target, position) => events.Add("attack");
            _runner.BattleCompleted += result =>
            {
                events.Add("battle");
                Assert.That(_runner.TryGetCharacter(_enemy.Id, out _), Is.False);
                Assert.That(_enemyView.IsInitialized, Is.False);
                Assert.That(_runner.IsBusy, Is.False);
            };
            AttachAnimator(_playerView, _player);
            Assert.That(_runner.TryAttack(_player.Id, _enemy.Id), Is.True);

            Step(0f);
            Step(0.6f);
            Assert.That(events, Is.EqualTo(new[] { "attack" }));
            Step(0.5f);
            Tick();
            Tick();

            Assert.That(events, Is.EqualTo(new[] { "attack", "battle" }));
            Assert.That(_runner.Result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void AttackCallbackDestructionIsIncludedInTheFinalRemovalSweep()
        {
            _runner.AttackCompleted += (result, target, position) =>
            {
                UnityEngine.Object.DestroyImmediate(_playerView.gameObject);
            };
            AttachAnimator(_playerView, _player);
            Assert.That(_runner.TryAttack(_player.Id, _enemy.Id), Is.True);

            Step(0f);
            Step(0.6f);

            Assert.That(_runner.Result, Is.Null);
            Tick();
            Tick();

            Assert.That(_runner.Result.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(_runner.Result.RemainingPlayerCount, Is.Zero);
        }

        [Test]
        public void RemovedIssuedAttackTargetIsFinalizedAfterFalseCompletionCleanup()
        {
            int notifications = 0;
            _runner.BattleCompleted += result => notifications++;
            Assert.That(_runner.TryAttack(_player.Id, _enemy.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);
            Assert.That(_runner.RemoveCharacter(_enemy.Id), Is.True);
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_runner.Result, Is.Null);

            Tick();

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_runner.Result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void AttackCallbackShutdownSuppressesTheAbandonedBattleResult()
        {
            int notifications = 0;
            _runner.AttackCompleted += (result, target, position) => _runner.ShutdownBattle();
            _runner.BattleCompleted += result => notifications++;
            AttachAnimator(_playerView, _player);
            Assert.That(_runner.TryAttack(_player.Id, _enemy.Id), Is.True);

            Step(0f);
            Step(0.6f);
            Tick();

            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.Result, Is.Null);
            Assert.That(notifications, Is.Zero);
            Assert.That(_simulation.Result, Is.Null);
        }

        [Test]
        public void CompletionCallbackCanShutdownWithoutOldSimulationAccessOrDuplicateNotice()
        {
            int notifications = 0;
            _runner.BattleCompleted += result =>
            {
                notifications++;
                _runner.ShutdownBattle();
            };
            Assert.That(_runner.RemoveCharacter(_enemy.Id), Is.True);

            Tick();
            Tick();

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(_runner.IsInitialized, Is.False);
            Assert.That(_runner.Result, Is.Null);
        }

        [Test]
        public void ReentrantUpdateFromCompletionDoesNotPublishAgain()
        {
            int notifications = 0;
            _runner.BattleCompleted += result =>
            {
                notifications++;
                Tick();
            };
            Assert.That(_runner.RemoveCharacter(_enemy.Id), Is.True);

            Tick();

            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void ShutdownBeforeHitAbortsIssuedLethalAttackWithoutResult()
        {
            int health = _enemy.Health;
            int notifications = 0;
            _runner.BattleCompleted += result => notifications++;
            Assert.That(_runner.TryAttack(_player.Id, _enemy.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);

            _runner.ShutdownBattle();
            Tick();

            Assert.That(notifications, Is.Zero);
            Assert.That(_runner.Result, Is.Null);
            Assert.That(_simulation.Result, Is.Null);
            Assert.That(_enemy.Health, Is.EqualTo(health));
        }

        [Test]
        public void PresentationStoppedCanReenterShutdownWithoutRecursion()
        {
            int notifications = 0;
            _runner.PresentationStopped += () =>
            {
                notifications++;
                _runner.ShutdownBattle();
            };

            _runner.ShutdownBattle();

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(_runner.IsInitialized, Is.False);
        }

        [Test]
        public void InitiallyIncompleteBattleDoesNotPublishAResult()
        {
            _runner.ShutdownBattle();
            BattleSimulation empty = new BattleSimulation(_map.Width, _map.Height, randomSeed: 3);
            Assert.That(empty.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", empty);
            int notifications = 0;
            _runner.BattleCompleted += result => notifications++;

            Tick();

            Assert.That(notifications, Is.Zero);
            Assert.That(_runner.Result, Is.Null);
            Assert.That(_runner.State, Is.EqualTo(BattleState.Waiting));
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            HexCharacterView view = CreateObject("Unit").AddComponent<HexCharacterView>();
            SetField(view, "_character", character);
            SetField(view, "_mapView", _map);
            SetField(view, "_model", view.transform);
            view.transform.position = _map.AxialToWorld(character.Coordinate);
            return view;
        }

        void AttachAnimator(HexCharacterView view, HexCharacter character)
        {
            BattleAnimationFixture fixture = new BattleAnimationFixture(view.gameObject);
            _fixtures.Add(fixture);
            view.Unbind();
            fixture.Attach(view);
            Assert.That(view.TryBind(character, _map), Is.True);
        }

        void Step(float deltaTime)
        {
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", deltaTime);
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        void Tick()
        {
            typeof(BattleRunner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_runner, null);
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
