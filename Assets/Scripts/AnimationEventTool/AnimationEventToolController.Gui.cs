#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RaggedBanner.Animations;
using UnityEngine;

namespace RaggedBanner.AnimationTools
{
    public sealed partial class AnimationEventToolController
    {
        const float _rowHeight = 23f;
        Vector2 _prefabScroll;
        Vector2 _entryScroll;
        Vector2 _clipScroll;
        Vector2 _sidebarScroll;
        Vector2 _markerScroll;
        GUIStyle _wrappedStyle;
        GUIStyle _titleStyle;
        GUIStyle _selectedStyle;

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureStyles();
            HandlePreviewInput();

            if (Screen.width < 900 || Screen.height < 600)
            {
                GUI.Box(new Rect(10f, 10f, Mathf.Max(100f, Screen.width - 20f), 80f),
                    "애니메이션 이벤트 툴\nGame 뷰를 900×600 이상으로 넓혀 주세요.");
                return;
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && _configured && _pendingAction == null && !_showClipPicker;
            DrawSidebar();
            DrawTimeline();
            GUI.enabled = previousEnabled;

            if (!_configured)
            {
                GUI.Box(GetPreviewArea(), _error ?? "Preview Camera와 Preview Root를 연결한 뒤 Play Mode로 다시 진입하세요.", _wrappedStyle);
            }
            else if (_prefab == null)
            {
                GUI.Label(GetPreviewArea(), "오른쪽의 Combat 프리팹을 선택하세요.", _wrappedStyle);
            }

            if (_showClipPicker)
            {
                DrawClipPicker();
            }

            if (_pendingAction != null)
            {
                DrawPendingDialog();
            }
        }

        void EnsureStyles()
        {
            if (_wrappedStyle != null)
            {
                return;
            }

            _wrappedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 };
            _selectedStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            _selectedStyle.normal.textColor = Color.cyan;
        }

        void DrawSidebar()
        {
            GUILayout.BeginArea(GetSidebarArea(), GUI.skin.box);
            _sidebarScroll = GUILayout.BeginScrollView(_sidebarScroll, false, false);
            GUILayout.Label("애니메이션 이벤트 툴", _titleStyle);
            GUILayout.Label(_error ?? _notice, _wrappedStyle);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("목록 새로고침"))
            {
                ValidateSources();
                RefreshCatalog();
            }

            bool enabled = GUI.enabled;
            GUI.enabled = enabled && _prefab != null;

