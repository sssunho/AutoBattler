using System;
using RaggedBanner.GameState;
using UnityEngine;

namespace RaggedBanner.WorldMap.Unity
{
    [DisallowMultipleComponent]
    public sealed class WorldMapController : MonoBehaviour
    {
        [SerializeField]
        WorldMapDefinition _definition;

        [SerializeField]
        WorldMapView _view;

        [SerializeField, Min(0.01f)]
        float _moveDuration = 0.6f;

        WorldMapTravel _travel;
        ExplorationState _explorationState;
        float _moveElapsed;
        float _activeMoveDuration;
        bool _travelAllowed;

        public event Action<WorldMapArrival> Arrived;

        public WorldMapDefinition Definition => _definition;
        public bool IsInitialized => _travel != null;
        public bool IsMoving => _travel != null && _travel.IsMoving;

        public void Initialize(ExplorationState explorationState)
        {
            if (explorationState == null)
            {
                throw new ArgumentNullException(nameof(explorationState));
            }

            if (_travel != null)
            {
                if (!ReferenceEquals(_explorationState, explorationState))
                {
                    throw new InvalidOperationException("The map is already initialized with another exploration state.");
                }

                return;
            }

            if (_definition == null || _view == null)
            {
                throw new InvalidOperationException("The map definition and view must be assigned before initialization.");
            }

            WorldMapGraph graph = _definition.CreateGraph();
            WorldMapTravel travel = new WorldMapTravel(graph, explorationState);
            _view.Initialize(_definition);
            _explorationState = explorationState;
            _travel = travel;
            _travelAllowed = false;
            _view.SetPartyPosition(_travel.CurrentLocationId, _travel.CurrentLocationId, 1f);
            _view.Refresh(_travel, false);

            if (isActiveAndEnabled)
            {
                _view.NodeClicked -= OnNodeClicked;
                _view.NodeClicked += OnNodeClicked;
            }
        }

        public void SetTravelAllowed(bool allowed)
        {
            _travelAllowed = allowed;

            if (_travel != null && _view != null)
            {
                _view.Refresh(_travel, _travelAllowed && isActiveAndEnabled);
            }
        }

        void OnEnable()
        {
            if (_travel != null && _view != null)
            {
                _view.NodeClicked -= OnNodeClicked;
                _view.NodeClicked += OnNodeClicked;
                _view.Refresh(_travel, _travelAllowed);
            }
        }

        void OnDisable()
        {
            if (_view != null)
            {
                _view.NodeClicked -= OnNodeClicked;
                _view.DisableInput();
            }
        }

        void Update()
        {
            if (_travel == null || !_travel.IsMoving || _view == null)
            {
                return;
            }

            _moveElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_moveElapsed / _activeMoveDuration);
            _view.SetPartyPosition(_travel.CurrentLocationId, _travel.DestinationLocationId, progress);

            if (progress >= 1f && _travel.TryCompleteTravel(out WorldMapArrival arrival))
            {
                _travelAllowed = false;
                _view.Refresh(_travel, false);
                Arrived?.Invoke(arrival);
            }
        }

        void OnNodeClicked(int locationId)
        {
            if (!isActiveAndEnabled || !_travelAllowed || _travel == null || !_travel.TryBeginTravel(locationId))
            {
                return;
            }

            _moveElapsed = 0f;
            _travelAllowed = false;
            _activeMoveDuration = float.IsNaN(_moveDuration) || float.IsInfinity(_moveDuration)
                ? 0.6f : Mathf.Max(0.01f, _moveDuration);
            _view.Refresh(_travel, false);
        }
    }
}
