using System.Collections.Generic;
using AutoBattler.HexGrid;
using AutoBattler.HexGrid.Unity;
using UnityEngine;

namespace AutoBattler.Combat.Unity
{
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField]
        HexMapView _mapView;

        [SerializeField]
        UnitDefinition _playerUnitDefinition;

        [SerializeField]
        UnitDefinition _enemyUnitDefinition;

        [SerializeField]
        Transform _unitViewRoot;

        [SerializeField]
        [Min(0.0001f)]
        float _movementTickInterval = 0.1f;

        readonly List<BattleUnitView> _unitViews = new List<BattleUnitView>();

        BattleSimulation _simulation;
        float _movementTickAccumulator;

        public BattleSimulation Simulation => _simulation;
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

        internal bool TryCreatePlacement(Team team, HexCoordinate coordinate, out BattlePlacement placement)
        {
            placement = default;
            UnitDefinition definition = GetDefinition(team);

            if (_mapView == null || !_mapView.IsValid(coordinate)
                || definition == null || !definition.IsValid())
            {
                return false;
            }

            placement = new BattlePlacement(definition, team, coordinate);
            return true;
        }

        internal BattleUnitView CreatePlacementView(BattlePlacement placement)
        {
            UnitDefinition definition = placement.Definition;
            BattleUnitView prefab = definition != null ? definition.ViewPrefab : null;

            if (definition == null || !definition.IsValid() || prefab == null || _mapView == null)
            {
                return null;
            }

            Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;
            BattleUnitView view = Instantiate(prefab, parent);
            view.ShowPlacement(placement.Coordinate, _mapView);
            return view;
        }

        internal bool TryStartBattle(IReadOnlyList<BattlePlacement> placements)
        {
            if (IsRunning || !isActiveAndEnabled || _mapView == null
                || _mapView.Width <= 0 || _mapView.Height <= 0
                || placements == null || !IsValidTickInterval())
            {
                return false;
            }

            BattleSimulation nextSimulation = new BattleSimulation(_mapView.Width, _mapView.Height);
            List<BattleUnit> spawnedUnits = new List<BattleUnit>(placements.Count);

            for (int i = 0; i < placements.Count; i++)
            {
                BattlePlacement placement = placements[i];
                UnitDefinition definition = placement.Definition;

                if (definition == null || !definition.IsValid()
                    || !nextSimulation.TrySpawnUnit(
                        placement.Team,
                        placement.Coordinate,
                        definition.Stat.CreateBattleUnitStat(),
                        out BattleUnit unit))
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
                    BattleUnitView view = Instantiate(placements[i].Definition.ViewPrefab, parent);
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
                BattleUnitView view = _unitViews[i];

                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }
            }

            _unitViews.Clear();
        }

        UnitDefinition GetDefinition(Team team)
        {
            switch (team)
            {
                case Team.Player:
                    return _playerUnitDefinition;
                case Team.Enemy:
                    return _enemyUnitDefinition;
                default:
                    return null;
            }
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
                BattleUnitView unitView = _unitViews[i];

                if (unitView == null)
                {
                    _unitViews.RemoveAt(i);
                    continue;
                }

                BattleUnit unit = unitView.Unit;

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
                BattleUnitView unitView = _unitViews[i];

                if (unitView != null)
                {
                    unitView.SyncMovement(_movementTickInterval);
                }
            }
        }
    }
}
