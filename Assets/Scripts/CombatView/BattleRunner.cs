using System.Collections.Generic;
using AutoBattler.HexGrid;
using AutoBattler.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField]
        HexMapView _mapView;

        [SerializeField]
        HexCharacterView[] _initialUnits = new HexCharacterView[0];

        [SerializeField]
        Transform _unitViewRoot;

        readonly Dictionary<int, HexCharacterView> _views = new Dictionary<int, HexCharacterView>();
        readonly HashSet<int> _spawnedViewIds = new HashSet<int>();
        readonly List<int> _destroyedUnitIds = new List<int>();

        BattleSimulation _simulation;
        HexMovementStep _step;
        float _hexSize;
        bool _hasStep;

        public HexMapView MapView => _mapView;
        public bool IsInitialized => _simulation != null;
        public bool IsBusy => _simulation != null && _simulation.IsBusy;
        public bool IsMovementPaused => _simulation != null && _simulation.IsMovementPaused;
        public int? CurrentUnitId => _simulation != null ? _simulation.CurrentUnitId : null;

        void Start()
        {
            if (!IsMapValid())
            {
                StopWithError("BattleRunner requires a non-empty HexMapView with a positive finite hex size.");
                return;
            }

            _simulation = new BattleSimulation(_mapView.Width, _mapView.Height);
            _hexSize = _mapView.HexSize;

            if (_initialUnits == null)
            {
                return;
            }

            for (int i = 0; i < _initialUnits.Length; i++)
            {
                if (!TryRegisterCharacter(_initialUnits[i], out _))
                {
                    StopWithError("BattleRunner could not register an initial unit. Check its view, starting position, and overlapping or duplicate units.");
                    return;
                }
            }
        }

        void Update()
        {
            if (_simulation == null)
            {
                return;
            }

            if (!IsConfigurationValid())
            {
                StopWithError("BattleRunner stopped because its map became invalid or its size changed.");
                return;
            }

            RemoveDestroyedUnits();
            AdvanceMovement(Time.deltaTime);
        }

        public bool TryRegisterCharacter(HexCharacterView view, out int unitId)
        {
            unitId = 0;

            if (!CanAcceptCommands() || view == null || view.IsInitialized
                || !view.isActiveAndEnabled || _simulation.IsBusy)
            {
                return false;
            }

            if (!TryFindNearestCoordinate(view.transform.position, out HexCoordinate coordinate))
            {
                Debug.LogError("BattleRunner could not find a valid starting hex for the character.", view);
                return false;
            }

            return TryRegisterAt(view, coordinate, false, out unitId);
        }

        public bool TrySpawnCharacter(HexCharacterView prefab, HexCoordinate coordinate, out int unitId)
        {
            unitId = 0;

            if (!CanAcceptCommands() || prefab == null || _simulation.IsBusy
                || !_simulation.Board.IsValid(coordinate) || _simulation.Board.IsOccupied(coordinate))
            {
                return false;
            }

            Transform parent = _unitViewRoot != null ? _unitViewRoot : transform;
            HexCharacterView view = Instantiate(prefab, parent);

            if (TryRegisterAt(view, coordinate, true, out unitId))
            {
                return true;
            }

            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
            return false;
        }

        public bool TryGetCharacter(int unitId, out HexCharacter character)
        {
            character = null;
            return _simulation != null && _simulation.Board.TryGetUnit(unitId, out character);
        }

        public bool TryMove(int unitId, HexCoordinate destination)
        {
            return CanAcceptCommands() && _views.TryGetValue(unitId, out HexCharacterView view)
                && view != null && view.IsReady && _simulation.TryMove(unitId, destination);
        }

        public bool TryEndTurn(int unitId)
        {
            return CanAcceptCommands() && _simulation.TryEndTurn(unitId);
        }

        public bool PauseMovement()
        {
            return CanAcceptCommands() && _simulation.PauseMovement();
        }

        public bool ResumeMovement()
        {
            return CanAcceptCommands() && _simulation.ResumeMovement();
        }

        public void CancelMovement()
        {
            if (_simulation == null)
            {
                return;
            }

            int unitId = _simulation.MovingUnitId;
            _simulation.CancelMovement();
            _hasStep = false;

            if (_views.TryGetValue(unitId, out HexCharacterView view) && view != null)
            {
                view.SnapToCharacter();
            }
        }

        public bool RemoveCharacter(int unitId)
        {
            if (_simulation == null || !_views.TryGetValue(unitId, out HexCharacterView view))
            {
                return false;
            }

            if (_simulation.MovingUnitId == unitId)
            {
                CancelMovement();
            }

            _simulation.RemoveUnit(unitId);
            _views.Remove(unitId);
            bool spawned = _spawnedViewIds.Remove(unitId);

            if (view != null)
            {
                view.Unbind();
                view.gameObject.SetActive(false);

                if (spawned)
                {
                    Destroy(view.gameObject);
                }
            }

            return true;
        }

        bool TryRegisterAt(HexCharacterView view, HexCoordinate coordinate, bool spawned, out int unitId)
        {
            unitId = 0;

            if (!_simulation.TrySpawnUnit(coordinate, out HexCharacter character))
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

        void AdvanceMovement(float remainingTime)
        {
            if (!_simulation.IsBusy)
            {
                return;
            }

            HexCharacterView view = _views[_simulation.MovingUnitId];

            if (!view.IsReady)
            {
                CancelMovement();
                return;
            }

            while (remainingTime > 0f)
            {
                if (!_hasStep)
                {
                    if (!_simulation.TryAdvanceMovement(out _step))
                    {
                        view.SetIdle();
                        return;
                    }

                    _hasStep = true;

                    if (!view.TryPlayMovement(_step))
                    {
                        CancelMovement();
                        return;
                    }
                }

                remainingTime = view.AdvanceMovement(remainingTime);

                if (view.IsPresenting)
                {
                    return;
                }

                _hasStep = false;
                _simulation.TryCompleteMovementStep(_step.Id);

                if (!_simulation.IsBusy || _simulation.IsMovementPaused)
                {
                    view.SetIdle();
                    return;
                }
            }
        }

        void RemoveDestroyedUnits()
        {
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
            return isActiveAndEnabled && _simulation != null && IsConfigurationValid();
        }

        bool IsConfigurationValid()
        {
            return IsMapValid() && _mapView.Width == _simulation.Board.Width
                && _mapView.Height == _simulation.Board.Height && _mapView.HexSize == _hexSize;
        }

        bool IsMapValid()
        {
            return _mapView != null && _mapView.Width > 0 && _mapView.Height > 0
                && _mapView.HexSize > 0f && !float.IsNaN(_mapView.HexSize) && !float.IsInfinity(_mapView.HexSize);
        }

        void OnDisable()
        {
            CancelMovement();
        }

        void OnDestroy()
        {
            ReleaseBattle();
        }

        void ReleaseBattle()
        {
            CancelMovement();

            foreach (KeyValuePair<int, HexCharacterView> entry in _views)
            {
                if (entry.Value != null)
                {
                    entry.Value.Unbind();

                    if (_spawnedViewIds.Contains(entry.Key))
                    {
                        entry.Value.gameObject.SetActive(false);
                        Destroy(entry.Value.gameObject);
                    }
                }
            }

            _views.Clear();
            _spawnedViewIds.Clear();
            _destroyedUnitIds.Clear();
            _simulation = null;
        }

        void StopWithError(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
            ReleaseBattle();
        }
    }
}
