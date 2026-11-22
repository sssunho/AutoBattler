using System;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleSkillSlotView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Text _skillLabel;
        [SerializeField] Text _keyLabel;
        [SerializeField] GameObject _dim;
        [SerializeField] Text _cooldownLabel;
        [SerializeField] GameObject _selection;

        Action<int> _onClick;
        int _slot;
        int _lastCooldown = -1;

        internal void Initialize(int slot, Action<int> onClick)
        {
            _slot = slot;
            _onClick = onClick;
            _keyLabel.text = slot.ToString();
            _button.onClick.RemoveListener(Click);
            _button.onClick.AddListener(Click);
        }

        internal void SetState(string label, bool usable, int cooldown, bool selected)
        {
            if (_skillLabel.text != label)
            {
                _skillLabel.text = label;
            }

            _button.interactable = usable;
            _dim.SetActive(!usable);
            _selection.SetActive(selected);
            _cooldownLabel.gameObject.SetActive(cooldown > 0);
            if (_lastCooldown != cooldown)
            {
                _lastCooldown = cooldown;
                _cooldownLabel.text = cooldown > 0 ? cooldown.ToString() : string.Empty;
            }
        }

        void Click()
        {
            _onClick?.Invoke(_slot);
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Click);
            }

            _onClick = null;
        }
    }
}
