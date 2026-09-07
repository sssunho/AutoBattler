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
        BattleUnitView _playerUnitPrefab;

        [SerializeField]
        BattleUnitView _enemyUnitPrefab;

        [SerializeField]
        Transform _unitViewRoot;

        [SerializeField]
        [Min(0)]
        int _attackRange = 1;

        [SerializeField]
        [Min(0f)]
        float _moveSpeed = 1f;

        [SerializeField]
        [Min(1)]
        int _maxHealth = 100;

        [SerializeField]
        [Min(0)]
        int _attackDamage = 10;

        [SerializeField]
        [Range(0.0001f, 1f)]
        float _attackHitNormalizedTime = 0.4f;

        [SerializeField]
        [Min(0f)]
        float _attackCooldownDuration = 0.4f;

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
            BattleUnitView prefab = GetPrefab(team);
            float attackAnimationDuration = prefab != null ? prefab.AttackAnimationDuration : 0f;

            if (_mapView == null || !_mapView.IsValid(coordinate)
                || prefab == null || _attackRange < 0
                || _moveSpeed < 0f || float.IsNaN(_moveSpeed) || float.IsInfinity(_moveSpeed)
                || _maxHealth <= 0 || _attackDamage < 0
                || attackAnimationDuration <= 0f
                || float.IsNaN(attackAnimationDuration)
                || float.IsInfinity(attackAnimationDuration)
                || _attackHitNormalizedTime <= 0f
                || _attackHitNormalizedTime > 1f
                || float.IsNaN(_attackHitNormalizedTime)
                || float.IsInfinity(_attackHitNormalizedTime)
                || _attackCooldownDuration < 0f
                || float.IsNaN(_attackCooldownDuration)
                || float.IsInfinity(_attackCooldownDuration))
            {
                return false;
            }

            placement = new BattlePlacement(
                team,
                coordinate,
                _attackRange,
                _moveSpeed,
                _maxHealth,
                _attackDamage,
                attackAnimationDuration,
                _attackHitNormalizedTime,
                _attackCooldownDuration);
            return true;
        }

        internal BattleUnitView CreatePlacementView(BattlePlacement placement)
        {
            BattleUnitView prefab = GetPrefab(placement.Team);

            if (prefab == null || _mapView == null)
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

            for (int i = 0; i < placements.Count; i++)
            {
                BattlePlacement placement = placements[i];

                if (GetPrefab(placement.Team) == null
                    || !nextSimulation.TrySpawnUnit(placement.Team, placement.Coordinate,
                        placement.AttackRange, placement.MoveSpeed, placement.MaxHealth,
                        placement.AttackDamage, placement.AttackAnimationDuration,
                        placement.AttackHitNormalizedTime, placement.AttackCooldownDuration, out _))
                {
                    return false;
                }
            }

            try
            {
                Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;

                foreach (BattleUnit unit in nextSimulation.Board.Units)
                {
                    BattleUnitView view = Instantiate(GetPrefab(unit.Team), parent);
                    _unitViews.Add(view);
                    view.Bind(unit, _mapView);
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

        BattleUnitView GetPrefab(Team team)
        {
            switch (team)
            {
                case Team.Player:
                    return _playerUnitPrefab;
                case Team.Enemy:
                    return _enemyUnitPrefab;
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
