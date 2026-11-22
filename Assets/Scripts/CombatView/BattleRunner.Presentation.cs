using System.Collections.Generic;
using RaggedBanner.Animations;
using RaggedBanner.HexGrid;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    public sealed partial class BattleRunner
    {
        sealed class BattleActionPresentation
        {
            readonly BattleRunner _owner;
            readonly List<BattleAttackResult> _attackResults = new List<BattleAttackResult>();
            bool _isCompletingAttack;
            readonly BattleProjectilePath _projectilePath = new BattleProjectilePath();
            readonly List<HexCoordinate> _projectileCells = new List<HexCoordinate>();
            GameObject _projectile;
            Vector3 _projectileDestination;
            float _projectileSpeed;
            bool _hitIssued;
            bool _animationCompleted;
            HexMovementStep _step;
            bool _hasStep;
            uint _actingDisableVersion;
            uint _pullTargetDisableVersion;
            uint _assassinTargetDisableVersion;
            int _movementViewUnitId;

            public BattleActionPresentation(BattleRunner owner)
            {
                _owner = owner;
            }

            public void CaptureActor(HexCharacterView actor)
            {
                if (actor != null)
                {
                    _actingDisableVersion = actor.DisableVersion;
                }
            }

            public void CaptureSkill(BattleSimulation simulation, SkillUseRequest request,
                HexCharacterView actor, HexCharacterView target, bool movesTarget, bool leap)
            {
                CaptureActor(actor);

                if (movesTarget)
                {
                    _pullTargetDisableVersion = target.DisableVersion;
                    _movementViewUnitId = simulation.MovingUnitId;
                }
                else if (leap)
                {
                    _assassinTargetDisableVersion = target.DisableVersion;
                    _movementViewUnitId = request.UnitId;
                }
            }

            public void ResetMovement()
            {
                ResetAttack();
                _hasStep = false;
                _step = default;
                _actingDisableVersion = 0;
                _pullTargetDisableVersion = 0;
                _assassinTargetDisableVersion = 0;
                _movementViewUnitId = 0;
            }

            public void FinishActionWithoutPresentation()
            {
                if (_owner._simulation == null)
                {
                    return;
                }

                BattleSimulation simulation = _owner._simulation;
                int unitId = simulation.ActingUnitId;
                bool wasReaction = simulation.HasPendingReactions;
                bool wasPull = simulation.IsPullMovement;
                bool wasAssassinLeap = simulation.IsAssassinLeap;

                if (simulation.MovingUnitId != 0)
                {
                    _movementViewUnitId = simulation.MovingUnitId;
                }

                simulation.StopAction();

                if (simulation.Phase == BattleActionPhase.Attacking)
                {
                    _owner._views.TryGetValue(unitId, out HexCharacterView view);
                    AbortAttack(view);
                }
                else if (simulation.IsWaitingForMovementCompletion)
                {
                    simulation.TryCompleteMovementStep();
                    _owner.DrainDamage(simulation, false);
                }

                if (_owner._simulation != simulation)
                {
                    return;
                }

                ClearPresentation(unitId, !wasReaction && !wasPull && !wasAssassinLeap);

                if (_owner._simulation == simulation)
                {
                    simulation.TrySettleAction();
                    _owner.DrainDamage(simulation, false);
                }
            }

            public void ClearPresentation(int unitId, bool markEnemyFailure = true)
            {
                ResetAttack();
                _hasStep = false;
                int movementUnitId = _movementViewUnitId;
                _movementViewUnitId = 0;
                BattleSimulation simulation = _owner._simulation;

                if (markEnemyFailure && simulation != null && simulation.Board.TryGetUnit(unitId, out HexCharacter character)
                    && character.Team == BattleTeam.Enemy)
                {
                    _owner._failedEnemyUnitId = unitId;
                }

                if (_owner._views.TryGetValue(unitId, out HexCharacterView view) && view != null)
                {
                    view.SnapToCharacter();
                }

                if (_owner._simulation == simulation && movementUnitId != 0 && movementUnitId != unitId
                    && _owner._views.TryGetValue(movementUnitId, out HexCharacterView movingView) && movingView != null)
                {
                    movingView.SnapToCharacter();
                }
            }

            public void UpdateCurrentAction(float deltaTime)
            {
                BattleSimulation simulation = _owner._simulation;

                if (!_owner.CanContinuePresentation(simulation) || !simulation.IsBusy)
                {
                    return;
                }

                if (simulation.Phase == BattleActionPhase.Settling)
                {
                    ClearPresentation(simulation.ActingUnitId, false);

                    if (_owner.CanContinuePresentation(simulation))
                    {
                        simulation.TrySettleAction();
                    }

                    return;
                }

                if (simulation.Phase == BattleActionPhase.Moving)
                {
                    _owner._views.TryGetValue(simulation.MovingUnitId, out HexCharacterView movingView);

                    if (simulation.IsAssassinLeap && !IsAssassinLeapPresentationReady(simulation)
                        || simulation.IsPullMovement && !IsPullPresentationReady(simulation)
                        || !simulation.IsAssassinLeap && !simulation.IsPullMovement && simulation.TargetUnitId == 0
                        && (movingView == null || !movingView.IsReady || movingView.DisableVersion != _actingDisableVersion))
                    {
                        FinishActionWithoutPresentation();
                        return;
                    }

                    AdvanceMovement(movingView, deltaTime);

                    if (!_owner.CanContinuePresentation(simulation))
                    {
                        return;
                    }
                }

                if (simulation.Phase == BattleActionPhase.ReadyToAttack
                    || simulation.Phase == BattleActionPhase.ResolvingReactions)
                {
                    if (simulation.IsAssassinLeap && !IsAssassinLeapPresentationReady(simulation))
                    {
                        FinishActionWithoutPresentation();
                        return;
                    }

                    bool wasReaction = simulation.HasPendingReactions;

                    if (!simulation.TryBeginAttack(out HexAttackStep attack))
                    {
                        if (!wasReaction)
                        {
                            _owner.CancelAction();
                        }

                        return;
                    }

                    _owner._views.TryGetValue(attack.AttackerId, out HexCharacterView attackView);

                    if (attackView != null)
                    {
                        attackView.ApplyFacing();
                    }

                    if (!_owner.CanContinuePresentation(simulation) || simulation.Phase != BattleActionPhase.Attacking
                        || simulation.ActingUnitId != attack.AttackerId)
                    {
                        return;
                    }

                    bool presented = attackView != null && attackView.IsReady && attackView.TryPlayAttack(attack);
                    ResetAttack();

                    if (!_owner.CanContinuePresentation(simulation) || simulation.Phase != BattleActionPhase.Attacking
                        || simulation.ActingUnitId != attack.AttackerId)
                    {
                        return;
                    }

                    if (!presented)
                    {
                        AbortAttack(attackView);
                    }
                    else
                    {
                        AdvanceAttack(attackView, 0f);
                    }

                    return;
                }

                if (simulation.Phase == BattleActionPhase.Attacking)
                {
                    _owner._views.TryGetValue(simulation.ActingUnitId, out HexCharacterView view);

                    AdvanceAttack(view, deltaTime);
                }
            }

            bool IsPullPresentationReady(BattleSimulation simulation)
            {
                return _owner._views.TryGetValue(simulation.ActingUnitId, out HexCharacterView actor)
                    && actor != null && actor.IsReady && actor.DisableVersion == _actingDisableVersion
                    && _owner._views.TryGetValue(simulation.TargetUnitId, out HexCharacterView target)
                    && target != null && target.IsReady && target.DisableVersion == _pullTargetDisableVersion;
            }

            bool IsAssassinLeapPresentationReady(BattleSimulation simulation)
            {
                return _owner._views.TryGetValue(simulation.ActingUnitId, out HexCharacterView actor)
                    && actor != null && actor.IsReady && actor.DisableVersion == _actingDisableVersion
                    && _owner._views.TryGetValue(simulation.TargetUnitId, out HexCharacterView target)
                    && target != null && target.IsReady && target.DisableVersion == _assassinTargetDisableVersion;
            }

            void ResetAttack()
            {
                if (_projectile != null)
                {
                    _projectile.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_projectile);
                    }
                    else
                    {
                        Object.DestroyImmediate(_projectile);
                    }
                }

                _projectile = null;
                _hitIssued = false;
                _animationCompleted = false;
                _projectileCells.Clear();
            }

            void AbortAttack(HexCharacterView view)
            {
                BattleSimulation simulation = _owner._simulation;
                int actorId = simulation.ActingUnitId;
                bool reaction = simulation.IsReactionAttack;
                ResetAttack();
                simulation.TryAbortAttack();
                if (view != null)
                {
                    view.SetIdle();
                }

                ClearPresentation(actorId, !reaction);
            }

            void AdvanceAttack(HexCharacterView view, float deltaTime)
            {
                BattleSimulation simulation = _owner._simulation;
                bool launched = false;
                if (!simulation.IsAttackDamageResolved && !simulation.HasAttackParticipants)
                {
                    AbortAttack(view);
                    return;
                }

                if (!_hitIssued && view != null && view.TryConsumeAttackHit(out AnimationAttackKind kind))
                {
                    _hitIssued = true;
                    if (kind == AnimationAttackKind.Melee)
                    {
                        ResolveDamage(true);
                    }
                    else if (!LaunchProjectile(view, simulation))
                    {
                        AbortAttack(view);
                        return;
                    }
                    else
                    {
                        launched = true;
                    }
                }

                if (!_owner.CanContinuePresentation(simulation) || simulation.Phase != BattleActionPhase.Attacking)
                {
                    ResetAttack();
                    return;
                }

                bool completed = false;
                bool readable = view != null && view.TryReadAttackCompletion(out completed);
                if (!readable)
                {
                    if (!_hitIssued)
                    {
                        AbortAttack(view);
                        return;
                    }

                    _animationCompleted = true;
                }
                else
                {
                    _animationCompleted |= completed;
                }

                if (!simulation.IsAttackDamageResolved && (_hitIssued && _projectile == null
                    || !_hitIssued && _animationCompleted))
                {
                    AbortAttack(view);
                    return;
                }

                if (_projectile != null && !simulation.IsAttackDamageResolved)
                {
                    Vector3 previous = _projectile.transform.position;
                    Vector3 next = Vector3.MoveTowards(previous, _projectileDestination,
                        launched ? 0f : _projectileSpeed * Mathf.Max(0f, deltaTime));
                    _projectilePath.Copy(_owner._mapView, previous, next, _projectileCells);
                    for (int i = 0; i < _projectileCells.Count && simulation.HasAttackParticipants; i++)
                    {
                        simulation.TryVisitAttackCell(_projectileCells[i]);
                    }

                    _projectile.transform.position = next;
                    _owner.DrainDamage(simulation);
                    if (!_owner.CanContinuePresentation(simulation))
                    {
                        ResetAttack();
                        return;
                    }

                    if (!simulation.HasAttackParticipants)
                    {
                        AbortAttack(view);
                        return;
                    }

                    if (next == _projectileDestination)
                    {
                        _projectile.SetActive(false);
                        ResolveDamage(false);
                    }
                }

                if (_owner.CanContinuePresentation(simulation) && simulation.Phase == BattleActionPhase.Attacking
                    && _animationCompleted && simulation.IsAttackDamageResolved)
                {
                    simulation.TryFinishAttackPresentation();
                    if (view != null)
                    {
                        view.SetIdle();
                    }

                    ResetAttack();
                }
            }

            bool LaunchProjectile(HexCharacterView view, BattleSimulation simulation)
            {
                if (!(view.ProjectileSpeed > 0f) || float.IsInfinity(view.ProjectileSpeed)
                    || float.IsNaN(view.ProjectileTargetHeight) || float.IsInfinity(view.ProjectileTargetHeight)
                    || !simulation.Board.TryGetUnit(simulation.TargetUnitId, out HexCharacter target))
                {
                    return false;
                }

                Vector3 origin = view.ProjectileOrigin;
                Vector3 destination = _owner._mapView.AxialToWorld(target.Coordinate)
                    + _owner._mapView.transform.up * view.ProjectileTargetHeight;
                if (!IsFinite(origin) || !IsFinite(destination))
                {
                    return false;
                }

                _projectile = view.ProjectilePrefab != null ? Object.Instantiate(view.ProjectilePrefab)
                    : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (view.ProjectilePrefab == null)
                {
                    _projectile.transform.localScale = Vector3.one * 0.15f;
                }

                Collider[] colliders = _projectile.GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }

                Rigidbody[] bodies = _projectile.GetComponentsInChildren<Rigidbody>();
                for (int i = 0; i < bodies.Length; i++)
                {
                    bodies[i].isKinematic = true;
                    bodies[i].useGravity = false;
                }

                _projectile.transform.SetParent(_owner._mapView.transform, true);
                _projectile.transform.position = origin;
                _projectileDestination = destination;
                _projectileSpeed = view.ProjectileSpeed;
                _projectile.SetActive(true);
                return true;
            }

            static bool IsFinite(Vector3 value)
            {
                return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                    && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                    && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
            }

            void ResolveDamage(bool applyWholePath)
            {
                if (_isCompletingAttack)
                {
                    return;
                }

                _isCompletingAttack = true;
                try
                {
                    BattleSimulation simulation = _owner._simulation;
                    uint presentationVersion = _owner._presentationVersion;
                    simulation.TryResolveAttackDamage(_attackResults, applyWholePath);
                    _owner.DrainDamage(simulation, presentationVersion == _owner._presentationVersion);
                }
                finally
                {
                    _attackResults.Clear();
                    _isCompletingAttack = false;
                }
            }

            void AdvanceMovement(HexCharacterView view, float remainingTime)
            {
                BattleSimulation simulation = _owner._simulation;

                while (remainingTime > 0f && _owner.CanContinuePresentation(simulation))
                {
                    if (simulation.IsPullMovement && !IsPullPresentationReady(simulation)
                        || simulation.IsAssassinLeap && !IsAssassinLeapPresentationReady(simulation))
                    {
                        FinishActionWithoutPresentation();
                        return;
                    }

                    bool canPresent = view != null && view.IsReady;

                    if (!_hasStep)
                    {
                        if (!simulation.TryBeginMovementStep(out _step))
                        {
                            if (view != null)
                            {
                                view.SetIdle();
                            }

                            return;
                        }

                        _hasStep = true;
                        _movementViewUnitId = _step.UnitId;

                        if (simulation.IsPullMovement
                            && _owner._views.TryGetValue(simulation.ActingUnitId, out HexCharacterView actor) && actor != null)
                        {
                            actor.ApplyFacing();
                        }

                        if (canPresent)
                        {
                            canPresent = simulation.IsAssassinLeap ? view.TryPlayLeap(_step) : view.TryPlayMovement(_step);
                        }
                    }

                    if (!_owner.CanContinuePresentation(simulation))
                    {
                        return;
                    }

                    if (simulation.IsPullMovement && !IsPullPresentationReady(simulation)
                        || simulation.IsAssassinLeap && !IsAssassinLeapPresentationReady(simulation))
                    {
                        FinishActionWithoutPresentation();
                        return;
                    }

                    if (canPresent)
                    {
                        remainingTime = view.UpdateMovementPresentation(remainingTime);

                        if (!_owner.CanContinuePresentation(simulation) || view.IsPresenting)
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (simulation.TargetUnitId == 0 || simulation.IsPullMovement || simulation.IsAssassinLeap)
                        {
                            FinishActionWithoutPresentation();
                            return;
                        }

                        if (view != null)
                        {
                            view.SnapToCharacter();
                        }
                    }

                    if (!_owner.CanContinuePresentation(simulation))
                    {
                        return;
                    }

                    _hasStep = false;
                    simulation.TryCompleteMovementStep();
                    _owner.DrainDamage(simulation);
                    if (!_owner.CanContinuePresentation(simulation))
                    {
                        return;
                    }

                    if (simulation.Phase != BattleActionPhase.Moving || simulation.IsMovementPaused)
                    {
                        if (simulation.Phase != BattleActionPhase.Settling)
                        {
                            _movementViewUnitId = 0;
                        }

                        if (view != null && simulation.Phase != BattleActionPhase.ReadyToAttack)
                        {
                            view.SetIdle();
                        }

                        return;
                    }
                }
            }
        }
    }
}
