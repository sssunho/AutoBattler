using System.Collections.Generic;
using System.Text;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleStatusView : MonoBehaviour
    {
        [SerializeField]
        BattleRunner _runner;

        [SerializeField]
        Text _statusText;

        [SerializeField]
        Button _endTurnButton;

        Button _boundEndTurnButton;
        Text _lastStatusText;
        BattleState _lastState;
        BattleTeam _lastTeam;
        BattleResult _lastResult;
        int _lastRound;
        int _lastUnitId;
        int _lastHealth;
        int _lastActionPoints;
        int _lastMovementRemaining;
        uint _lastCooldownVersion;
        bool _lastConnected;
        bool _lastInitialized;
        bool _lastActive;
        bool _lastBusy;
        bool _lastStealthed;
        bool _lastInvisible;
        bool _lastAmbushUsed;
        bool _lastSelecting;
        bool _lastRecoveryUsed;
        bool _lastHasRecovery;
        bool _lastFourElement;
        SkillSet _lastSkills;
        SpiritType _lastPreparedSpirit;
        IReadOnlyList<SkillChoice> _lastSkillChoices;
        SkillType? _lastSelectedSkill;
        readonly StringBuilder _skillText = new StringBuilder();
        bool _hasSnapshot;

        void OnEnable()
        {
            _hasSnapshot = false;
            Refresh();
        }

        void OnDisable()
        {
            if (_boundEndTurnButton != null)
            {
                _boundEndTurnButton.onClick.RemoveListener(EndPlayerTurn);
                _boundEndTurnButton.interactable = false;
            }

            _boundEndTurnButton = null;
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            BindEndTurnButton();
            bool connected = _runner != null;
            bool initialized = connected && _runner.IsInitialized;
            bool active = connected && _runner.isActiveAndEnabled;
            bool busy = initialized && _runner.IsBusy;
            BattleState state = initialized ? _runner.State : BattleState.Preparing;
            BattleResult result = initialized ? _runner.Result : null;
            int round = initialized ? _runner.RoundNumber : 0;
            HexCharacter character = null;

            if (initialized && _runner.CurrentUnitId.HasValue)
            {
                _runner.TryGetCharacter(_runner.CurrentUnitId.Value, out character);
            }

            int unitId = character != null ? character.Id : 0;
            BattleTeam team = character != null ? character.Team : BattleTeam.Player;
            int health = character != null ? character.Health : 0;
            int actionPoints = character != null ? character.ActionPoints : 0;
            int movementRemaining = character != null ? character.MovementRemaining : 0;
            bool stealthed = character != null && character.IsStealthed;
            bool invisible = character != null && character.IsInvisible;
            bool ambushUsed = character != null && character.HasUsedAmbush;
            bool selecting = initialized && _runner.IsSelectingAction;
            bool recoveryUsed = character != null && character.HasUsedSkillMovementRecovery;
            bool hasRecovery = character != null && character.Traits.Has(TraitType.SkillMovementRecovery);
            bool fourElement = character != null && character.Traits.Has(TraitType.FourElementBasicAttack);
            SpiritType preparedSpirit = character != null ? character.PreparedSpirit : SpiritType.None;
            IReadOnlyList<SkillChoice> skillChoices = initialized ? _runner.SelectedSkillChoices : null;
            SkillType? selectedSkill = initialized ? _runner.SelectedSkill : null;
            SkillSet skills = character != null ? character.Skills : null;
            uint cooldownVersion = character != null ? character.CooldownVersion : 0;

            if (_boundEndTurnButton != null)
            {
                _boundEndTurnButton.interactable = active && initialized
                    && state == BattleState.Running && !busy && !selecting
                    && character != null && team == BattleTeam.Player;
            }

            if (_hasSnapshot && _lastStatusText == _statusText
                && _lastConnected == connected && _lastInitialized == initialized
                && _lastActive == active && _lastBusy == busy && _lastState == state
                && _lastRound == round && _lastUnitId == unitId && _lastTeam == team
                && _lastHealth == health && _lastActionPoints == actionPoints
                && _lastMovementRemaining == movementRemaining && _lastResult == result
                && _lastStealthed == stealthed && _lastAmbushUsed == ambushUsed && _lastSelecting == selecting
                && _lastInvisible == invisible
                && _lastRecoveryUsed == recoveryUsed && _lastHasRecovery == hasRecovery
                && _lastFourElement == fourElement && ReferenceEquals(_lastSkills, skills)
                && _lastPreparedSpirit == preparedSpirit && ReferenceEquals(_lastSkillChoices, skillChoices)
                && _lastSelectedSkill == selectedSkill
                && _lastCooldownVersion == cooldownVersion)
            {
                return;
            }

            _hasSnapshot = true;
            _lastStatusText = _statusText;
            _lastConnected = connected;
            _lastInitialized = initialized;
            _lastActive = active;
            _lastBusy = busy;
            _lastState = state;
            _lastRound = round;
            _lastUnitId = unitId;
            _lastTeam = team;
            _lastHealth = health;
            _lastActionPoints = actionPoints;
            _lastMovementRemaining = movementRemaining;
            _lastResult = result;
            _lastStealthed = stealthed;
            _lastInvisible = invisible;
            _lastAmbushUsed = ambushUsed;
            _lastSelecting = selecting;
            _lastRecoveryUsed = recoveryUsed;
            _lastHasRecovery = hasRecovery;
            _lastFourElement = fourElement;
            _lastSkills = skills;
            _lastPreparedSpirit = preparedSpirit;
            _lastSkillChoices = skillChoices;
            _lastSelectedSkill = selectedSkill;
            _lastCooldownVersion = cooldownVersion;

            if (_statusText == null)
            {
                return;
            }

            _statusText.resizeTextForBestFit = true;
            _statusText.resizeTextMinSize = 14;
            _statusText.resizeTextMaxSize = Mathf.Max(14, _statusText.fontSize);

            if (!connected)
            {
                _statusText.text = "전투 연결 없음";
                return;
            }

            if (!initialized)
            {
                _statusText.text = "전투 준비 중";
                return;
            }

            if (result != null)
            {
                _statusText.text = $"라운드 {result.RoundNumber} · 전투 종료\n{BattleResultView.OutcomeText(result.Outcome)}";
                return;
            }

            string stateText = !active ? "전투 비활성"
                : state == BattleState.Preparing ? "준비 중"
                : state == BattleState.Waiting ? "진행 대기"
                : state == BattleState.Completed ? "전투 종료"
                : busy ? "행동 중" : selecting ? "행동 선택 중" : "명령 대기";

            if (character == null)
            {
                _statusText.text = $"라운드 {round} · {stateText}\n현재 턴 없음";
                return;
            }

            string teamText = team == BattleTeam.Player ? "아군" : "적";
            _statusText.text = $"라운드 {round} · {stateText}\n현재 턴: {teamText} #{unitId}\n"
                + $"HP {health} · AP {actionPoints} · 이동력 {movementRemaining}";

            if (team == BattleTeam.Player)
            {
                string stealthText = invisible ? "투명화 중 ◆" : stealthed ? "은신 중 ◆" : "은신 해제";
                _skillText.Clear();
                _skillText.Append('\n').Append(stealthText);

                if (preparedSpirit != SpiritType.None)
                {
                    _skillText.Append(" · 준비 정령: ").Append(SpiritName(preparedSpirit));
                }

                if (skillChoices != null && skillChoices.Count > 0 && selectedSkill.HasValue)
                {
                    _skillText.Append('\n').Append(SkillName(selectedSkill.Value)).Append(" 선택: ");

                    for (int i = 0; i < skillChoices.Count; i++)
                    {
                        _skillText.Append(i + 1).Append(' ').Append(skillChoices[i].DisplayName).Append(" · ");
                    }

                    _skillText.Append("우클릭 취소");
                    _statusText.text += _skillText.ToString();
                    return;
                }

                if (hasRecovery)
                {
                    _skillText.Append(recoveryUsed ? " · 틈새 전진 사용 완료" : " · 틈새 전진 사용 가능");
                }

                if (fourElement)
                {
                    _skillText.Append(" · 4속성 기본 공격");
                }

                if (skills.Contains(SkillType.Ambush))
                {
                    _skillText.Append(ambushUsed ? " · 암습 사용 완료"
                        : character.GetRemainingCooldown(SkillType.Ambush) > 0 ? " · 암습 쿨타임 중" : " · 암습 사용 가능");
                }

                _skillText.Append('\n');

                for (int slot = 1; slot <= 9; slot++)
                {
                    if (skills.TryGetSlot(slot, out BattleSkillDefinition definition))
                    {
                        _skillText.Append(slot).Append(' ').Append(SkillName(definition.Type));
                        int remaining = character.GetRemainingCooldown(definition.Type);
                        if (remaining > 0)
                        {
                            _skillText.Append(" (쿨타임 ").Append(remaining).Append(')');
                        }

                        _skillText.Append(" · ");
                    }
                }

                _skillText.Append("우클릭 취소");
                _statusText.text += _skillText.ToString();
            }
        }

        static string SpiritName(SpiritType spirit)
        {
            switch (spirit)
            {
                case SpiritType.Fire:
                    return "불";
                case SpiritType.Water:
                    return "물";
                case SpiritType.Electricity:
                    return "전기";
                case SpiritType.Poison:
                    return "독";
                default:
                    return string.Empty;
            }
        }

        static string SkillName(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.SpiritImbuement:
                    return "정령 부여";
                case SkillType.Stealth:
                    return "은신: 자기 칸 선택";
                case SkillType.Ambush:
                    return "암습: 대상 선택";
                case SkillType.Overwatch:
                    return "경계사격";
                case SkillType.PullSelf:
                    return "끌려가기";
                case SkillType.PullTarget:
                    return "끌어오기";
                case SkillType.Invisibility:
                    return "투명화";
                case SkillType.AssassinLeap:
                    return "후방 도약";
                default:
                    return string.Empty;
            }
        }

        void BindEndTurnButton()
        {
            if (_boundEndTurnButton == _endTurnButton)
            {
                return;
            }

            if (_boundEndTurnButton != null)
            {
                _boundEndTurnButton.onClick.RemoveListener(EndPlayerTurn);
                _boundEndTurnButton.interactable = false;
            }

            _boundEndTurnButton = _endTurnButton;

            if (_boundEndTurnButton != null)
            {
                _boundEndTurnButton.onClick.AddListener(EndPlayerTurn);
            }
        }

        void EndPlayerTurn()
        {
            if (!isActiveAndEnabled || _runner == null || !_runner.isActiveAndEnabled
                || !_runner.IsInitialized || _runner.State != BattleState.Running
                || _runner.IsBusy || _runner.IsSelectingAction || !_runner.CurrentUnitId.HasValue
                || !_runner.TryGetCharacter(_runner.CurrentUnitId.Value, out HexCharacter character)
                || character.Team != BattleTeam.Player)
            {
                return;
            }

            _runner.TryEndTurn(character.Id);
            Refresh();
        }
    }
}
