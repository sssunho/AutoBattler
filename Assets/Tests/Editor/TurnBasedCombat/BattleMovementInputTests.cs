using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleMovementInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        BattleRunner _runner;
        BattleInputController _input;
        HexMapView _map;
        Camera _camera;
        BattleSimulation _simulation;
        HexCharacter _player;
        HexCharacter _enemy;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _map.transform.SetPositionAndRotation(new Vector3(2f, 0f, 3f), Quaternion.Euler(0f, 31f, 0f));
            _map.transform.localScale = new Vector3(1.3f, 1f, 0.8f);
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 15f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            _simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 7);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                BattleTestUtility.CreateStats(movementPerTurn: 5), HexDirection.East, out _player), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(6, 6),
                BattleTestUtility.CreateStats(), HexDirection.West, out _enemy), Is.True);
            BattleTestUtility.StartTurn(_simulation, _player.Id);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            CreateView(_player);
            CreateView(_enemy);
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
            BattleTestViews.DisposeFixtures(_animations);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HoverLineControlsTheFrozenMoveOrApproachPath(bool attack)
        {
            HexCoordinate destination = new HexCoordinate(4, 4);

            if (attack)
            {
                MoveEnemy(destination);
            }

            Vector2 first = ScreenPoint(destination, 0.35f);
            Vector2 second = ScreenPoint(destination, -0.35f);
            List<HexCoordinate> expected = new List<HexCoordinate>();
            List<HexCoordinate> other = new List<HexCoordinate>();
            Assert.That(_input.TryUpdateMovementPreview(first), Is.True);
            Assert.That(_input.TryCopyMovementPreview(_runner, expected), Is.True);
            Assert.That(_input.TryUpdateMovementPreview(second), Is.True);
            Assert.That(_input.TryCopyMovementPreview(_runner, other), Is.True);
            Assert.That(other, Is.Not.EqualTo(expected));
            if (attack)
            {
                expected = new List<HexCoordinate>(other);
            }
            Assert.That(_input.TryConfirmMovementPosition(first), Is.True);
            Assert.That(_input.TryCopyMovementPreview(_runner, other), Is.False);
            Assert.That(_runner.TryCopyMovementDisplayPath(_input, other), Is.True);
            Assert.That(other, Is.EqualTo(expected));

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.To, Is.EqualTo(expected[i]));
                Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(_player.MovementRemaining, Is.EqualTo(5 - expected.Count));
            Assert.That(_runner.TryCopyMovementDisplayPath(_input, other), Is.False);

            if (attack)
            {
                Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
                Assert.That(_simulation.TryBeginAttack(out _), Is.True);
            }
            else
            {
                Assert.That(_player.Coordinate, Is.EqualTo(destination));
                Assert.That(_simulation.IsBusy, Is.False);
            }
        }

        [Test]
        public void ChangedOccupancyRejectsTheOldPreviewWithoutSpending()
        {
            Assert.That(_input.TryUpdateMovementPreview(ScreenPoint(new HexCoordinate(4, 4))), Is.True);
            MoveEnemy(new HexCoordinate(4, 4));
            Assert.That(_runner.TryConfirmMovementPreview(_input), Is.False);
            Assert.That(_input.TryCopyMovementPreview(_runner, new List<HexCoordinate>()), Is.False);
            Assert.That(_player.MovementRemaining, Is.EqualTo(5));
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [Test]
        public void InvalidAimClearsTheNormalPreviewAndCannotCommit()
        {
            Assert.That(_input.TryUpdateMovementPreview(ScreenPoint(new HexCoordinate(4, 4))), Is.True);
            Assert.That(_input.TryUpdateMovementPreview(new Vector2(float.NaN, 0f)), Is.False);
            Assert.That(_runner.TryConfirmMovementPreview(_input), Is.False);
            Assert.That(_input.TryCopyMovementPreview(_runner, new List<HexCoordinate>()), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
        }

        [Test]
        public void InRangeAttackNeedsNoMovementPath()
        {
            MoveEnemy(new HexCoordinate(3, 2));
            Vector2 position = ScreenPoint(_enemy.Coordinate);
            Assert.That(_input.TryUpdateMovementPreview(position), Is.True);
            Assert.That(_input.TryCopyMovementPreview(_runner, new List<HexCoordinate>()), Is.False);
            Assert.That(_input.TryConfirmMovementPosition(position), Is.True);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(_player.MovementRemaining, Is.EqualTo(5));
        }

        [Test]
        public void AttackClickWithoutPreviewWaitsForTheDisplayedPath()
        {
            MoveEnemy(new HexCoordinate(4, 4));
            Vector2 position = ScreenPoint(_enemy.Coordinate);
            Assert.That(_input.TryConfirmMovementPosition(position), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_input.TryConfirmMovementPosition(position), Is.True);
        }

        [Test]
        public void ChangedBoardRejectsCachedAttackAndRequiresAnotherConfirmation()
        {
            MoveEnemy(new HexCoordinate(4, 4));
            Vector2 position = ScreenPoint(_enemy.Coordinate);
            Assert.That(_input.TryUpdateMovementPreview(position), Is.True);
            MoveEnemy(new HexCoordinate(5, 3));
            position = ScreenPoint(_enemy.Coordinate);
            Assert.That(_input.TryConfirmMovementPosition(position), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_input.TryConfirmMovementPosition(position), Is.True);
        }

        [Test]
        public void StaleAttackAtTheOldTargetCellDoesNotBecomeMovement()
        {
            MoveEnemy(new HexCoordinate(4, 4));
            Vector2 position = ScreenPoint(_enemy.Coordinate);
            Assert.That(_input.TryUpdateMovementPreview(position), Is.True);
            HexCoordinate origin = _player.Coordinate;
            int movement = _player.MovementRemaining;
            int actionPoints = _player.ActionPoints;
            MoveEnemy(new HexCoordinate(5, 3));
            Assert.That(_input.TryConfirmMovementPosition(position), Is.False);
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_player.Coordinate, Is.EqualTo(origin));
            Assert.That(_player.MovementRemaining, Is.EqualTo(movement));
            Assert.That(_player.ActionPoints, Is.EqualTo(actionPoints));
        }

        Vector2 ScreenPoint(HexCoordinate coordinate, float offset = 0f)
        {
            Vector2 center = HexLayout.AxialToLocal(coordinate, _map.HexSize);
            Vector3 screen = _camera.WorldToScreenPoint(_map.transform.TransformPoint(
                new Vector3(center.x, 0f, center.y + offset * _map.HexSize)));
            return new Vector2(screen.x, screen.y);
        }

        void MoveEnemy(HexCoordinate destination)
        {
            MethodInfo move = _simulation.Board.GetType().GetMethod("TryMove",
                BindingFlags.Instance | BindingFlags.NonPublic);

            while (_enemy.Coordinate.Q != destination.Q)
            {
                int offset = _enemy.Coordinate.Q < destination.Q ? 1 : -1;
                HexCoordinate next = new HexCoordinate(_enemy.Coordinate.Q + offset, _enemy.Coordinate.R);
                Assert.That((bool)move.Invoke(_simulation.Board, new object[] { _enemy.Id, next }), Is.True);
            }

            while (_enemy.Coordinate.R != destination.R)
            {
                int offset = _enemy.Coordinate.R < destination.R ? 1 : -1;
                HexCoordinate next = new HexCoordinate(_enemy.Coordinate.Q, _enemy.Coordinate.R + offset);
                Assert.That((bool)move.Invoke(_simulation.Board, new object[] { _enemy.Id, next }), Is.True);
            }
        }

        void CreateView(HexCharacter character)
        {
            HexCharacterView view = BattleTestViews.CreateCharacterView("Unit " + character.Id, _objects, _animations);
            Assert.That(view.TryBind(character, _map), Is.True);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)_runner.GetType()
                .GetField("_views", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_runner);
            views.Add(character.Id, view);
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
