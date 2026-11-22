using System;
using RaggedBanner.BattleMaps;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    internal enum BattleStartPhase
    {
        Idle,
        Loading,
        Running,
        Result,
        Restarting,
        Error,
        Closing
    }

    [DisallowMultipleComponent]
    public sealed class BattleStartController : MonoBehaviour
    {
        [SerializeField]
        BattleMapDefinition _mapDefinition;

        [SerializeField]
        MercenaryClass[] _party = { MercenaryClass.Warrior, MercenaryClass.Rogue, MercenaryClass.Archer };

        [SerializeField]
        BattlePartySpecialization[] _partySpecializations = Array.Empty<BattlePartySpecialization>();

        [SerializeField]
        BattlePartyEquipment[] _partyEquipment = Array.Empty<BattlePartyEquipment>();

        [SerializeField]
        MercenaryDataList _classData;

        [SerializeField]
        BattleClassViewPrefab[] _classPrefabs = Array.Empty<BattleClassViewPrefab>();

        [SerializeField]
        int _randomSeed = 12345;

        [SerializeField]
        BattleRunner _runner;

        [SerializeField]
        BattleInputController _input;

        [SerializeField]
        GameObject _battleHud;

        [SerializeField]
        Camera _battleCamera;

        [SerializeField]
        Behaviour _cameraMovement;

        [SerializeField]
        BattleResultView _resultView;

        [SerializeField]
        BattleUnitOverlayController _overlays;

        BattleMapSceneSession _session;
        BattleMapDefinition _requestedDefinition;
        MercenaryClass[] _requestedParty;
        BattlePartySpecialization[] _requestedPartySpecializations;
        BattleEquipmentLoadout[] _requestedEquipment;
        MercenaryDataList _requestedClassData;
        BattleClassViewPrefab[] _requestedPrefabs;
        string _requestedScenePath;
        int _requestedSeed;
        BattleRunner _boundRunner;
        BattleResultView _boundResultView;
        BattleRestartSnapshot _snapshot;
        BattleResult _result;
        BattleStartPhase _phase;
        string _status = "전투 준비 중...";
        bool _ownsBattle;
        bool _restartPending;
        int _restartRequestedFrame;
        int _skippedPlayerCount;

        internal string Status => _status;
        internal bool IsRunning => _phase == BattleStartPhase.Running;
        internal int SkippedPlayerCount => _skippedPlayerCount;
        internal BattleStartPhase Phase => _phase;
        internal BattleResult Result => _result;
        internal bool CanRestart => isActiveAndEnabled && _ownsBattle && _snapshot != null
            && _boundRunner != null && _boundRunner.isActiveAndEnabled && _boundRunner.IsInitialized
            && (_phase == BattleStartPhase.Running || _phase == BattleStartPhase.Result);

        void Awake()
        {
            SetInputEnabled(false);
            SetCameraMovementEnabled(false);
            BindResultView();
            RefreshPresentation();
        }

        void Start()
        {
            if (!TryBegin(out string error) && _phase != BattleStartPhase.Closing)
            {
                Fail(error);
            }
        }

        void Update()
        {
            if (_phase == BattleStartPhase.Restarting)
            {
                if (_restartPending && Time.frameCount != _restartRequestedFrame)
                {
                    CompleteRestart();
                }

                return;
            }

            if ((_phase == BattleStartPhase.Running || _phase == BattleStartPhase.Result)
                && _ownsBattle && (_boundRunner == null || !_boundRunner.isActiveAndEnabled
                    || !_boundRunner.IsInitialized))
            {
                EnterError("전투 연결이 중단되었습니다. 설정을 확인한 뒤 Play를 다시 시작하세요.", false);
            }
        }

        internal bool TryBegin(out string error)
        {
            if (_phase != BattleStartPhase.Idle || !isActiveAndEnabled || !Application.isPlaying)
            {
                error = "이미 시작했거나 시작할 수 없는 전투입니다.";
                return false;
            }

            _phase = BattleStartPhase.Loading;

            if (!ValidateHost(out error)
                || !BattleMapPreparation.TryValidateConfiguration(_mapDefinition, _party, _classData,
                    _classPrefabs, _runner.MaxPlayerUnits, out error, _partySpecializations)
                || !BattlePartyEquipment.TryCapture(_party.Length, _partyEquipment, out _requestedEquipment, out error)
                || !BattleMapPreparation.TryValidateEquipment(_party, _classData, _requestedEquipment, out error))
            {
                return false;
            }

            SetInputEnabled(false);
            SetCameraMovementEnabled(false);
            BindResultView();
            BindRunner();
            _requestedDefinition = _mapDefinition;
            _requestedScenePath = _mapDefinition.ScenePath;
            _requestedParty = (MercenaryClass[])_party.Clone();
            _requestedPartySpecializations = _partySpecializations != null
                ? (BattlePartySpecialization[])_partySpecializations.Clone() : Array.Empty<BattlePartySpecialization>();
            _requestedClassData = _classData;
            _requestedPrefabs = (BattleClassViewPrefab[])_classPrefabs.Clone();
            _requestedSeed = _randomSeed;
            _session = new BattleMapSceneSession(gameObject.scene);
            _session.Ready += OnMapReady;
            _session.Failed += Fail;

            if (!_session.TryLoad(_requestedScenePath, out error))
            {
                return false;
            }

            _status = "전장 불러오는 중...";
            RefreshPresentation();
            return true;
        }

        bool ValidateHost(out string error)
        {
            if (_runner == null || !_runner.isActiveAndEnabled || !_runner.UsesExternalInitialization
                || _runner.IsInitialized || _runner.gameObject.scene != gameObject.scene)
            {
                error = "Battle requires an uninitialized, enabled Runner in External mode in its host scene.";
                return false;
            }

            if (_input == null || _input.gameObject.scene != gameObject.scene
                || !_input.gameObject.activeInHierarchy || _battleHud == null
                || _battleHud.scene != gameObject.scene || _battleHud.GetComponent<Canvas>() == null
                || transform.IsChildOf(_battleHud.transform) || _runner.transform.IsChildOf(_battleHud.transform)
                || _input.transform.IsChildOf(_battleHud.transform))
            {
                error = "Battle requires separate input and HUD objects in its host scene.";
                return false;
            }

            if (_battleCamera == null || !_battleCamera.isActiveAndEnabled
                || _battleCamera.gameObject.scene != gameObject.scene
                || _battleCamera.transform.IsChildOf(_battleHud.transform))
            {
                error = "Battle requires an active camera in its host scene.";
                return false;
            }

            if (_cameraMovement != null && (_cameraMovement == this || _cameraMovement == _runner
                || _cameraMovement == _input || _cameraMovement == _battleCamera
                || _cameraMovement.gameObject != _battleCamera.gameObject))
            {
                error = "Camera movement must reference a separate behaviour on the battle camera.";
                return false;
            }

            if (_resultView == null || _overlays == null
                || !_resultView.transform.IsChildOf(_battleHud.transform)
                || !_overlays.transform.IsChildOf(_battleHud.transform))
            {
                error = "Battle requires result and unit overlay views under its HUD.";
                return false;
            }

            error = null;
            return true;
        }

        void OnMapReady(HexMapView mapView)
        {
            if (this == null || !isActiveAndEnabled || _phase != BattleStartPhase.Loading)
            {
                return;
            }

            try
            {
                BindRunner();

                if (_requestedDefinition == null || _boundRunner == null
                    || !string.Equals(_requestedDefinition.ScenePath, _requestedScenePath, StringComparison.Ordinal))
                {
                    Fail("전장을 불러오는 동안 전투 연결 또는 맵 참조가 변경되었습니다.");
                    return;
                }

                if (!BattleMapPreparation.TryPrepare(_requestedDefinition, _requestedParty,
                    _requestedClassData, _requestedPrefabs,
                    mapView.Width, mapView.Height, _boundRunner.MaxPlayerUnits,
                    out BattleSpawnInput[] inputs, out int skippedCount, out string error, _requestedPartySpecializations,
                    _requestedEquipment)
                    || !TryFocusCamera(mapView, inputs, out error))
                {
                    Fail(error);
                    return;
                }

                if (!_boundRunner.TryInitializeBattle(mapView, inputs, _requestedSeed, out error))
                {
                    if (_phase == BattleStartPhase.Loading)
                    {
                        Fail(error);
                    }

                    return;
                }

                _ownsBattle = true;

                if (!CanCompleteInitialization(BattleStartPhase.Loading, mapView))
                {
                    StopOwnedBattle();

                    if (this != null && isActiveAndEnabled && _phase == BattleStartPhase.Loading)
                    {
                        Fail("초기화 중 전장 또는 전투 연결이 비활성화되었습니다.");
                    }

                    return;
                }

                _snapshot = new BattleRestartSnapshot(mapView, inputs, _boundRunner.Rules, _requestedSeed, skippedCount);
                _skippedPlayerCount = skippedCount;
                _phase = BattleStartPhase.Running;
                _status = string.Empty;
                SetCameraMovementEnabled(true);
                RefreshPresentation();
            }
            catch (Exception exception)
            {
                if (_phase != BattleStartPhase.Closing)
                {
                    Fail("전투를 시작하지 못했습니다: " + exception.Message);
                }
            }
        }

        void BindRunner()
        {
            if (_boundRunner != null)
            {
                return;
            }

            _boundRunner = _runner;

            if (_boundRunner != null)
            {
                _boundRunner.BattleCompleted += OnBattleCompleted;
            }
        }

        void BindResultView()
        {
            if (_boundResultView != null || _resultView == null)
            {
                return;
            }

            _boundResultView = _resultView;
            _boundResultView.RestartRequested += OnRestartRequested;
        }

        void OnBattleCompleted(BattleResult result)
        {
            if (_phase != BattleStartPhase.Running || !_ownsBattle || result == null
                || _boundRunner == null || !ReferenceEquals(_boundRunner.Result, result))
            {
                return;
            }

            _phase = BattleStartPhase.Result;
            _result = result;
            _status = string.Empty;
            RefreshPresentation();
        }

        void OnRestartRequested()
        {
            TryRequestRestart(out _);
        }

        internal bool TryRequestRestart(out string error)
        {
            error = null;

            if (!CanRestart)
            {
                error = "현재 상태에서는 재시작할 수 없습니다.";
                return false;
            }

            if (_boundRunner.MapView != _snapshot.MapView || !ValidateSnapshot(out error))
            {
                error = error ?? "현재 전장 연결이 최초 전장과 다릅니다.";
                _status = error;
                RefreshPresentation();
                return false;
            }

            _phase = BattleStartPhase.Restarting;
            _result = null;
            _restartPending = false;
            _restartRequestedFrame = Time.frameCount;
            _status = "같은 조건으로 다시 시작하는 중...";

            try
            {
                RefreshPresentation();

                if (_overlays != null)
                {
                    _overlays.ResetPresentation();
                }

                StopOwnedBattle();

                if (this == null || !isActiveAndEnabled || _phase != BattleStartPhase.Restarting)
                {
                    error = "재시작이 취소되었습니다.";
                    return false;
                }

                _restartPending = true;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "기존 전투를 정리하지 못했습니다: " + exception.Message;

                if (_phase != BattleStartPhase.Closing)
                {
                    EnterError(error, false);
                }

                return false;
            }
        }

        bool ValidateSnapshot(out string error)
        {
            if (_snapshot == null || _boundRunner == null || !_boundRunner.isActiveAndEnabled)
            {
                error = "보관된 시작 조건 또는 전투 연결이 없습니다.";
                return false;
            }

            return _snapshot.TryValidateMap(out error)
                && _boundRunner.ValidateRestartInputs(_snapshot.MapView, _snapshot.Inputs, _snapshot.Rules, out error);
        }

        void CompleteRestart()
        {
            if (_phase != BattleStartPhase.Restarting || !_restartPending || !isActiveAndEnabled)
            {
                return;
            }

            _restartPending = false;

            try
            {
                if (!ValidateSnapshot(out string error))
                {
                    EnterError(error, false);
                    return;
                }

                if (!_boundRunner.TryInitializeBattle(_snapshot.MapView, _snapshot.Inputs,
                    _snapshot.Rules, _snapshot.Seed, out error))
                {
                    if (_phase == BattleStartPhase.Restarting)
                    {
                        EnterError(error, false);
                    }

                    return;
                }

                _ownsBattle = true;

                if (!CanCompleteInitialization(BattleStartPhase.Restarting, _snapshot.MapView))
                {
                    StopOwnedBattle();

                    if (this != null && isActiveAndEnabled && _phase == BattleStartPhase.Restarting)
                    {
                        EnterError("재생성 중 전장 또는 전투 연결이 비활성화되었습니다.", false);
                    }

                    return;
                }

                if (!_snapshot.TryValidateMap(out error))
                {
                    EnterError(error, false);
                    return;
                }

                _phase = BattleStartPhase.Running;
                _status = string.Empty;
                RefreshPresentation();
            }
            catch (Exception exception)
            {
                if (_phase != BattleStartPhase.Closing)
                {
                    EnterError("유닛을 다시 생성하지 못했습니다: " + exception.Message, false);
                }
            }
        }

        bool CanCompleteInitialization(BattleStartPhase expected, HexMapView mapView)
        {
            return this != null && isActiveAndEnabled && _phase == expected
                && mapView != null && mapView.isActiveAndEnabled
                && _boundRunner != null && _boundRunner.isActiveAndEnabled && _boundRunner.IsInitialized;
        }

        bool TryFocusCamera(HexMapView mapView, BattleSpawnInput[] inputs, out string error)
        {
            Vector3 center = Vector3.zero;
            int count = 0;

            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].Team == BattleTeam.Player)
                {
                    center += mapView.AxialToWorld(inputs[i].Coordinate);
                    count++;
                }
            }

            Plane plane = new Plane(mapView.transform.up, mapView.transform.position);
            Ray ray = new Ray(_battleCamera.transform.position, _battleCamera.transform.forward);

            if (count == 0 || !plane.Raycast(ray, out float distance))
            {
                error = "The battle camera must face the map plane.";
                return false;
            }

            Vector3 position = _battleCamera.transform.position + center / count - ray.GetPoint(distance);

            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                error = "The battle camera focus position is not finite.";
                return false;
            }

            _battleCamera.transform.position = position;
            error = null;
            return true;
        }

        void RefreshPresentation()
        {
            if (this == null)
            {
                return;
            }

            SetInputEnabled(_phase == BattleStartPhase.Running);

            if (_battleHud != null && _battleHud.scene == gameObject.scene
                && !transform.IsChildOf(_battleHud.transform)
                && (_runner == null || !_runner.transform.IsChildOf(_battleHud.transform))
                && (_input == null || !_input.transform.IsChildOf(_battleHud.transform))
                && (_battleCamera == null || !_battleCamera.transform.IsChildOf(_battleHud.transform)))
            {
                _battleHud.SetActive(_phase != BattleStartPhase.Closing && isActiveAndEnabled);
            }

            if (this != null && _boundResultView != null)
            {
                int seed = _snapshot != null ? _snapshot.Seed : _requestedSeed;
                _boundResultView.Refresh(_result, _skippedPlayerCount, seed, CanRestart, _status);
            }
        }

        void SetInputEnabled(bool value)
        {
            if (this != null && _input != null && _input.gameObject.scene == gameObject.scene)
            {
                _input.enabled = value && _phase == BattleStartPhase.Running && isActiveAndEnabled;
            }
        }

        void SetCameraMovementEnabled(bool value)
        {
            if (this != null && _cameraMovement != null && _cameraMovement.gameObject.scene == gameObject.scene
                && _cameraMovement != this && _cameraMovement != _runner
                && _cameraMovement != _input && _cameraMovement != _battleCamera)
            {
                _cameraMovement.enabled = value && _phase != BattleStartPhase.Closing && isActiveAndEnabled;
            }
        }

        void StopOwnedBattle()
        {
            if (!_ownsBattle)
            {
                return;
            }

            _ownsBattle = false;

            if (_boundRunner != null)
            {
                _boundRunner.ShutdownBattle();
            }
        }

        void Fail(string error)
        {
            EnterError(error, true);
        }

        void EnterError(string error, bool unloadMap)
        {
            if (_phase == BattleStartPhase.Closing)
            {
                return;
            }

            _phase = BattleStartPhase.Error;
            _restartPending = false;
            _result = null;
            _status = error;
            SetInputEnabled(false);
            StopOwnedBattle();

            if (_overlays != null)
            {
                _overlays.ResetPresentation();
            }

            if (unloadMap)
            {
                DisposeMap();
            }

            RefreshPresentation();
        }

        void DisposeMap()
        {
            if (_session == null)
            {
                return;
            }

            _session.Ready -= OnMapReady;
            _session.Failed -= Fail;
            _session.Dispose();

            if (!string.IsNullOrEmpty(_session.CleanupError))
            {
                _status += "\n" + _session.CleanupError;
            }
        }

        void Shutdown()
        {
            if (_phase == BattleStartPhase.Closing)
            {
                return;
            }

            _phase = BattleStartPhase.Closing;
            _restartPending = false;

            if (_boundResultView != null)
            {
                _boundResultView.RestartRequested -= OnRestartRequested;
            }

            if (_boundRunner != null)
            {
                _boundRunner.BattleCompleted -= OnBattleCompleted;
            }

            SetInputEnabled(false);
            SetCameraMovementEnabled(false);

            try
            {
                StopOwnedBattle();

                if (_overlays != null)
                {
                    _overlays.ResetPresentation();
                }
            }
            finally
            {
                DisposeMap();
                RefreshPresentation();
            }
        }

        void OnDisable()
        {
            Shutdown();
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        void OnGUI()
        {
            if (_boundResultView != null || string.IsNullOrEmpty(_status))
            {
                return;
            }

            float width = Mathf.Min(640f, Mathf.Max(1f, Screen.width - 24f));
            GUILayout.BeginArea(new Rect(12f, Mathf.Max(12f, Screen.height - 104f), width, 92f), GUI.skin.box);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }
    }
}
