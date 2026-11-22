using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleSkillBarView : MonoBehaviour
    {
        [SerializeField] BattleRunner _runner;
        [SerializeField] BattleInputController _input;
        [SerializeField] BattleSkillSlotView[] _slots;

        void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Initialize(i + 1, SelectSlot);
            }
        }

        void OnEnable()
        {
            Refresh();
        }

        void LateUpdate()
        {
            Refresh();
        }

        void Refresh()
        {
            HexCharacter character = null;
            if (_runner != null && _runner.isActiveAndEnabled && _runner.IsInitialized
                && _runner.State == BattleState.Running && _runner.CurrentUnitId.HasValue)
            {
                _runner.TryGetCharacter(_runner.CurrentUnitId.Value, out character);
            }

            if (character != null && (character.Team != BattleTeam.Player || character.Health <= 0))
            {
                character = null;
            }

            SkillType? selected = _runner != null ? _runner.SelectedSkill : null;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (character != null && character.Skills.TryGetSlot(i + 1, out BattleSkillDefinition skill))
                {
                    _slots[i].SetState(SkillLabel(skill.Type),
                        _runner.CanBeginSkillSelection(_input, character.Id, skill.Type),
                        character.GetRemainingCooldown(skill.Type), selected == skill.Type);
                }
                else
                {
                    _slots[i].SetState(string.Empty, false, 0, false);
                }
            }
        }

        void SelectSlot(int slot)
        {
            if (_input != null && isActiveAndEnabled)
            {
                _input.TryBeginSkillSlot(slot);
                Refresh();
            }
        }

        static string SkillLabel(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Stealth:
                    return "은신";
                case SkillType.Ambush:
                    return "암습";
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
                case SkillType.SpiritImbuement:
                    return "정령 부여";
                default:
                    return string.Empty;
            }
        }
    }
}
