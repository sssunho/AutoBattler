using System;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace AutoBattler.Combat.Unity
{
    public sealed class AutoBattlerBattleUnitView : MonoBehaviour
    {
        static readonly int _idleState = Animator.StringToHash("Base Layer.Idle");
        static readonly int _walkState = Animator.StringToHash("Base Layer.Walk");
        static readonly int _attackState = Animator.StringToHash("Base Layer.Attack");

        [SerializeField]
        Animator _animator;

        [SerializeField]
        AnimationClip _attackAnimationClip;

        AutoBattlerBattleUnit _unit;
        HexMapView _mapView;
        HexCoordinate _displayCoordinate;
        Vector3 _moveStart;
        Vector3 _moveDestination;
        float _moveDuration;
        float _moveElapsed;
        bool _isMoving;
        bool _isWalking;
        AutoBattlerBattleAttackPhase _displayedAttackPhase;

        public AutoBattlerBattleUnit Unit => _unit;

        void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }
        }

        void LateUpdate()
        {
            if (SyncAttackAnimation())
            {
                return;
            }

            SetWalking(_isMoving);
        }

        internal void AdvanceMovement(float deltaTime)
        {
            if (_isMoving)
            {
                _moveElapsed += deltaTime;
                float progress = Mathf.Clamp01(_moveElapsed / _moveDuration);
                transform.position = Vector3.Lerp(_moveStart, _moveDestination, progress);

                if (progress >= 1f)
                {
                    _isMoving = false;
                }
            }
        }

        void OnDisable()
        {
            _isMoving = false;
            _moveElapsed = 0f;
            _displayedAttackPhase = AutoBattlerBattleAttackPhase.None;

            if (_animator != null)
            {
                _animator.speed = 1f;
            }

            SetWalking(false, true);
        }

        internal void ShowPlacement(HexCoordinate coordinate, HexMapView mapView)
        {
            _unit = null;
            _mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
            SnapTo(coordinate);
        }

        public void Bind(AutoBattlerBattleUnit unit, HexMapView mapView)
        {
            _unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
            SyncPosition();
        }

        public void SyncPosition()
        {
            if (_unit == null || _mapView == null)
            {
                return;
            }

            SnapTo(_unit.Coordinate);
        }

        internal void SyncMovement(float duration)
        {
            if (_unit == null || _mapView == null || _displayCoordinate == _unit.Coordinate)
            {
                return;
            }

            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                SnapTo(_unit.Coordinate);
                return;
            }

            _displayCoordinate = _unit.Coordinate;
            _moveStart = transform.position;
            _moveDestination = _mapView.AxialToWorld(_displayCoordinate);
            _moveDuration = duration;
            _moveElapsed = 0f;
            _isMoving = _moveStart != _moveDestination;

            Vector3 direction = Vector3.ProjectOnPlane(_moveDestination - _moveStart, _mapView.transform.up);

            if (direction.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, _mapView.transform.up);
            }
        }

        void SnapTo(HexCoordinate coordinate)
        {
            _displayCoordinate = coordinate;
            transform.position = _mapView.AxialToWorld(coordinate);
            _isMoving = false;
            _moveElapsed = 0f;
            SetWalking(false);
        }

        bool SyncAttackAnimation()
        {
            if (_unit == null || _animator == null || _attackAnimationClip == null
                || _animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AutoBattlerBattleAttackPhase attackPhase = _unit.AttackPhase;

            if (attackPhase == AutoBattlerBattleAttackPhase.Animation)
            {
                if (_displayedAttackPhase != AutoBattlerBattleAttackPhase.Animation)
                {
                    FaceAttackTarget();
                    _animator.speed = _attackAnimationClip.length / _unit.AttackAnimationDuration;
                    _animator.CrossFadeInFixedTime(_attackState, 0.2f, 0, 0f);
                    _displayedAttackPhase = AutoBattlerBattleAttackPhase.Animation;
                }

                return true;
            }

            if (attackPhase == AutoBattlerBattleAttackPhase.Cooldown)
            {
                if (_displayedAttackPhase != AutoBattlerBattleAttackPhase.Cooldown)
                {
                    if (_displayedAttackPhase == AutoBattlerBattleAttackPhase.None)
                    {
                        FaceAttackTarget();
                    }

                    _animator.Play(_attackState, 0, 1f);
                    _animator.speed = 0f;
                    _displayedAttackPhase = AutoBattlerBattleAttackPhase.Cooldown;
                }

                return true;
            }

            if (_displayedAttackPhase != AutoBattlerBattleAttackPhase.None)
            {
                _animator.speed = 1f;
                _displayedAttackPhase = AutoBattlerBattleAttackPhase.None;
                SetWalking(_isMoving, true);
            }

            return false;
        }

        void FaceAttackTarget()
        {
            if (_unit.AttackTarget == null || _mapView == null)
            {
                return;
            }

            Vector3 targetPosition = _mapView.AxialToWorld(_unit.AttackTarget.Coordinate);
            Vector3 direction = Vector3.ProjectOnPlane(
                targetPosition - transform.position,
                _mapView.transform.up);

            if (direction.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, _mapView.transform.up);
            }
        }

        void SetWalking(bool walking, bool force = false)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null
                || (!force && _isWalking == walking))
            {
                return;
            }

            _isWalking = walking;
            _animator.CrossFadeInFixedTime(walking ? _walkState : _idleState, 0.2f, 0, 0f);
        }
    }
}
