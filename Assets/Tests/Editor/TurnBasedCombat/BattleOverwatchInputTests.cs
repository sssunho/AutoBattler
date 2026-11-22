using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleOverwatchInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        HexMapView _map;
        Camera _camera;
        BattleRunner _runner;
        BattleInputController _input;
        BattleStatusView _status;
        Button _endTurnButton;
        BattleSimulation _simulation;
        HexCharacter _archer;
        HexCharacter _enemy;
        HexCharacterView _archerView;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Input Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            _endTurnButton = CreateObject("End Turn", typeof(RectTransform)).AddComponent<Button>();
            _status = CreateObject("Status").AddComponent<BattleStatusView>();
            SetField(_status, "_runner", _runner);
            SetField(_status, "_endTurnButton", _endTurnButton);
            InitializeBattle(MercenaryClass.Archer, 4);
        }

        [TearDown]
        public void TearDown()
        {
            if (_input != null)
            {
                _input.CancelOverwatchSelection();
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

            for (int i = 0; i < _animations.Count; i++)
            {
                _animations[i].Dispose();
            }

            _animations.Clear();
        }

        [Test]
        public void PreviewAndCancellationPreserveResourcesFacingAndTurn()
        {
            int actionPoints = _archer.ActionPoints;
            int movement = _archer.MovementRemaining;
            Quaternion rotation = _archerView.transform.rotation;
            Assert.That(_input.TryBeginOverwatchSelection(), Is.True);
            Assert.That(UpdateDirection(_archer.Coordinate.GetNeighbor(HexDirection.West)), Is.True);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out BattleOverwatchState preview), Is.True);
            Assert.That(preview.UnitId, Is.EqualTo(_archer.Id));
            Assert.That(preview.Direction, Is.EqualTo(HexDirection.West));
            Assert.That(_archer.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(_archer.ActionPoints, Is.EqualTo(actionPoints));
            Assert.That(_archer.IsOverwatching, Is.False);
            Assert.That(Quaternion.Angle(_archerView.transform.rotation, rotation), Is.LessThan(0.01f));

            _input.CancelOverwatchSelection();
            _input.CancelOverwatchSelection();

            Assert.That(_input.IsSelectingOverwatch, Is.False);
            Assert.That(_runner.IsSelectingOverwatch, Is.False);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out _), Is.False);
            Assert.That(_archer.ActionPoints, Is.EqualTo(actionPoints));
            Assert.That(_archer.MovementRemaining, Is.EqualTo(movement));
            Assert.That(_archer.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_archer.Id));
            Assert.That(_runner.TryEndTurn(_archer.Id), Is.True);
        }

        [TestCase(MercenaryClass.Warrior, 4)]
        [TestCase(MercenaryClass.Rogue, 4)]
        [TestCase(MercenaryClass.Archer, 1)]
        public void UnsupportedClassOrInsufficientApCannotStartSelection(MercenaryClass unitClass, int actionPoints)
        {
            InitializeBattle(unitClass, actionPoints);
            Assert.That(_input.TryBeginOverwatchSelection(), Is.False);
            Assert.That(_runner.IsSelectingOverwatch, Is.False);
            Assert.That(_archer.ActionPoints, Is.EqualTo(actionPoints));
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_archer.Id));
        }

        [TestCase("busy")]
        [TestCase("enemy-turn")]
        [TestCase("unready-view")]
        public void UnavailableCommandContextCannotStartSelection(string reason)
        {
            switch (reason)
            {
                case "busy":
                    Assert.That(_simulation.TryMove(_archer.Id, new HexCoordinate(3, 2)), Is.True);
                    break;
                case "enemy-turn":
                    Assert.That(_simulation.TryEndTurn(_archer.Id), Is.True);
                    break;
                default:
                    _archerView.enabled = false;
                    break;
            }

            Assert.That(_input.TryBeginOverwatchSelection(), Is.False);
            Assert.That(_runner.IsSelectingOverwatch, Is.False);
        }

        [Test]
        public void SelectionBlocksRunnerCommandsAndHudButtonUntilCancelled()
        {
            Invoke(_status, "Refresh");
            Assert.That(_endTurnButton.interactable, Is.True);
            Assert.That(_input.TryBeginOverwatchSelection(), Is.True);
            Assert.That(_runner.TryMove(_archer.Id, new HexCoordinate(3, 2)), Is.False);
            Assert.That(_runner.TryAttack(_archer.Id, _enemy.Id), Is.False);
            Assert.That(_runner.TryEndTurn(_archer.Id), Is.False);
            _input.EndPlayerTurn();
            Invoke(_status, "Refresh");
            Assert.That(_endTurnButton.interactable, Is.False);
            _endTurnButton.onClick.Invoke();
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_archer.Id));
            Assert.That(_simulation.IsBusy, Is.False);
            Assert.That(_archer.ActionPoints, Is.EqualTo(4));

            _input.CancelOverwatchSelection();
            Invoke(_status, "Refresh");
            Assert.That(_endTurnButton.interactable, Is.True);
            _endTurnButton.onClick.Invoke();
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_enemy.Id));
        }

        [Test]
        public void ConfirmationSpendsOnceRotatesViewAndEndsTurnWithoutLaterCancelRefund()
        {
            Assert.That(_input.TryBeginOverwatchSelection(), Is.True);
            Assert.That(_runner.TryConfirmOverwatch(_input, _simulation, _archer.Id, HexDirection.West), Is.True);
            Assert.That(_archer.ActionPoints, Is.EqualTo(2));
            Assert.That(_archer.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(_archer.OverwatchDirection, Is.EqualTo(HexDirection.West));
            Assert.That(_archer.IsOverwatching, Is.True);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_enemy.Id));
            Assert.That(_input.IsSelectingOverwatch, Is.False);
            Vector3 expectedDirection = _map.AxialToWorld(_archer.Coordinate.GetNeighbor(HexDirection.West))
                - _map.AxialToWorld(_archer.Coordinate);
            Assert.That(Vector3.Angle(_archerView.transform.forward, expectedDirection), Is.LessThan(0.1f));

            Assert.That(_runner.TryConfirmOverwatch(_input, _simulation, _archer.Id, HexDirection.East), Is.False);
            _input.CancelOverwatchSelection();
            Assert.That(_archer.ActionPoints, Is.EqualTo(2));
            Assert.That(_archer.IsOverwatching, Is.True);
            Assert.That(_archer.OverwatchDirection, Is.EqualTo(HexDirection.West));
        }

        [TestCase("removed")]
        [TestCase("next-turn")]
        [TestCase("same-unit-new-round")]
        [TestCase("replacement")]
        [TestCase("camera-disabled")]
        [TestCase("input-disabled")]
        [TestCase("shutdown")]
        public void LostSelectionContextClearsPreviewAndCannotCommit(string reason)
        {
            Assert.That(_input.TryBeginOverwatchSelection(), Is.True);
            Assert.That(UpdateDirection(_archer.Coordinate.GetNeighbor(HexDirection.West)), Is.True);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out _), Is.True);

            switch (reason)
            {
                case "removed":
                    Assert.That(_runner.RemoveCharacter(_archer.Id), Is.True);
                    break;
                case "next-turn":
                    Assert.That(_simulation.TryEndTurn(_archer.Id), Is.True);
                    break;
                case "same-unit-new-round":
                    Assert.That(_simulation.TryEndTurn(_archer.Id), Is.True);
                    Assert.That(_simulation.TryEndTurn(_enemy.Id), Is.True);
                    Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_archer.Id));
                    break;
                case "replacement":
                    SetField(_runner, "_simulation", new BattleSimulation(_map.Width, _map.Height));
                    break;
                case "camera-disabled":
                    _camera.enabled = false;
                    break;
                case "input-disabled":
                    _input.enabled = false;
                    Invoke(_input, "OnDisable");
                    break;
                default:
                    _runner.ShutdownBattle();
                    break;
            }

            Assert.That(_input.IsSelectingOverwatch, Is.False);
            Assert.That(_runner.IsSelectingOverwatch, Is.False);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out _), Is.False);
            Assert.That(_runner.TryConfirmOverwatch(_input, _simulation, _archer.Id, HexDirection.West), Is.False);
            Assert.That(_archer.IsOverwatching, Is.False);
            Assert.That(_archer.Facing, Is.EqualTo(HexDirection.East));
        }

        [TestCase(HexDirection.East)]
        [TestCase(HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthWest)]
        [TestCase(HexDirection.West)]
        [TestCase(HexDirection.SouthWest)]
        [TestCase(HexDirection.SouthEast)]
        public void MouseProjectionSelectsAllSixDirectionsWithoutRotatingCharacter(HexDirection direction)
        {
            _map.transform.SetPositionAndRotation(new Vector3(1f, 0f, 2f), Quaternion.Euler(0f, 27f, 0f));
            _map.transform.localScale = new Vector3(1.3f, 1f, 0.8f);
            Assert.That(_input.TryBeginOverwatchSelection(), Is.True);
            Assert.That(UpdateDirection(_archer.Coordinate.GetNeighbor(direction)), Is.True);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out BattleOverwatchState preview), Is.True);
            Assert.That(preview.Direction, Is.EqualTo(direction));
            Assert.That(_archer.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(_archer.ActionPoints, Is.EqualTo(4));
        }

        [Test]
        public void CenterUsesInitialFacingThenRetainsLastValidDirection()
        {
            Assert.That(_input.TryBeginOverwatchSelection(), Is.True);
            Assert.That(UpdateDirection(_archer.Coordinate), Is.True);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out BattleOverwatchState preview), Is.True);
            Assert.That(preview.Direction, Is.EqualTo(HexDirection.East));
            Assert.That(UpdateDirection(_archer.Coordinate.GetNeighbor(HexDirection.West)), Is.True);
            Assert.That(UpdateDirection(_archer.Coordinate), Is.True);
            Assert.That(_input.TryGetOverwatchPreview(_runner, out preview), Is.True);
            Assert.That(preview.Direction, Is.EqualTo(HexDirection.West));
        }

        void InitializeBattle(MercenaryClass unitClass, int actionPoints)
        {
            if (_runner.IsInitialized)
            {
                _runner.ShutdownBattle();
            }

            _simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: actionPoints), 7);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleTestUnit(unitClass, 20, 2, 3, 3, 0, initiative: 10),
                HexDirection.East, out _archer), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0),
                HexDirection.West, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            _archerView = CreateReadyView(_archer);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(_archer.Id, _archerView);
            views.Add(_enemy.Id, CreateReadyView(_enemy));
        }

        HexCharacterView CreateReadyView(HexCharacter character)
        {
            GameObject gameObject = CreateObject("Character");
            HexCharacterView view = gameObject.AddComponent<HexCharacterView>();
            BattleAnimationFixture animation = new BattleAnimationFixture(gameObject);
            _animations.Add(animation);
            animation.Attach(view);
            Assert.That(view.TryBind(character, _map), Is.True);
            view.transform.position = _map.AxialToWorld(character.Coordinate);
            view.ApplyFacing();
            Assert.That(view.IsReady, Is.True);
            return view;
        }

        bool UpdateDirection(HexCoordinate coordinate)
        {
            Vector3 screenPoint = _camera.WorldToScreenPoint(_map.AxialToWorld(coordinate));
            bool valid = (bool)Invoke(_input, "TryUpdateSelectionDirection", new Vector2(screenPoint.x, screenPoint.y));
            SetField(_input, "_hasPreviewPosition", valid);
            return valid;
        }

        GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject instance = new GameObject(name, components);
            _objects.Add(instance);
            return instance;
        }

        static object Invoke(object target, string name, params object[] arguments)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
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
