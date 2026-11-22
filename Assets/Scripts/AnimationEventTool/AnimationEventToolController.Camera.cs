#if UNITY_EDITOR
using UnityEngine;

namespace RaggedBanner.AnimationTools
{
    public sealed partial class AnimationEventToolController
    {
        Camera _viewCamera;
        Vector3 _originalViewPosition;
        Quaternion _originalViewRotation;
        Vector3 _ownedViewPosition;
        Quaternion _ownedViewRotation;
        float _viewYaw;
        float _viewPitch;
        float _viewDistance;
        float _defaultViewYaw;
        float _defaultViewPitch;
        float _defaultViewDistance;
        bool _ownsViewPose;
        int _dragControl;
        UnityEditor.EditorWindow _dragWindow;
        Vector2Int _viewScreenSize;

        bool CanControlCamera => _configured && !_blocked && _pendingAction == null && !_showClipPicker
            && _prefab != null && _preview.IsReady && _viewCamera != null && _viewCamera == _previewCamera
            && _viewCamera.isActiveAndEnabled && _previewRoot != null;

        void CaptureCameraView()
        {
            _viewCamera = _previewCamera;
            _originalViewPosition = _viewCamera.transform.position;
            _originalViewRotation = _viewCamera.transform.rotation;
            Vector3 offset = _originalViewPosition - CameraCenter();
            _defaultViewDistance = Mathf.Clamp(offset.magnitude, 1.5f, 10f);
            Quaternion look = offset.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-offset) : _originalViewRotation;
            _defaultViewYaw = look.eulerAngles.y;
            _defaultViewPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, look.eulerAngles.x), 15f, 80f);
            _viewYaw = _defaultViewYaw;
            _viewPitch = _defaultViewPitch;
            _viewDistance = _defaultViewDistance;
            _viewScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        Vector3 CameraCenter()
        {
            return _previewRoot.TransformPoint(new Vector3(0f, 0.9f, 0f));
        }

        void ResetCameraView()
        {
            CancelCameraDrag();

            if (!CanControlCamera)
            {
                return;
            }

            _viewYaw = _defaultViewYaw;
            _viewPitch = _defaultViewPitch;
            _viewDistance = _defaultViewDistance;
            ApplyCameraView();
        }

        void AdjustCameraView(Vector2 rotation, float scroll)
        {
            _viewYaw = Mathf.Repeat(_viewYaw + rotation.x * 0.35f, 360f);
            _viewPitch = Mathf.Clamp(_viewPitch - rotation.y * 0.35f, 15f, 80f);
            _viewDistance = Mathf.Clamp(_viewDistance * Mathf.Exp(Mathf.Clamp(scroll, -20f, 20f) * 0.1f), 1.5f, 10f);
            ApplyCameraView();
        }

        void ApplyCameraView()
        {
            if (_viewCamera == null || _previewRoot == null)
            {
                return;
            }

            _ownedViewRotation = Quaternion.Euler(_viewPitch, _viewYaw, 0f);
            _ownedViewPosition = CameraCenter() - _ownedViewRotation * Vector3.forward * _viewDistance;
            _viewCamera.transform.SetPositionAndRotation(_ownedViewPosition, _ownedViewRotation);
            _ownsViewPose = true;
        }

        void ReleaseCameraView()
        {
            CancelCameraDrag();

            if (_ownsViewPose && _viewCamera != null && _viewCamera.transform.position == _ownedViewPosition
                && _viewCamera.transform.rotation == _ownedViewRotation)
            {
                _viewCamera.transform.SetPositionAndRotation(_originalViewPosition, _originalViewRotation);
            }

            _ownsViewPose = false;
            _viewCamera = null;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelCameraDrag();
            }
        }

        void ValidateCameraDrag()
        {
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);

            if (!CanControlCamera || size != _viewScreenSize || size.x < 900 || size.y < 600
                || (_dragControl != 0 && UnityEditor.EditorWindow.focusedWindow != _dragWindow))
            {
                CancelCameraDrag();
            }

            _viewScreenSize = size;
        }

        void CancelCameraDrag()
        {
            if (_dragControl != 0 && GUIUtility.hotControl == _dragControl)
            {
                GUIUtility.hotControl = 0;
            }

            _dragControl = 0;
            _dragWindow = null;
        }

        void HandlePreviewInput()
        {
            ValidateCameraDrag();
            Event input = Event.current;

            if (input == null)
            {
                return;
            }

            if (input.rawType == EventType.MouseUp || input.type == EventType.MouseLeaveWindow
                || input.type == EventType.Ignore)
            {
                CancelCameraDrag();
            }

            if (!CanControlCamera || Screen.width < 900 || Screen.height < 600)
            {
                return;
            }

            Rect area = GetPreviewArea();
            int control = GUIUtility.GetControlID(FocusType.Passive, area);

            if (input.type == EventType.MouseDown && area.Contains(input.mousePosition) && GUIUtility.hotControl == 0)
            {
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;

                if (input.button == 1)
                {
                    _dragControl = control;
                    _dragWindow = UnityEditor.EditorWindow.focusedWindow;
                    GUIUtility.hotControl = control;
                    input.Use();
                }
            }
            else if (input.type == EventType.MouseDrag && input.button == 1 && _dragControl != 0
                && GUIUtility.hotControl == _dragControl)
            {
                if (area.Contains(input.mousePosition))
                {
                    AdjustCameraView(input.delta, 0f);
                }
                else
                {
                    CancelCameraDrag();
                }

                input.Use();
            }
            else if (input.type == EventType.ScrollWheel && area.Contains(input.mousePosition)
                && GUIUtility.hotControl == 0 && GUIUtility.keyboardControl == 0)
            {
                AdjustCameraView(Vector2.zero, input.delta.y);
                input.Use();
            }
        }
    }
}
#endif
