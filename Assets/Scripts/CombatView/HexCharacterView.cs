using AutoBattler.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class HexCharacterView : MonoBehaviour
    {
        [SerializeField]
        Transform _model;

        [SerializeField]
        Animator _animator;

        [SerializeField]
        [Min(0.0001f)]
        float _moveSpeed = 3f;

        [SerializeField]
        string _idleStateName = "Base Layer.Idle";

        [SerializeField]
        string _moveStateName = "Base Layer.Walk";

        HexCharacter _character;
        HexMapView _mapView;
        HexMovementStep _step;
        int _idleState;
        int _moveState;
        bool _isPresenting;
        bool _isWalking;

        public int UnitId => _character != null ? _character.Id : 0;
        public bool IsInitialized => _character != null;
        public bool IsPresenting => _isPresenting;

        internal bool IsReady => isActiveAndEnabled && _character != null && _mapView != null
            && _model != null && HasAnimator() && _moveSpeed > 0f
            && !float.IsNaN(_moveSpeed) && !float.IsInfinity(_moveSpeed);

        internal bool TryBind(HexCharacter character, HexMapView mapView)
        {
            if (_character != null || character == null || mapView == null || !isActiveAndEnabled)
            {
                return false;
            }

            if (_animator == null)
            {
                _animator = _model != null
                    ? _model.GetComponentInChildren<Animator>()
                    : GetComponentInChildren<Animator>();
            }

            if (_model == null && _animator != null)
            {
                _model = _animator.transform;
            }

            if (_model == null || !_model.IsChildOf(transform)
                || !HasAnimator() || !_animator.transform.IsChildOf(_model)
                || string.IsNullOrWhiteSpace(_idleStateName) || string.IsNullOrWhiteSpace(_moveStateName)
                || _moveSpeed <= 0f || float.IsNaN(_moveSpeed) || float.IsInfinity(_moveSpeed))
            {
                Debug.LogError("HexCharacterView requires a model, Animator, Idle/Move state names, and a positive finite move speed.", this);
                return false;
            }

            _idleState = Animator.StringToHash(_idleStateName);
            _moveState = Animator.StringToHash(_moveStateName);

            if (!_animator.HasState(0, _idleState) || !_animator.HasState(0, _moveState))
            {
                Debug.LogError("HexCharacterView could not find the configured Idle/Move states on Animator layer 0.", this);
                return false;
            }

            _character = character;
            _mapView = mapView;
            _animator.applyRootMotion = false;
            SnapToCharacter();
            _animator.Play(_idleState, 0, 0f);
            return true;
        }

        internal bool TryPlayMovement(HexMovementStep step)
        {
            if (!IsReady || _isPresenting || step.UnitId != _character.Id)
            {
                return false;
            }

            _step = step;
            _isPresenting = true;
            transform.position = _mapView.AxialToWorld(step.From);
            SetWalking(true);
            return true;
        }

        internal float AdvanceMovement(float deltaTime)
        {
            if (!_isPresenting || deltaTime <= 0f)
            {
                return deltaTime;
            }

            Vector3 destination = _mapView.AxialToWorld(_step.To);
            Vector3 offset = destination - transform.position;
            float distance = offset.magnitude;
            Vector3 forward = Vector3.ProjectOnPlane(offset, _mapView.transform.up);

            if (forward.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(forward, _mapView.transform.up);
            }

            float remainingDistance = _moveSpeed * deltaTime;

            if (distance > remainingDistance)
            {
                transform.position += offset * (remainingDistance / distance);
                return 0f;
            }

            transform.position = destination;
            _isPresenting = false;
            return Mathf.Max(0f, deltaTime - distance / _moveSpeed);
        }

        internal void SetIdle()
        {
            SetWalking(false);
        }

        internal void SnapToCharacter()
        {
            if (_character != null && _mapView != null && _mapView.IsValid(_character.Coordinate))
            {
                transform.position = _mapView.AxialToWorld(_character.Coordinate);
            }

            _isPresenting = false;
            SetIdle();
        }

        internal void Unbind()
        {
            SnapToCharacter();
            _character = null;
            _mapView = null;
        }

        void OnDisable()
        {
            SnapToCharacter();
        }

        void SetWalking(bool walking)
        {
            if (_isWalking == walking)
            {
                return;
            }

            _isWalking = walking;

            if (HasAnimator())
            {
                _animator.CrossFadeInFixedTime(walking ? _moveState : _idleState, 0.1f, 0);
            }
        }

        bool HasAnimator()
        {
            return _animator != null && _animator.runtimeAnimatorController != null;
        }
    }
}
