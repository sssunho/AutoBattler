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
    public sealed class BattleStealthInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        HexMapView _map;
        Camera _camera;
        BattleRunner _runner;
        BattleInputController _input;
        BattleStatusView _status;
        Button _endTurn;
        BattleSimulation _simulation;
        HexCharacter _rogue;
        HexCharacter _enemy;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            _status = CreateObject("Status").AddComponent<BattleStatusView>();
            _endTurn = CreateObject("End Turn", typeof(RectTransform)).AddComponent<Button>();
            SetField(_status, "_runner", _runner);
            SetField(_status, "_endTurnButton", _endTurn);
            _simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: 4), 7);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleTestUnit(MercenaryClass.Rogue, 20, 3, 5, 1, 0, initiative: 10),
                HexDirection.East, out _rogue), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0),
                HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(_rogue.Id, CreateView(_rogue));
            views.Add(_enemy.Id, CreateView(_enemy));
        }

        [TearDown]
        public void TearDown()
        {
            _input.CancelActionSelection();
            _runner.ShutdownBattle();

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
        public void StealthPreviewLocksAllCommandsAndCancellationCostsNothing()
        {
            Assert.That(_input.TryBeginStealthSelection(), Is.True);
            Assert.That(_runner.IsSelectingAction, Is.True);
            Assert.That(_input.TryBeginAmbushSelection(), Is.False);
            Assert.That(_input.TryBeginOverwatchSelection(), Is.False);
            Assert.That(_runner.TryMove(_rogue.Id, new HexCoordinate(1, 2)), Is.False);
            Assert.That(_runner.TryAttack(_rogue.Id, _enemy.Id), Is.False);
            Assert.That(_runner.TryEndTurn(_rogue.Id), Is.False);
            _input.EndPlayerTurn();
            Invoke(_status, "Refresh");
            Assert.That(_endTurn.interactable, Is.False);
            _endTurn.onClick.Invoke();
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_rogue.Id));
            _runner.CancelAction();
            Assert.That(_runner.IsSelectingAction, Is.False);
            Assert.That(_rogue.IsStealthed, Is.False);
            Assert.That(_rogue.ActionPoints, Is.EqualTo(4));
            Assert.That(_rogue.MovementRemaining, Is.EqualTo(5));
            Invoke(_status, "Refresh");
            Assert.That(_endTurn.interactable, Is.True);
        }

        [Test]
        public void ConfirmingOwnHexUsesMapTransformAndSpendsOnlyOnce()
        {
            _map.transform.SetPositionAndRotation(new Vector3(1f, 0f, 2f), Quaternion.Euler(0f, 27f, 0f));
            _map.transform.localScale = new Vector3(1.3f, 1f, 0.8f);
            Assert.That(_input.TryBeginStealthSelection(), Is.True);
            Assert.That(ConfirmCoordinate(_enemy.Coordinate), Is.False);
            Assert.That(ConfirmCoordinate(_rogue.Coordinate), Is.True);
            Assert.That(_rogue.IsStealthed, Is.True);
            Assert.That(_rogue.ActionPoints, Is.EqualTo(2));
            Assert.That(_rogue.MovementRemaining, Is.EqualTo(3));
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_rogue.Id));
            Assert.That(_runner.IsSelectingAction, Is.False);
            Assert.That(_runner.TryConfirmStealth(_input, _simulation, _rogue.Id), Is.False);
            Assert.That(_input.TryGetStealthContext(_runner, out _, out HexCharacter unit), Is.True);
            Assert.That(unit, Is.SameAs(_rogue));
        }

        [Test]
        public void VisiblePositionCannotBeginStealthSelectionOrSpendResources()
        {
            typeof(HexCharacter).GetMethod("SetFacing", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_enemy, new object[] { HexDirection.West, false });
            Assert.That(_input.TryBeginStealthSelection(), Is.False);
            Assert.That(_runner.TryConfirmStealth(_input, _simulation, _rogue.Id), Is.False);
            Assert.That(_input.IsSelectingStealth, Is.False);
            Assert.That(_rogue.ActionPoints, Is.EqualTo(4));
            Assert.That(_rogue.MovementRemaining, Is.EqualTo(5));
            Assert.That(_rogue.IsStealthed, Is.False);
        }

        [Test]
        public void AmbushSelectionConfirmsOnlyValidTargetAndPreservesStealthUntilAttackEnds()
        {
            Assert.That(_simulation.TryEnterStealth(_rogue.Id), Is.True);
            Assert.That(_input.TryBeginAmbushSelection(), Is.True);
            Assert.That(_input.TryGetAmbushContext(_runner, out _, out HexCharacter unit), Is.True);
            Assert.That(unit, Is.SameAs(_rogue));
            Assert.That(ConfirmCoordinate(_rogue.Coordinate), Is.False);
            Assert.That(_runner.TryEndTurn(_rogue.Id), Is.False);
            Assert.That(ConfirmCoordinate(_enemy.Coordinate), Is.True);
            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Assert.That(_rogue.ActionPoints, Is.EqualTo(2));
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);
            Assert.That(_rogue.HasUsedAmbush, Is.True);
            Assert.That(_rogue.IsStealthed, Is.True);
            Assert.That(_simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(5));
            Assert.That(_rogue.IsStealthed, Is.False);
        }

        [TestCase("camera")]
        [TestCase("input")]
        [TestCase("turn")]
        [TestCase("round")]
        [TestCase("replacement")]
        [TestCase("shutdown")]
        public void StaleSelectionCannotConfirm(string reason)
        {
            Assert.That(_input.TryBeginStealthSelection(), Is.True);

            switch (reason)
            {
                case "camera":
                    _camera.enabled = false;
                    break;
                case "input":
                    _input.enabled = false;
                    Invoke(_input, "OnDisable");
                    break;
                case "turn":
                    Assert.That(_simulation.TryEndTurn(_rogue.Id), Is.True);
                    break;
                case "round":
                    Assert.That(_simulation.TryEndTurn(_rogue.Id), Is.True);
                    Assert.That(_simulation.TryEndTurn(_enemy.Id), Is.True);
                    break;
                case "replacement":
                    SetField(_runner, "_simulation", new BattleSimulation(_map.Width, _map.Height));
                    break;
                default:
                    _runner.ShutdownBattle();
                    break;
            }

            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_runner.IsSelectingAction, Is.False);
            Assert.That(_runner.TryConfirmStealth(_input, _simulation, _rogue.Id), Is.False);
            Assert.That(_rogue.IsStealthed, Is.False);
        }

        bool ConfirmCoordinate(HexCoordinate coordinate)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(coordinate));
            _input.TryUpdateSelectedPosition(new Vector2(screen.x, screen.y));
            return _input.TryConfirmSelectedPosition(new Vector2(screen.x, screen.y));
        }

        HexCharacterView CreateView(HexCharacter character)
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

        GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject instance = new GameObject(name, components);
            _objects.Add(instance);
            return instance;
        }

        static object Invoke(object target, string name)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
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
