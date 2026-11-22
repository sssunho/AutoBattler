using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(110)]
    public sealed class BattleRangeController : MonoBehaviour
    {
        static readonly Color _detectionColor = new Color(1f, 0.25f, 0.3f, 0.22f);
        static readonly Color _stealthReadyColor = new Color(0.25f, 1f, 0.65f, 0.5f);
        static readonly Color _stealthBlockedColor = new Color(1f, 0.25f, 0.3f, 0.6f);
        static readonly Color _ambushColor = new Color(0.7f, 0.4f, 1f, 0.6f);
        static readonly Color _pioneerPathColor = new Color(0.15f, 0.75f, 1f, 0.4f);
        static readonly Color _pioneerLandingColor = new Color(1f, 0.9f, 0.2f, 0.65f);
        static readonly Color _movementPathColor = new Color(0.15f, 0.75f, 1f, 0.3f);
        static readonly Color _movementLandingColor = new Color(1f, 0.9f, 0.2f, 0.65f);

        [SerializeField]
        BattleRunner _runner;

        [SerializeField]
        BattleInputController _input;

        [SerializeField]
        Material _material;

        readonly BattleRangeOutline _outline = new BattleRangeOutline();
        readonly List<HexCoordinate> _movement = new List<HexCoordinate>();
        readonly List<BattleAttackTarget> _targets = new List<BattleAttackTarget>();
        readonly List<BattleHexEdge> _movementEdges = new List<BattleHexEdge>();
        readonly List<BattleHexEdge> _attackEdges = new List<BattleHexEdge>();
        readonly List<BattleGroundMarker> _markers = new List<BattleGroundMarker>();
        readonly List<BattleOverwatchState> _overwatchStates = new List<BattleOverwatchState>();
        readonly List<BattleOverwatchState> _cachedOverwatchStates = new List<BattleOverwatchState>();
        readonly List<HexCoordinate> _overwatchCells = new List<HexCoordinate>();
        readonly List<HexCoordinate> _previewCells = new List<HexCoordinate>();
        readonly List<HexCoordinate> _areaCells = new List<HexCoordinate>();
        readonly List<HexCoordinate> _detectionCells = new List<HexCoordinate>();
        readonly List<HexCoordinate> _stealthMarkers = new List<HexCoordinate>();
        readonly HashSet<HexCoordinate> _detectionUnion = new HashSet<HexCoordinate>();
        readonly List<HexCoordinate> _pioneerPath = new List<HexCoordinate>(3);
        readonly List<HexCoordinate> _pioneerLanding = new List<HexCoordinate>(1);
        readonly List<HexCoordinate> _movementPath = new List<HexCoordinate>();
        readonly List<HexCoordinate> _movementLanding = new List<HexCoordinate>(1);
        readonly List<HexCoordinate> _assassinLanding = new List<HexCoordinate>(1);

        readonly BattleRangePresentationLayer _rangeLayer = new BattleRangePresentationLayer("Battle Ground Ranges");
        readonly BattleRangePresentationLayer _overwatchLayer = new BattleRangePresentationLayer("Battle Overwatch Ranges");
        readonly BattleRangePresentationLayer _stealthLayer = new BattleRangePresentationLayer("Battle Detection Ranges");
        readonly BattleRangePresentationLayer _pioneerLayer = new BattleRangePresentationLayer("Battle Pioneer Path");
        readonly BattleRangePresentationLayer _movementPathLayer = new BattleRangePresentationLayer("Battle Movement Path");
        readonly BattleRangePresentationLayer _assassinLayer = new BattleRangePresentationLayer("Battle Assassin Landing");
        readonly BattleRangePresentationLayer _spiritPathLayer = new BattleRangePresentationLayer("Battle Spirit Path");

        BattleRunner _boundRunner;
        HexMapView _viewMap;
        BattleSimulation _cachedSimulation;
        HexCharacter _cachedUnit;
        HexCoordinate _cachedCoordinate;
        uint _cachedOccupancyVersion;
        uint _cachedEnvironmentVersion;
        uint _cachedVisibilityVersion;
        int _cachedActionPoints;
        int _cachedMovement;
        bool _cachedCanMove;
        SkillType? _cachedSelectedSkill;
        bool _hasCache;
        BattleOverwatchState _cachedPreview;
        bool _cachedHasPreview;
        bool _hasOverwatchCache;
        BattleSimulation _cachedStealthSimulation;
        HexCharacter _cachedStealthUnit;
        uint _cachedStealthVisibilityVersion;
        uint _cachedStealthOccupancyVersion;
        uint _cachedStealthEnvironmentVersion;
        int _cachedStealthActionPoints;
        int _cachedStealthMovement;
        bool _cachedStealthed;
        bool _cachedInvisible;
        bool _cachedAmbushUsed;
        bool _cachedSelectingStealth;
        bool _cachedSelectingAmbush;
        bool _cachedSelectingInvisibility;
        bool _hasStealthCache;

        void OnEnable()
        {
            BindRunner();
            InvalidatePresentation();
        }

        void LateUpdate()
        {
            BindRunner();
            HexMapView map = _runner != null ? _runner.MapView : null;

            if (_viewMap != map || map == null && !ReferenceEquals(_viewMap, null))
            {
                ReleaseView();
            }

            if (!isActiveAndEnabled || _material == null || _runner == null || _input == null
                || !_input.CanDisplayActionRange(_runner) || map == null || !map.isActiveAndEnabled)
            {
                HidePresentation();
                return;
            }

            _viewMap = map;
            UpdateOverwatchPresentation(map);
            UpdateStealthPresentation(map);
            UpdatePioneerPresentation(map);
            UpdateAssassinPresentation(map);
            UpdateMovementPathPresentation(map);
            UpdateSpiritPathPresentation(map);

            SkillType? selectedSkill = _input.SelectedSkill;
            if (selectedSkill.HasValue && _input.SelectedInputMode != SkillInputMode.Unit
                || !_runner.TryGetRangeContext(out BattleSimulation simulation, out HexCharacter unit, out bool canMove))
            {
                _rangeLayer.Hide();

                return;
            }

            if (!_rangeLayer.Prepare(map, _material))
            {
                _hasCache = false;
                return;
            }

            if (!_hasCache || _rangeLayer.RequiresRebuild || !ReferenceEquals(_cachedSimulation, simulation) || _cachedUnit != unit
                || _cachedCoordinate != unit.Coordinate
                || _cachedOccupancyVersion != simulation.Board.OccupancyVersion
                || _cachedEnvironmentVersion != simulation.Board.EnvironmentVersion
                || _cachedVisibilityVersion != simulation.VisibilityVersion
                || _cachedActionPoints != unit.ActionPoints || _cachedMovement != unit.MovementRemaining
                || _cachedCanMove != canMove || _cachedSelectedSkill != selectedSkill)
            {
                _movementEdges.Clear();
                _markers.Clear();
                bool canUse;
                if (selectedSkill.HasValue)
                {
                    canUse = simulation.CanSelectSkill(unit.Id, selectedSkill.Value).CanUse;
                }
                else
                {
                    if (!_runner.TryCopyActionRange(_movement, _targets))
                    {
                        _hasCache = false;
                        _rangeLayer.Hide();
                        return;
                    }

                    _outline.Build(map.Width, map.Height, unit.Coordinate, _movement, _movementEdges);
                    for (int i = 0; i < _targets.Count; i++)
                    {
                        BattleAttackTarget target = _targets[i];
                        if (_runner.TryGetCharacter(target.UnitId, out HexCharacter character)
                            && map.IsValid(character.Coordinate))
                        {
                            _markers.Add(new BattleGroundMarker(character.Coordinate, target.RequiresMovement));
                        }
                    }

                    canUse = unit.ActionPoints >= unit.Skills.BasicAttack.ActionPointCost;
                }

                BuildSkillEdges(map, simulation, unit, selectedSkill ?? SkillType.BasicAttack);
                if (!_rangeLayer.RebuildOutline(map, _movementEdges, _attackEdges, _markers, canUse, _material))
                {
                    _hasCache = false;
                    _rangeLayer.Hide();
                    return;
                }

                _cachedSimulation = simulation;
                _cachedUnit = unit;
                _cachedCoordinate = unit.Coordinate;
                _cachedOccupancyVersion = simulation.Board.OccupancyVersion;
                _cachedEnvironmentVersion = simulation.Board.EnvironmentVersion;
                _cachedVisibilityVersion = simulation.VisibilityVersion;
                _cachedActionPoints = unit.ActionPoints;
                _cachedMovement = unit.MovementRemaining;
                _cachedCanMove = canMove;
                _cachedSelectedSkill = selectedSkill;
                _hasCache = true;
            }

            _rangeLayer.Show();
        }

        readonly List<HexCoordinate> _spiritPath = new List<HexCoordinate>();
        readonly List<HexCoordinate> _spiritLanding = new List<HexCoordinate>();

        void UpdateMovementPathPresentation(HexMapView map)
        {
            if (!_runner.TryCopyMovementDisplayPath(_input, _movementPath) || _movementPath.Count == 0)
            {
                _movementPathLayer.Invalidate();
                return;
            }

            _movementLanding.Clear();
            _movementLanding.Add(_movementPath[_movementPath.Count - 1]);
            _movementPathLayer.ShowFilled(map, _movementPath, _movementLanding, _material,
                _movementPathColor, _movementLandingColor);
        }

        void UpdateSpiritPathPresentation(HexMapView map)
        {
            if (!_input.TryCopySpiritPreview(_runner, _spiritPath) || _spiritPath.Count == 0)
            {
                _spiritPathLayer.Invalidate();
                return;
            }

            _spiritLanding.Clear();
            _spiritLanding.Add(_spiritPath[_spiritPath.Count - 1]);
            _spiritPathLayer.ShowFilled(map, _spiritPath, _spiritLanding, _material,
                _movementPathColor, _movementLandingColor);
        }

        void UpdatePioneerPresentation(HexMapView map)
        {
            if (!_input.TryGetPioneerContext(_runner, out _,
                out HexCharacter unit, out SkillType skill, out int targetId)
                || targetId == 0 || !_input.TryGetPioneerPath(_runner, unit.Id, targetId, skill,
                    out IReadOnlyList<HexCoordinate> path) || path.Count == 0)
            {
                _pioneerLayer.Invalidate();
                return;
            }

            _pioneerPath.Clear();

            for (int i = 0; i < path.Count; i++)
            {
                _pioneerPath.Add(path[i]);
            }

            _pioneerLanding.Clear();
            _pioneerLanding.Add(_pioneerPath[_pioneerPath.Count - 1]);
            _pioneerLayer.ShowFilled(map, _pioneerPath, _pioneerLanding, _material,
                _pioneerPathColor, _pioneerLandingColor);
        }

        void UpdateAssassinPresentation(HexMapView map)
        {
            if (!_input.TryGetAssassinLeapContext(_runner, out _, out _, out AssassinLeapPreview preview))
            {
                _assassinLayer.Invalidate();
                return;
            }

            _assassinLanding.Clear();
            _assassinLanding.Add(preview.Landing);
            _assassinLayer.ShowFilled(map, _assassinLanding, _assassinLanding, _material,
                _pioneerLandingColor, _pioneerLandingColor);
        }

        void UpdateStealthPresentation(HexMapView map)
        {
            bool hasStealthContext = _input.TryGetStealthContext(_runner, out BattleSimulation simulation,
                out HexCharacter unit);
            bool selectingAmbush = _input.TryGetAmbushContext(_runner, out BattleSimulation ambushSimulation,
                out HexCharacter ambushUnit);

            if (!hasStealthContext && selectingAmbush)
            {
                simulation = ambushSimulation;
                unit = ambushUnit;
            }

            if (!hasStealthContext && !selectingAmbush)
            {
                _hasStealthCache = false;

                _stealthLayer.Hide();

                return;
            }

            bool selectingStealth = _input.IsSelectingStealth;
            bool selectingInvisibility = _input.IsSelectingInvisibility;

            if (!_stealthLayer.Prepare(map, _material))
            {
                _hasStealthCache = false;
                return;
            }

            if (!_hasStealthCache || _stealthLayer.RequiresRebuild || !ReferenceEquals(_cachedStealthSimulation, simulation)
                || _cachedStealthUnit != unit
                || _cachedStealthVisibilityVersion != simulation.VisibilityVersion
                || _cachedStealthOccupancyVersion != simulation.Board.OccupancyVersion
                || _cachedStealthEnvironmentVersion != simulation.Board.EnvironmentVersion
                || _cachedStealthActionPoints != unit.ActionPoints
                || _cachedStealthMovement != unit.MovementRemaining || _cachedStealthed != unit.IsStealthed
                || _cachedInvisible != unit.IsInvisible || _cachedSelectingInvisibility != selectingInvisibility
                || _cachedAmbushUsed != unit.HasUsedAmbush || _cachedSelectingStealth != selectingStealth
                || _cachedSelectingAmbush != selectingAmbush)
            {
                _detectionCells.Clear();
                _detectionUnion.Clear();
                _stealthMarkers.Clear();

                foreach (HexCharacter observer in simulation.Board.Units)
                {
                    if (observer.Team == unit.Team || observer.Health <= 0)
                    {
                        continue;
                    }

                    if (!unit.IsInvisible && !selectingInvisibility)
                    {
                        BattleDetectionArea.CopyCells(map.Width, map.Height, observer.Coordinate,
                            observer.Facing, _areaCells);

                        for (int i = 0; i < _areaCells.Count; i++)
                        {
                            if (_detectionUnion.Add(_areaCells[i]))
                            {
                                _detectionCells.Add(_areaCells[i]);
                            }
                        }
                    }

                    if (selectingAmbush && simulation.CanAmbush(unit.Id, observer.Id))
                    {
                        _stealthMarkers.Add(observer.Coordinate);
                    }
                }

                Color markerColor = _ambushColor;

                if (selectingStealth)
                {
                    _stealthMarkers.Add(unit.Coordinate);
                    markerColor = simulation.CanEnterStealth(unit.Id) ? _stealthReadyColor : _stealthBlockedColor;
                }
                else if (selectingInvisibility)
                {
                    _stealthMarkers.Add(unit.Coordinate);
                    markerColor = simulation.CanEnterInvisibility(unit.Id) ? _stealthReadyColor : _stealthBlockedColor;
                }

                if (!_stealthLayer.ShowFilled(map, _detectionCells, _stealthMarkers, _material,
                    _detectionColor, markerColor))
                {
                    _hasStealthCache = false;
                    return;
                }

                _cachedStealthSimulation = simulation;
                _cachedStealthUnit = unit;
                _cachedStealthVisibilityVersion = simulation.VisibilityVersion;
                _cachedStealthOccupancyVersion = simulation.Board.OccupancyVersion;
                _cachedStealthEnvironmentVersion = simulation.Board.EnvironmentVersion;
                _cachedStealthActionPoints = unit.ActionPoints;
                _cachedStealthMovement = unit.MovementRemaining;
                _cachedStealthed = unit.IsStealthed;
                _cachedInvisible = unit.IsInvisible;
                _cachedAmbushUsed = unit.HasUsedAmbush;
                _cachedSelectingStealth = selectingStealth;
                _cachedSelectingAmbush = selectingAmbush;
                _cachedSelectingInvisibility = selectingInvisibility;
                _hasStealthCache = true;
            }

            _stealthLayer.Show();
        }

        void UpdateOverwatchPresentation(HexMapView map)
        {
            if (!_runner.TryCopyOverwatchStates(_overwatchStates))
            {
                _hasOverwatchCache = false;

                _overwatchLayer.Hide();

                return;
            }

            bool hasPreview = _input.TryGetOverwatchPreview(_runner, out BattleOverwatchState preview);

            if (_overwatchStates.Count == 0 && !hasPreview)
            {
                _hasOverwatchCache = false;

                _overwatchLayer.Hide();

                return;
            }

            if (!_overwatchLayer.Prepare(map, _material))
            {
                _hasOverwatchCache = false;
                return;
            }

            if (!_hasOverwatchCache || _overwatchLayer.RequiresRebuild || _cachedHasPreview != hasPreview
                || hasPreview && !SameOverwatchState(_cachedPreview, preview)
                || !SameOverwatchStates())
            {
                _overwatchCells.Clear();
                _previewCells.Clear();

                for (int i = 0; i < _overwatchStates.Count; i++)
                {
                    BattleOverwatchState state = _overwatchStates[i];
                    BattleOverwatchArea.CopyCells(map.Width, map.Height, state.Coordinate,
                        state.Direction, state.Range, _areaCells, state.MinRange);
                    _overwatchCells.AddRange(_areaCells);
                }

                if (hasPreview)
                {
                    BattleOverwatchArea.CopyCells(map.Width, map.Height, preview.Coordinate,
                        preview.Direction, preview.Range, _previewCells, preview.MinRange);
                }

                if (!_overwatchLayer.RebuildFilled(map, _overwatchCells, _previewCells, _material))
                {
                    _hasOverwatchCache = false;
                    return;
                }

                _cachedOverwatchStates.Clear();
                _cachedOverwatchStates.AddRange(_overwatchStates);
                _cachedPreview = preview;
                _cachedHasPreview = hasPreview;
                _hasOverwatchCache = true;
            }

            _overwatchLayer.Show();
        }

        bool SameOverwatchStates()
        {
            if (_cachedOverwatchStates.Count != _overwatchStates.Count)
            {
                return false;
            }

            for (int i = 0; i < _overwatchStates.Count; i++)
            {
                if (!SameOverwatchState(_cachedOverwatchStates[i], _overwatchStates[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool SameOverwatchState(BattleOverwatchState first, BattleOverwatchState second)
        {
            return first.UnitId == second.UnitId && first.Coordinate == second.Coordinate
                && first.Direction == second.Direction && first.Range == second.Range && first.MinRange == second.MinRange;
        }

        void BuildSkillEdges(HexMapView map, BattleSimulation simulation, HexCharacter unit, SkillType skill)
        {
            HexCoordinate origin = unit.Coordinate;
            unit.Skills.GetRange(skill, out int minimumRange, out int range);
            _attackEdges.Clear();
            int minQ = (int)Math.Max(0L, (long)origin.Q - range);
            int maxQ = (int)Math.Min(map.Width - 1L, (long)origin.Q + range);
            int minR = (int)Math.Max(0L, (long)origin.R - range);
            int maxR = (int)Math.Min(map.Height - 1L, (long)origin.R + range);

            for (int r = minR; r <= maxR; r++)
            {
                for (int q = minQ; q <= maxQ; q++)
                {
                    HexCoordinate coordinate = new HexCoordinate(q, r);

                    int distance = coordinate.DistanceTo(origin);

                    if (coordinate == origin || distance < minimumRange || distance > range
                        || !simulation.HasSkillLineOfSight(unit, skill, origin, coordinate))
                    {
                        continue;
                    }

                    for (int direction = 0; direction < 6; direction++)
                    {
                        _attackEdges.Add(new BattleHexEdge(coordinate, (HexDirection)direction));
                    }
                }
            }
        }

        void BindRunner()
        {
            if (_boundRunner == _runner)
            {
                return;
            }

            UnbindRunner();
            InvalidatePresentation();
            _boundRunner = _runner;

            if (_boundRunner != null)
            {
                _boundRunner.PresentationStopped += InvalidatePresentation;
                _boundRunner.BattleCompleted += OnBattleCompleted;
            }
        }

        void UnbindRunner()
        {
            if (_boundRunner != null)
            {
                _boundRunner.PresentationStopped -= InvalidatePresentation;
                _boundRunner.BattleCompleted -= OnBattleCompleted;
            }

            _boundRunner = null;
        }

        void OnBattleCompleted(BattleResult result)
        {
            InvalidatePresentation();
        }

        void HidePresentation()
        {
            _rangeLayer.Hide();
            _overwatchLayer.Hide();
            _stealthLayer.Hide();
            _pioneerLayer.Hide();
            _movementPathLayer.Hide();
            _assassinLayer.Hide();
            _spiritPathLayer.Hide();
        }

        void InvalidatePresentation()
        {
            _hasCache = false;
            _cachedSimulation = null;
            _cachedUnit = null;
            _hasOverwatchCache = false;
            _cachedOverwatchStates.Clear();
            _hasStealthCache = false;
            _cachedStealthSimulation = null;
            _cachedStealthUnit = null;
            _rangeLayer.Invalidate();
            _overwatchLayer.Invalidate();
            _stealthLayer.Invalidate();
            _pioneerLayer.Invalidate();
            _movementPathLayer.Invalidate();
            _assassinLayer.Invalidate();
            _spiritPathLayer.Invalidate();
        }

        void ReleaseView()
        {
            InvalidatePresentation();
            _rangeLayer.Release();
            _overwatchLayer.Release();
            _stealthLayer.Release();
            _pioneerLayer.Release();
            _movementPathLayer.Release();
            _assassinLayer.Release();
            _spiritPathLayer.Release();
            _assassinLanding.Clear();
            _spiritPath.Clear();
            _spiritLanding.Clear();
            _viewMap = null;
            _movement.Clear();
            _targets.Clear();
            _movementEdges.Clear();
            _attackEdges.Clear();
            _markers.Clear();
            _overwatchStates.Clear();
            _overwatchCells.Clear();
            _previewCells.Clear();
            _areaCells.Clear();
            _detectionCells.Clear();
            _stealthMarkers.Clear();
            _detectionUnion.Clear();
            _pioneerPath.Clear();
            _pioneerLanding.Clear();
            _movementPath.Clear();
            _movementLanding.Clear();
        }

        void OnDisable()
        {
            UnbindRunner();
            ReleaseView();
        }

        void OnDestroy()
        {
            UnbindRunner();
            ReleaseView();
        }
    }
}
