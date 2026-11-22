using System;
using System.Collections.Generic;
using System.Text;
using RaggedBanner.Events;
using RaggedBanner.GameState;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RaggedBanner.Events.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class EventPanel : MonoBehaviour
    {
        [SerializeField]
        Text _titleText;

        [SerializeField]
        Text _bodyText;

        [SerializeField]
        Text _effectsText;

        [SerializeField]
        ScrollRect _bodyScroll;

        [SerializeField]
        RectTransform _choicesRoot;

        [SerializeField]
        Button _choiceButtonTemplate;

        [SerializeField]
        Button _continueButton;

        readonly List<Button> _choiceButtons = new List<Button>();
        readonly List<Text> _choiceLabels = new List<Text>();
        readonly List<UnityAction> _choiceHandlers = new List<UnityAction>();
        IReadOnlyList<EventChoice> _visibleChoices = Array.Empty<EventChoice>();
        CanvasGroup _canvasGroup;
        Text _continueLabel;
        bool _initialized;

        public event Action<string> ChoiceSelected;
        public event Action ContinueClicked;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (_titleText == null || _bodyText == null || _effectsText == null
                || _bodyScroll == null || _bodyScroll.content == null || _choicesRoot == null
                || _choiceButtonTemplate == null || _continueButton == null)
            {
                throw new InvalidOperationException("Assign the event panel texts, scroll content, choices root and buttons.");
            }

            if (!_choiceButtonTemplate.transform.IsChildOf(_choicesRoot)
                || _choiceButtonTemplate == _continueButton
                || _choiceButtonTemplate.GetComponentInChildren<Text>(true) == null)
            {
                throw new InvalidOperationException("The choice button template must be a child of Choices Root and contain a UI Text label.");
            }

            _continueLabel = _continueButton.GetComponentInChildren<Text>(true);
            if (_continueLabel == null)
            {
                throw new InvalidOperationException("The continue button must contain a UI Text label.");
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            _choiceButtonTemplate.gameObject.SetActive(false);
            _continueButton.onClick.AddListener(OnContinueClicked);
            _initialized = true;
            Hide();
        }

        public void ShowEvent(EventDefinition definition, IReadOnlyList<EventChoice> choices)
        {
            Initialize();
            ClearChoices();
            _titleText.text = definition.Title;
            _bodyText.text = definition.Description;
            _effectsText.text = string.Empty;
            _visibleChoices = choices;

            for (int i = 0; i < choices.Count; i++)
            {
                EnsureChoiceButton(i);
                _choiceLabels[i].text = choices[i].Text;
                _choiceButtons[i].gameObject.SetActive(true);
            }

            _continueLabel.text = "계속";
            _continueButton.gameObject.SetActive(definition.IsAutomatic);
            Show();
        }

        public void ShowResult(EventDefinition definition, EventResult result, AppliedEventEffects effects)
        {
            Initialize();
            ClearChoices();
            _titleText.text = definition.Title;
            _bodyText.text = result.Text;
            var changes = new StringBuilder();
            AppendChange(changes, "돈", effects.MoneyChange);
            AppendChange(changes, "명성", effects.FameChange);
            AppendChange(changes, "평판", effects.ReputationChange);
            _effectsText.text = changes.ToString();
            _continueLabel.text = "확인";
            _continueButton.gameObject.SetActive(true);
            Show();
        }

        public void ShowRunEnd()
        {
            Initialize();
            ClearChoices();
            _titleText.text = "원정 종료";
            _bodyText.text = "흰밀물이 밀려온다. 이번 원정의 시간이 끝났다.";
            _effectsText.text = string.Empty;
            _continueButton.gameObject.SetActive(false);
            Show();
        }

        public void Hide()
        {
            if (!_initialized)
            {
                return;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_initialized)
            {
                _canvasGroup.interactable = enabled;
            }
        }

        void Show()
        {
            transform.SetAsLastSibling();
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyScroll.content);
            _bodyScroll.StopMovement();
            _bodyScroll.verticalNormalizedPosition = 1f;
        }

        void ClearChoices()
        {
            _visibleChoices = Array.Empty<EventChoice>();
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                _choiceButtons[i].gameObject.SetActive(false);
            }
        }

        void EnsureChoiceButton(int index)
        {
            if (index < _choiceButtons.Count)
            {
                return;
            }

            Button button = Instantiate(_choiceButtonTemplate, _choicesRoot, false);
            Text label = button.GetComponentInChildren<Text>(true);
            UnityAction handler = () => OnChoiceClicked(index);
            button.onClick.AddListener(handler);
            _choiceButtons.Add(button);
            _choiceLabels.Add(label);
            _choiceHandlers.Add(handler);
        }

        void OnChoiceClicked(int index)
        {
            if (!isActiveAndEnabled || !_canvasGroup.interactable
                || index < 0 || index >= _visibleChoices.Count)
            {
                return;
            }

            ChoiceSelected?.Invoke(_visibleChoices[index].Id);
        }

        void OnContinueClicked()
        {
            if (isActiveAndEnabled && _canvasGroup.interactable)
            {
                ContinueClicked?.Invoke();
            }
        }

        static void AppendChange(StringBuilder builder, string name, int value)
        {
            if (value == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("  ·  ");
            }

            builder.Append(name);
            builder.Append(' ');
            if (value > 0)
            {
                builder.Append('+');
            }

            builder.Append(value);
        }

        void OnDestroy()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueClicked);
            }

            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                if (_choiceButtons[i] != null)
                {
                    _choiceButtons[i].onClick.RemoveListener(_choiceHandlers[i]);
                    Destroy(_choiceButtons[i].gameObject);
                }
            }
        }
    }
}
