using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RaggedBanner.WorldMap.Unity
{
    public sealed class WorldMapViewportController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        RectTransform _viewport;

        [SerializeField]
        Image _mapImage;

        [SerializeField]
        Vector2 _initialCenter = new Vector2(0.5f, 0.5f);

        [SerializeField, Min(0f)]
        float _panSpeed = 800f;

        [SerializeField, Min(0f)]
        float _zoomSensitivity = 0.0015f;

        [SerializeField, Min(0.01f)]
        float _followSpeed = 12f;

        RectTransform _mapRect;
        Vector2 _viewportSize;
        Vector2 _mapSize;
        Vector2 _offset;
        Vector2 _targetOffset;
        Vector2 _lastPointerPosition;
        Vector2 _pendingDragDelta;
        float _minZoom;
        float _maxZoom;
        float _zoom;
        float _targetZoom;
        bool _hasInitialized;
        bool _isDragging;
        int _dragPointerId;

        internal event Action<float> ZoomChanged;

        void OnEnable()
        {
            if (_viewport == null || _mapImage == null || _mapImage.sprite == null)
            {
                enabled = false;
                return;
            }

            _mapRect = _mapImage.rectTransform;
            _mapImage.SetNativeSize();
            StopMovement();
            Canvas.willRenderCanvases += OnBeforeCanvasRender;
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= OnBeforeCanvasRender;
            StopMovement();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                StopMovement();
            }
        }

        void OnBeforeCanvasRender()
        {
            RefreshViewport();
        }

        void Update()
        {
            if (!_hasInitialized || !Application.isFocused || !RefreshViewport())
            {
                StopMovement();
                return;
            }

            Vector2 panDelta = Vector2.zero;
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                float horizontal = (keyboard.dKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed ? 1f : 0f);
                float vertical = (keyboard.wKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed ? 1f : 0f);
                Vector2 direction = new Vector2(horizontal, vertical);

                if (direction.sqrMagnitude > 1f)
                {
                    direction.Normalize();
                }

                panDelta -= direction * (Mathf.Max(0f, _panSpeed) * Time.unscaledDeltaTime);
            }

            UpdateMouse();
            panDelta += _pendingDragDelta;
            _pendingDragDelta = Vector2.zero;
            _targetOffset = ClampOffset(_targetOffset + panDelta, _targetZoom);

            float followAmount = 1f - Mathf.Exp(-Mathf.Max(0.01f, _followSpeed) * Time.unscaledDeltaTime);
            _offset = Vector2.Lerp(_offset, _targetOffset, followAmount);
            _zoom = Mathf.Lerp(_zoom, _targetZoom, followAmount);
            ApplyView();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || !_hasInitialized || !Application.isFocused
                || eventData.button != PointerEventData.InputButton.Left
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _viewport, eventData.pressPosition, eventData.pressEventCamera, out _lastPointerPosition))
            {
                return;
            }

            _dragPointerId = eventData.pointerId;
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData.pointerId != _dragPointerId
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!isActiveAndEnabled || !Application.isFocused
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _viewport, eventData.position, eventData.pressEventCamera, out Vector2 pointerPosition)
                || !_viewport.rect.Contains(pointerPosition))
            {
                _isDragging = false;
                _pendingDragDelta = Vector2.zero;
                return;
            }

            _pendingDragDelta += pointerPosition - _lastPointerPosition;
            _lastPointerPosition = pointerPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _dragPointerId && eventData.button == PointerEventData.InputButton.Left)
            {
                _isDragging = false;
            }
        }

        void UpdateMouse()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _viewport, mouse.position.ReadValue(), null, out Vector2 pointerPosition)
                || !_viewport.rect.Contains(pointerPosition))
            {
                _isDragging = false;
                _pendingDragDelta = Vector2.zero;
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;

            if (scroll != 0f)
            {
                float nextZoom = Mathf.Clamp(
                    _targetZoom * Mathf.Exp(scroll * Mathf.Max(0f, _zoomSensitivity)), _minZoom, _maxZoom);

                if (nextZoom != _targetZoom)
                {
                    _targetOffset = pointerPosition - (pointerPosition - _offset) * (nextZoom / _zoom);
                    _targetZoom = nextZoom;
                }
            }
        }

        bool RefreshViewport()
        {
            if (_viewport == null || _mapRect == null)
            {
                return false;
            }

            Vector2 viewportSize = _viewport.rect.size;
            Vector2 mapSize = _mapRect.rect.size;

            if (viewportSize.x <= 0f || viewportSize.y <= 0f || mapSize.x <= 0f || mapSize.y <= 0f)
            {
                return false;
            }

            if (_hasInitialized && viewportSize == _viewportSize && mapSize == _mapSize)
            {
                return true;
            }

            _viewportSize = viewportSize;
            _mapSize = mapSize;
            _minZoom = Mathf.Max(viewportSize.x / mapSize.x, viewportSize.y / mapSize.y);
            _maxZoom = Mathf.Max(_minZoom, 1f);

            if (_hasInitialized)
            {
                float nextZoom = Mathf.Clamp(_zoom, _minZoom, _maxZoom);
                _offset *= nextZoom / _zoom;
                _zoom = nextZoom;
            }
            else
            {
                _zoom = Mathf.Clamp(1f, _minZoom, _maxZoom);
                Vector2 initialCenter = new Vector2(Mathf.Clamp01(_initialCenter.x), Mathf.Clamp01(_initialCenter.y));
                _offset = Vector2.Scale(Vector2.one * 0.5f - initialCenter, _mapSize) * _zoom;
                _hasInitialized = true;
            }

            ApplyView();
            StopMovement();
            return true;
        }

        void StopMovement()
        {
            _targetOffset = _offset;
            _targetZoom = _zoom;
            _isDragging = false;
            _pendingDragDelta = Vector2.zero;
        }

        Vector2 ClampOffset(Vector2 offset, float zoom)
        {
            float horizontalLimit = Mathf.Max(0f, (_mapSize.x * zoom - _viewportSize.x) * 0.5f);
            float verticalLimit = Mathf.Max(0f, (_mapSize.y * zoom - _viewportSize.y) * 0.5f);
            offset.x = Mathf.Clamp(offset.x, -horizontalLimit, horizontalLimit);
            offset.y = Mathf.Clamp(offset.y, -verticalLimit, verticalLimit);
            return offset;
        }

        void ApplyView()
        {
            _offset = ClampOffset(_offset, _zoom);
            bool zoomChanged = _mapRect.localScale.x != _zoom;
            _mapRect.localScale = new Vector3(_zoom, _zoom, 1f);
            _mapRect.anchoredPosition = _offset;

            if (zoomChanged)
            {
                ZoomChanged?.Invoke(_zoom);
            }
        }
    }
}
