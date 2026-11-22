using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class HexCharacterOverlayView : MonoBehaviour
    {
        [SerializeField]
        Image _turnMarker;

        [SerializeField]
        Image _healthSegmentTemplate;

        [SerializeField]
        RectTransform _healthRoot;

        [SerializeField]
        Text _combatTextTemplate;

        [SerializeField]
        Vector2 _segmentSize = new Vector2(4f, 16f);

        [SerializeField]
        [Min(0f)]
        float _segmentSpacing = 2f;

        Image[] _segments;
        Image _stealthMarker;
        int _lastHealth;
        bool _lastCurrentTurn;

        internal Text CombatTextTemplate => _combatTextTemplate;

        internal bool TryInitialize(int segmentCount)
        {
            if (_segments != null || segmentCount <= 0 || _turnMarker == null
                || _healthSegmentTemplate == null || _healthRoot == null
                || _turnMarker == _healthSegmentTemplate
                || _turnMarker.transform == transform || !_turnMarker.transform.IsChildOf(transform)
                || _turnMarker.transform.IsChildOf(_healthSegmentTemplate.transform)
                || _healthRoot == transform || !_healthRoot.IsChildOf(transform)
                || _healthRoot.IsChildOf(_turnMarker.transform)
                || _healthSegmentTemplate.transform.parent != _healthRoot
                || !IsPositiveFinite(_segmentSize.x) || !IsPositiveFinite(_segmentSize.y)
                || _segmentSpacing < 0f || float.IsNaN(_segmentSpacing) || float.IsInfinity(_segmentSpacing))
            {
                return false;
            }

            float width = _segmentSize.x * segmentCount + _segmentSpacing * (segmentCount - 1);

            if (!IsPositiveFinite(width))
            {
                return false;
            }

            _turnMarker.raycastTarget = false;
            _turnMarker.gameObject.SetActive(false);
            _healthSegmentTemplate.raycastTarget = false;
            _healthSegmentTemplate.gameObject.SetActive(false);

            if (_combatTextTemplate != null)
            {
                _combatTextTemplate.raycastTarget = false;
                _combatTextTemplate.gameObject.SetActive(false);
            }

            _healthRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _healthRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _segmentSize.y);
            _segments = new Image[segmentCount];
            float firstPosition = (_segmentSize.x - width) * 0.5f;
            float stride = _segmentSize.x + _segmentSpacing;

            for (int i = 0; i < segmentCount; i++)
            {
                Image segment = Instantiate(_healthSegmentTemplate, _healthRoot, false);
                RectTransform rectTransform = segment.rectTransform;
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = _segmentSize;
                rectTransform.anchoredPosition3D = new Vector3(firstPosition + stride * i, 0f, 0f);
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
                segment.raycastTarget = false;
                segment.gameObject.SetActive(true);
                _segments[i] = segment;
            }

            _lastHealth = segmentCount;
            _lastCurrentTurn = false;
            return true;
        }

        internal void SetStatus(int health, bool isCurrentTurn)
        {
            if (_segments == null)
            {
                return;
            }

            int visibleSegments = Mathf.Clamp(health, 0, _segments.Length);

            if (_lastHealth != visibleSegments)
            {
                int start = Mathf.Min(_lastHealth, visibleSegments);
                int end = Mathf.Max(_lastHealth, visibleSegments);

                for (int i = start; i < end; i++)
                {
                    _segments[i].gameObject.SetActive(i < visibleSegments);
                }

                _lastHealth = visibleSegments;
            }

            if (_lastCurrentTurn != isCurrentTurn)
            {
                _turnMarker.gameObject.SetActive(isCurrentTurn);
                _lastCurrentTurn = isCurrentTurn;
            }
        }

        internal void SetStealthStatus(bool isStealthed, bool isInvisible = false)
        {
            if (_segments == null)
            {
                return;
            }

            if (isStealthed && _stealthMarker == null)
            {
                GameObject markerObject = new GameObject("Stealth Marker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(transform, false);
                _stealthMarker = markerObject.GetComponent<Image>();
                _stealthMarker.raycastTarget = false;
                _stealthMarker.color = new Color(0.35f, 0.8f, 1f, 1f);
                RectTransform markerRect = _stealthMarker.rectTransform;
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.sizeDelta = new Vector2(9f, 9f);
                markerRect.anchoredPosition = new Vector2(-20f, 24f);
                markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            if (_stealthMarker != null && _stealthMarker.gameObject.activeSelf != isStealthed)
            {
                _stealthMarker.gameObject.SetActive(isStealthed);
            }

            if (_stealthMarker != null && isStealthed)
            {
                _stealthMarker.color = isInvisible ? new Color(0.8f, 0.45f, 1f, 1f) : new Color(0.35f, 0.8f, 1f, 1f);
            }
        }

        static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
