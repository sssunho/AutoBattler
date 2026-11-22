using RaggedBanner.Animations;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
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
        CharacterAnimationProfile _animationProfile;

        [SerializeField]
        Transform _overlayAnchor;

        [SerializeField]
        Vector3 _overlayOffset = new Vector3(0f, 2.1f, 0f);

        [SerializeField]
        [Min(0.0001f)]
        float _moveSpeed = 3f;

        [SerializeField] GameObject _projectilePrefab;
        [SerializeField] Transform _projectileOrigin;
        [SerializeField] Vector3 _projectileOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] float _projectileSpeed = 8f;
        [SerializeField] float _projectileTargetHeight = 1.2f;

        readonly CharacterAnimationPlayer _animationPlayer = new CharacterAnimationPlayer();
        HexCharacter _character;
        HexMapView _mapView;
        Quaternion _modelRotationOffset = Quaternion.identity;
        HexMovementStep _step;
        ulong _attackRequestId;
        bool _isPresenting;
        bool _isAttacking;
        bool _isLeaping;
        float _leapElapsed;
        float _leapDuration;
        uint _disableVersion;

        public int UnitId => _character != null ? _character.Id : 0;
        public bool IsInitialized => _character != null;
        public bool IsPresenting => _isPresenting || _isAttacking;
        internal uint DisableVersion => _disableVersion;
        internal GameObject ProjectilePrefab => _projectilePrefab;
        internal Vector3 ProjectileOrigin => _projectileOrigin != null ? _projectileOrigin.position : transform.TransformPoint(_projectileOffset);
        internal float ProjectileSpeed => _projectileSpeed;
        internal float ProjectileTargetHeight => _projectileTargetHeight;
        internal Vector3 OverlayWorldPosition => _overlayAnchor != null
            ? _overlayAnchor.position : transform.TransformPoint(_overlayOffset);

        internal bool IsReady => isActiveAndEnabled && _character != null && _mapView != null
            && _model != null && _animationPlayer.IsReady && _moveSpeed > 0f
            && !float.IsNaN(_moveSpeed) && !float.IsInfinity(_moveSpeed);

        internal bool TryGetInitialFacing(HexMapView mapView, out HexDirection facing)
        {
            facing = HexDirection.East;
            ResolveModelAndAnimator();

            if (mapView == null || _model == null || !_model.IsChildOf(transform))
            {
                return false;
            }

            Vector3 up = mapView.transform.up;
            Vector3 forward = Vector3.ProjectOnPlane(_model.forward, up);

            if (!(forward.sqrMagnitude > 0.000001f))
            {
                return false;
            }

            forward.Normalize();
            Vector3 nearestDirection = Vector3.zero;
            float nearestDot = float.NegativeInfinity;

            for (int i = 0; i < 6; i++)
            {
                HexDirection candidate = (HexDirection)i;
                Vector3 direction = GetFacingDirection(mapView, candidate);

                if (!(direction.sqrMagnitude > 0f))
                {
                    return false;
                }

                float dot = Vector3.Dot(forward, direction);

                if (dot > nearestDot + 0.000001f
                    || Mathf.Abs(dot - nearestDot) <= 0.000001f
                    && Vector3.Dot(up, Vector3.Cross(nearestDirection, direction)) > 0f)
                {
                    facing = candidate;
                    nearestDirection = direction;
                    nearestDot = dot;
                }
            }

            return !float.IsNaN(nearestDot) && !float.IsInfinity(nearestDot);
        }

        internal bool TryBind(HexCharacter character, HexMapView mapView)
        {
            if (_character != null || character == null || mapView == null || !isActiveAndEnabled)
            {
                return false;
            }

            ResolveModelAndAnimator();
            Quaternion modelRotationOffset = _model != null
                ? Quaternion.Inverse(transform.rotation) * _model.rotation : Quaternion.identity;

            if (!TryInitializeAnimation())
            {
                return false;
            }

            _character = character;
            _mapView = mapView;
            _modelRotationOffset = modelRotationOffset;
            SnapToCharacter();
            return true;
        }

        internal void AdvanceAnimation(float deltaTime)
        {
            if (isActiveAndEnabled && _character != null)
            {
                _animationPlayer.TryAdvance(deltaTime);
            }
        }

        internal bool TryPlayMovement(HexMovementStep step)
        {
            if (!IsReady || _isPresenting || _isAttacking || step.UnitId != _character.Id)
            {
                return false;
            }

            _step = step;
            _isLeaping = false;
            _isPresenting = true;
            transform.position = _mapView.AxialToWorld(step.From);
            ApplyFacing();

            if (!SetWalking(true))
            {
                _isPresenting = false;
                return false;
            }

            return true;
        }

        internal bool TryPlayLeap(HexMovementStep step)
        {
            if (!IsReady || _isPresenting || _isAttacking || step.UnitId != _character.Id)
            {
                return false;
            }

            _step = step;
            _leapElapsed = 0f;
            _leapDuration = Mathf.Max(0.1f,
                Vector3.Distance(_mapView.AxialToWorld(step.From), _mapView.AxialToWorld(step.To)) / _moveSpeed);
            _isLeaping = true;
            _isPresenting = true;
            transform.position = _mapView.AxialToWorld(step.From);
            ApplyFacing();

            if (!SetWalking(false))
            {
                _isPresenting = false;
                _isLeaping = false;
                return false;
            }

            return true;
        }

        internal float UpdateMovementPresentation(float deltaTime)
        {
            if (!_isPresenting || deltaTime <= 0f)
            {
                return deltaTime;
            }

            if (_isLeaping)
            {
                float consumed = Mathf.Min(deltaTime, _leapDuration - _leapElapsed);
                _leapElapsed += consumed;
                float progress = Mathf.Clamp01(_leapElapsed / _leapDuration);
                Vector3 origin = _mapView.AxialToWorld(_step.From);
                Vector3 landing = _mapView.AxialToWorld(_step.To);
                Vector3 height = _mapView.transform.TransformVector(Vector3.up * _mapView.HexSize);
                transform.position = Vector3.Lerp(origin, landing, progress)
                    + height * (4f * progress * (1f - progress));

                if (_leapElapsed >= _leapDuration)
                {
                    transform.position = landing;
                    _isPresenting = false;
                    _isLeaping = false;
                }

                return Mathf.Max(0f, deltaTime - consumed);
            }

            Vector3 destination = _mapView.AxialToWorld(_step.To);
            Vector3 offset = destination - transform.position;
            float distance = offset.magnitude;

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
            _isAttacking = false;
            _attackRequestId = 0;
            SetWalking(false);
        }

        internal bool ValidateAttackAnimation()
        {
            return IsReady;
        }

        internal bool TryPlayAttack(HexAttackStep step)
        {
            if (_isPresenting || _isAttacking || _character == null
                || step.AttackerId != _character.Id || !ValidateAttackAnimation())
            {
                return false;
            }

            ApplyFacing();

            if (!_animationPlayer.TryPlayAttack(out _attackRequestId))
            {
                return false;
            }

            _isAttacking = true;

            if (!TryReadAttackCompletion(out _))
            {
                SetIdle();
                return false;
            }

            return true;
        }

        internal bool TryConsumeAttackHit(out AnimationAttackKind kind)
        {
            kind = default;
            return _isAttacking && IsReady && _animationPlayer.TryConsumeAttackHit(_attackRequestId, out kind);
        }

        internal bool TryReadAttackCompletion(out bool completed)
        {
            completed = false;

            if (!_isAttacking || !IsReady)
            {
                return false;
            }

            AnimationPlaybackStatus status = _animationPlayer.GetAttackStatus(_attackRequestId);

            if (status != AnimationPlaybackStatus.Playing && status != AnimationPlaybackStatus.Completed)
            {
                return false;
            }

            completed = status == AnimationPlaybackStatus.Completed;
            return true;
        }

        internal void SnapToCharacter()
        {
            ResetMovementPresentation();
            SetIdle();
        }

        void ResetMovementPresentation()
        {
            if (_character != null && _mapView != null && _mapView.IsValid(_character.Coordinate))
            {
                transform.position = _mapView.AxialToWorld(_character.Coordinate);
                ApplyFacing();
            }

            _isPresenting = false;
            _isLeaping = false;
            _leapElapsed = 0f;
            _leapDuration = 0f;
        }

        internal void ApplyFacing()
        {
            if (_character == null || _mapView == null || _model == null)
            {
                return;
            }

            Vector3 direction = GetFacingDirection(_mapView, _character.Facing);

            if (direction.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(direction, _mapView.transform.up)
                    * Quaternion.Inverse(_modelRotationOffset);
            }
        }

        internal void Unbind()
        {
            ReleaseAnimation();
            _character = null;
            _mapView = null;
        }

        void OnEnable()
        {
            if (_character == null)
            {
                return;
            }

            if (TryInitializeAnimation())
            {
                SnapToCharacter();
            }
        }

        void OnDisable()
        {
            unchecked
            {
                _disableVersion++;
            }

            ReleaseAnimation();
        }

        void OnDestroy()
        {
            ReleaseAnimation();
        }

        bool SetWalking(bool walking)
        {
            return walking ? _animationPlayer.TryPlayMove() : _animationPlayer.TryPlayIdle();
        }

        bool TryInitializeAnimation()
        {
            ResolveModelAndAnimator();

            if (_model == null || !_model.IsChildOf(transform) || _animator == null
                || !_animator.transform.IsChildOf(_model) || _animationProfile == null
                || _moveSpeed <= 0f || float.IsNaN(_moveSpeed) || float.IsInfinity(_moveSpeed))
            {
                Debug.LogError("HexCharacterView requires a model, Animator, animation profile, and a positive finite move speed.", this);
                return false;
            }

            if (!_animationPlayer.TryInitialize(_animator, _animationProfile, out _))
            {
                Debug.LogError("HexCharacterView could not initialize its animation profile.", this);
                return false;
            }

            return true;
        }

        void ReleaseAnimation()
        {
            _animationPlayer.Dispose();
            _attackRequestId = 0;
            _isAttacking = false;
            ResetMovementPresentation();
        }

        static Vector3 GetFacingDirection(HexMapView mapView, HexDirection facing)
        {
            Vector2 localDirection = HexLayout.AxialToLocal(default(HexCoordinate).GetNeighbor(facing), 1f);
            Vector3 direction = Vector3.ProjectOnPlane(
                mapView.transform.TransformVector(new Vector3(localDirection.x, 0f, localDirection.y)),
                mapView.transform.up);
            float magnitude = direction.magnitude;

            if (!(magnitude > 0f) || float.IsInfinity(magnitude))
            {
                return Vector3.zero;
            }

            return direction / magnitude;
        }

        void ResolveModelAndAnimator()
        {
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
        }
    }
}
