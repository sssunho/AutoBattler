using System;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleResultView : MonoBehaviour
    {
        [SerializeField]
        GameObject _resultPanel;

        [SerializeField]
        Text _resultText;

        [SerializeField]
        Button _restartButton;

        [SerializeField]
        GameObject _statusPanel;

        [SerializeField]
        Text _statusText;

        Button _boundRestartButton;
        BattleResult _result;
        Text _displayedResultText;
        Text _displayedStatusText;
        string _message;
        int _skippedPlayerCount;
        int _seed;
        bool _canRestart;
        bool _hasSnapshot;

        internal event Action RestartRequested;

        internal void Refresh(BattleResult result, int skippedPlayerCount, int seed, bool canRestart, string message)
        {
            bool resultChanged = !_hasSnapshot || _result != result || _skippedPlayerCount != skippedPlayerCount
                || _seed != seed || _displayedResultText != _resultText;
            bool messageChanged = !_hasSnapshot || _message != message || _displayedStatusText != _statusText;
            _result = result;
            _skippedPlayerCount = skippedPlayerCount;
            _seed = seed;
            _canRestart = canRestart;
            _message = message;
            _hasSnapshot = true;
            BindRestartButton();

            if (_boundRestartButton != null)
            {
                _boundRestartButton.interactable = canRestart && isActiveAndEnabled;
            }

            SetPanelActive(_resultPanel, result != null);
            SetPanelActive(_statusPanel, !string.IsNullOrEmpty(message));

            if (_resultText != null && resultChanged)
            {
                _resultText.text = result == null ? string.Empty
                    : $"{OutcomeText(result.Outcome)}\n종료 라운드 {result.RoundNumber}\n"
                        + $"남은 아군 {result.RemainingPlayerCount}/{result.InitialPlayerCount}\n"
                        + $"남은 적 {result.RemainingEnemyCount}/{result.InitialEnemyCount}\n"
                        + $"미배치 아군 {skippedPlayerCount}명\nSeed {seed}";
                _displayedResultText = _resultText;
            }

            if (_statusText != null && messageChanged)
            {
                _statusText.text = message ?? string.Empty;
                _displayedStatusText = _statusText;
            }
        }

        internal static string OutcomeText(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "승리";
                case BattleOutcome.PlayerDefeat:
                    return "패배";
                default:
                    return "무승부";
            }
        }

        void OnEnable()
        {
            _hasSnapshot = false;
            Refresh(_result, _skippedPlayerCount, _seed, _canRestart, _message);
        }

        void OnDisable()
        {
            if (_boundRestartButton != null)
            {
                _boundRestartButton.onClick.RemoveListener(RequestRestart);
                _boundRestartButton.interactable = false;
            }

            _boundRestartButton = null;
        }

        void BindRestartButton()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_boundRestartButton == _restartButton)
            {
                return;
            }

            if (_boundRestartButton != null)
            {
                _boundRestartButton.onClick.RemoveListener(RequestRestart);
                _boundRestartButton.interactable = false;
            }

            _boundRestartButton = _restartButton;

            if (_boundRestartButton != null && isActiveAndEnabled)
            {
                _boundRestartButton.onClick.AddListener(RequestRestart);
            }
        }

        void RequestRestart()
        {
            if (isActiveAndEnabled && _canRestart && _boundRestartButton != null
                && _boundRestartButton.isActiveAndEnabled && _boundRestartButton.IsInteractable())
            {
                RestartRequested?.Invoke();
            }
        }

        void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null && !transform.IsChildOf(panel.transform) && panel.activeSelf != active)
            {
                panel.SetActive(active);
            }
        }
    }
}
