using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    sealed class BattleMovementExecution
    {
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();

        int _index;
        int _moverId;
        MovementKind _kind;
        HexCoordinate _actorOrigin;
        HexCoordinate _targetOrigin;
        AssassinLeapPreview _leapPreview;
        bool _waitingForCompletion;
        bool _stopRequested;
        bool _isPaused;

        public IReadOnlyList<HexCoordinate> Path => _path;
        public int Index => _index;
        public int MoverId => _moverId;
        public MovementKind Kind => _kind;
        public HexCoordinate ActorOrigin => _actorOrigin;
        public HexCoordinate TargetOrigin => _targetOrigin;
        public AssassinLeapPreview LeapPreview => _leapPreview;
        public bool WaitingForCompletion => _waitingForCompletion;
        public bool StopRequested => _stopRequested;
        public bool IsPaused => _isPaused;
        public bool IsComplete => _index == _path.Count;

        public void Begin(IReadOnlyList<HexCoordinate> path, int moverId, MovementKind kind,
            HexCoordinate actorOrigin, HexCoordinate targetOrigin = default)
        {
            if (!ReferenceEquals(path, _path))
            {
                _path.Clear();
                for (int i = 0; i < path.Count; i++)
                {
                    _path.Add(path[i]);
                }
            }

            _index = 0;
            _moverId = moverId;
            _kind = kind;
            _actorOrigin = actorOrigin;
            _targetOrigin = targetOrigin;
            _leapPreview = default;
            _waitingForCompletion = false;
            _stopRequested = false;
            _isPaused = false;
        }

        public void BeginLeap(AssassinLeapPreview preview)
        {
            Reset();
            _path.Add(preview.Landing);
            _moverId = preview.ActorId;
            _kind = MovementKind.Skill;
            _actorOrigin = preview.ActorOrigin;
            _targetOrigin = preview.TargetOrigin;
            _leapPreview = preview;
        }

        public bool TryGetNext(out HexCoordinate destination)
        {
            destination = default;
            if (_isPaused || _waitingForCompletion || _index >= _path.Count)
            {
                return false;
            }

            destination = _path[_index];
            return true;
        }

        public void MarkStepIssued()
        {
            if (!TryGetNext(out _))
            {
                throw new InvalidOperationException("No movement step is ready to issue.");
            }

            _index++;
            _waitingForCompletion = true;
        }

        public bool TryCompleteStep()
        {
            if (!_waitingForCompletion)
            {
                return false;
            }

            _waitingForCompletion = false;
            return true;
        }

        public void RequestStop()
        {
            _stopRequested = true;
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        public void DiscardRemainingPath()
        {
            _path.Clear();
            _index = 0;
            _isPaused = false;
        }

        public bool CopyRemainingPath(List<HexCoordinate> destination)
        {
            destination.Clear();
            if (_path.Count == 0)
            {
                return false;
            }

            int start = _waitingForCompletion ? _index - 1 : _index;
            for (int i = start; i < _path.Count; i++)
            {
                destination.Add(_path[i]);
            }

            return destination.Count > 0;
        }

        public void Reset()
        {
            DiscardRemainingPath();
            _moverId = 0;
            _kind = default;
            _actorOrigin = default;
            _targetOrigin = default;
            _leapPreview = default;
            _waitingForCompletion = false;
            _stopRequested = false;
        }
    }
}
