using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace AutoBattler.Combat.Unity
{
    public sealed class AutoBattlerBattleRunner : MonoBehaviour
    {
        [SerializeField]
        HexMapView _mapView;

        [SerializeField]
        Transform _unitViewRoot;

        [SerializeField]
        [Min(0.0001f)]
        float _movementTickInterval = 0.1f;

        readonly List<AutoBattlerBattleUnitView> _unitViews = new List<AutoBattlerBattleUnitView>();

        AutoBattlerBattleSimulation _simulation;
        float _movementTickAccumulator;

        public AutoBattlerBattleSimulation Simulation => _simulation;
        internal HexMapView MapView => _mapView;
        internal bool IsRunning => _simulation != null;

        void Update()
        {
            if (_simulation == null || !IsValidTickInterval())
            {
                return;
            }

            for (int i = 0; i < _unitViews.Count; i++)
            {
                if (_unitViews[i] != null)
                {
                    _unitViews[i].AdvanceMovement(Time.deltaTime);
                }
            }

            _movementTickAccumulator += Time.deltaTime;
            bool advanced = false;

            while (_movementTickAccumulator >= _movementTickInterval)
            {
                _simulation.TickMovement(_movementTickInterval);
                _movementTickAccumulator -= _movementTickInterval;
                advanced = true;
            }

            _simulation.TickAttacks(Time.deltaTime);
            RemoveDeadUnitViews();

            if (advanced)
            {
                SyncUnitViews();
            }
        }

        void OnDisable()
        {
            StopBattle();
        }

        void OnDestroy()
        {
            StopBattle();
        }

        internal bool TryCreatePlacement(
            AutoBattlerUnitDefinition definition,
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            out AutoBattlerBattlePlacement placement)
        {
            placement = default;

            if (_mapView == null || !_mapView.IsValid(coordinate)
                || definition == null || !definition.IsValid())
            {
                return false;
            }

            placement = new AutoBattlerBattlePlacement(definition, team, coordinate);
            return true;
        }

        internal AutoBattlerBattleUnitView CreatePlacementView(AutoBattlerBattlePlacement placement)
        {
            AutoBattlerUnitDefinition definition = placement.Definition;
            AutoBattlerBattleUnitView prefab = definition != null ? definition.ViewPrefab : null;

            if (definition == null || !definition.IsValid() || prefab == null || _mapView == null)
            {
                return null;
            }

            Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;
            AutoBattlerBattleUnitView view = Instantiate(prefab, parent);
            view.ShowPlacement(placement.Coordinate, _mapView);
            return view;
        }

        internal bool TryStartBattle(IReadOnlyList<AutoBattlerBattlePlacement> placements)
        {
            if (IsRunning || !isActiveAndEnabled || _mapView == null
                || _mapView.Width <= 0 || _mapView.Height <= 0
                || placements == null || !IsValidTickInterval())
            {
                return false;
            }

            AutoBattlerBattleSimulation nextSimulation = new AutoBattlerBattleSimulation(_mapView.Width, _mapView.Height);
            List<AutoBattlerBattleUnit> spawnedUnits = new List<AutoBattlerBattleUnit>(placements.Count);

            for (int i = 0; i < placements.Count; i++)
            {
                AutoBattlerBattlePlacement placement = placements[i];
                AutoBattlerUnitDefinition definition = placement.Definition;

                if (definition == null || !definition.IsValid()
                    || !nextSimulation.TrySpawnUnit(
                        placement.Team,
                        placement.Coordinate,
                        definition.Stat.CreateBattleUnitStat(),
                        out AutoBattlerBattleUnit unit))
                {
                    return false;
                }

                spawnedUnits.Add(unit);
            }

            try
            {
                Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;

                for (int i = 0; i < placements.Count; i++)
                {
                    AutoBattlerBattleUnitView view = Instantiate(placements[i].Definition.ViewPrefab, parent);
                    _unitViews.Add(view);
                    view.Bind(spawnedUnits[i], _mapView);
                }

                _movementTickAccumulator = 0f;
                _simulation = nextSimulation;
                return true;
            }
            catch
            {
                StopBattle();
                throw;
            }
        }

        internal void StopBattle()
        {
            _simulation = null;
            _movementTickAccumulator = 0f;

            for (int i = 0; i < _unitViews.Count; i++)
            {
                AutoBattlerBattleUnitView view = _unitViews[i];

                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }
            }

            _unitViews.Clear();
        }

        bool IsValidTickInterval()
        {
            return _movementTickInterval > 0f
                && !float.IsNaN(_movementTickInterval)
                && !float.IsInfinity(_movementTickInterval);
        }

        void RemoveDeadUnitViews()
        {
            for (int i = _unitViews.Count - 1; i >= 0; i--)
            {
                AutoBattlerBattleUnitView unitView = _unitViews[i];

                if (unitView == null)
                {
                    _unitViews.RemoveAt(i);
                    continue;
                }

                AutoBattlerBattleUnit unit = unitView.Unit;

                if (unit == null || unit.IsAlive)
                {
                    continue;
                }

                unitView.gameObject.SetActive(false);
                Destroy(unitView.gameObject);
                _unitViews.RemoveAt(i);
            }
        }

        void SyncUnitViews()
        {
            for (int i = 0; i < _unitViews.Count; i++)
            {
                AutoBattlerBattleUnitView unitView = _unitViews[i];

                if (unitView != null)
                {
                    unitView.SyncMovement(_movementTickInterval);
                }
            }
        }
    }
}
