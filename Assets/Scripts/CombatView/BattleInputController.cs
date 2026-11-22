using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RaggedBanner.Combat.Unity
{
    [DefaultExecutionOrder(-100)]
    public sealed class BattleInputController : MonoBehaviour
    {
        [SerializeField]
        Camera _inputCamera;

        [SerializeField]
        BattleRunner _runner;

        [SerializeField]
        Key _endTurnKey = Key.E;

        [SerializeField]
        LayerMask _characterLayerMask = Physics.DefaultRaycastLayers;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        readonly List<HexCoordinate> _selectionPath = new List<HexCoordinate>();
        SkillInputPreview _selectionPreview;
        bool _hasSelectionPreview;
        SkillInputMode _selectionMode;
        uint _previewOccupancyVersion;
        uint _previewEnvironmentVersion;
        uint _previewVisibilityVersion;
        uint _previewCooldownVersion;
        HexCoordinate _previewActorOrigin;
        HexCoordinate _previewTargetOrigin;
        int _previewActionPoints;
        int _previewMovementPoints;
        readonly List<HexCoordinate> _movementPreviewPath = new List<HexCoordinate>();
        BattleSimulation _movementPreviewSimulation;
        int _movementPreviewUnitId;
        int _movementPreviewTargetId;
        int _movementPreviewRound;
        int _movementPreviewPoints;
        int _movementPreviewActionPoints;
        uint _movementPreviewOccupancyVersion;
        uint _movementPreviewEnvironmentVersion;
        uint _movementPreviewVisibilityVersion;
        HexCoordinate _movementPreviewOrigin;
        HexCoordinate _movementPreviewDestination;
        EventSystem _eventSystem;
        PointerEventData _pointerData;
        BattleRunner _selectionRunner;
        BattleSimulation _selectionSimulation;
        int _selectionUnitId;
        int _selectionRound;
        HexDirection _selectionDirection;
        bool _hasPreviewPosition;
        SkillType _selection;

        internal SkillType? SelectedSkill => IsSelectingAction ? _selection : (SkillType?)null;
        internal SkillInputMode? SelectedInputMode => IsSelectingAction ? _selectionMode : (SkillInputMode?)null;
        internal IReadOnlyList<SkillChoice> SelectedSkillChoices => IsSelectingAction
            ? BattleSkillChoices.Get(_selection) : System.Array.Empty<SkillChoice>();

        internal bool IsSelectingSpiritImbuement => IsSelectingAction && _selection == SkillType.SpiritImbuement;

        internal bool IsSelectingOverwatch => IsSelectingAction && _selection == SkillType.Overwatch;
        internal bool IsSelectingStealth => IsSelectingAction && _selection == SkillType.Stealth;
        internal bool IsSelectingAmbush => IsSelectingAction && _selection == SkillType.Ambush;
        internal bool IsSelectingInvisibility => IsSelectingAction && _selection == SkillType.Invisibility;
        internal bool IsSelectingAssassinLeap => IsSelectingAction && _selection == SkillType.AssassinLeap;

        internal bool IsSelectingAction
        {
            get
            {
                if (_selectionSimulation != null && !IsSelectionValid())
                {
                    CancelActionSelection();
                }

                return _selectionSimulation != null;
            }
        }

        void Start()
        {
            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }

            if (_inputCamera == null || _runner == null)
            {
                Debug.LogError("Battle input requires a camera and BattleRunner.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (IsSelectingAction)
            {
                UpdateActionSelection();
                return;
            }

            if (!TryGetCurrentPlayer(out HexCharacter attacker))
            {
                ClearMovementPreview();
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                ClearMovementPreview();
                if (!IsPointerOverUI(mouse.position.ReadValue()))
                {
                    _runner.CancelAction();
                }

                return;
            }

            if (_runner.IsBusy)
            {
                ClearMovementPreview();
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && !IsEditingText())
            {
                for (int slot = 1; slot <= 9; slot++)
                {
                    Key key = (Key)((int)Key.Digit1 + slot - 1);

                    if (keyboard[key].wasPressedThisFrame && attacker.Skills.TryGetSlot(slot, out _))
                    {
                        ClearMovementPreview();
                        TryBeginSkillSlot(slot);
                        return;
                    }
                }

                if (_endTurnKey != Key.None && keyboard[_endTurnKey].wasPressedThisFrame)
                {
                    ClearMovementPreview();
                    EndPlayerTurn();
                    return;
                }
            }

            if (_inputCamera == null || mouse == null)
            {
                ClearMovementPreview();
                return;
            }

            Vector2 mousePosition = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryConfirmMovementPosition(mousePosition);
            }
            else
            {
                TryUpdateMovementPreview(mousePosition);
            }
        }

        internal bool TryUpdateMovementPreview(Vector2 position)
        {
            ClearMovementPreview();

            if (!CanDisplayActionRange(_runner) || _runner == null || IsSelectingAction || _runner.IsSelectingAction
                || !_runner.TryGetRangeContext(out BattleSimulation simulation, out HexCharacter actor, out bool canMove)
                || IsPointerOverUI(position)
                || !TryGetGroundPathLine(_runner.MapView, actor, position, out Vector3 groundPoint, out BattlePathLine line))
            {
                return false;
            }

            Ray ray = _inputCamera.ScreenPointToRay(position);
            int targetId = 0;

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _characterLayerMask, QueryTriggerInteraction.Collide))
            {
                HexCharacterView targetView = hit.collider.GetComponentInParent<HexCharacterView>();

                if (targetView != null)
                {
                    if (!_runner.TryGetUnitId(targetView, out targetId))
                    {
                        return false;
                    }
                }
            }

            HexCoordinate destination = _runner.MapView.WorldToAxial(groundPoint);

            if (targetId == 0 && simulation.Board.TryGetUnit(destination, out HexCharacter occupant))
            {
                targetId = occupant.Id;
            }

            bool valid = targetId != 0
                ? simulation.PreviewSkill(new SkillInputQuery(SkillType.BasicAttack, actor.Id, targetId, line: line), _movementPreviewPath).CanUse
                : simulation.TryCopyMovementPath(actor.Id, destination, line, _movementPreviewPath);

            if (!valid || !canMove && _movementPreviewPath.Count > 0)
            {
                _movementPreviewPath.Clear();
                return false;
            }

            _movementPreviewSimulation = simulation;
            _movementPreviewUnitId = actor.Id;
            _movementPreviewTargetId = targetId;
            _movementPreviewRound = simulation.RoundNumber;
            _movementPreviewPoints = actor.MovementRemaining;
            _movementPreviewActionPoints = actor.ActionPoints;
            _movementPreviewOccupancyVersion = simulation.Board.OccupancyVersion;
            _movementPreviewEnvironmentVersion = simulation.Board.EnvironmentVersion;
            _movementPreviewVisibilityVersion = simulation.VisibilityVersion;
            _movementPreviewOrigin = actor.Coordinate;
            _movementPreviewDestination = destination;
            return true;
        }

        internal bool TryConfirmMovementPosition(Vector2 position)
        {
            bool hadAttackPreview = _movementPreviewSimulation != null && _movementPreviewTargetId != 0;
            bool validPreview = TryGetMovementCommand(_runner, out BattleSimulation current, out _,
                out int previewTarget, out _, out _);
            if (hadAttackPreview && !validPreview)
            {
                TryUpdateMovementPreview(position);
                return false;
            }

            if (validPreview && previewTarget != 0)
            {
                if (!IsPointerOverUI(position) && TryGetUnitAtPosition(_runner, current, position, out int clickedTarget)
                    && clickedTarget == previewTarget)
                {
                    bool confirmed = _runner.TryConfirmMovementPreview(this);
                    ClearMovementPreview();
                    return confirmed;
                }
            }

            if (!TryUpdateMovementPreview(position) || _movementPreviewTargetId != 0)
            {
                return false;
            }

            bool accepted = _runner.TryConfirmMovementPreview(this);
            ClearMovementPreview();
            return accepted;
        }

        internal bool TryGetMovementCommand(BattleRunner runner, out BattleSimulation simulation,
            out int unitId, out int targetId, out HexCoordinate destination, out IReadOnlyList<HexCoordinate> path)
        {
            simulation = null;
            unitId = 0;
            targetId = 0;
            destination = default;
            path = null;

            if (_movementPreviewSimulation == null || runner == null || !CanDisplayActionRange(runner)
                || IsSelectingAction || runner.IsSelectingAction || runner.MapView == null || !runner.MapView.isActiveAndEnabled
                || !runner.TryGetRangeContext(out BattleSimulation current, out HexCharacter actor, out bool canMove)
                || !ReferenceEquals(current, _movementPreviewSimulation) || actor.Id != _movementPreviewUnitId
                || actor.Coordinate != _movementPreviewOrigin || current.RoundNumber != _movementPreviewRound
                || actor.MovementRemaining != _movementPreviewPoints || actor.ActionPoints != _movementPreviewActionPoints
                || current.Board.OccupancyVersion != _movementPreviewOccupancyVersion
                || current.Board.EnvironmentVersion != _movementPreviewEnvironmentVersion
                || current.VisibilityVersion != _movementPreviewVisibilityVersion
                || !canMove && _movementPreviewPath.Count > 0)
            {
                ClearMovementPreview();
                return false;
            }

            simulation = current;
            unitId = actor.Id;
            targetId = _movementPreviewTargetId;
            destination = _movementPreviewDestination;
            path = _movementPreviewPath;
            return true;
        }

        internal bool TryCopyMovementPreview(BattleRunner runner, List<HexCoordinate> destination)
        {
            destination?.Clear();

            if (destination == null || !TryGetMovementCommand(runner, out _, out _, out _, out _,
                    out IReadOnlyList<HexCoordinate> path))
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                destination.Add(path[i]);
            }

            return destination.Count > 0;
        }

        void ClearMovementPreview()
        {
            _movementPreviewSimulation = null;
            _movementPreviewUnitId = 0;
            _movementPreviewTargetId = 0;
            _movementPreviewRound = 0;
            _movementPreviewPoints = 0;
            _movementPreviewActionPoints = 0;
            _movementPreviewOccupancyVersion = 0;
            _movementPreviewVisibilityVersion = 0;
            _movementPreviewOrigin = default;
            _movementPreviewDestination = default;
            _movementPreviewPath.Clear();
        }

        public void EndPlayerTurn()
        {
            if (!IsSelectingAction && TryGetCurrentPlayer(out HexCharacter character) && !_runner.IsBusy)
            {
                _runner.TryEndTurn(character.Id);
            }
        }

        internal bool TryBeginOverwatchSelection()
        {
            return TryBeginActionSelection(SkillType.Overwatch);
        }

        internal bool TryBeginSkillSelection(SkillType skill)
        {
            return TryBeginActionSelection(skill);
        }

        internal bool TryBeginSpiritImbuementSelection()
        {
            return TryBeginActionSelection(SkillType.SpiritImbuement);
        }

        internal bool TryConfirmChoice(int optionId)
        {
            if (!IsSelectingAction || _selectionMode != SkillInputMode.Choice)
            {
                return false;
            }

            return UpdateSelectionPreview(new SkillInputQuery(_selection, _selectionUnitId, optionId: optionId))
                && _selectionRunner.TryConfirmSkillPreview(this);
        }

        internal bool TryCopySpiritPreview(BattleRunner runner, List<HexCoordinate> destination)
        {
            destination.Clear();
            if (!TryGetMovementCommand(runner, out BattleSimulation simulation, out int actorId,
                out int targetId, out _, out IReadOnlyList<HexCoordinate> movement)
                || !simulation.Board.TryGetUnit(actorId, out HexCharacter actor)
                || actor.PreparedSpirit == SpiritType.None || targetId == 0
                || !simulation.Board.TryGetUnit(targetId, out HexCharacter target))
            {
                return false;
            }

            HexCoordinate origin = movement.Count > 0 ? movement[movement.Count - 1] : actor.Coordinate;
            BattleSpiritPath.Copy(origin, target.Coordinate, destination);
            return destination.Count > 0;
        }
        internal bool TryBeginStealthSelection()
        {
            return TryBeginActionSelection(SkillType.Stealth);
        }

        internal bool TryBeginAmbushSelection()
        {
            return TryBeginActionSelection(SkillType.Ambush);
        }

        internal bool TryBeginInvisibilitySelection()
        {
            return TryBeginActionSelection(SkillType.Invisibility);
        }

        internal bool TryBeginAssassinLeapSelection()
        {
            return TryBeginActionSelection(SkillType.AssassinLeap);
        }

        internal bool IsSelectingPioneerSkill(SkillType skill)
        {
            return IsSelectingAction && (skill == SkillType.PullSelf || skill == SkillType.PullTarget)
                && _selection == skill;
        }

        internal bool TryBeginPioneerSelection(SkillType skill)
        {
            if (skill != SkillType.PullSelf && skill != SkillType.PullTarget)
            {
                return false;
            }

            return TryBeginActionSelection(skill);
        }

        bool TryBeginActionSelection(SkillType selection)
        {
            if (IsSelectingAction || !TryGetCurrentPlayer(out HexCharacter character)
                || !CanDisplayActionRange(_runner)
                || !_runner.TryBeginActionSelection(this, character.Id, selection, out BattleSimulation simulation))
            {
                return false;
            }

            _selectionRunner = _runner;
            ClearMovementPreview();
            _selection = selection;
            _selectionMode = character.Skills.Get(selection).InputMode;
            _selectionSimulation = simulation;
            _selectionUnitId = character.Id;
            _selectionRound = simulation.RoundNumber;
            _selectionDirection = character.Facing;
            _hasPreviewPosition = false;
            ClearSelectionPreview();
            _selectionRunner.PresentationStopped += CancelActionSelection;
            _selectionRunner.BattleCompleted += OnBattleCompleted;
            return true;
        }

        bool IsSelectionValid()
        {
            return _selectionRunner != null && _selectionRunner == _runner
                && CanDisplayActionRange(_runner) && _selectionSimulation.RoundNumber == _selectionRound
                && _selectionRunner.IsActionSelectionValid(this, _selectionSimulation, _selectionUnitId, _selection);
        }

        internal void CancelOverwatchSelection()
        {
            if (_selection == SkillType.Overwatch)
            {
                CancelActionSelection();
            }
        }

        internal void CancelActionSelection()
        {
            ClearMovementPreview();
            BattleRunner runner = _selectionRunner;
            _selectionRunner = null;
            _selectionSimulation = null;
            _selectionUnitId = 0;
            _selectionRound = 0;
            _selectionDirection = default;
            _hasPreviewPosition = false;
            _selection = default;
            _selectionMode = default;
            ClearSelectionPreview();

            if (runner != null)
            {
                runner.PresentationStopped -= CancelActionSelection;
                runner.BattleCompleted -= OnBattleCompleted;
                runner.EndActionSelection(this);
            }
        }

        void OnBattleCompleted(BattleResult result)
        {
            CancelActionSelection();
        }

        void OnDisable()
        {
            ClearMovementPreview();
            CancelActionSelection();
        }

        void UpdateActionSelection()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                CancelActionSelection();
                return;
            }

            if (_selectionMode == SkillInputMode.Choice)
            {
                Keyboard keyboard = Keyboard.current;
                IReadOnlyList<SkillChoice> choices = SelectedSkillChoices;
                if (keyboard != null && !IsEditingText())
                {
                    for (int i = 0; i < choices.Count && i < 9; i++)
                    {
                        if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                        {
                            TryConfirmChoice(choices[i].Id);
                            return;
                        }
                    }
                }

                return;
            }

            if (mouse == null)
            {
                ClearSelectionPreview();
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryConfirmSelectedPosition(position);
            }
            else
            {
                TryUpdateSelectedPosition(position);
            }
        }

        internal bool TryUpdateSelectedPosition(Vector2 position)
        {
            ClearSelectionPreview();
            if (!IsSelectingAction || IsPointerOverUI(position))
            {
                return false;
            }

            if (_selectionMode == SkillInputMode.Direction)
            {
                _hasPreviewPosition = TryUpdateSelectionDirection(position);
                return _hasPreviewPosition && UpdateSelectionPreview(new SkillInputQuery(_selection,
                    _selectionUnitId, direction: _selectionDirection));
            }

            if ((_selectionMode != SkillInputMode.Self && _selectionMode != SkillInputMode.Unit)
                || !TryGetSelectedUnitAtPosition(position, out int targetId)
                || !_selectionSimulation.Board.TryGetUnit(_selectionUnitId, out HexCharacter actor))
            {
                return false;
            }

            BattlePathLine? line = TryGetGroundPathLine(_selectionRunner.MapView, actor, position,
                out _, out BattlePathLine groundLine) ? groundLine : (BattlePathLine?)null;
            if (_selectionMode == SkillInputMode.Unit && !line.HasValue)
            {
                return false;
            }

            return UpdateSelectionPreview(new SkillInputQuery(_selection, _selectionUnitId, targetId, line: line));
        }

        bool UpdateSelectionPreview(SkillInputQuery query)
        {
            _selectionPreview = _selectionSimulation.PreviewSkill(query, _selectionPath);
            _hasSelectionPreview = _selectionPreview.CanUse;
            if (_hasSelectionPreview && _selectionSimulation.Board.TryGetUnit(_selectionUnitId, out HexCharacter actor))
            {
                _previewOccupancyVersion = _selectionSimulation.Board.OccupancyVersion;
                _previewEnvironmentVersion = _selectionSimulation.Board.EnvironmentVersion;
                _previewVisibilityVersion = _selectionSimulation.VisibilityVersion;
                _previewCooldownVersion = actor.CooldownVersion;
                _previewActorOrigin = actor.Coordinate;
                _previewActionPoints = actor.ActionPoints;
                _previewMovementPoints = actor.MovementRemaining;
                _previewTargetOrigin = _selectionSimulation.Board.TryGetUnit(_selectionPreview.Request.TargetId,
                    out HexCharacter target) ? target.Coordinate : default;
            }

            return _hasSelectionPreview;
        }

        internal bool TryConfirmSelectedPosition(Vector2 position)
        {
            if (!IsSelectingAction || IsPointerOverUI(position))
            {
                ClearSelectionPreview();
                return false;
            }

            if (_selectionMode == SkillInputMode.Direction || _selectionMode == SkillInputMode.Self)
            {
                return TryUpdateSelectedPosition(position) && _selectionRunner.TryConfirmSkillPreview(this);
            }

            if (_selectionMode != SkillInputMode.Unit
                || !TryGetSelectedUnitAtPosition(position, out int targetId))
            {
                ClearSelectionPreview();
                return false;
            }

            if (!_hasSelectionPreview || _selectionPreview.Request.TargetId != targetId)
            {
                TryUpdateSelectedPosition(position);
                return false;
            }

            bool accepted = _selectionRunner.TryConfirmSkillPreview(this);
            if (!accepted)
            {
                ClearSelectionPreview();
            }

            return accepted;
        }

        internal bool TryGetSkillCommand(BattleRunner runner, out BattleSimulation simulation,
            out SkillUseRequest request)
        {
            simulation = null;
            request = default;
            if (!IsSelectingAction || runner != _selectionRunner || !_hasSelectionPreview
                || _selectionSimulation.Board.OccupancyVersion != _previewOccupancyVersion
                || _selectionSimulation.Board.EnvironmentVersion != _previewEnvironmentVersion
                || _selectionSimulation.VisibilityVersion != _previewVisibilityVersion
                || !_selectionSimulation.Board.TryGetUnit(_selectionUnitId, out HexCharacter actor)
                || actor.Coordinate != _previewActorOrigin || actor.CooldownVersion != _previewCooldownVersion
                || actor.ActionPoints != _previewActionPoints || actor.MovementRemaining != _previewMovementPoints
                || _selectionPreview.Request.TargetId != 0
                    && (!_selectionSimulation.Board.TryGetUnit(_selectionPreview.Request.TargetId, out HexCharacter target)
                        || target.Coordinate != _previewTargetOrigin)
                || !_selectionSimulation.CanUseSkill(_selectionPreview.Request).CanUse)
            {
                return false;
            }

            simulation = _selectionSimulation;
            request = _selectionPreview.Request;
            return true;
        }

        void ClearSelectionPreview()
        {
            _selectionPath.Clear();
            _selectionPreview = default;
            _hasSelectionPreview = false;
            _hasPreviewPosition = false;
        }

        bool TryGetSelectedUnitAtPosition(Vector2 position, out int unitId)
        {
            return TryGetUnitAtPosition(_selectionRunner, _selectionSimulation, position, out unitId);
        }

        bool TryGetUnitAtPosition(BattleRunner runner, BattleSimulation simulation, Vector2 position, out int unitId)
        {
            unitId = 0;
            HexMapView map = runner.MapView;

            if (map == null || !map.isActiveAndEnabled || float.IsNaN(position.x) || float.IsInfinity(position.x)
                || float.IsNaN(position.y) || float.IsInfinity(position.y))
            {
                return false;
            }

            Ray ray = _inputCamera.ScreenPointToRay(position);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _characterLayerMask, QueryTriggerInteraction.Collide))
            {
                HexCharacterView view = hit.collider.GetComponentInParent<HexCharacterView>();

                if (view != null)
                {
                    return runner.TryGetUnitId(view, out unitId);
                }
            }

            Plane plane = new Plane(map.transform.up, map.transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            HexCoordinate coordinate = map.WorldToAxial(ray.GetPoint(distance));
            if (!simulation.Board.TryGetUnit(coordinate, out HexCharacter target))
            {
                return false;
            }

            unitId = target.Id;
            return true;
        }

        internal bool TryUpdateAssassinLeapTarget(Vector2 position)
        {
            return IsSelectingAssassinLeap && TryUpdateSelectedPosition(position);
        }

        internal bool TryGetAssassinLeapContext(BattleRunner runner, out BattleSimulation simulation,
            out HexCharacter unit, out AssassinLeapPreview preview)
        {
            unit = null;
            preview = default;
            simulation = null;
            if (!IsSelectingAssassinLeap || !TryGetSkillCommand(runner, out simulation, out SkillUseRequest request)
                || !request.LeapPreview.HasValue)
            {
                return false;
            }

            preview = request.LeapPreview.Value;
            return simulation.Board.TryGetUnit(_selectionUnitId, out unit);
        }

        internal bool TryUpdatePioneerTarget(Vector2 position)
        {
            return (IsSelectingPioneerSkill(SkillType.PullSelf) || IsSelectingPioneerSkill(SkillType.PullTarget))
                && TryUpdateSelectedPosition(position);
        }

        bool TryGetGroundPathLine(HexMapView map, HexCharacter actor, Vector2 position,
            out Vector3 groundPoint, out BattlePathLine line)
        {
            groundPoint = default;
            line = default;
            if (map == null || !map.isActiveAndEnabled || map.HexSize <= 0f
                || float.IsNaN(map.HexSize) || float.IsInfinity(map.HexSize)
                || float.IsNaN(position.x) || float.IsInfinity(position.x)
                || float.IsNaN(position.y) || float.IsInfinity(position.y))
            {
                return false;
            }

            Ray ray = _inputCamera.ScreenPointToRay(position);
            Plane plane = new Plane(map.transform.up, map.transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            groundPoint = ray.GetPoint(distance);
            Vector3 delta = groundPoint - map.AxialToWorld(actor.Coordinate);
            Vector2 localQ = HexLayout.AxialToLocal(new HexCoordinate(1, 0), map.HexSize);
            Vector2 localR = HexLayout.AxialToLocal(new HexCoordinate(0, 1), map.HexSize);
            Vector3 axisQ = map.transform.TransformVector(new Vector3(localQ.x, 0f, localQ.y));
            Vector3 axisR = map.transform.TransformVector(new Vector3(localR.x, 0f, localR.y));
            float tolerance = Mathf.Max(axisQ.magnitude, axisR.magnitude) * 0.001f;

            if (delta.sqrMagnitude <= tolerance * tolerance)
            {
                return false;
            }

            line = new BattlePathLine(Vector3.Dot(Vector3.Cross(delta, axisQ), map.transform.up),
                Vector3.Dot(Vector3.Cross(delta, axisR), map.transform.up));
            return line.IsValid;
        }

        internal bool TryGetPioneerPath(BattleRunner runner, int unitId, int targetId, SkillType skill,
            out IReadOnlyList<HexCoordinate> path)
        {
            path = null;
            if (!IsSelectingPioneerSkill(skill) || !TryGetSkillCommand(runner, out _, out SkillUseRequest request)
                || request.UnitId != unitId || request.TargetId != targetId || request.Path == null)
            {
                return false;
            }

            path = request.Path;
            return true;
        }

        internal bool TryGetPioneerContext(BattleRunner runner, out BattleSimulation simulation,
            out HexCharacter unit, out SkillType skill, out int targetId)
        {
            simulation = null;
            unit = null;
            skill = default;
            targetId = 0;
            if (!IsSelectingAction || _selectionRunner != runner
                || (_selection != SkillType.PullSelf && _selection != SkillType.PullTarget))
            {
                return false;
            }

            simulation = _selectionSimulation;
            skill = _selection;
            if (TryGetSkillCommand(runner, out _, out SkillUseRequest request))
            {
                targetId = request.TargetId;
            }

            return simulation.Board.TryGetUnit(_selectionUnitId, out unit);
        }

        internal bool TryGetStealthContext(BattleRunner runner, out BattleSimulation simulation, out HexCharacter unit)
        {
            simulation = null;
            unit = null;

            if (!CanDisplayActionRange(runner) || runner == null
                || !runner.TryGetRangeContext(out BattleSimulation current, out HexCharacter character, out _))
            {
                return false;
            }

            bool selecting = IsSelectingAction;

            if (selecting ? _selection != SkillType.Stealth && _selection != SkillType.Invisibility
                : !character.IsStealthed || character.IsInvisible)
            {
                return false;
            }

            simulation = current;
            unit = character;
            return true;
        }

        internal bool TryGetAmbushContext(BattleRunner runner, out BattleSimulation simulation, out HexCharacter unit)
        {
            simulation = null;
            unit = null;

            if (!IsSelectingAmbush || _selectionRunner != runner)
            {
                return false;
            }

            simulation = _selectionSimulation;
            return simulation.Board.TryGetUnit(_selectionUnitId, out unit);
        }

        bool TryUpdateSelectionDirection(Vector2 screenPosition)
        {
            HexMapView map = _selectionRunner.MapView;

            if (map == null || !map.isActiveAndEnabled
                || !_selectionSimulation.Board.TryGetUnit(_selectionUnitId, out HexCharacter character))
            {
                return false;
            }

            Ray ray = _inputCamera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(map.transform.up, map.transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 localPoint = map.transform.InverseTransformPoint(ray.GetPoint(distance));
            Vector2 center = HexLayout.AxialToLocal(character.Coordinate, map.HexSize);
            Vector2 delta = new Vector2(localPoint.x - center.x, localPoint.z - center.y);

            if (float.IsNaN(delta.x) || float.IsInfinity(delta.x)
                || float.IsNaN(delta.y) || float.IsInfinity(delta.y))
            {
                return false;
            }

            double centerTolerance = map.HexSize * 0.001d;

            if ((double)delta.x * delta.x + (double)delta.y * delta.y <= centerTolerance * centerTolerance)
            {
                return true;
            }

            double bestDot = double.NegativeInfinity;

            for (int i = 0; i < 6; i++)
            {
                Vector2 direction = HexLayout.AxialToLocal(default(HexCoordinate).GetNeighbor((HexDirection)i), 1f);
                double dot = (double)delta.x * direction.x + (double)delta.y * direction.y;

                if (dot > bestDot)
                {
                    bestDot = dot;
                    _selectionDirection = (HexDirection)i;
                }
            }

            return true;
        }

        internal bool TryGetOverwatchPreview(BattleRunner runner, out BattleOverwatchState preview)
        {
            preview = default;

            if (!IsSelectingOverwatch || runner != _selectionRunner || !_hasPreviewPosition
                || !_selectionSimulation.Board.TryGetUnit(_selectionUnitId, out HexCharacter character))
            {
                return false;
            }

            character.Skills.GetRange(SkillType.Overwatch, out int minRange, out int maxRange);
            preview = new BattleOverwatchState(character.Id, character.Coordinate, _selectionDirection,
                maxRange, minRange);
            return true;
        }

        internal bool TryBeginSkillSlot(int slot)
        {
            return slot >= 1 && slot <= 9 && TryGetCurrentPlayer(out HexCharacter character)
                && character.Skills.TryGetSlot(slot, out BattleSkillDefinition definition)
                && TryBeginSkillSelection(definition.Type);
        }

        internal bool CanDisplayActionRange(BattleRunner runner)
        {
            return isActiveAndEnabled && _runner == runner && _inputCamera != null
                && _inputCamera.isActiveAndEnabled;
        }

        bool TryGetCurrentPlayer(out HexCharacter character)
        {
            character = null;
            return isActiveAndEnabled && _runner != null && _runner.isActiveAndEnabled
                && _runner.IsInitialized && _runner.State == BattleState.Running
                && _runner.CurrentUnitId.HasValue
                && _runner.TryGetCharacter(_runner.CurrentUnitId.Value, out character)
                && character.Team == BattleTeam.Player;
        }

        bool IsEditingText()
        {
            EventSystem current = EventSystem.current;

            if (current == null || current.currentSelectedGameObject == null)
            {
                return false;
            }

            UnityEngine.UI.InputField inputField = current.currentSelectedGameObject
                .GetComponentInParent<UnityEngine.UI.InputField>();
            return inputField != null && inputField.isFocused;
        }

        bool IsPointerOverUI(Vector2 position)
        {
            EventSystem current = EventSystem.current;

            if (current == null)
            {
                return false;
            }

            if (_eventSystem != current)
            {
                _eventSystem = current;
                _pointerData = new PointerEventData(current);
            }

            _pointerData.Reset();
            _pointerData.position = position;
            _uiHits.Clear();
            current.RaycastAll(_pointerData, _uiHits);
            bool overUI = false;

            for (int i = 0; i < _uiHits.Count; i++)
            {
                if (_uiHits[i].module is UnityEngine.UI.GraphicRaycaster)
                {
                    overUI = true;
                    break;
                }
            }

            _uiHits.Clear();
            return overUI;
        }
    }
}
