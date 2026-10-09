using System;
using System.Collections.Generic;
using AutoBattler.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed class BattleSimulation
    {
        readonly BattleBoard _board;
        readonly HexPathfinder _pathfinder = new HexPathfinder();
        readonly List<int> _turnOrder = new List<int>();
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();

        int _lastUnitId;
        int _turnIndex;
        int _movingUnitId;
        int _pathIndex;
        long _lastStepId;
        long _pendingStepId;
        bool _isMovementPaused;

        public BattleBoard Board => _board;
        public int? CurrentUnitId => _turnOrder.Count > 0 ? _turnOrder[_turnIndex] : (int?)null;
        public bool IsBusy => _movingUnitId != 0;
        public int MovingUnitId => _movingUnitId;
        public bool IsMovementPaused => _isMovementPaused;
        public long PendingStepId => _pendingStepId;

        public BattleSimulation(int width, int height)
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
        }

        public bool TrySpawnUnit(HexCoordinate coordinate, out HexCharacter unit)
        {
            unit = null;

            if (IsBusy || _lastUnitId == int.MaxValue
                || !_board.TryAdd(_lastUnitId + 1, coordinate, out unit))
            {
                return false;
            }

            _lastUnitId = unit.Id;
            _turnOrder.Add(unit.Id);
            return true;
        }

        public bool RemoveUnit(int unitId)
        {
            int index = _turnOrder.IndexOf(unitId);

            if (index < 0)
            {
                return false;
            }

            if (_movingUnitId == unitId)
            {
                CancelMovement();
            }

            _board.Remove(unitId);
            _turnOrder.RemoveAt(index);

            if (index < _turnIndex)
            {
                _turnIndex--;
            }

            if (_turnIndex >= _turnOrder.Count)
            {
                _turnIndex = 0;
            }

            return true;
        }

        public bool TryEndTurn(int unitId)
        {
            if (IsBusy || CurrentUnitId != unitId)
            {
                return false;
            }

            _turnIndex = (_turnIndex + 1) % _turnOrder.Count;
            return true;
        }

        public bool TryMove(int unitId, HexCoordinate destination)
        {
            if (IsBusy || CurrentUnitId != unitId
                || !_board.TryGetUnit(unitId, out HexCharacter unit) || destination == unit.Coordinate)
            {
                return false;
            }

            if (!_pathfinder.TryFindPath(_board, unit.Coordinate, destination, _path) || _path.Count == 0)
            {
                return false;
            }

            _movingUnitId = unitId;
            _pathIndex = 0;
            return true;
        }

        public bool TryAdvanceMovement(out HexMovementStep step)
        {
            step = default;

            if (!IsBusy || _isMovementPaused || _pendingStepId != 0 || _pathIndex >= _path.Count)
            {
                return false;
            }

            long stepId = checked(_lastStepId + 1);
            HexCoordinate destination = _path[_pathIndex];

            if (!_board.TryGetUnit(_movingUnitId, out HexCharacter unit))
            {
                CancelMovement();
                return false;
            }

            HexCoordinate origin = unit.Coordinate;

            if (!_board.TryMove(_movingUnitId, destination))
            {
                CancelMovement();
                return false;
            }

            step = new HexMovementStep(stepId, _movingUnitId, origin, destination);
            _lastStepId = stepId;
            _pendingStepId = stepId;
            _pathIndex++;
            return true;
        }

        public bool TryCompleteMovementStep(long stepId)
        {
            if (stepId == 0 || stepId != _pendingStepId)
            {
                return false;
            }

            _pendingStepId = 0;

            if (_pathIndex == _path.Count)
            {
                CancelMovement();
            }

            return true;
        }

        public bool PauseMovement()
        {
            if (!IsBusy || _isMovementPaused)
            {
                return false;
            }

            _isMovementPaused = true;
            return true;
        }

        public bool ResumeMovement()
        {
            if (!IsBusy || !_isMovementPaused)
            {
                return false;
            }

            _isMovementPaused = false;
            return true;
        }

        public void CancelMovement()
        {
            _path.Clear();
            _pathIndex = 0;
            _movingUnitId = 0;
            _pendingStepId = 0;
            _isMovementPaused = false;
        }
    }
}
