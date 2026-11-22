using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat
{
    public sealed partial class BattleSimulation
    {
        const int _criticalDamageBonus = 2;

        readonly BattleBoard _board;
        readonly HexPathfinder _pathfinder = new HexPathfinder();
        readonly HexPathfinder _rangePathfinder = new HexPathfinder();
        readonly List<HexPathfinder.ReachablePosition> _rangePositions = new List<HexPathfinder.ReachablePosition>();
        readonly BattleTurnOrder _turnOrder;
        readonly Random _combatRandom;
        readonly BattleMovementExecution _movement = new BattleMovementExecution();
        readonly BattleReactionExecution _reactions = new BattleReactionExecution();
        readonly PullPathfinder _pullPathfinder = new PullPathfinder();

        BattleMovementRequest _movementRequest;
        readonly BattleAttackExecution _attackExecution = new BattleAttackExecution();

        bool _turnAdvancePending;
        bool _stopAfterAttack;
        uint _visibilityVersion;
        int _lastUnitId;
        int _actingUnitId;
        int _targetUnitId;
        bool _hasStartedBattle;
        int _initialPlayerCount;
        int _initialEnemyCount;
        BattleActionPhase _phase;
        BattleState _state;
        BattleResult _result;

        public BattleBoard Board => _board;
        public BattleRules Rules { get; }
        public BattleState State => _state;
        public BattleResult Result => _result;
        public int RoundNumber => _turnOrder.RoundNumber;
        public int? CurrentUnitId => _state == BattleState.Running ? _turnOrder.CurrentUnitId : null;
        public bool IsBusy => _phase != BattleActionPhase.None;
        public BattleActionPhase Phase => _phase;
        public int ActingUnitId => _phase == BattleActionPhase.Attacking ? _attackExecution.Context.AttackerId : _actingUnitId;
        public int MovingUnitId => _phase == BattleActionPhase.Moving ? _movement.MoverId : 0;
        bool IsQueuedAmbush => _skillExecution != null && _skillExecution.Definition.Type == SkillType.Ambush;
        bool IsSkillMovement => IsPullMovement || IsAssassinLeap;
        bool HasStartedSkillMovement => IsSkillMovement && _skillExecution.HasStartedMovement;

        public int TargetUnitId => _phase == BattleActionPhase.Attacking ? _attackExecution.Context.TargetId : _targetUnitId;
        public bool HasPendingReactions => _reactions.IsActive;
        public bool IsReactionAttack => _phase == BattleActionPhase.Attacking && _attackExecution.Context.Origin == BattleAttackOrigin.Reaction;
        public bool IsMovementPaused => _movement.IsPaused;
        public bool IsWaitingForMovementCompletion => _movement.WaitingForCompletion;
        public uint VisibilityVersion => _visibilityVersion;

        public BattleSimulation(int width, int height, BattleRules rules = null, int? randomSeed = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            _board = new BattleBoard(width, height);
            Rules = rules ?? new BattleRules();
            _turnOrder = new BattleTurnOrder(randomSeed);
            _combatRandom = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
        }

        public bool TrySpawnUnit(BattleTeam team, HexCoordinate coordinate, BattleUnitStats stats,
            HexDirection facing, out HexCharacter unit, TraitCollection traits = null,
            BattleTraitSettings traitSettings = null,
            MercenarySpecialization specialization = MercenarySpecialization.None, SkillSet skills = null,
            BattleEquipmentLoadout equipment = null)
        {
            unit = null;
            traits = traits ?? TraitCollection.Empty;

            if (_state != BattleState.Preparing || _lastUnitId == int.MaxValue || stats == null || skills == null || (uint)facing >= 6
                || !MercenarySpecializationRules.IsCompatible(stats.UnitClass, specialization)
                || (team != BattleTeam.Player && team != BattleTeam.Enemy)
                || (team == BattleTeam.Player && CountPlayers() >= Rules.MaxPlayerUnits)
                || (traits.Has(TraitType.DirectionalPhysicalNegation) && traitSettings == null)
                || !_board.TryAdd(_lastUnitId + 1, team, coordinate, Rules, stats, facing,
                    traits, traitSettings ?? BattleTraitSettings.None, out unit, specialization, skills, equipment))
            {
                return false;
            }

            _lastUnitId = unit.Id;
            InvalidateVisibility();
            return true;
        }

        public bool TryStartBattle()
        {
            if (_state != BattleState.Preparing)
            {
                return false;
            }

            CountTeams(out int playerCount, out int enemyCount);

            if (playerCount == 0 || enemyCount == 0)
            {
                _state = BattleState.Waiting;
                return true;
            }

            _hasStartedBattle = true;
            _initialPlayerCount = playerCount;
            _initialEnemyCount = enemyCount;

            foreach (HexCharacter unit in _board.Units)
            {
                if (unit.Traits.Has(TraitType.BattleStartInvisibility))
                {
                    unit.EnterInvisibility();
                }
            }

            InvalidateVisibility();
            _state = BattleState.Running;
            _turnOrder.StartRound(_board);
            BeginCurrentTurn();
            return true;
        }

        public bool TryFinalizeResult(out BattleResult result)
        {
            result = null;

            if (!_hasStartedBattle || _state != BattleState.Waiting || IsBusy || _result != null)
            {
                return false;
            }

            CountTeams(out int playerCount, out int enemyCount);

            if (playerCount > 0 && enemyCount > 0)
            {
                return false;
            }

            BattleOutcome outcome = playerCount > 0 ? BattleOutcome.PlayerVictory
                : enemyCount > 0 ? BattleOutcome.PlayerDefeat : BattleOutcome.Draw;
            _result = new BattleResult(outcome, RoundNumber, _initialPlayerCount, _initialEnemyCount,
                playerCount, enemyCount);
            _state = BattleState.Completed;
            result = _result;
            return true;
        }

        public bool RemoveUnit(int unitId)
        {
            if (!_board.TryGetUnit(unitId, out HexCharacter removed))
            {
                return false;
            }

            bool wasCurrent = _turnOrder.CurrentUnitId == unitId;
            removed.ClearOverwatch();
            removed.ClearStealth();
            _board.Remove(unitId);
            InvalidateVisibility();

            if (_state == BattleState.Running)
            {
                _turnAdvancePending |= wasCurrent;

                if (!HasBothTeams())
                {
                    _state = BattleState.Waiting;

                    foreach (HexCharacter unit in _board.Units)
                    {
                        unit.ClearOverwatch();
                    }
                }
            }

            if (_state != BattleState.Running && _state != BattleState.Waiting)
            {
                return true;
            }

            if (IsAssassinLeap && (_state == BattleState.Waiting || wasCurrent
                    || unitId == _actingUnitId || unitId == _targetUnitId)
                || IsPullMovement && (_state == BattleState.Waiting || wasCurrent
                    || unitId == _actingUnitId || unitId == _targetUnitId || unitId == _movement.MoverId))
            {
                StopSkillMovement();
                return true;
            }

            if (wasCurrent || (_state == BattleState.Waiting && _reactions.IsActive))
            {
                _reactions.ClearPending();

                if (_phase != BattleActionPhase.Attacking)
                {
                    ClearMovement();
                    _phase = BattleActionPhase.Settling;
                }
            }
            else if (!_reactions.IsActive && _phase != BattleActionPhase.Attacking
                && (_state == BattleState.Waiting || _actingUnitId == unitId || _targetUnitId == unitId))
            {
                CancelAction();
            }

            return true;
        }

        public bool TryEndTurn(int unitId)
        {
            if (_state != BattleState.Running || IsBusy || CurrentUnitId != unitId)
            {
                return false;
            }

            _turnOrder.MoveToNextTurn(_board);
            BeginCurrentTurn();
            return true;
        }

        public bool CanPrepareOverwatch(int unitId)
        {
            return CanSelectSkill(unitId, SkillType.Overwatch).CanUse;
        }

        public bool IsVisibleTo(BattleTeam team, int targetId)
        {
            return _board.TryGetUnit(targetId, out HexCharacter target) && target.Health > 0
                && (target.Team == team || !target.IsStealthed);
        }

        public bool CanEnterStealth(int unitId)
        {
            return CanSelectSkill(unitId, SkillType.Stealth).CanUse;
        }

        bool ExecuteEnterStealth(int unitId)
        {
            if (!CanEnterStealth(unitId))
            {
                return false;
            }

            _board.TryGetUnit(unitId, out HexCharacter unit);
            SpendSkillCost(unit);
            unit.EnterStealth();
            InvalidateVisibility();
            return true;
        }

        public bool CanSelectAmbush(int unitId)
        {
            return CanSelectSkill(unitId, SkillType.Ambush).CanUse;
        }

        public bool CanAmbush(int attackerId, int targetId)
        {
            return CanSelectAmbush(attackerId)
                && TryGetOpponents(attackerId, targetId, out HexCharacter attacker, out HexCharacter target)
                && IsAmbushTarget(attacker, target);
        }

        bool ExecuteAmbush(int attackerId, int targetId)
        {
            if (!CanAmbush(attackerId, targetId))
            {
                return false;
            }

            _movement.DiscardRemainingPath();
            _actingUnitId = attackerId;
            _targetUnitId = targetId;
            _phase = BattleActionPhase.ReadyToAttack;
            return true;
        }

        bool CanUseAmbush(HexCharacter unit)
        {
            return unit.Health > 0 && unit.IsStealthed && !unit.HasUsedAmbush
                && HasSkillResources(unit, SkillType.Ambush);
        }

        bool IsAmbushTarget(HexCharacter attacker, HexCharacter target)
        {
            return IsVisibleTo(attacker.Team, target.Id)
                && attacker.Skills.IsInRange(SkillType.Ambush, attacker.Coordinate.DistanceTo(target.Coordinate))
                && HasSkillLineOfSight(attacker, SkillType.Ambush, attacker.Coordinate, target.Coordinate)
                && BattleFacing.Classify(target.Facing, target.Coordinate, attacker.Coordinate)
                    == BattleRelativeDirection.Rear;
        }

        bool ExecutePrepareOverwatch(int unitId, HexDirection direction)
        {
            if ((uint)direction >= 6 || !CanPrepareOverwatch(unitId))
            {
                return false;
            }

            _board.TryGetUnit(unitId, out HexCharacter unit);
            SpendSkillCost(unit);
            unit.SetFacing(direction);
            RefreshDetection();
            unit.PrepareOverwatch(direction);
            Reveal(unit);
            _turnOrder.MoveToNextTurn(_board);
            BeginCurrentTurn();
            return true;
        }

        public void CopyOverwatchStates(List<BattleOverwatchState> destination)
        {
            destination?.Clear();

            if (destination == null || _state != BattleState.Running)
            {
                return;
            }

            foreach (HexCharacter unit in _board.Units)
            {
                if (unit.IsOverwatching)
                {
                    unit.Skills.GetRange(SkillType.Overwatch, out int minRange, out int maxRange);
                    destination.Add(new BattleOverwatchState(unit.Id, unit.Coordinate,
                        unit.OverwatchDirection, maxRange, minRange));
                }
            }
        }

        public bool TryGetActionRange(int unitId, List<HexCoordinate> movementDestinations,
            List<BattleAttackTarget> attackTargets)
        {
            movementDestinations?.Clear();
            attackTargets?.Clear();

            if (movementDestinations == null || attackTargets == null
                || _state != BattleState.Running || IsBusy || CurrentUnitId != unitId
                || !_board.TryGetUnit(unitId, out HexCharacter unit))
            {
                return false;
            }

            _rangePathfinder.FindReachablePositions(_board, unit.Coordinate, unit.MovementRemaining, _rangePositions);

            for (int i = 1; i < _rangePositions.Count; i++)
            {
                movementDestinations.Add(_rangePositions[i].Coordinate);
            }

            if (unit.ActionPoints < unit.Skills.BasicAttack.ActionPointCost)
            {
                return true;
            }

            foreach (HexCharacter candidate in _board.Units)
            {
                if (!CanStartAttack(unitId, candidate.Id, out _, out HexCharacter target))
                {
                    continue;
                }

                if (unit.Skills.IsInRange(SkillType.BasicAttack, unit.Coordinate.DistanceTo(target.Coordinate))
                    && HasSkillLineOfSight(unit, SkillType.BasicAttack, unit.Coordinate, target.Coordinate))
                {
                    attackTargets.Add(new BattleAttackTarget(target.Id, false));
                    continue;
                }

                for (int i = 1; i < _rangePositions.Count; i++)
                {
                    if (unit.Skills.IsInRange(SkillType.BasicAttack, _rangePositions[i].Coordinate.DistanceTo(target.Coordinate))
                        && HasSkillLineOfSight(unit, SkillType.BasicAttack, _rangePositions[i].Coordinate, target.Coordinate))
                    {
                        attackTargets.Add(new BattleAttackTarget(target.Id, true));
                        break;
                    }
                }
            }

            return true;
        }

        static long GetHexDistance(HexCoordinate from, HexCoordinate to)
        {
            long q = (long)to.Q - from.Q;
            long r = (long)to.R - from.R;
            return Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(q + r)));
        }

        void StopSkillMovement()
        {
            switch (_skillExecution.Definition.Type)
            {
                case SkillType.AssassinLeap:
                    StopAssassinLeap();
                    break;
                case SkillType.PullSelf:
                case SkillType.PullTarget:
                    StopPullMovement();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        void FinishSkillMovement()
        {
            if (HasStartedSkillMovement && _board.TryGetUnit(_actingUnitId, out HexCharacter actor))
            {
                Reveal(actor);
            }

            ClearMovement();
            _phase = BattleActionPhase.Settling;
        }

        public bool TryCopyMovementPath(int unitId, HexCoordinate destination, BattlePathLine line,
            List<HexCoordinate> result)
        {
            result?.Clear();
            return CanStartMovement(unitId, out HexCharacter unit) && destination != unit.Coordinate
                && _pathfinder.TryFindPath(_board, unit.Coordinate, destination, unit.MovementRemaining, line, result)
                && result.Count > 0;
        }

        public bool TryCopyAttackPath(int attackerId, int targetId, BattlePathLine line,
            List<HexCoordinate> result)
        {
            result?.Clear();
            return CanStartAttack(attackerId, targetId, out HexCharacter attacker, out HexCharacter target)
                && _pathfinder.TryFindPathToAttackPosition(_board, attacker.Coordinate, target.Coordinate,
                    attacker.Skills.BasicAttack.MaxRange, attacker.MovementRemaining, line, result, attacker.Skills.BasicAttack.MinRange);
        }

        public bool TryCopyRemainingMovementPath(List<HexCoordinate> result)
        {
            if (result == null)
            {
                return false;
            }

            result.Clear();

            if (_phase != BattleActionPhase.Moving || IsSkillMovement || _movement.Path.Count == 0)
            {
                return false;
            }

            return _movement.CopyRemainingPath(result);
        }

        public bool TryMove(int unitId, HexCoordinate destination)
        {
            if (!CanStartMovement(unitId, out HexCharacter unit)
                || !TryCopyMovementPath(unitId, destination, BattlePathLine.Between(unit.Coordinate, destination), _skillQueryPath))
            {
                return false;
            }

            return QueueMovement(unit, _skillQueryPath);
        }

        public bool TryMove(int unitId, HexCoordinate destination, IReadOnlyList<HexCoordinate> path)
        {
            if (!CanStartMovement(unitId, out HexCharacter unit) || path == null || path.Count == 0
                || !IsValidMovementPath(unit, path) || path[path.Count - 1] != destination)
            {
                return false;
            }

            return QueueMovement(unit, path);
        }

        bool CanStartMovement(int unitId, out HexCharacter unit)
        {
            unit = null;
            return _state == BattleState.Running && !IsBusy && CurrentUnitId == unitId
                && _board.TryGetUnit(unitId, out unit) && unit.Health > 0 && unit.MovementRemaining > 0;
        }

        bool QueueMovement(HexCharacter unit, IReadOnlyList<HexCoordinate> path)
        {
            _actingUnitId = unit.Id;
            _movement.Begin(path, unit.Id, MovementKind.Normal, unit.Coordinate);
            _phase = BattleActionPhase.Moving;
            return true;
        }

        bool ExecuteAttack(int attackerId, int targetId, IReadOnlyList<HexCoordinate> path)
        {
            if (!CanStartAttack(attackerId, targetId, out HexCharacter attacker, out HexCharacter target)
                || !IsValidMovementPath(attacker, path))
            {
                return false;
            }

            HexCoordinate attackPosition = path.Count == 0 ? attacker.Coordinate : path[path.Count - 1];

            if (!attacker.Skills.IsInRange(SkillType.BasicAttack, attackPosition.DistanceTo(target.Coordinate))
                || !HasSkillLineOfSight(attacker, SkillType.BasicAttack, attackPosition, target.Coordinate))
            {
                return false;
            }

            return TryQueueAttack(attacker, targetId, path);
        }

        bool IsValidMovementPath(HexCharacter unit, IReadOnlyList<HexCoordinate> path)
        {
            if (path == null || path.Count > unit.MovementRemaining)
            {
                return false;
            }

            HexCoordinate current = unit.Coordinate;

            for (int i = 0; i < path.Count; i++)
            {
                HexCoordinate next = path[i];

                if (!_board.IsValid(next) || _board.IsOccupied(next) || current.DistanceTo(next) != 1)
                {
                    return false;
                }

                current = next;
            }

            return true;
        }

        bool IsValidInitialMovement(HexCharacter unit)
        {
            if (_state != BattleState.Running || CurrentUnitId != unit.Id || unit.Coordinate != _movement.ActorOrigin
                || !IsValidMovementPath(unit, _movement.Path))
            {
                return false;
            }

            if (_targetUnitId == 0)
            {
                return true;
            }

            return TryGetOpponents(unit.Id, _targetUnitId, out _, out HexCharacter target)
                && IsVisibleTo(unit.Team, target.Id) && unit.ActionPoints >= unit.Skills.BasicAttack.ActionPointCost
                && unit.Skills.IsInRange(SkillType.BasicAttack, _movement.Path[_movement.Path.Count - 1].DistanceTo(target.Coordinate))
                && HasSkillLineOfSight(unit, SkillType.BasicAttack, _movement.Path[_movement.Path.Count - 1], target.Coordinate);
        }

        bool CanStartAttack(int attackerId, int targetId, out HexCharacter attacker, out HexCharacter target)
        {
            attacker = null;
            target = null;
            return _state == BattleState.Running && !IsBusy && CurrentUnitId == attackerId
                && TryGetOpponents(attackerId, targetId, out attacker, out target)
                && IsVisibleTo(attacker.Team, target.Id)
                && HasSkillResources(attacker, SkillType.BasicAttack);
        }

        bool TryQueueAttack(HexCharacter attacker, int targetId, IReadOnlyList<HexCoordinate> path)
        {
            if (path.Count > attacker.MovementRemaining)
            {
                return false;
            }

            _actingUnitId = attacker.Id;
            _targetUnitId = targetId;
            _movement.Begin(path, attacker.Id, MovementKind.Normal, attacker.Coordinate);
            _phase = path.Count > 0 ? BattleActionPhase.Moving : BattleActionPhase.ReadyToAttack;
            return true;
        }

        public bool TryBeginMovementStep(out HexMovementStep step)
        {
            step = default;

            if (_phase != BattleActionPhase.Moving || !_movement.TryGetNext(out HexCoordinate destination))
            {
                return false;
            }

            if (IsSkillMovement)
            {
                switch (_skillExecution.Definition.Type)
                {
                    case SkillType.AssassinLeap:
                        return TryBeginAssassinLeap(out step);
                    case SkillType.PullSelf:
                    case SkillType.PullTarget:
                        return TryBeginPullStep(out step);
                    default:
                        throw new InvalidOperationException();
                }
            }

            if (!_board.TryGetUnit(_actingUnitId, out HexCharacter unit) || unit.MovementRemaining <= 0)
            {
                CancelAction();
                return false;
            }

            if (_movement.Index == 0 && !IsValidInitialMovement(unit))
            {
                CancelAction();
                return false;
            }

            HexCoordinate origin = unit.Coordinate;
            BattleMovementRequest request = new BattleMovementRequest(unit.Id, origin, destination, _movement.Kind);
            BattleMovementPermission permission = EvaluateMovement(request);

            if (permission == BattleMovementPermission.PendingReaction)
            {
                _movementRequest = request;
                _reactions.BeginBatch();
                ClearMovement();
                _targetUnitId = 0;
                _phase = BattleActionPhase.ResolvingReactions;
                return false;
            }

            if (permission != BattleMovementPermission.Allowed || !_board.TryMove(_actingUnitId, destination))
            {
                CancelAction();
                return false;
            }

            unit.SpendMovement();
            unit.SetFacing(BattleFacing.GetDirection(origin, destination));
            step = new HexMovementStep(_actingUnitId, origin, destination);
            _movement.MarkStepIssued();
            return true;
        }

        public bool TryCompleteMovementStep()
        {
            if (_phase != BattleActionPhase.Moving || !_movement.TryCompleteStep())
            {
                return false;
            }

            if (_board.TryGetUnit(_movement.MoverId, out HexCharacter mover))
            {
                ApplyEnvironmentAt(mover);

                if (mover.Health <= 0)
                {
                    return true;
                }
            }

            if (IsSkillMovement)
            {
                switch (_skillExecution.Definition.Type)
                {
                    case SkillType.AssassinLeap:
                        CompleteAssassinLeap();
                        break;
                    case SkillType.PullSelf:
                    case SkillType.PullTarget:
                        RefreshDetection();

                        if (_movement.StopRequested || _movement.IsComplete)
                        {
                            FinishSkillMovement();
                        }

                        break;
                    default:
                        throw new InvalidOperationException();
                }

                return true;
            }

            RefreshDetection();

            if (_movement.IsComplete)
            {
                if (_targetUnitId != 0 && IsTargetInRange())
                {
                    ClearMovement();
                    _phase = BattleActionPhase.ReadyToAttack;
                }
                else
                {
                    CancelAction();
                }
            }

            return true;
        }

        public bool TryBeginAttack(out HexAttackStep step)
        {
            step = default;

            if (_phase == BattleActionPhase.ResolvingReactions)
            {
                return TryBeginReaction(out step);
            }

            if (_phase != BattleActionPhase.ReadyToAttack || _skillExecution == null)
            {
                return false;
            }

            int cost = _skillExecution.CostCommitted
                ? 0 : _skillExecution.Definition.ActionPointCost;

            if (!IsTargetInRange()
                || !_board.TryGetUnit(_actingUnitId, out HexCharacter attacker)
                || attacker.ActionPoints < cost
                || !_skillExecution.CostCommitted && attacker.GetRemainingCooldown(_skillExecution.Definition.Type) > 0)
            {
                if (IsAssassinLeap)
                {
                    FinishSkillMovement();
                }
                else
                {
                    CancelAction();
                }

                return false;
            }

            _board.TryGetUnit(_targetUnitId, out HexCharacter target);

            if (IsQueuedAmbush && (!CanUseAmbush(attacker) || !IsAmbushTarget(attacker, target)))
            {
                CancelAction();
                return false;
            }

            SpendSkillCost(attacker);

            if (IsQueuedAmbush)
            {
                attacker.UseAmbush();
            }

            step = IssueAttack(attacker, target, BattleAttackOrigin.Command, IsQueuedAmbush);
            return true;
        }

        public bool PauseMovement()
        {
            if (IsSkillMovement || _phase != BattleActionPhase.Moving || _movement.IsPaused)
            {
                return false;
            }

            _movement.SetPaused(true);
            return true;
        }

        public bool ResumeMovement()
        {
            if (IsSkillMovement || _phase != BattleActionPhase.Moving || !_movement.IsPaused)
            {
                return false;
            }

            _movement.SetPaused(false);
            return true;
        }

        public void CancelAction()
        {
            if (HasStartedSkillMovement)
            {
                return;
            }

            if (_reactions.IsActive || _phase == BattleActionPhase.Attacking || _phase == BattleActionPhase.Settling)
            {
                return;
            }

            if (_phase == BattleActionPhase.Moving && _movement.WaitingForCompletion)
            {
                _movement.DiscardRemainingPath();
                _targetUnitId = 0;
                return;
            }

            ResetAction();
        }

        void ResetAction()
        {
            ClearMovement();
            _skillExecution = null;
            _actingUnitId = 0;
            _targetUnitId = 0;
            _attackExecution.Clear();
            _movementRequest = default;
            _reactions.ResetBatch();
            _stopAfterAttack = false;
            _phase = BattleActionPhase.None;
        }

        void ClearMovement()
        {
            _movement.Reset();
        }

        BattleMovementPermission EvaluateMovement(BattleMovementRequest request)
        {
            if (!_board.TryGetUnit(request.UnitId, out HexCharacter unit) || unit.Health <= 0
                || unit.Coordinate != request.From || request.From.DistanceTo(request.To) != 1
                || !_board.IsValid(request.To) || _board.IsOccupied(request.To))
            {
                return BattleMovementPermission.Rejected;
            }

            _reactions.ClearPending();

            if (request.Kind == MovementKind.Normal)
            {
                for (int i = 0; i < _turnOrder.Count; i++)
                {
                    int reactorId = _turnOrder.GetUnitId(i);

                    if (CanUseOpportunityAttack(reactorId, request, out _, out _))
                    {
                        _reactions.Add(new BattleReactionRequest(BattleReactionType.OpportunityAttack,
                            reactorId, request.UnitId, i));
                    }

                    if (CanUseOverwatch(reactorId, request, out _, out _))
                    {
                        _reactions.Add(new BattleReactionRequest(BattleReactionType.Overwatch,
                            reactorId, request.UnitId, i));
                    }
                }
            }

            return _reactions.HasPending ? BattleMovementPermission.PendingReaction : BattleMovementPermission.Allowed;
        }

        bool CanUseOpportunityAttack(int reactorId, BattleMovementRequest request,
            out HexCharacter reactor, out HexCharacter target)
        {
            reactor = null;
            target = null;
            return request.Kind == MovementKind.Normal
                && TryGetOpponents(reactorId, request.UnitId, out reactor, out target)
                && IsVisibleTo(reactor.Team, target.Id)
                && target.Coordinate == request.From
                && reactor.Traits.Has(TraitType.OpportunityAttack)
                && reactor.Coordinate.DistanceTo(request.From) == 1
                && reactor.Coordinate.DistanceTo(request.To) > 1
                && reactor.Skills.IsInRange(SkillType.BasicAttack, reactor.Coordinate.DistanceTo(target.Coordinate))
                && HasSkillLineOfSight(reactor, SkillType.BasicAttack, reactor.Coordinate, target.Coordinate)
                && !_reactions.HasUsedOpportunity(reactorId, target.Id);
        }

        bool TryBeginReaction(out HexAttackStep step)
        {
            step = default;

            while (_state == BattleState.Running && _reactions.TryTakeNext(out BattleReactionRequest reaction))
            {
                if (reaction.TargetId != _movementRequest.UnitId)
                {
                    continue;
                }

                HexCharacter reactor;
                HexCharacter target;

                switch (reaction.Type)
                {
                    case BattleReactionType.OpportunityAttack:
                        if (!CanUseOpportunityAttack(reaction.ReactorId, _movementRequest, out reactor, out target))
                        {
                            continue;
                        }

                        _reactions.RecordOpportunity(reactor.Id, target.Id);
                        break;
                    case BattleReactionType.Overwatch:
                        if (!CanUseOverwatch(reaction.ReactorId, _movementRequest, out reactor, out target))
                        {
                            continue;
                        }

                        reactor.ClearOverwatch();
                        break;
                    default:
                        continue;
                }

                step = IssueAttack(reactor, target, BattleAttackOrigin.Reaction);
                return true;
            }

            _phase = BattleActionPhase.Settling;
            return false;
        }

        bool CanUseOverwatch(int reactorId, BattleMovementRequest request,
            out HexCharacter reactor, out HexCharacter target)
        {
            reactor = null;
            target = null;
            if (request.Kind != MovementKind.Normal
                || !TryGetOpponents(reactorId, request.UnitId, out reactor, out target)
                || !IsVisibleTo(reactor.Team, target.Id)
                || target.Coordinate != request.From || !reactor.IsOverwatching
                || !reactor.Skills.Contains(SkillType.Overwatch))
            {
                return false;
            }

            reactor.Skills.GetRange(SkillType.Overwatch, out int minRange, out int maxRange);
            return reactor.Skills.IsInRange(SkillType.BasicAttack, reactor.Coordinate.DistanceTo(target.Coordinate))
                && HasSkillLineOfSight(reactor, SkillType.BasicAttack, reactor.Coordinate, target.Coordinate)
                && HasSkillLineOfSight(reactor, SkillType.Overwatch, reactor.Coordinate, target.Coordinate)
                && BattleOverwatchArea.Contains(reactor.Coordinate, reactor.OverwatchDirection,
                    maxRange, request.From, minRange);
        }

        HexAttackStep IssueAttack(HexCharacter attacker, HexCharacter target, BattleAttackOrigin origin,
            bool ambush = false)
        {
            BattleAttackContext context = new BattleAttackContext(attacker.Id, target.Id, origin,
                ambush ? BattleAttackKind.Ambush : BattleAttackKind.Basic,
                ambush ? attacker.Skills.Get(SkillType.Ambush).DamageBonus : 0, attacker.IsStealthed);
            if (_attackExecution.Begin(context, attacker, target))
            {
                attacker.PrepareSpirit(SpiritType.None);
            }
            attacker.SetFacing(BattleFacing.GetDirection(attacker.Coordinate, target.Coordinate),
                origin == BattleAttackOrigin.Reaction);
            RefreshDetection();
            _phase = BattleActionPhase.Attacking;
            return new HexAttackStep(attacker.Id, target.Id, target.Coordinate);
        }

        void FinishAttack()
        {
            if (_attackExecution.Context.EndsStealth && _board.TryGetUnit(_attackExecution.Context.AttackerId, out HexCharacter attacker))
            {
                Reveal(attacker);
            }

            _attackExecution.Clear();

            if (IsAssassinLeap)
            {
                FinishSkillMovement();
            }
            else if (_reactions.IsActive || _turnAdvancePending || _stopAfterAttack)
            {
                _phase = !_stopAfterAttack && _state == BattleState.Running && _reactions.HasPending
                    ? BattleActionPhase.ResolvingReactions : BattleActionPhase.Settling;
            }
            else
            {
                ResetAction();
            }
        }

        public bool TrySettleAction()
        {
            if (_phase != BattleActionPhase.Settling)
            {
                return false;
            }

            ResetAction();

            if (_turnAdvancePending)
            {
                _turnAdvancePending = false;

                if (_state == BattleState.Running)
                {
                    _turnOrder.MoveToNextTurn(_board);
                    BeginCurrentTurn();
                }
            }

            return true;
        }

        public void StopAction()
        {
            if (IsSkillMovement)
            {
                StopSkillMovement();
                return;
            }

            _reactions.ClearPending();

            if (_phase == BattleActionPhase.Attacking)
            {
                _stopAfterAttack = true;
                return;
            }

            if (_phase == BattleActionPhase.Moving && _movement.WaitingForCompletion)
            {
                _movement.DiscardRemainingPath();
                _targetUnitId = 0;
                return;
            }

            _phase = BattleActionPhase.Settling;
        }

        bool IsTargetInRange()
        {
            return TryGetOpponents(_actingUnitId, _targetUnitId, out HexCharacter attacker, out HexCharacter target)
                && IsVisibleTo(attacker.Team, target.Id)
                && attacker.Skills.IsInRange(IsQueuedAmbush ? SkillType.Ambush : SkillType.BasicAttack,
                    attacker.Coordinate.DistanceTo(target.Coordinate))
                && HasSkillLineOfSight(attacker, IsQueuedAmbush ? SkillType.Ambush : SkillType.BasicAttack,
                    attacker.Coordinate, target.Coordinate);
        }

        bool IsDetected(HexCharacter target)
        {
            foreach (HexCharacter observer in _board.Units)
            {
                if (observer.Team != target.Team && observer.Health > 0
                    && BattleDetectionArea.Contains(observer.Coordinate, observer.Facing, target.Coordinate))
                {
                    return true;
                }
            }

            return false;
        }

        void RefreshDetection()
        {
            InvalidateVisibility();

            foreach (HexCharacter unit in _board.Units)
            {
                if (unit.IsStealthed && !unit.IsInvisible && IsDetected(unit))
                {
                    unit.ClearStealth();
                }
            }
        }

        void Reveal(HexCharacter unit)
        {
            if (unit.ClearStealth())
            {
                InvalidateVisibility();
            }
        }

        void InvalidateVisibility()
        {
            unchecked
            {
                _visibilityVersion++;
            }
        }

        bool TryGetOpponents(int attackerId, int targetId, out HexCharacter attacker, out HexCharacter target)
        {
            attacker = null;
            target = null;
            return attackerId != targetId
                && _board.TryGetUnit(attackerId, out attacker)
                && _board.TryGetUnit(targetId, out target)
                && attacker.Health > 0 && target.Health > 0
                && attacker.Team != target.Team;
        }

        void BeginCurrentTurn()
        {
            int remaining = _board.Units.Count;

            while (_state == BattleState.Running && remaining-- > 0)
            {
                _reactions.BeginRound(_turnOrder.RoundNumber);

                if (!CurrentUnitId.HasValue || !_board.TryGetUnit(CurrentUnitId.Value, out HexCharacter unit))
                {
                    return;
                }

                unit.BeginTurn(Rules);
                ApplyEnvironmentAt(unit);

                if (unit.Health > 0)
                {
                    return;
                }

                ResetAction();
                _turnAdvancePending = false;

                if (_state == BattleState.Running)
                {
                    _turnOrder.MoveToNextTurn(_board);
                }
            }
        }

        int CountPlayers()
        {
            int count = 0;

            foreach (HexCharacter unit in _board.Units)
            {
                if (unit.Team == BattleTeam.Player)
                {
                    count++;
                }
            }

            return count;
        }

        void CountTeams(out int playerCount, out int enemyCount)
        {
            playerCount = 0;
            enemyCount = 0;

            foreach (HexCharacter unit in _board.Units)
            {
                if (unit.Team == BattleTeam.Player)
                {
                    playerCount++;
                }
                else
                {
                    enemyCount++;
                }
            }
        }

        bool HasBothTeams()
        {
            bool hasPlayer = false;
            bool hasEnemy = false;

            foreach (HexCharacter unit in _board.Units)
            {
                hasPlayer |= unit.Team == BattleTeam.Player;
                hasEnemy |= unit.Team == BattleTeam.Enemy;

                if (hasPlayer && hasEnemy)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
