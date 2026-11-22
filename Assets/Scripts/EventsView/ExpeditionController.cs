using System;
using System.Collections.Generic;
using RaggedBanner.Events;
using RaggedBanner.GameState;
using RaggedBanner.WorldMap;
using RaggedBanner.WorldMap.Unity;
using UnityEngine;
using RunState = RaggedBanner.GameState.GameState;

namespace RaggedBanner.Events.Unity
{
    [DisallowMultipleComponent]
    public sealed class ExpeditionController : MonoBehaviour
    {
        [SerializeField]
        WorldMapController _worldMap;

        [SerializeField]
        WorldMapViewportController _mapViewport;

        [SerializeField]
        EventPanel _eventPanel;

        [SerializeField]
        TextAsset[] _eventFiles = Array.Empty<TextAsset>();

        [SerializeField, Min(1)]
        int _areaId = 1;

        [SerializeField, Min(0)]
        int _initialMoney = 10;

        [SerializeField, Range(0, 10)]
        int _initialFame;

        [SerializeField, Range(-3, 3)]
        int _initialReputation;

        EventRunner _runner;
        bool _bound;
        bool _viewportLocked;
        bool _viewportWasEnabled;

        public RunState State => _runner?.State;

        void Start()
        {
            try
            {
                Initialize();
            }
            catch
            {
                if (_worldMap != null)
                {
                    _worldMap.SetTravelAllowed(false);
                }

                if (_eventPanel != null)
                {
                    _eventPanel.Hide();
                }

                enabled = false;
                throw;
            }
        }

        void Initialize()
        {
            if (_worldMap == null || _worldMap.Definition == null || _mapViewport == null || _eventPanel == null)
            {
                throw new InvalidOperationException("Assign a world map controller with a definition, a map viewport and an event panel.");
            }

            if (_eventFiles == null || _eventFiles.Length == 0 || _areaId <= 0)
            {
                throw new InvalidOperationException("Assign the prototype event JSON files and a positive Area ID.");
            }

            WorldMapDefinition definition = _worldMap.Definition;
            if (definition.StartLocationId != 1)
            {
                throw new InvalidOperationException("The prototype expedition must start at the city, location 1.");
            }

            if (definition.Locations == null || definition.Locations.Count == 0)
            {
                throw new InvalidOperationException("Populate the world map locations and roads before starting the expedition.");
            }

            var sources = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < _eventFiles.Length; i++)
            {
                TextAsset file = _eventFiles[i];
                if (file == null || sources.ContainsKey(file.name))
                {
                    throw new InvalidOperationException("Event files must be assigned once and have unique names.");
                }

                sources.Add(file.name, file.text);
            }

            int[] locationIds = new int[definition.Locations.Count];
            for (int i = 0; i < locationIds.Length; i++)
            {
                if (definition.Locations[i] == null)
                {
                    throw new InvalidOperationException("World map locations cannot be null.");
                }

                locationIds[i] = definition.Locations[i].Id;
            }

            EventCatalog catalog = EventCatalog.Load(sources, locationIds, definition.StartLocationId);
            RunState state = catalog.CreateGameState(_areaId, definition.InitialExpeditionTime,
                _initialMoney, _initialFame, _initialReputation);
            _eventPanel.Initialize();
            _worldMap.Initialize(state.ExplorationState);
            _runner = new EventRunner(catalog, state);
            Bind();
            HandleArrival();
        }

        void OnEnable()
        {
            if (_runner == null)
            {
                return;
            }

            Bind();
            if (_runner.Stage == EventRunnerStage.Idle && !_worldMap.IsMoving)
            {
                HandleArrival();
            }
            else
            {
                RefreshPresentation();
            }
        }

        void OnDisable()
        {
            Unbind();
            SetViewportBlocked(false);
            if (_worldMap != null)
            {
                _worldMap.SetTravelAllowed(false);
            }

            if (_eventPanel != null)
            {
                _eventPanel.Hide();
            }
        }

        void Bind()
        {
            if (_bound)
            {
                return;
            }

            _worldMap.Arrived += OnArrived;
            _eventPanel.ChoiceSelected += OnChoiceSelected;
            _eventPanel.ContinueClicked += OnContinueClicked;
            _bound = true;
        }

        void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            if (_worldMap != null)
            {
                _worldMap.Arrived -= OnArrived;
            }

            if (_eventPanel != null)
            {
                _eventPanel.ChoiceSelected -= OnChoiceSelected;
                _eventPanel.ContinueClicked -= OnContinueClicked;
            }

            _bound = false;
        }

        void OnArrived(WorldMapArrival arrival)
        {
            HandleArrival();
        }

        void HandleArrival()
        {
            _worldMap.SetTravelAllowed(false);
            if (State.RunPhase != RunPhase.Ended)
            {
                _runner.TryEnterLocation(State.ExplorationState.CurrentLocation);
                if (_runner.Stage == EventRunnerStage.Idle)
                {
                    CompleteArrival();
                }
            }

            RefreshPresentation();
        }

        void OnChoiceSelected(string choiceId)
        {
            if (!isActiveAndEnabled || _runner == null || State.RunPhase == RunPhase.Ended)
            {
                return;
            }

            _eventPanel.SetInputEnabled(false);
            _runner.TryChoose(choiceId);
            RefreshPresentation();
        }

        void OnContinueClicked()
        {
            if (!isActiveAndEnabled || _runner == null || State.RunPhase == RunPhase.Ended)
            {
                return;
            }

            _eventPanel.SetInputEnabled(false);
            if (_runner.Stage == EventRunnerStage.PresentingEvent && _runner.CurrentEvent.IsAutomatic)
            {
                _runner.TryChoose("auto");
            }
            else if (_runner.Stage == EventRunnerStage.PresentingResult && _runner.TryConfirmResult())
            {
                CompleteArrival();
            }

            RefreshPresentation();
        }

        void CompleteArrival()
        {
            if (State.RunPhase == RunPhase.Normal && State.ExplorationState.RemainingExpeditionTime <= 0)
            {
                State.TryEndRun(new RunResult(RunOutcome.TimeExpired));
            }
        }

        void RefreshPresentation()
        {
            SetViewportBlocked(State.RunPhase == RunPhase.Ended || _runner.Stage != EventRunnerStage.Idle);
            bool canTravel = State.RunPhase == RunPhase.Normal
                && _runner.Stage == EventRunnerStage.Idle && !_worldMap.IsMoving
                && State.ExplorationState.RemainingExpeditionTime > 0;
            _worldMap.SetTravelAllowed(canTravel);

            if (State.RunPhase == RunPhase.Ended)
            {
                _eventPanel.ShowRunEnd();
            }
            else if (_runner.Stage == EventRunnerStage.PresentingEvent)
            {
                _eventPanel.ShowEvent(_runner.CurrentEvent, _runner.AvailableChoices);
            }
            else if (_runner.Stage == EventRunnerStage.PresentingResult)
            {
                _eventPanel.ShowResult(_runner.CurrentEvent, _runner.CurrentResult, _runner.AppliedEffects);
            }
            else
            {
                _eventPanel.Hide();
            }
        }

        void SetViewportBlocked(bool blocked)
        {
            if (_mapViewport == null || _viewportLocked == blocked)
            {
                return;
            }

            if (blocked)
            {
                _viewportWasEnabled = _mapViewport.enabled;
                _mapViewport.enabled = false;
            }
            else
            {
                _mapViewport.enabled = _viewportWasEnabled;
            }

            _viewportLocked = blocked;
        }
    }
}
