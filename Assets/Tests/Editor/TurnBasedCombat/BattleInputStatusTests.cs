using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleInputStatusTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        BattleSimulation _simulation;
        BattleRunner _runner;
        BattleInputController _input;
        BattleStatusView _status;
        Text _text;
        Button _button;
        HexCharacter _first;
        HexCharacter _second;
        HexCharacter _enemy;

        [SetUp]
        public void SetUp()
        {
            HexMapView map = CreateObject("Map").AddComponent<HexMapView>();
            _simulation = new BattleSimulation(map.Width, map.Height, randomSeed: 0);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out _first), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _second), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(), HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);

            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", map);
            SetField(_runner, "_hexSize", map.HexSize);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            _text = CreateUIObject("Status Text").AddComponent<Text>();
            _button = CreateUIObject("End Turn Button").AddComponent<Button>();
            _status = CreateObject("Status").AddComponent<BattleStatusView>();
            SetField(_status, "_runner", _runner);
            SetField(_status, "_statusText", _text);
            SetField(_status, "_endTurnButton", _button);
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
        public void PlayerInputFollowsCurrentUnitWithoutFixedCharacter()
        {
            AdvanceTo(_first.Id);
            Assert.That(ReadControlledCharacter(), Is.SameAs(_first));
            AssertEndTurnChangesPosition(_input.EndPlayerTurn);

            AdvanceTo(_second.Id);
            Assert.That(ReadControlledCharacter(), Is.SameAs(_second));
            AssertEndTurnChangesPosition(_input.EndPlayerTurn);
        }

        [Test]
        public void EnemyTurnRejectsPlayerEndTurnAndDisablesButton()
        {
            AdvanceTo(_enemy.Id);
            int round = _simulation.RoundNumber;

            _input.EndPlayerTurn();
            RefreshStatus();
            _button.onClick.Invoke();

            Assert.That(ReadControlledCharacter(), Is.Null);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_enemy.Id));
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(_button.interactable, Is.False);
            StringAssert.Contains($"현재 턴: 적 #{_enemy.Id}", _text.text);
        }

        [Test]
        public void BusyPlayerRejectsEndTurnUntilMovementCompletes()
        {
            AdvanceTo(_first.Id);
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(1, 0)), Is.True);
            int round = _simulation.RoundNumber;

            _input.EndPlayerTurn();
            RefreshStatus();
            _button.onClick.Invoke();

            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(_simulation.IsBusy, Is.True);
            Assert.That(_button.interactable, Is.False);
            StringAssert.Contains("행동 중", _text.text);

            Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            RefreshStatus();

            Assert.That(_button.interactable, Is.True);
            AssertEndTurnChangesPosition(_input.EndPlayerTurn);
        }

        [Test]
        public void WaitingBattleRejectsEndTurnAndShowsNoCurrentUnit()
        {
            AdvanceTo(_first.Id);
            Assert.That(_simulation.RemoveUnit(_enemy.Id), Is.True);
            int round = _simulation.RoundNumber;

            _input.EndPlayerTurn();
            RefreshStatus();
            _button.onClick.Invoke();

            Assert.That(_simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(_simulation.CurrentUnitId, Is.Null);
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(_button.interactable, Is.False);
            StringAssert.Contains("진행 대기", _text.text);
            StringAssert.Contains("현재 턴 없음", _text.text);
        }

        [Test]
        public void CompletedBattleShowsItsOutcomeAndRejectsTurnCommands()
        {
            AdvanceTo(_first.Id);
            RefreshStatus();
            Assert.That(_simulation.RemoveUnit(_enemy.Id), Is.True);
            Assert.That(_simulation.TryFinalizeResult(out BattleResult result), Is.True);

            RefreshStatus();
            _input.EndPlayerTurn();
            _button.onClick.Invoke();

            Assert.That(_simulation.State, Is.EqualTo(BattleState.Completed));
            Assert.That(_simulation.Result, Is.SameAs(result));
            Assert.That(ReadControlledCharacter(), Is.Null);
            Assert.That(_button.interactable, Is.False);
            StringAssert.Contains("전투 종료", _text.text);
            StringAssert.Contains("승리", _text.text);
        }

        [Test]
        public void StatusTracksCurrentUnitHealthAndSpentResources()
        {
            AdvanceTo(_enemy.Id);
            Assert.That(_simulation.TryAttack(_enemy.Id, _first.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);
            Assert.That(_simulation.TryCompleteAttack(out _), Is.True);
            AdvanceTo(_first.Id);
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(1, 0)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(_simulation.TryAttack(_first.Id, _enemy.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);

            RefreshStatus();

            StringAssert.Contains($"라운드 {_simulation.RoundNumber}", _text.text);
            StringAssert.Contains($"현재 턴: 아군 #{_first.Id}", _text.text);
            StringAssert.Contains("HP 7", _text.text);
            StringAssert.Contains($"AP {_first.ActionPoints}", _text.text);
            StringAssert.Contains("이동력 1", _text.text);
            Assert.That(_button.interactable, Is.False);
        }

        [Test]
        public void DisabledStatusCannotEndTurnAndReenableRestoresButton()
        {
            AdvanceTo(_first.Id);
            RefreshStatus();
            Assert.That(_button.interactable, Is.True);
            int round = _simulation.RoundNumber;

            _status.enabled = false;
            typeof(BattleStatusView).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_status, null);
            Assert.That(_button.interactable, Is.False);
            _button.onClick.Invoke();

            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));

            _status.enabled = true;
            typeof(BattleStatusView).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_status, null);
            Assert.That(_button.interactable, Is.True);
            AssertEndTurnChangesPosition(_button.onClick.Invoke);
        }

        [Test]
        public void ReplacingButtonRemovesOldListener()
        {
            AdvanceTo(_first.Id);
            RefreshStatus();
            Button replacement = CreateUIObject("Replacement Button").AddComponent<Button>();
            SetField(_status, "_endTurnButton", replacement);
            RefreshStatus();
            int round = _simulation.RoundNumber;

            _button.onClick.Invoke();

            Assert.That(_button.interactable, Is.False);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(replacement.interactable, Is.True);
            AssertEndTurnChangesPosition(replacement.onClick.Invoke);
        }

        [Test]
        public void DisabledRunnerBlocksInputAndStatusButton()
        {
            AdvanceTo(_first.Id);
            int round = _simulation.RoundNumber;
            _runner.enabled = false;

            _input.EndPlayerTurn();
            RefreshStatus();
            _button.onClick.Invoke();

            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(_button.interactable, Is.False);
            StringAssert.Contains("전투 비활성", _text.text);
        }

        void AdvanceTo(int unitId)
        {
            for (int i = 0; i < 16 && _simulation.CurrentUnitId != unitId; i++)
            {
                Assert.That(_simulation.CurrentUnitId.HasValue, Is.True);
                Assert.That(_simulation.TryEndTurn(_simulation.CurrentUnitId.Value), Is.True);
            }

            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(unitId));
        }

        HexCharacter ReadControlledCharacter()
        {
            object[] arguments = { null };
            bool found = (bool)typeof(BattleInputController)
                .GetMethod("TryGetCurrentPlayer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_input, arguments);
            return found ? (HexCharacter)arguments[0] : null;
        }

        void AssertEndTurnChangesPosition(System.Action endTurn)
        {
            int? unitId = _simulation.CurrentUnitId;
            int round = _simulation.RoundNumber;
            endTurn();
            Assert.That(_simulation.CurrentUnitId != unitId || _simulation.RoundNumber > round, Is.True);
        }

        void RefreshStatus()
        {
            typeof(BattleStatusView).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_status, null);
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        GameObject CreateUIObject(string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            _objects.Add(gameObject);
            return gameObject;
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
