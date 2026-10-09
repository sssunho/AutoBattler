using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoBattler
{
    public sealed class CameraLocalMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        float _moveSpeed = 2f;

        [SerializeField]
        float _horizontalMin = -5f;

        [SerializeField, Min(0f)]
        float _horizontalMax = 5f;

        [SerializeField]
        float _verticalMin = -3f;

        [SerializeField, Min(0f)]
        float _verticalMax = 3f;

        [SerializeField, Min(0.01f)]
        float _minOrthographicSize = 1f;

        [SerializeField, Min(0.01f)]
        float _maxOrthographicSize = 6f;

        [SerializeField, Min(0f)]
        float _zoomSpeed = 0.5f;

        [SerializeField, Min(0.01f)]
        float _moveSharpness = 10f;

        [SerializeField, Min(0.01f)]
        float _zoomSharpness = 10f;

        Camera _camera;
        Vector3 _startPosition;
        Quaternion _startRotation;
        float _targetSize;
        Vector2 _targetOffset;
        Vector2 _offset;

        void Start()
        {
            _camera = GetComponent<Camera>();

            if (_camera == null || !_camera.orthographic)
            {
                enabled = false;
                return;
            }

            _startPosition = transform.position;
            _startRotation = transform.rotation;
            float maxSize = Mathf.Max(0.01f, _maxOrthographicSize);
            float minSize = Mathf.Min(Mathf.Max(0.01f, _minOrthographicSize), maxSize);
            _targetSize = Mathf.Clamp(_camera.orthographicSize, minSize, maxSize);
            _camera.orthographicSize = _targetSize;
        }

        void Update()
        {
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

            float maxSize = Mathf.Max(0.01f, _maxOrthographicSize);
            float minSize = Mathf.Min(Mathf.Max(0.01f, _minOrthographicSize), maxSize);
            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                _targetSize -= mouse.scroll.ReadValue().y * Mathf.Max(0f, _zoomSpeed);
            }

            _targetSize = Mathf.Clamp(_targetSize, minSize, maxSize);
            float zoomFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, _zoomSharpness) * Time.deltaTime);
            float size = Mathf.Lerp(_camera.orthographicSize, _targetSize, zoomFactor);

            if (Mathf.Abs(size - _targetSize) < 0.0001f)
            {
                size = _targetSize;
            }

            size = Mathf.Clamp(size, minSize, maxSize);
            _camera.orthographicSize = size;

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

                _targetOffset += direction * (_moveSpeed * Time.deltaTime);
            }

            float horizontalMin = Mathf.Min(0f, _horizontalMin);
            float horizontalMax = Mathf.Max(0f, _horizontalMax);
            float verticalMin = Mathf.Min(0f, _verticalMin);
            float verticalMax = Mathf.Max(0f, _verticalMax);
            float aspect = Mathf.Max(0.0001f, _camera.aspect);
            float targetVerticalExtension = maxSize - _targetSize;
            float targetHorizontalExtension = targetVerticalExtension * aspect;
            _targetOffset.x = Mathf.Clamp(_targetOffset.x,
                horizontalMin - targetHorizontalExtension, horizontalMax + targetHorizontalExtension);
            _targetOffset.y = Mathf.Clamp(_targetOffset.y,
                verticalMin - targetVerticalExtension, verticalMax + targetVerticalExtension);

            float moveFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, _moveSharpness) * Time.deltaTime);
            _offset = Vector2.Lerp(_offset, _targetOffset, moveFactor);

            if ((_offset - _targetOffset).sqrMagnitude < 0.00000001f)
            {
                _offset = _targetOffset;
            }

            float verticalExtension = maxSize - size;
            float horizontalExtension = verticalExtension * aspect;
            _offset.x = Mathf.Clamp(_offset.x,
                horizontalMin - horizontalExtension, horizontalMax + horizontalExtension);
            _offset.y = Mathf.Clamp(_offset.y,
                verticalMin - verticalExtension, verticalMax + verticalExtension);

            transform.SetPositionAndRotation(
                _startPosition + _startRotation * new Vector3(_offset.x, _offset.y, 0f),
                _startRotation);
        }
    }
}
