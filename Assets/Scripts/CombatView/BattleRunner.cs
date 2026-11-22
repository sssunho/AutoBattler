using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed partial class BattleRunner : MonoBehaviour
    {
        [SerializeField]
        BattleInitializationMode _initializationMode;

        [SerializeField]
        HexMapView _mapView;

        [SerializeField]
        BattleUnitRegistration[] _initialUnits = new BattleUnitRegistration[0];

        [SerializeField]
        MercenaryDataList _classData;

        [SerializeField]
        Transform _unitViewRoot;

        [SerializeField, Min(1)]
        int _maxPlayerUnits = 4;

        [SerializeField, Min(0)]
        int _startingActionPoints = 4;

        [SerializeField, Min(1)]
        int _maxActionPoints = 8;

        [SerializeField, Min(0)]
        int _actionPointsPerTurn = 4;

        readonly Dictionary<int, HexCharacterView> _views = new Dictionary<int, HexCharacterView>();
        readonly HashSet<int> _spawnedViewIds = new HashSet<int>();
        readonly List<int> _destroyedUnitIds = new List<int>();
        uint _presentationVersion;
        readonly BattleEnemyAI _enemyAI = new BattleEnemyAI();

        BattleActionPresentation _presentation;
        BattleActionPresentation Presentation => _presentation ?? (_presentation = new BattleActionPresentation(this));

        BattleSimulation _simulation;
        BattleInputController _selectionInput;
        float _hexSize;
        bool _isInitializing;
        bool _isReleasing;
        bool _isUpdating;
        bool _resultPublished;
        int _failedEnemyUnitId;

        public HexMapView MapView => _mapView;
        public bool IsInitialized => _simulation != null;
        public bool IsBusy => _simulation != null && _simulation.IsBusy;
        public bool IsSelectingOverwatch => _selectionInput != null && _selectionInput.IsSelectingOverwatch;
        public bool IsSelectingSpiritImbuement => _selectionInput != null && _selectionInput.IsSelectingSpiritImbuement;
        internal SkillType? SelectedSkill => _selectionInput != null ? _selectionInput.SelectedSkill : null;
        internal IReadOnlyList<SkillChoice> SelectedSkillChoices => _selectionInput != null
            ? _selectionInput.SelectedSkillChoices : System.Array.Empty<SkillChoice>();
        public bool IsSelectingAction => _selectionInput != null && _selectionInput.IsSelectingAction;
        public bool IsMovementPaused => _simulation != null && _simulation.IsMovementPaused;
        public int? CurrentUnitId => _simulation != null ? _simulation.CurrentUnitId : null;
        public BattleState State => _simulation != null ? _simulation.State : BattleState.Preparing;
        public int RoundNumber => _simulation != null ? _simulation.RoundNumber : 0;
        public BattleRules Rules => _simulation != null ? _simulation.Rules : null;
        public BattleResult Result => _simulation != null ? _simulation.Result : null;

        internal int MaxPlayerUnits => _maxPlayerUnits;
        internal bool UsesExternalInitialization => _initializationMode == BattleInitializationMode.External;

        internal event Action<BattleAttackResult, HexCharacterView, Vector3> AttackCompleted;
        internal event Action PresentationStopped;
        internal event Action<BattleResult> BattleCompleted;

        void Start()
        {
            if (_initializationMode != BattleInitializationMode.Manual || _simulation != null || _isInitializing)
            {
                return;
            }

            if (!IsMapValid())
            {
                StopWithError("BattleRunner requires a non-empty HexMapView with a positive finite hex size.");
                return;
            }

            try
            {
                BattleRules rules = new BattleRules(_maxPlayerUnits, _startingActionPoints, _maxActionPoints,
                    _actionPointsPerTurn);
                _simulation = new BattleSimulation(_mapView.Width, _mapView.Height, rules);
            }
            catch (ArgumentException exception)
            {
                StopWithError(exception.Message);
                return;
            }

            _hexSize = _mapView.HexSize;

            if (_initialUnits != null)
            {
                for (int i = 0; i < _initialUnits.Length; i++)
                {
                    BattleUnitRegistration registration = _initialUnits[i];

                    if (registration == null
                        || !TryRegisterCharacter(registration.View, registration.Team, registration.UnitClass, out _))
                    {
                        StopWithError("BattleRunner could not register an initial unit. Check its view, starting position, party limit, and overlapping or duplicate units.");
                        return;
                    }
                }
            }

            _simulation.TryStartBattle();
        }

        internal bool TryInitializeBattle(HexMapView mapView, IReadOnlyList<BattleSpawnInput> inputs,
            int randomSeed, out string error)
        {
            if (!UsesExternalInitialization || !isActiveAndEnabled || _simulation != null
                || _isInitializing || _isReleasing)
            {
                error = "BattleRunner must be active, externally initialized, and without an existing battle.";
                return false;
            }

            BattleRules rules;

            try
            {
                rules = new BattleRules(_maxPlayerUnits, _startingActionPoints, _maxActionPoints,
                    _actionPointsPerTurn);
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }

            return TryInitializeBattle(mapView, inputs, rules, randomSeed, out error);
        }

        internal bool TryInitializeBattle(HexMapView mapView, IReadOnlyList<BattleSpawnInput> inputs,
            BattleRules rules, int randomSeed, out string error)
        {
            if (_simulation != null)
            {
                error = "BattleRunner cannot replace an existing battle during initialization.";
                return false;
            }

            if (!ValidateRestartInputs(mapView, inputs, rules, out error))
            {
                return false;
            }

            HexMapView previousMapView = _mapView;
            bool createdBattle = false;
            bool initialized = false;
            _isInitializing = true;

            try
            {
                BattleSimulation simulation = new BattleSimulation(mapView.Width, mapView.Height, rules, randomSeed);
                _mapView = mapView;
                _hexSize = mapView.HexSize;
                _simulation = simulation;
                _resultPublished = false;
                createdBattle = true;

                for (int i = 0; i < inputs.Count; i++)
                {
                    if (!TrySpawnInput(inputs[i], simulation))
                    {
                        error = $"Could not create battle participant {i}. Check its view and animation setup.";
                        return false;
                    }
                }

                if (!isActiveAndEnabled || _simulation != simulation || !IsConfigurationValid()
                    || !simulation.TryStartBattle() || simulation.State != BattleState.Running)
                {
                    error = "The battle could not start after its participants were created.";
                    return false;
                }

                initialized = true;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (createdBattle && !initialized)
                {
                    ReleaseBattle();
                    _mapView = previousMapView;
                }

                _isInitializing = false;
            }
        }

        internal void ShutdownBattle()
        {
            if (_isReleasing)
            {
                return;
            }

            bool wasInitializing = _isInitializing;
            _isInitializing = true;

            try
            {
                ReleaseBattle();

                if (UsesExternalInitialization)
                {
                    _mapView = null;
                }
            }
            finally
            {
                _isInitializing = wasInitializing;
            }
        }

        internal bool ValidateRestartInputs(HexMapView mapView, IReadOnlyList<BattleSpawnInput> inputs,
            BattleRules rules, out string error)
        {
            if (!UsesExternalInitialization || !isActiveAndEnabled || _isInitializing || _isReleasing)
            {
                error = "BattleRunner must be active and externally initialized before preparing a battle.";
                return false;
            }

            if (_initialUnits != null && _initialUnits.Length != 0)
            {
                error = "External initialization cannot include scene unit registrations.";
                return false;
            }

            if (rules == null)
            {
                error = "Battle rules are required.";
                return false;
            }

            return ValidateSpawnInputs(mapView, inputs, rules, out error);
        }

        bool ValidateSpawnInputs(HexMapView mapView, IReadOnlyList<BattleSpawnInput> inputs,
            BattleRules rules, out string error)
        {
            error = null;

            if (!IsMapValid(mapView) || !mapView.isActiveAndEnabled)
            {
                error = "The battle requires an active, non-empty hex map with a positive finite hex size.";
                return false;
            }

            Matrix4x4 mapMatrix = mapView.transform.localToWorldMatrix;

            for (int i = 0; i < 16; i++)
            {
                if (float.IsNaN(mapMatrix[i]) || float.IsInfinity(mapMatrix[i]))
                {
                    error = "The battle map transform must be finite.";
                    return false;
                }
            }

            float determinant = mapMatrix.determinant;

            if (determinant == 0f || float.IsNaN(determinant) || float.IsInfinity(determinant))
            {
                error = "The battle map requires a finite, non-zero transform scale.";
                return false;
            }

            if (inputs == null || inputs.Count == 0)
            {
                error = "The battle requires participant inputs.";
                return false;
            }

            if (_unitViewRoot != null && !_unitViewRoot.gameObject.activeInHierarchy)
            {
                error = "The battle unit view root must be active.";
                return false;
            }

            HashSet<HexCoordinate> coordinates = new HashSet<HexCoordinate>();
            int playerCount = 0;
            int enemyCount = 0;

            for (int i = 0; i < inputs.Count; i++)
            {
                BattleSpawnInput input = inputs[i];

                if (input == null || input.Prefab == null || !input.Prefab.enabled
                    || !input.Prefab.gameObject.activeSelf || input.Prefab.IsInitialized || input.Stats == null || input.Skills == null
                    || (input.Team != BattleTeam.Player && input.Team != BattleTeam.Enemy)
                    || !MercenarySpecializationRules.IsCompatible(input.Stats.UnitClass, input.Specialization)
                    || (uint)input.Facing >= 6 || !mapView.IsValid(input.Coordinate))
                {
                    error = $"Battle participant {i} has an invalid prefab, stats, specialization, team, coordinate, or facing.";
                    return false;
                }

                if (!coordinates.Add(input.Coordinate))
                {
                    error = $"Battle participant {i} overlaps another participant.";
                    return false;
                }

                if (input.Traits.Has(TraitType.DirectionalPhysicalNegation) && input.TraitSettings == null)
                {
                    error = $"Battle participant {i} requires directional physical negation settings.";
                    return false;
                }

                if (input.Team == BattleTeam.Player)
                {
                    playerCount++;
                }
                else
                {
                    enemyCount++;
                }
            }

            if (playerCount == 0 || enemyCount == 0 || playerCount > rules.MaxPlayerUnits)
            {
                error = "The battle requires both teams and cannot exceed the player unit limit.";
                return false;
            }

            return true;
        }

        bool TrySpawnInput(BattleSpawnInput input, BattleSimulation simulation)
        {
            if (!isActiveAndEnabled || _simulation != simulation)
            {
                return false;
            }

            Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;
            HexCharacterView view = Instantiate(input.Prefab, parent);
            bool registered = false;

            try
            {
                registered = isActiveAndEnabled && _simulation == simulation
                    && TryRegisterAt(view, input.Team, input.Stats, input.Coordinate, input.Facing, true, out _,
                        input.Traits, input.TraitSettings, input.Specialization, input.Skills, input.Equipment);
                return registered && view.ValidateAttackAnimation();
            }
            finally
            {
                if (!registered && view != null)
                {
                    view.Unbind();
                    view.gameObject.SetActive(false);
                    DestroySpawnedView(view);
                }
            }
        }

        void Update()
        {
            if (_simulation == null || _isInitializing || _isReleasing || _isUpdating || !isActiveAndEnabled)
            {
                return;
            }

            _isUpdating = true;

            try
            {
                BattleSimulation simulation = _simulation;

                if (!IsConfigurationValid())
                {
                    StopWithError("BattleRunner stopped because its map became invalid or its size changed.");
                    return;
                }

                DrainDamage(simulation);
                if (!CanContinuePresentation(simulation))
                {
                    return;
                }

                RemoveDestroyedUnits();

                if (!CanContinuePresentation(simulation))
                {
                    return;
                }

                AdvanceAnimations(Time.deltaTime);

                if (!CanContinuePresentation(simulation))
                {
                    return;
                }

                if (simulation.IsBusy)
                {
                    Presentation.UpdateCurrentAction(Time.deltaTime);
                    TryPublishBattleResult(simulation);
                    return;
                }

                if (TryPublishBattleResult(simulation))
                {
                    return;
                }

                ExecuteEnemyDecision();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        void AdvanceAnimations(float deltaTime)
        {
            foreach (KeyValuePair<int, HexCharacterView> entry in _views)
            {
                if (entry.Value != null && entry.Value.isActiveAndEnabled)
                {
                    entry.Value.AdvanceAnimation(deltaTime);
                }
            }
        }

        bool TryPublishBattleResult(BattleSimulation simulation)
        {
            if (_simulation != simulation || _isInitializing || _isReleasing || !isActiveAndEnabled)
            {
                return true;
            }

            DrainDamage(simulation);
            if (!CanContinuePresentation(simulation) || _isDrainingDamage || simulation.HasPendingDamage)
            {
                return true;
            }

            if (_resultPublished)
            {
                return true;
            }

            if (simulation.State == BattleState.Waiting && !simulation.IsBusy)
            {
                RemoveDestroyedUnits();

                if (_simulation != simulation || _isInitializing || _isReleasing || !isActiveAndEnabled)
                {
                    return true;
                }
            }

            BattleResult result = simulation.Result;

            if (result == null && !simulation.TryFinalizeResult(out result))
            {
                return false;
            }

            _resultPublished = true;
            BattleCompleted?.Invoke(result);
            return true;
        }

        public bool TryRegisterCharacter(HexCharacterView view, BattleTeam team, MercenaryClass unitClass, out int unitId)
        {
            unitId = 0;

            if (!CanAcceptCommands() || view == null || view.IsInitialized
                || !view.isActiveAndEnabled || _simulation.State != BattleState.Preparing)
            {
                return false;
            }

            if (!TryFindNearestCoordinate(view.transform.position, out HexCoordinate coordinate))
            {
                Debug.LogError("BattleRunner could not find a valid starting hex for the character.", view);
                return false;
            }

            return TryRegisterAt(view, team, unitClass, coordinate, false, out unitId);
        }

        public bool TrySpawnCharacter(HexCharacterView prefab, BattleTeam team, MercenaryClass unitClass,
            HexCoordinate coordinate, out int unitId)
        {
            unitId = 0;

            if (!CanAcceptCommands() || prefab == null || _simulation.State != BattleState.Preparing
                || !_simulation.Board.IsValid(coordinate) || _simulation.Board.IsOccupied(coordinate))
            {
                return false;
            }

            Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;
            HexCharacterView view = Instantiate(prefab, parent);

            if (TryRegisterAt(view, team, unitClass, coordinate, true, out unitId))
            {
                return true;
            }

            view.gameObject.SetActive(false);
            DestroySpawnedView(view);
            return false;
        }

        public bool TryGetCharacter(int unitId, out HexCharacter character)
        {
            character = null;
            return _simulation != null && _simulation.Board.TryGetUnit(unitId, out character);
        }

        internal bool TryGetEnvironmentContext(out BattleSimulation simulation, out HexMapView map)
        {
            simulation = null;
            map = null;

            if (!CanAcceptCommands() || !_mapView.isActiveAndEnabled)
            {
                return false;
            }

            simulation = _simulation;
            map = _mapView;
            return true;
        }

        internal bool TryApplyGroundEffect(HexCoordinate coordinate, BattleGroundEffect effect, out string error)
        {
            if (!TryGetEditableEnvironment(out BattleSimulation simulation, out error)
                || !simulation.TryApplyGroundEffect(coordinate, effect, out error))
            {
                return false;
            }

            DrainDamage(simulation);
            return true;
        }

        internal bool TryApplyAirEffect(HexCoordinate coordinate, BattleAirEffect effect, out string error)
        {
            if (!TryGetEditableEnvironment(out BattleSimulation simulation, out error)
                || !simulation.TryApplyAirEffect(coordinate, effect, out error))
            {
                return false;
            }

            DrainDamage(simulation);
            return true;
        }

        internal bool TryClearEnvironment(HexCoordinate coordinate, bool clearGround, bool clearAir, out string error)
        {
            return TryGetEditableEnvironment(out BattleSimulation simulation, out error)
                && simulation.TryClearEnvironment(coordinate, clearGround, clearAir, out error);
        }

        bool TryGetEditableEnvironment(out BattleSimulation simulation, out string error)
        {
            error = null;

            if (!TryGetEnvironmentContext(out simulation, out _) || _isUpdating || IsSelectingAction)
            {
                simulation = null;
                error = "현재 전장 연결이 유효하지 않거나 행동을 선택·처리 중입니다.";
                return false;
            }

            return true;
        }

        internal bool TryGetRangeContext(out BattleSimulation simulation, out HexCharacter unit, out bool canMove)
        {
            simulation = null;
            unit = null;
            canMove = false;

            if (!CanAcceptCommands() || _simulation.State != BattleState.Running || _simulation.IsBusy
                || !_simulation.CurrentUnitId.HasValue
                || !_simulation.Board.TryGetUnit(_simulation.CurrentUnitId.Value, out HexCharacter current)
                || current.Team != BattleTeam.Player)
            {
                return false;
            }

            simulation = _simulation;
            unit = current;
            canMove = _views.TryGetValue(current.Id, out HexCharacterView view) && view != null && view.IsReady;
            return true;
        }

        internal bool TryBeginOverwatchSelection(BattleInputController input, int unitId,
            out BattleSimulation simulation)
        {
            return TryBeginActionSelection(input, unitId, SkillType.Overwatch, out simulation);
        }

        internal bool TryBeginActionSelection(BattleInputController input, int unitId,
            SkillType selection, out BattleSimulation simulation)
        {
            simulation = null;

            if (!CanBeginSkillSelection(input, unitId, selection))
            {
                return false;
            }

            _selectionInput = input;
            simulation = _simulation;
            return true;
        }

        internal bool CanBeginSkillSelection(BattleInputController input, int unitId, SkillType skill)
        {
            return CanAcceptCommands() && _mapView.isActiveAndEnabled && !IsSelectingAction
                && input != null && input.CanDisplayActionRange(this)
                && CanSelectAction(unitId, skill)
                && _views.TryGetValue(unitId, out HexCharacterView view) && view != null && view.IsReady;
        }

        internal bool IsOverwatchSelectionValid(BattleInputController input, BattleSimulation simulation, int unitId)
        {
            return IsActionSelectionValid(input, simulation, unitId, SkillType.Overwatch);
        }

        internal bool IsActionSelectionValid(BattleInputController input, BattleSimulation simulation,
            int unitId, SkillType selection)
        {
            return input != null && _selectionInput == input && CanAcceptCommands() && _mapView.isActiveAndEnabled
                && _simulation == simulation
                && CanSelectAction(unitId, selection)
                && _views.TryGetValue(unitId, out HexCharacterView view) && view != null && view.IsReady;
        }

        bool CanSelectAction(int unitId, SkillType selection)
        {
            if (_simulation.State != BattleState.Running || _simulation.IsBusy
                || _simulation.CurrentUnitId != unitId
                || !_simulation.Board.TryGetUnit(unitId, out HexCharacter unit)
                || unit.Health <= 0 || unit.Team != BattleTeam.Player)
            {
                return false;
            }

            return _simulation.CanSelectSkill(unitId, selection).CanUse;
        }

        internal void EndActionSelection(BattleInputController input)
        {
            if (_selectionInput == input)
            {
                _selectionInput = null;
            }
        }

        internal bool TryConfirmSkillPreview(BattleInputController input)
        {
            if (input == null || !input.TryGetSkillCommand(this, out BattleSimulation simulation,
                out SkillUseRequest request))
            {
                return false;
            }

            return TryConfirmSelectedSkill(input, simulation, request);
        }

        bool TryConfirmSelectedSkill(BattleInputController input, BattleSimulation simulation, SkillUseRequest request)
        {
            if (!IsActionSelectionValid(input, simulation, request.UnitId, request.Skill)
                || input.SelectedSkill != request.Skill || !TryExecuteSkillRequest(simulation, request))
            {
                return false;
            }

            input.CancelActionSelection();
            return true;
        }

        bool TryExecuteSkillRequest(BattleSimulation simulation, SkillUseRequest request)
        {
            _views.TryGetValue(request.UnitId, out HexCharacterView actor);
            if (!ReferenceEquals(simulation, _simulation)
                || actor == null && (request.Skill != SkillType.BasicAttack || request.Path != null && request.Path.Count > 0))
            {
                return false;
            }

            bool movesTarget = request.Skill == SkillType.PullSelf || request.Skill == SkillType.PullTarget;
            bool leap = request.LeapPreview.HasValue;
            HexCharacterView target = null;
            if ((movesTarget || leap) && (!actor.IsReady
                || !_views.TryGetValue(request.TargetId, out target) || target == null || !target.IsReady))
            {
                return false;
            }

            if (!simulation.TryUseSkill(request))
            {
                return false;
            }

            Presentation.CaptureSkill(simulation, request, actor, target, movesTarget, leap);

            if (request.Skill == SkillType.Overwatch)
            {
                _failedEnemyUnitId = 0;
                actor.ApplyFacing();
            }

            return true;
        }

        internal bool TryConfirmOverwatch(BattleInputController input, BattleSimulation simulation,
            int unitId, HexDirection direction)
        {
            return TryConfirmSelectedSkill(input, simulation,
                new SkillUseRequest(SkillType.Overwatch, unitId, direction: direction));
        }

        internal bool TryConfirmStealth(BattleInputController input, BattleSimulation simulation, int unitId)
        {
            return TryConfirmSelectedSkill(input, simulation, new SkillUseRequest(SkillType.Stealth, unitId));
        }

        internal bool TryConfirmPullSkill(BattleInputController input, BattleSimulation simulation,
            int unitId, int targetId, SkillType skill)
        {
            return ReferenceEquals(simulation, _simulation) && input != null
                && input.TryGetPullPath(this, unitId, targetId, skill, out _)
                && TryConfirmSkillPreview(input);
        }

        internal bool TryConfirmAssassinLeap(BattleInputController input, BattleSimulation simulation,
            int unitId, int targetId, AssassinLeapPreview preview)
        {
            return ReferenceEquals(simulation, _simulation) && input != null
                && input.TryGetSkillCommand(this, out _, out SkillUseRequest request)
                && request.Skill == SkillType.AssassinLeap && request.UnitId == unitId && request.TargetId == targetId
                && request.LeapPreview.HasValue && request.LeapPreview.Value.Equals(preview)
                && TryConfirmSkillPreview(input);
        }

        internal bool TryCopyOverwatchStates(List<BattleOverwatchState> destination)
        {
            destination?.Clear();

            if (destination == null || !CanAcceptCommands() || _simulation.State != BattleState.Running)
            {
                return false;
            }

            _simulation.CopyOverwatchStates(destination);
            return true;
        }

        internal bool TryCopyActionRange(List<HexCoordinate> movementDestinations, List<BattleAttackTarget> attackTargets)
        {
            movementDestinations?.Clear();
            attackTargets?.Clear();

            if (movementDestinations == null || attackTargets == null
                || !TryGetRangeContext(out BattleSimulation simulation, out HexCharacter unit, out bool canMove)
                || !simulation.TryGetActionRange(unit.Id, movementDestinations, attackTargets))
            {
                return false;
            }

            if (!canMove)
            {
                movementDestinations.Clear();
            }

            return true;
        }

        public bool TryGetUnitId(HexCharacterView view, out int unitId)
        {
            unitId = 0;

            if (view == null || !_views.TryGetValue(view.UnitId, out HexCharacterView registered) || registered != view)
            {
                return false;
            }

            unitId = view.UnitId;
            return true;
        }

        internal void CopyRegisteredViews(List<HexCharacterView> destination)
        {
            destination.Clear();

            foreach (KeyValuePair<int, HexCharacterView> entry in _views)
            {
                if (entry.Value != null)
                {
                    destination.Add(entry.Value);
                }
            }
        }

        public bool TryMove(int unitId, HexCoordinate destination)
        {
            if (!CanAcceptCommands() || IsSelectingAction || !_views.TryGetValue(unitId, out HexCharacterView view)
                || view == null || !view.IsReady || !_simulation.TryMove(unitId, destination))
            {
                return false;
            }

            Presentation.CaptureActor(view);
            return true;
        }

        internal bool TryConfirmMovementPreview(BattleInputController input)
        {
            if (!CanAcceptCommands() || IsSelectingAction || input == null
                || !input.TryGetMovementCommand(this, out BattleSimulation simulation, out int unitId,
                    out int targetId, out HexCoordinate destination, out IReadOnlyList<HexCoordinate> path)
                || !ReferenceEquals(simulation, _simulation))
            {
                return false;
            }

            _views.TryGetValue(unitId, out HexCharacterView view);

            if (path.Count > 0 && (view == null || !view.IsReady))
            {
                return false;
            }

            bool accepted = targetId == 0
                ? simulation.TryMove(unitId, destination, path)
                : TryExecuteSkillRequest(simulation, new SkillUseRequest(SkillType.BasicAttack, unitId, targetId, path: path));

            if (accepted && view != null)
            {
                Presentation.CaptureActor(view);
            }

            return accepted;
        }

        internal bool TryCopyMovementDisplayPath(BattleInputController input, List<HexCoordinate> destination)
        {
            destination?.Clear();

            if (destination == null || !CanAcceptCommands() || IsSelectingAction
                || _simulation.State != BattleState.Running)
            {
                return false;
            }

            if (_simulation.IsBusy)
            {
                return _simulation.Phase == BattleActionPhase.Moving && !_simulation.IsPullMovement
                    && !_simulation.IsAssassinLeap
                    && _simulation.Board.TryGetUnit(_simulation.ActingUnitId, out HexCharacter actor)
                    && actor.Team == BattleTeam.Player && actor.Health > 0
                    && _simulation.TryCopyRemainingMovementPath(destination);
            }

            return input != null && input.TryCopyMovementPreview(this, destination);
        }

        public bool TryEndTurn(int unitId)
        {
            if (!CanAcceptCommands() || IsSelectingAction || !_simulation.TryEndTurn(unitId))
            {
                return false;
            }

            _failedEnemyUnitId = 0;
            DrainDamage(_simulation);
            return true;
        }

        public bool TryAttack(int attackerId, int targetId)
        {
            if (!CanAcceptCommands() || IsSelectingAction
                || !_simulation.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, attackerId, targetId)))
            {
                return false;
            }

            if (_views.TryGetValue(attackerId, out HexCharacterView view) && view != null)
            {
                Presentation.CaptureActor(view);
            }

            return true;
        }

        bool TryAttack(int attackerId, int targetId, HexCoordinate attackPosition)
        {
            if (!CanAcceptCommands() || IsSelectingAction
                || !_simulation.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, attackerId, targetId, attackPosition: attackPosition)))
            {
                return false;
            }

            if (_views.TryGetValue(attackerId, out HexCharacterView view) && view != null)
            {
                Presentation.CaptureActor(view);
            }

            return true;
        }

        public bool PauseMovement()
        {
            return CanAcceptCommands() && _simulation.PauseMovement();
        }

        public bool ResumeMovement()
        {
            return CanAcceptCommands() && _simulation.ResumeMovement();
        }

        public void CancelAction()
        {
            if (_selectionInput != null)
            {
                _selectionInput.CancelActionSelection();
            }

            if (!CanAcceptCommands() || _simulation.HasPendingReactions
                || _simulation.HasStartedPullMovement
                || _simulation.HasStartedAssassinLeap
                || _simulation.Phase == BattleActionPhase.Settling)
            {
                return;
            }

            int unitId = _simulation.ActingUnitId;
            _simulation.CancelAction();

            if (_simulation.Phase == BattleActionPhase.Attacking)
            {
                return;
            }

            if (_simulation.IsWaitingForMovementCompletion)
            {
                if (_simulation.Board.TryGetUnit(unitId, out HexCharacter character) && character.Team == BattleTeam.Enemy)
                {
                    _failedEnemyUnitId = unitId;
                }

                return;
            }

            Presentation.ClearPresentation(unitId);
        }

        public bool RemoveCharacter(int unitId)
        {
            if (_isInitializing || _isReleasing || _simulation == null || !_views.ContainsKey(unitId))
            {
                return false;
            }

            BattleSimulation simulation = _simulation;
            int actingUnitId = simulation.ActingUnitId;
            bool wasReaction = simulation.HasPendingReactions;

            if (!simulation.RemoveUnit(unitId))
            {
                return false;
            }

            if (actingUnitId != 0 && simulation.ActingUnitId != actingUnitId)
            {
                Presentation.ClearPresentation(actingUnitId, !wasReaction);
            }

            if (_simulation != simulation || _isReleasing)
            {
                return true;
            }

            ReleaseView(unitId);
            return true;
        }

        void ReleaseView(int unitId)
        {
            if (!_views.TryGetValue(unitId, out HexCharacterView view))
            {
                return;
            }

            _views.Remove(unitId);
            bool spawned = _spawnedViewIds.Remove(unitId);

            if (view != null)
            {
                view.Unbind();
                view.gameObject.SetActive(false);

                if (spawned)
                {
                    DestroySpawnedView(view);
                }
            }
        }

        bool TryRegisterAt(HexCharacterView view, BattleTeam team, MercenaryClass unitClass,
            HexCoordinate coordinate, bool spawned, out int unitId)
        {
            unitId = 0;

            if (unitClass < MercenaryClass.Warrior || unitClass > MercenaryClass.Archer
                || !view.TryGetInitialFacing(_mapView, out HexDirection facing)
                || _classData == null || !_classData.Validate(out _)
                || !_classData.TryCreateSkillSet(unitClass, MercenarySpecialization.None, out SkillSet skills, out _))
            {
                return false;
            }

            TraitCollection traits = TraitCollection.Empty;

            if (unitClass == MercenaryClass.Warrior || unitClass == MercenaryClass.Rogue)
            {
                TraitType traitType = unitClass == MercenaryClass.Warrior
                    ? TraitType.OpportunityAttack : TraitType.RearGuaranteedCritical;
                traits = new TraitCollection(new[]
                {
                    new Trait(traitType, new TraitSource(TraitSourceKind.Class, (int)unitClass))
                });
            }

            return TryRegisterAt(view, team, BattleUnitStats.ForClass(unitClass), coordinate, facing, spawned,
                out unitId, traits, skills: skills);
        }

        bool TryRegisterAt(HexCharacterView view, BattleTeam team, BattleUnitStats stats,
            HexCoordinate coordinate, HexDirection facing, bool spawned, out int unitId,
            TraitCollection traits = null, BattleTraitSettings traitSettings = null,
            MercenarySpecialization specialization = MercenarySpecialization.None, SkillSet skills = null,
            BattleEquipmentLoadout equipment = null)
        {
            unitId = 0;

            if (view == null || view.IsInitialized || !view.isActiveAndEnabled
                || !_simulation.TrySpawnUnit(team, coordinate, stats, facing, out HexCharacter character,
                    traits, traitSettings, specialization, skills, equipment))
            {
                return false;
            }

            if (!view.TryBind(character, _mapView))
            {
                _simulation.RemoveUnit(character.Id);
                return false;
            }

            _views.Add(character.Id, view);

            if (spawned)
            {
                _spawnedViewIds.Add(character.Id);
            }

            unitId = character.Id;
            return true;
        }

        bool CanContinuePresentation(BattleSimulation simulation)
        {
            return simulation != null && _simulation == simulation && isActiveAndEnabled
                && !_isInitializing && !_isReleasing;
        }

        void PublishAttackResult(BattleAttackResult result, HexCharacterView view, Vector3 position)
        {
            AttackCompleted?.Invoke(result, view, position);
        }

        void ExecuteEnemyDecision()
        {
            if (_simulation.State != BattleState.Running || !_simulation.CurrentUnitId.HasValue)
            {
                return;
            }

            int unitId = _simulation.CurrentUnitId.Value;

            if (!_simulation.Board.TryGetUnit(unitId, out HexCharacter character) || character.Team != BattleTeam.Enemy)
            {
                return;
            }

            if (_failedEnemyUnitId == unitId)
            {
                TryEndTurn(unitId);
                return;
            }

            BattleAiDecision decision = _enemyAI.Decide(_simulation);
            bool accepted;

            switch (decision.Kind)
            {
                case BattleAiDecisionKind.Move:
                    accepted = TryMove(decision.UnitId, decision.Destination);
                    break;
                case BattleAiDecisionKind.Attack:
                    accepted = TryAttack(decision.UnitId, decision.TargetId, decision.Destination);
                    break;
                case BattleAiDecisionKind.EndTurn:
                    TryEndTurn(unitId);
                    return;
                default:
                    return;
            }

            if (!accepted)
            {
                TryEndTurn(unitId);
            }
        }

        void RemoveDestroyedUnits()
        {
            BattleSimulation simulation = _simulation;
            _destroyedUnitIds.Clear();

            foreach (KeyValuePair<int, HexCharacterView> entry in _views)
            {
                if (entry.Value == null)
                {
                    _destroyedUnitIds.Add(entry.Key);
                }
            }

            for (int i = 0; i < _destroyedUnitIds.Count; i++)
            {
                RemoveCharacter(_destroyedUnitIds[i]);

                if (!CanContinuePresentation(simulation))
                {
                    return;
                }
            }
        }

        bool TryFindNearestCoordinate(Vector3 position, out HexCoordinate coordinate)
        {
            coordinate = default;
            double nearestDistance = double.PositiveInfinity;

            for (int r = 0; r < _mapView.Height; r++)
            {
                for (int q = 0; q < _mapView.Width; q++)
                {
                    HexCoordinate candidate = new HexCoordinate(q, r);
                    Vector3 center = _mapView.AxialToWorld(candidate);
                    double x = (double)center.x - position.x;
                    double y = (double)center.y - position.y;
                    double z = (double)center.z - position.z;
                    double distance = x * x + y * y + z * z;

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        coordinate = candidate;
                    }
                }
            }

            return !double.IsPositiveInfinity(nearestDistance);
        }

        bool CanAcceptCommands()
        {
            return !_isInitializing && !_isReleasing && isActiveAndEnabled
                && _simulation != null && IsConfigurationValid();
        }

        bool IsConfigurationValid()
        {
            return IsMapValid() && _mapView.Width == _simulation.Board.Width
                && _mapView.Height == _simulation.Board.Height && _mapView.HexSize == _hexSize;
        }

        bool IsMapValid()
        {
            return IsMapValid(_mapView);
        }

        static bool IsMapValid(HexMapView mapView)
        {
            return mapView != null && mapView.Width > 0 && mapView.Height > 0
                && mapView.HexSize > 0f && !float.IsNaN(mapView.HexSize) && !float.IsInfinity(mapView.HexSize);
        }

        void OnDisable()
        {
            _presentationVersion++;
            if (_isReleasing)
            {
                return;
            }

            if (_selectionInput != null)
            {
                _selectionInput.CancelActionSelection();
            }

            Presentation.FinishActionWithoutPresentation();
            PresentationStopped?.Invoke();
        }

        void OnDestroy()
        {
            ReleaseBattle();
        }

        void ReleaseBattle()
        {
            if (_isReleasing)
            {
                return;
            }

            _isReleasing = true;

            try
            {
                if (_selectionInput != null)
                {
                    _selectionInput.CancelActionSelection();
                }

                Presentation.FinishActionWithoutPresentation();

                foreach (KeyValuePair<int, HexCharacterView> entry in _views)
                {
                    if (entry.Value != null)
                    {
                        entry.Value.Unbind();

                        if (_spawnedViewIds.Contains(entry.Key))
                        {
                            entry.Value.gameObject.SetActive(false);
                            DestroySpawnedView(entry.Value);
                        }
                    }
                }

                _views.Clear();
                _spawnedViewIds.Clear();
                _destroyedUnitIds.Clear();
                _failedEnemyUnitId = 0;
                _presentation?.ResetMovement();
                _simulation?.DiscardPendingDamage();
                _simulation = null;
                _resultPublished = false;
                PresentationStopped?.Invoke();
            }
            finally
            {
                _isReleasing = false;
            }
        }

        static void DestroySpawnedView(HexCharacterView view)
        {
            if (Application.isPlaying)
            {
                Destroy(view.gameObject);
            }
            else
            {
                DestroyImmediate(view.gameObject);
            }
        }

        void StopWithError(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
            ReleaseBattle();
        }
    }
}
