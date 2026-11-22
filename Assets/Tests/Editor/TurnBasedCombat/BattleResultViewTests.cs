using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleResultViewTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        BattleResultView _view;
        GameObject _resultPanel;
        GameObject _statusPanel;
        Text _resultText;
        Text _statusText;
        Button _restartButton;

        [SetUp]
        public void SetUp()
        {
            GameObject root = CreateObject("Result View");
            _resultPanel = CreateObject("Result Panel", root.transform);
            _resultText = CreateObject("Result Text", _resultPanel.transform).AddComponent<Text>();
            _statusPanel = CreateObject("Status Panel", root.transform);
            _statusText = CreateObject("Status Text", _statusPanel.transform).AddComponent<Text>();
            _restartButton = CreateObject("Restart Button", root.transform).AddComponent<Button>();
            _view = root.AddComponent<BattleResultView>();
            SetField(_view, "_resultPanel", _resultPanel);
            SetField(_view, "_resultText", _resultText);
            SetField(_view, "_restartButton", _restartButton);
            SetField(_view, "_statusPanel", _statusPanel);
            SetField(_view, "_statusText", _statusText);
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

        [TestCase(BattleOutcome.PlayerVictory, "승리")]
        [TestCase(BattleOutcome.PlayerDefeat, "패배")]
        [TestCase(BattleOutcome.Draw, "무승부")]
        public void ResultDisplaysOutcomeActualParticipantsAndSeed(BattleOutcome outcome, string label)
        {
            BattleResult result = CreateResult(outcome);
            _view.Refresh(result, 1, 12345, true, string.Empty);

            Assert.That(_resultPanel.activeSelf, Is.True);
            Assert.That(_statusPanel.activeSelf, Is.False);
            Assert.That(_restartButton.interactable, Is.True);
            StringAssert.StartsWith(label + "\n", _resultText.text);
            StringAssert.Contains($"종료 라운드 {result.RoundNumber}", _resultText.text);
            StringAssert.Contains($"남은 아군 {result.RemainingPlayerCount}/3", _resultText.text);
            StringAssert.Contains($"남은 적 {result.RemainingEnemyCount}/2", _resultText.text);
            StringAssert.Contains("미배치 아군 1명", _resultText.text);
            StringAssert.Contains("Seed 12345", _resultText.text);
        }

        [Test]
        public void PreparingANewAttemptClearsThePreviousResultAndBlocksTheButton()
        {
            _view.Refresh(CreateResult(BattleOutcome.PlayerVictory), 1, 3, true, string.Empty);
            _view.Refresh(null, 1, 3, false, "전투를 다시 준비하는 중입니다.");

            Assert.That(_resultPanel.activeSelf, Is.False);
            Assert.That(_resultText.text, Is.Empty);
            Assert.That(_restartButton.interactable, Is.False);
            Assert.That(_statusPanel.activeSelf, Is.True);
            Assert.That(_statusText.text, Is.EqualTo("전투를 다시 준비하는 중입니다."));
            Assert.That(_view.gameObject.activeSelf, Is.True);
            Assert.That(_restartButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void DisabledOrUnavailableViewDoesNotSendRestartRequests()
        {
            int calls = 0;
            _view.RestartRequested += () => calls++;
            _view.Refresh(null, 0, 1, false, null);
            _restartButton.onClick.Invoke();
            Assert.That(calls, Is.Zero);

            _view.Refresh(null, 0, 1, true, null);
            _restartButton.onClick.Invoke();
            Assert.That(calls, Is.EqualTo(1));

            _view.enabled = false;
            Invoke(_view, "OnDisable");
            _view.Refresh(null, 0, 1, true, null);
            _restartButton.onClick.Invoke();
            Assert.That(calls, Is.EqualTo(1));

            _view.enabled = true;
            Invoke(_view, "OnEnable");
            _restartButton.onClick.Invoke();
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void ReplacingTheButtonRemovesItsOldListener()
        {
            int calls = 0;
            _view.RestartRequested += () => calls++;
            _view.Refresh(null, 0, 1, true, null);
            Button replacement = CreateObject("Replacement", _view.transform).AddComponent<Button>();
            SetField(_view, "_restartButton", replacement);
            _view.Refresh(null, 0, 1, true, null);

            _restartButton.onClick.Invoke();
            Assert.That(calls, Is.Zero);
            replacement.onClick.Invoke();
            Assert.That(calls, Is.EqualTo(1));
        }

        static BattleResult CreateResult(BattleOutcome outcome)
        {
            BattleSimulation simulation = new BattleSimulation(5, 1, randomSeed: 0);
            List<int> players = new List<int>();
            List<int> enemies = new List<int>();

            for (int i = 0; i < 5; i++)
            {
                BattleTeam team = i < 3 ? BattleTeam.Player : BattleTeam.Enemy;
                Assert.That(simulation.TrySpawnUnit(team, new HexCoordinate(i, 0),
                    BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter character), Is.True);
                (team == BattleTeam.Player ? players : enemies).Add(character.Id);
            }

            Assert.That(simulation.TryStartBattle(), Is.True);

            if (outcome != BattleOutcome.PlayerVictory)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    Assert.That(simulation.RemoveUnit(players[i]), Is.True);
                }
            }
            else
            {
                Assert.That(simulation.RemoveUnit(players[0]), Is.True);
            }

            if (outcome != BattleOutcome.PlayerDefeat)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    Assert.That(simulation.RemoveUnit(enemies[i]), Is.True);
                }
            }

            if (simulation.IsBusy)
            {
                Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
                Assert.That(simulation.TrySettleAction(), Is.True);
            }

            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            return result;
        }

        GameObject CreateObject(string name, Transform parent = null)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            _objects.Add(instance);
            return instance;
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
