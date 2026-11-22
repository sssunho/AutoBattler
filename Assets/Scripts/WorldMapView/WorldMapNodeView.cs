using System;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.WorldMap.Unity
{
    public sealed class WorldMapNodeView : MonoBehaviour
    {
        int _locationId;
        Button _button;
        Image _icon;
        Text _label;
        Image _selectionIndicator;
        Vector2 _iconSize;
        bool _hasIcon;

        internal event Action<int> Clicked;

        internal void Initialize(WorldMapLocationDefinition location, bool isStartingCity, Font font)
        {
            _locationId = location.Id;
            Vector2 size = location.IconSize;
            _iconSize = size;
            _hasIcon = location.Icon != null;
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = location.Position;
            rect.anchorMax = location.Position;
            rect.sizeDelta = size;

            RectTransform symbol = WorldMapView.CreateRect("Symbol", rect, size);
            _icon = symbol.gameObject.AddComponent<Image>();
            _icon.sprite = location.Icon;
            _icon.preserveAspect = true;
            _icon.raycastTarget = true;

            if (location.Icon == null)
            {
                symbol.localRotation = Quaternion.Euler(0f, 0f, 45f);
                symbol.sizeDelta *= 0.72f;
            }

            Outline outline = symbol.gameObject.AddComponent<Outline>();
            outline.enabled = location.Icon == null;
            outline.effectColor = new Color(0.12f, 0.1f, 0.07f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);

            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _icon;
            _button.transition = Selectable.Transition.None;
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
            _button.onClick.AddListener(OnClicked);

            RectTransform indicatorRect = WorldMapView.CreateRect("Selection", rect, new Vector2(10f, 10f));
            indicatorRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _selectionIndicator = indicatorRect.gameObject.AddComponent<Image>();
            _selectionIndicator.raycastTarget = false;
            _selectionIndicator.color = Color.clear;
            Outline indicatorOutline = indicatorRect.gameObject.AddComponent<Outline>();
            indicatorOutline.effectColor = new Color(0.15f, 0.12f, 0.08f, 0.8f);
            indicatorOutline.effectDistance = new Vector2(1f, -1f);

            RectTransform labelRect = WorldMapView.CreateRect("Label", rect, new Vector2(380f, 54f));
            labelRect.anchoredPosition = new Vector2(0f, -size.y * 0.5f - 37f);
            _label = labelRect.gameObject.AddComponent<Text>();
            _label.font = font;
            _label.fontSize = 32;
            _label.fontStyle = isStartingCity ? FontStyle.Bold : FontStyle.Normal;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.text = location.DisplayName;
            _label.raycastTarget = false;
            Outline labelOutline = labelRect.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(0.08f, 0.06f, 0.04f, 1f);
            labelOutline.effectDistance = new Vector2(2f, -2f);
        }

        internal void SetState(bool isCurrent, bool canTravel, bool isDestination)
        {
            _button.interactable = canTravel;
            Color stateColor;

            if (isCurrent)
            {
                stateColor = new Color(0.25f, 0.85f, 0.85f, 1f);
            }
            else if (isDestination)
            {
                stateColor = new Color(1f, 0.94f, 0.67f, 1f);
            }
            else if (canTravel)
            {
                stateColor = new Color(1f, 0.76f, 0.25f, 1f);
            }
            else
            {
                stateColor = new Color(0.42f, 0.36f, 0.26f, 1f);
            }

            _icon.color = _hasIcon ? Color.white : stateColor;
            _selectionIndicator.color = isCurrent || canTravel || isDestination ? stateColor : Color.clear;

            _label.color = isCurrent || canTravel || isDestination
                ? new Color(1f, 0.96f, 0.83f, 1f)
                : new Color(0.78f, 0.74f, 0.64f, 1f);
        }

        internal void SetZoom(float zoom)
        {
            if (zoom <= 0f || float.IsNaN(zoom) || float.IsInfinity(zoom))
            {
                return;
            }
            float inverseZoom = 1f / zoom;
            Vector3 scale = new Vector3(inverseZoom, inverseZoom, 1f);
            _label.rectTransform.localScale = scale;
            _label.rectTransform.anchoredPosition = new Vector2(0f, -_iconSize.y * 0.5f - 37f * inverseZoom);
            _selectionIndicator.rectTransform.localScale = scale;
            _selectionIndicator.rectTransform.anchoredPosition = _iconSize * 0.5f
                + Vector2.one * (8f * inverseZoom);
        }

        internal void DisableInput()
        {
            _button.interactable = false;
        }

        void OnClicked()
        {
            Clicked?.Invoke(_locationId);
        }

        void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }
        }
    }
}
