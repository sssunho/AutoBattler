#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.AnimationTools
{
    [DisallowMultipleComponent]
    public sealed partial class AnimationEventToolController : MonoBehaviour
    {
        [SerializeField]
        Camera _previewCamera;

        [SerializeField]
        Transform _previewRoot;

        readonly AnimationEventToolCatalog _catalog = new AnimationEventToolCatalog();
        readonly AnimationEventToolPreview _preview = new AnimationEventToolPreview();
        readonly List<AnimationEventToolCatalog.Entry<AnimationClip>> _filteredClips = new List<AnimationEventToolCatalog.Entry<AnimationClip>>();
        AnimationEventToolDocument _document;
        AnimationEventToolSourceSnapshot _snapshot;
        AnimationEventToolKey _selectedKey;
        GameObject _prefab;
        CharacterAnimationProfile _profile;
        Action _pendingAction;
        string _pendingMessage;
        bool _pendingCanSave;
        bool _pendingReauthor;
        bool _configured;
        bool _blocked;
        bool _isSaving;
        bool _checkSourcesPending;
        bool _sourceChangesSubscribed;
        bool _ownsProfile;
        bool _setupUncertain;
        bool _showClipPicker;
        string _setupRecoveryPath;
        string _ownershipError;
        string _error;
        string _notice;
        string _entryKeyInput = string.Empty;
        string _newEntryKey = "skill1";
        string _frameInput = "0";
        string _clipFilter = string.Empty;
        Rect _originalCameraRect;
        Rect _ownedCameraRect;
        bool _ownsCameraRect;

        bool CanEditList => _document != null && _ownsProfile && !_blocked && !_document.IsSaveUncertain;
        bool CanEdit => CanEditList && _document.SelectedEntry != null && !_document.NeedsReauthor && _preview.IsReady;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _configured = _previewCamera != null && _previewCamera.isActiveAndEnabled
                && _previewRoot != null && _previewRoot.gameObject.activeInHierarchy
                && _previewRoot.gameObject.scene.IsValid();
            _error = _configured ? null : "Inspector에서 활성 Preview Camera와 씬의 Preview Root를 연결하세요.";
            _notice = "프리팹을 선택하면 캐릭터가 스폰됩니다. 저장하지 않은 변경은 Play 종료 시 사라집니다.";
            SubscribeSourceChanges();

            if (_configured)
            {
                CaptureCameraView();
                _originalCameraRect = _previewCamera.rect;
                _ownsCameraRect = true;
                UpdateCameraViewport();
                RefreshCatalog();
            }
        }

        void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            AnimationEventToolAssetPostprocessor.AssetsChanged -= OnAssetsImported;
            _sourceChangesSubscribed = false;
            _preview.Dispose();

            if (_ownsCameraRect && _viewCamera != null && _viewCamera.rect == _ownedCameraRect)
            {
                _viewCamera.rect = _originalCameraRect;
            }

            ReleaseCameraView();

            _ownsCameraRect = false;
            _configured = false;
            _document = null;
            _snapshot = null;
            _selectedKey = null;
            _prefab = null;
            _profile = null;
            _pendingAction = null;
            _blocked = false;
            _checkSourcesPending = false;
            _ownsProfile = false;
            _setupUncertain = false;
            _setupRecoveryPath = null;
            _showClipPicker = false;
        }

        void Update()
        {
            if (!Application.isPlaying || !_configured)
            {
                return;
            }

            ValidateCameraDrag();
            UpdateCameraViewport();
            ProcessPendingSourceChanges();

            if (_prefab == null || _blocked)
            {
                return;
            }

            if (!_preview.IsReady)
            {
                Block("프리뷰 자원이 변경되거나 사라졌습니다. 원본을 다시 불러오세요.");
                return;
            }

            if (_preview.IsPlaying && !_preview.TryAdvance(Time.unscaledDeltaTime, out string error))
            {
                Block(error);
            }
        }

        void SubscribeSourceChanges()
        {
            if (_sourceChangesSubscribed)
            {
                return;
            }

            EditorApplication.projectChanged += OnProjectChanged;
            AnimationEventToolAssetPostprocessor.AssetsChanged += OnAssetsImported;
            _sourceChangesSubscribed = true;
        }

        void OnAssetsImported()
        {
            if (_configured)
            {
                _checkSourcesPending = true;
            }
        }

        void ProcessPendingSourceChanges()
        {
            if (!_configured || !_checkSourcesPending || _isSaving)
            {
                return;
            }

            _checkSourcesPending = false;
            ValidateSources();
            RefreshCatalog();
        }

        void OnProjectChanged()
        {
            if (!_configured)
            {
                return;
            }

            if (_isSaving)
            {
                _checkSourcesPending = true;
                return;
            }

            ValidateSources();
            RefreshCatalog();
        }

        void RefreshCatalog()
        {
            try
            {
                _catalog.Refresh();
                FilterClips();
            }
            catch (Exception exception)
            {
                _error = "에셋 목록을 읽지 못했습니다: " + exception.Message;
            }
        }

        void FilterClips()
        {
            _filteredClips.Clear();

            for (int i = 0; i < _catalog.Clips.Length; i++)
            {
                AnimationEventToolCatalog.Entry<AnimationClip> entry = _catalog.Clips[i];

                if (string.IsNullOrWhiteSpace(_clipFilter)
                    || entry.SearchText.IndexOf(_clipFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _filteredClips.Add(entry);
                }
            }

            _clipScroll = Vector2.zero;
        }

        void SelectPrefab(GameObject prefab)
        {
            RequestChange(() => OpenPrefab(prefab), "캐릭터를 바꾸고 해당 캐릭터의 애니메이션 목록을 불러옵니다.");
        }

        void OpenPrefab(GameObject prefab)
        {
            CharacterAnimationProfile profile = AnimationEventToolCatalog.GetProfile(prefab);
            AnimationEventToolDocument document = null;

            if (profile != null && !AnimationEventToolDocument.TryOpen(profile, out document, out string error))
            {
                _error = error;
                return;
            }

            TryOpenSession(prefab, profile, document);
        }

        bool TryOpenSession(GameObject prefab, CharacterAnimationProfile profile, AnimationEventToolDocument document)
        {
            if (!AnimationEventToolSourceSnapshot.TryCapture(prefab, profile, out AnimationEventToolSourceSnapshot snapshot, out string error))
            {
                _error = error;
                return false;
            }

            if (!_preview.TryCreate(prefab, document != null ? document.Clip : null, _previewRoot, out error)
                || !snapshot.Validate(out error))
            {
                Block(error);
                return false;
            }

            bool changedCharacter = _prefab != prefab;
            _prefab = prefab;
            _profile = profile;
            _document = document;
            _snapshot = snapshot;
            _blocked = false;
            _setupUncertain = false;
            _setupRecoveryPath = null;
            _showClipPicker = false;
            _ownsProfile = AnimationEventToolProfileOwnership.TryValidateOwnership(prefab, profile, out _ownershipError);
            ResetSelection();

            if (changedCharacter)
            {
                ResetCameraView();
            }

            _error = null;
            _notice = document == null || document.Entries.Count == 0
                ? "캐릭터를 스폰했습니다. 전용 목록에서 애니메이션을 불러오세요."
                : "캐릭터 목록을 불러왔습니다. 키별 이벤트를 편집한 뒤 목록 전체를 저장하세요.";
            return true;
        }

        void CreateDedicatedProfile()
        {
            if (_prefab == null || _setupUncertain || !ValidateSources())
            {
                return;
            }

            _isSaving = true;
            bool success;
            CharacterAnimationProfile created;
            string error;

            try
            {
                success = AnimationEventToolProfileOwnership.TryCreateDedicated(_prefab, _profile,
                    out created, out _setupRecoveryPath, out _setupUncertain, out error);
            }
            finally
            {
                _isSaving = false;
            }

            if (success)
            {
                OpenPrefab(_prefab);
                _notice = "캐릭터 전용 목록 생성과 프리팹 연결을 저장했습니다.";
            }
            else if (_setupUncertain)
            {
                Block("목록 생성·연결 결과 미확정: " + error + " 생성 경로: " + _setupRecoveryPath
                    + " 원본 다시 불러오기로 연결 상태를 확인하세요.");
            }
            else
            {
                _error = error;
            }
        }

        void SelectEntry(AnimationEventToolEntry entry)
        {
            if (_document == null || _blocked || !ValidateSources() || !_document.TrySelectEntry(entry))
            {
                return;
            }

            RefreshPreview();
        }

        bool RefreshPreview()
        {
            if (!ValidateSources())
            {
                return false;
            }

            if (!_preview.TryCreate(_prefab, _document != null ? _document.Clip : null, _previewRoot, out string error)
                || !_snapshot.Validate(out error))
            {
                Block(error);
                return false;
            }

            ResetSelection();
            return true;
        }

        void ResetSelection()
        {
            _selectedKey = null;
            _frameInput = "0";
            _entryKeyInput = _document != null && _document.SelectedEntry != null ? _document.SelectedEntry.Key : string.Empty;
            _markerScroll = Vector2.zero;
        }

        void AddClip(AnimationClip clip)
        {
            if (!CanEditList || !ValidateSources())
            {
                return;
            }

            if (!_snapshot.TryWithClip(clip, out AnimationEventToolSourceSnapshot snapshot, out string error))
            {
                _error = error;
                ValidateSources();
                return;
            }

            if (!_document.TryAddEntry(_newEntryKey, clip, out AnimationEventToolEntry entry, out error))
            {
                _error = error;
                return;
            }

            _snapshot = snapshot;
            _document.TrySelectEntry(entry);
            _showClipPicker = false;
            _error = null;
            RefreshPreview();
        }

        void RenameEntry()
        {
            if (!CanEditList || !ValidateSources())
            {
                return;
            }

            if (_document.TryRenameEntry(_document.SelectedEntry, _entryKeyInput, out string error))
            {
                _entryKeyInput = _document.SelectedEntry.Key;
                _error = null;
            }
            else
            {
                _error = error;
            }
        }

        void RemoveEntry()
        {
            if (CanEditList && ValidateSources() && _document.RemoveEntry(_document.SelectedEntry))
            {
                RefreshPreview();
            }
        }

        void ConfirmReauthor(AnimationClip clip)
        {
            Pause();
            _pendingMessage = "현재 클립 기준으로 다시 작성하면 선택한 애니메이션의 이벤트를 비웁니다. 저장 전까지 원본은 유지됩니다.";
            _pendingCanSave = false;
            _pendingReauthor = true;
            _pendingAction = () => Reauthor(clip);
        }

        void Reauthor(AnimationClip clip)
        {
            if (!CanEditList || !ValidateSources())
            {
                return;
            }

            if (!_snapshot.TryWithClip(clip, out AnimationEventToolSourceSnapshot snapshot, out string error)
                || !_document.TryReauthor(clip, out error))
            {
                _error = error;
                return;
            }

            _snapshot = snapshot;
            _error = null;
            _notice = "선택한 애니메이션을 현재 클립 기준으로 다시 작성했습니다. 저장하면 원본에 반영됩니다.";
            RefreshPreview();
        }

        void RequestReload()
        {
            Pause();

            if (_prefab == null)
            {
                return;
            }

            _pendingMessage = "캐릭터의 작업본을 버리고 디스크 목록과 프리뷰를 다시 불러옵니다. 불러오기에 실패하면 작업본을 보존합니다.";
            _pendingCanSave = false;
            _pendingReauthor = false;
            _pendingAction = Reload;
        }

        void Reload()
        {
            CharacterAnimationProfile profile = AnimationEventToolCatalog.GetProfile(_prefab);

            if (profile == null || _document == null || _document.Source != profile)
            {
                OpenPrefab(_prefab);
                return;
            }

            string selectedKey = _document.SelectedEntry != null ? _document.SelectedEntry.Key : null;

            if (!AnimationEventToolAssetStore.TryReload(_document, out AnimationEventToolDocument document, out string error))
            {
                Block(error);
                return;
            }

            for (int i = 0; i < document.Entries.Count; i++)
            {
                if (document.Entries[i].Key == selectedKey)
                {
                    document.TrySelectEntry(document.Entries[i]);
                    break;
                }
            }

            TryOpenSession(_prefab, profile, document);
        }

        bool Save()
        {
            if (!CanEditList || !ValidateSources())
            {
                return false;
            }

            AnimationEventSaveResult result;
            AnimationEventToolSourceSnapshot snapshot;
            string error;
            _isSaving = true;

            try
            {
                result = AnimationEventToolAssetStore.TrySave(_document, _snapshot, out snapshot, out error);
            }
            finally
            {
                _isSaving = false;
            }

            if (result == AnimationEventSaveResult.Saved)
            {
                _snapshot = snapshot;
                _profile = _document.Source;
                _notice = "캐릭터 애니메이션 목록 저장 및 디스크 재로드 검증을 완료했습니다.";
                _error = null;
                RefreshCatalog();
                return true;
            }

            if (result == AnimationEventSaveResult.Uncertain)
            {
                Block("저장 결과 미확정: " + error + " 원본 다시 불러오기로 디스크 상태를 확인하세요.");
            }
            else
            {
                _error = error;
                ValidateSources();
            }

            return false;
        }

        bool ValidateSources()
        {
            if (_prefab == null && _snapshot == null)
            {
                return !_blocked;
            }

            string error = null;

            if (_snapshot == null || !_snapshot.Validate(out error))
            {
                Block(_snapshot == null ? "원본 검사 기준이 없습니다. 다시 불러오세요." : error);
                return false;
            }

            if (_ownsProfile && !AnimationEventToolProfileOwnership.TryValidateOwnership(_prefab, _profile, out error))
            {
                Block(error);
                return false;
            }

            return !_blocked;
        }

        void Block(string error)
        {
            CancelCameraDrag();
            _blocked = true;
            _error = error;
            _preview.Dispose();
        }

        void RequestChange(Action action, string message)
        {
            if (_pendingAction != null)
            {
                return;
            }

            Pause();

            if (!_setupUncertain && (_document == null || !_document.IsDirty && !_document.IsSaveUncertain))
            {
                action();
                return;
            }

            _pendingAction = action;
            _pendingMessage = message + " 저장하지 않은 변경 또는 확인하지 않은 저장 결과가 있습니다.";
            _pendingCanSave = CanEditList;
            _pendingReauthor = false;
        }

        void ResolvePending(bool save)
        {
            if (save && !Save())
            {
                return;
            }

            Action action = _pendingAction;
            _pendingAction = null;
            action?.Invoke();
        }

        void Pause()
        {
            if (_preview.IsPlaying && !_preview.TrySetPlaying(false, out string error))
            {
                Block(error);
            }
        }

        void SetFrame(int frame)
        {
            if (!CanEdit)
            {
                return;
            }

            Pause();

            if (!_preview.TrySetFrame(frame, out string error))
            {
                Block(error);
            }
        }

        void SelectKey(AnimationEventToolKey key)
        {
            _selectedKey = key;
            _frameInput = key.Frame.ToString();
            SetFrame(key.Frame);
        }

        void UpdateCameraViewport()
        {
            if (_viewCamera == null || _viewCamera != _previewCamera || Screen.width < 900 || Screen.height < 600)
            {
                return;
            }

            Rect area = GetPreviewArea();
            _ownedCameraRect = new Rect(area.x / Screen.width, 1f - area.yMax / Screen.height,
                area.width / Screen.width, area.height / Screen.height);
            _viewCamera.rect = _ownedCameraRect;
        }
    }
}
#endif