            if (GUILayout.Button("원본 다시 불러오기"))
            {
                RequestReload();
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled && _prefab != null && !_blocked;

            if (GUILayout.Button("다시 스폰"))
            {
                RefreshPreview();
            }

            GUI.enabled = enabled && CanControlCamera;

            if (GUILayout.Button("시점 초기화"))
            {
                ResetCameraView();
            }

            GUI.enabled = enabled && CanEditList;

            if (GUILayout.Button("저장" + (_document != null && _document.IsDirty ? " *" : "")))
            {
                Save();
            }

            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
            GUILayout.Label("휠: 확대/축소 · 오른쪽 드래그: 회전", _wrappedStyle);
            GUILayout.Label("Combat 프리팹 · 선택하면 스폰");
            DrawPicker(_catalog.Prefabs, _prefab, ref _prefabScroll, SelectPrefab, 92f);
            GUILayout.Label("캐릭터에 배정된 애니메이션");
            GUI.enabled = enabled && CanEditList;

            if (GUILayout.Button("애니메이션 불러오기"))
            {
                Pause();
                _showClipPicker = true;
            }

            GUI.enabled = enabled && _document != null && !_blocked;
            _entryScroll = GUILayout.BeginScrollView(_entryScroll, GUILayout.Height(92f));

            if (_document != null)
            {
                for (int i = 0; i < _document.Entries.Count; i++)
                {
                    AnimationEventToolEntry entry = _document.Entries[i];
                    string label = entry.Key + " · " + (entry.Clip != null ? entry.Clip.name : "클립 없음");

                    if (GUILayout.Button(new GUIContent(label, label), entry == _document.SelectedEntry ? _selectedStyle : GUI.skin.button, GUILayout.Width(264f)))
                    {
                        SelectEntry(entry);
                    }
                }

                if (_document.Entries.Count == 0)
                {
                    GUILayout.Label("배정된 애니메이션이 없습니다.", _wrappedStyle);
                }
            }

            GUILayout.EndScrollView();
            GUI.enabled = enabled;
            DrawProperties();
            GUILayout.Label("미저장 작업본은 Play 종료 시 사라집니다.", _wrappedStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawClipPicker()
        {
            Rect area = new Rect(40f, 40f, Screen.width - 80f, Screen.height - 80f);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 16f, area.y + 16f, area.width - 32f, area.height - 32f));
            GUILayout.Label("애니메이션 불러오기", _titleStyle);
            GUILayout.Label("Assets/Art/Character/Animation · 클립을 선택하면 독립적인 항목을 추가합니다.", _wrappedStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("저장 키", GUILayout.Width(60f));
            _newEntryKey = GUILayout.TextField(_newEntryKey);
            GUILayout.Label("검색", GUILayout.Width(35f));
            string filter = GUILayout.TextField(_clipFilter);

            if (filter != _clipFilter)
            {
                _clipFilter = filter;
                FilterClips();
            }

            GUILayout.EndHorizontal();
            DrawPicker(_filteredClips, (AnimationClip)null, ref _clipScroll, AddClip, area.height - 175f);
            GUILayout.Label(_error ?? "같은 클립을 다른 키로 여러 번 추가할 수 있습니다.", _wrappedStyle);

            if (GUILayout.Button("닫기"))
            {
                _showClipPicker = false;
            }

            GUILayout.EndArea();
        }

        void DrawPicker<T>(IReadOnlyList<AnimationEventToolCatalog.Entry<T>> entries, T selected,
            ref Vector2 scroll, Action<T> select, float height = 138f) where T : UnityEngine.Object
        {
            Rect area = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            Rect content = new Rect(0f, 0f, Mathf.Max(10f, area.width - 18f), Mathf.Max(area.height, entries.Count * _rowHeight));
            scroll = GUI.BeginScrollView(area, scroll, content);
            int start = Mathf.Max(0, Mathf.FloorToInt(scroll.y / _rowHeight));
            int end = Mathf.Min(entries.Count, start + Mathf.CeilToInt(area.height / _rowHeight) + 1);

            for (int i = start; i < end; i++)
            {
                AnimationEventToolCatalog.Entry<T> entry = entries[i];
                Rect button = new Rect(0f, i * _rowHeight, content.width, _rowHeight - 2f);

                if (GUI.Button(button, entry.Label, entry.Asset == selected ? _selectedStyle : GUI.skin.button))
                {
                    select(entry.Asset);
                }
            }

            GUI.EndScrollView();
        }

        void DrawProperties()
        {
            GUILayout.Label("애니메이션 / 이벤트", _titleStyle);

            if (_prefab == null)
            {
                GUILayout.Label("Combat 프리팹을 선택하세요.", _wrappedStyle);
                return;
            }

            if (!_ownsProfile)
            {
                GUILayout.Label(_ownershipError ?? "캐릭터 전용 애니메이션 목록이 필요합니다.", _wrappedStyle);
                bool setupEnabled = GUI.enabled;
                GUI.enabled = setupEnabled && !_blocked && !_setupUncertain;

                if (GUILayout.Button(_profile == null ? "캐릭터 전용 목록 만들기" : "캐릭터 전용 목록 복사"))
                {
                    CreateDedicatedProfile();
                }

                GUI.enabled = setupEnabled;
            }

            if (_document == null)
            {
                return;
            }

            GUILayout.Label(_document.Source != null ? _document.Source.name
                : "원본 목록이 삭제되었습니다: " + _document.RecoveryPath, _wrappedStyle);
            GUILayout.Label("게임 사용 키: idle · move · attack1", _wrappedStyle);
            bool listEnabled = GUI.enabled;
            GUI.enabled = listEnabled && CanEditList && _document.SelectedEntry != null;
            GUILayout.BeginHorizontal();
            _entryKeyInput = GUILayout.TextField(_entryKeyInput, GUILayout.MinWidth(30f));

            if (GUILayout.Button("키 변경", GUILayout.Width(65f)))
            {
                RenameEntry();
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("선택 애니메이션 삭제"))
            {
                RemoveEntry();
            }

            GUI.enabled = listEnabled;

            if (_document.SelectedEntry == null)
            {
                GUILayout.Label("애니메이션을 불러오거나 목록에서 선택하세요.", _wrappedStyle);
                return;
            }

            if (_document.NeedsReauthor)
            {
                GUILayout.Label("작성 기준과 현재 클립이 다릅니다. 키를 비우고 다시 작성해야 편집·저장할 수 있습니다.", _wrappedStyle);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && CanEditList;

                if (GUILayout.Button("현재 클립 기준으로 다시 작성"))
                {
                    ConfirmReauthor(_document.Clip);
                }

                GUI.enabled = wasEnabled;
            }

            bool enabled = GUI.enabled;
            GUI.enabled = enabled && CanEdit;

            if (_selectedKey != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label("이벤트: 공격 판정");
                GUILayout.BeginHorizontal();
                GUILayout.Label("프레임", GUILayout.Width(48f));
                _frameInput = GUILayout.TextField(_frameInput);

                if (GUILayout.Button("적용", GUILayout.Width(50f)))
                {
                    if (!int.TryParse(_frameInput, out int frame))
                    {
                        _error = "프레임은 정수여야 합니다.";
                    }
                    else if (_document.TryEdit(_selectedKey, frame, _selectedKey.AttackKind, out string error))
                    {
                        SetFrame(frame);
                        _error = null;
                    }
                    else
                    {
                        _error = error;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                DrawAttackKind("근접", AnimationAttackKind.Melee);
                DrawAttackKind("투사체", AnimationAttackKind.Projectile);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("선택 키 삭제"))
                {
                    _document.Remove(_selectedKey);
                    _selectedKey = null;
                }
            }
            else
            {
                GUILayout.Label("슬라이더 위 키 또는 아래 목록에서 선택하세요.", _wrappedStyle);
            }

            GUILayout.Label("키 목록 (같은 프레임도 개별 선택)");

            for (int i = 0; i < _document.Keys.Count; i++)
            {
                AnimationEventToolKey key = _document.Keys[i];
                string label = (i + 1) + ". " + key.Frame + "F · 공격 판정 · "
                    + (key.AttackKind == AnimationAttackKind.Melee ? "근접" : "투사체");

                if (GUILayout.Button(label, key == _selectedKey ? _selectedStyle : GUI.skin.button))
                {
                    SelectKey(key);
                }
            }

            GUI.enabled = enabled;
        }

        void DrawAttackKind(string label, AnimationAttackKind kind)
        {
            if (GUILayout.Button(label, _selectedKey.AttackKind == kind ? _selectedStyle : GUI.skin.button)
                && !_document.TryEdit(_selectedKey, _selectedKey.Frame, kind, out string error))
            {
                _error = error;
            }
        }

        void DrawTimeline()
        {
            Rect area = GetTimelineArea();
            GUI.Box(area, GUIContent.none);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && CanEdit;

            if (_document != null && _document.SelectedEntry != null)
            {
                DrawMarkers(new Rect(area.x + 10f, area.y + 5f, area.width - 20f, 50f));
                Rect slider = new Rect(area.x + 22f, area.y + 61f, area.width - 44f, 20f);
                GUI.enabled = enabled && CanEdit && _document.LastFrame > 0;
                float value = GUI.HorizontalSlider(slider, _preview.CurrentFrame, 0f, _document.LastFrame);
                int frame = (int)Math.Min(_document.LastFrame, Math.Max(0d, Math.Round(value)));

                if (frame != _preview.CurrentFrame)
                {
                    SetFrame(frame);
                }

                GUI.enabled = enabled && CanEdit;
                GUI.Label(new Rect(area.x + 12f, area.y + 81f, area.width - 24f, 22f),
                    "프레임 " + _preview.CurrentFrame + " / " + _document.LastFrame
                    + " · " + Math.Min(_preview.CurrentFrame / (double)_document.FrameRate, _document.Length).ToString("0.000")
                    + "초 · " + _document.FrameRate + " FPS · " + (_document.IsDirty ? "미저장 변경 *" : "저장 상태"));
            }

            GUILayout.BeginArea(new Rect(area.x + 10f, area.y + 105f, area.width - 20f, 32f));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("이전 프레임", GUILayout.Width(100f)))
            {
                SetFrame(Mathf.Max(0, _preview.CurrentFrame - 1));
            }

            if (GUILayout.Button(_preview.IsPlaying ? "정지" : "재생", GUILayout.Width(75f)))
            {
                bool play = !_preview.IsPlaying;

                if (play && _preview.CurrentFrame == _document.LastFrame)
                {
                    SetFrame(0);
                }

                if (!_preview.TrySetPlaying(play, out string error))
                {
                    Block(error);
                }
            }

            if (GUILayout.Button("다음 프레임", GUILayout.Width(100f)))
            {
                SetFrame((int)Math.Min(_document.LastFrame, (long)_preview.CurrentFrame + 1));
            }

            if (GUILayout.Button("공격 판정 추가", GUILayout.Width(120f)))
            {
                AnimationEventToolKey key = _document.Add(_preview.CurrentFrame);

                if (key != null)
                {
                    SelectKey(key);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.enabled = enabled;
        }

        void DrawMarkers(Rect area)
        {
            int maximumStack = 1;
            int stack = 0;
            int previousFrame = -1;

            for (int i = 0; i < _document.Keys.Count; i++)
            {
                int frame = _document.Keys[i].Frame;
                stack = frame == previousFrame ? stack + 1 : 1;
                maximumStack = Mathf.Max(maximumStack, stack);
                previousFrame = frame;
            }

            Rect content = new Rect(0f, 0f, area.width - 18f, Mathf.Max(area.height, maximumStack * 20f));
            _markerScroll = GUI.BeginScrollView(area, _markerScroll, content);
            stack = 0;
            previousFrame = -1;

            for (int i = 0; i < _document.Keys.Count; i++)
            {
                AnimationEventToolKey key = _document.Keys[i];
                stack = key.Frame == previousFrame ? stack + 1 : 1;
                previousFrame = key.Frame;
                float x = _document.LastFrame == 0 ? 0f : (float)(key.Frame / (double)_document.LastFrame) * (content.width - 20f);
                Rect marker = new Rect(x, content.height - stack * 20f, 20f, 20f);

                if (GUI.Button(marker, "◆", key == _selectedKey ? _selectedStyle : GUI.skin.button))
                {
                    SelectKey(key);
                }
            }

            GUI.EndScrollView();
        }

        void DrawPendingDialog()
        {
            Rect area = new Rect((Screen.width - 520f) * 0.5f, (Screen.height - 210f) * 0.5f, 520f, 210f);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 18f, area.y + 18f, area.width - 36f, area.height - 36f));
            GUILayout.Label(_pendingMessage, _wrappedStyle);

            if (!string.IsNullOrEmpty(_error))
            {
                GUILayout.Label(_error, _wrappedStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (_pendingCanSave)
            {
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && !_blocked && !_document.IsSaveUncertain;

                if (GUILayout.Button("저장 후 계속"))
                {
                    ResolvePending(true);
                }

                GUI.enabled = enabled;
            }

            if (GUILayout.Button(_pendingReauthor ? "현재 클립 기준으로 다시 작성" : "변경 버리고 계속"))
            {
                ResolvePending(false);
            }

            if (GUILayout.Button("취소"))
            {
                _pendingAction = null;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        static Rect GetSidebarArea()
        {
            return new Rect(Screen.width - 332f, 12f, 320f, Screen.height - 24f);
        }

        static Rect GetTimelineArea()
        {
            return new Rect(12f, Screen.height - 152f, Mathf.Max(40f, Screen.width - 356f), 140f);
        }

        static Rect GetPreviewArea()
        {
            return new Rect(12f, 12f, Mathf.Max(40f, Screen.width - 356f), Mathf.Max(40f, Screen.height - 176f));
        }
    }
}
#endif
