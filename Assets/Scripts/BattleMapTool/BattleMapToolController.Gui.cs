#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.BattleMaps.Authoring
{
    public sealed partial class BattleMapToolController
    {
        static readonly string[] _libraryTabs = { "Map scenes", "Saved maps" };
        static readonly string[] _placementLabels = { "Enemy unit", "Player start" };
        static readonly string[] _classLabels = { "Warrior", "Rogue", "Archer" };
        static readonly string[] _directionLabels = { "E", "NE", "NW", "W", "SW", "SE" };

        string[] _scenePaths = Array.Empty<string>();
        string[] _definitionPaths = Array.Empty<string>();
        string _filter = "";
        int _libraryTab;
        bool _showLibrary = true;
        bool _showEntries;
        bool _clearFocus;
        Vector2 _panelScroll;
        Vector2 _libraryScroll;
        Vector2 _entryScroll;
        GUIStyle _wrappedLabel;
        GUIStyle _titleLabel;
        GUIStyle _listButton;

        void RefreshCatalogs()
        {
            _scenePaths = FindAssetPaths("t:Scene");
            _definitionPaths = FindAssetPaths("t:BattleMapDefinition");
        }

        static string[] FindAssetPaths(string query)
        {
            string[] guids = AssetDatabase.FindAssets(query, new[] { "Assets" });
            string[] paths = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        void OnGUI()
        {
            if (_wrappedLabel == null)
            {
                _wrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
                _titleLabel = new GUIStyle(_wrappedLabel) { fontSize = 19, fontStyle = FontStyle.Bold };
                _listButton = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true
                };
            }

            if (_clearFocus)
            {
                GUI.FocusControl(null);
                _clearFocus = false;
            }

            if (Ready && _toolCamera != null)
            {
                _overlay.Draw(_toolCamera, _session.MapView, _units, _startPoints,
                    _hasSelection, _selectedCoordinate);
            }

            Rect panelRect = new Rect(0f, 0f, PanelWidth, Screen.height);
            Color previousColor = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(10f, 10f, Mathf.Max(1f, PanelWidth - 20f), Screen.height - 20f));
            _panelScroll = GUILayout.BeginScrollView(_panelScroll);
            GUILayout.Label("Battle Map Tool", _titleLabel);
            GUILayout.Label("Placement data authoring", _wrappedLabel);
            bool canInteract = _session != null && !_session.IsBusy && _pendingAction == null;
            GUI.enabled = canInteract;
            _showLibrary = GUILayout.Toggle(_showLibrary, "Scene / asset browser", GUI.skin.button);

            if (_showLibrary)
            {
                DrawLibrary();
            }

            GUILayout.Space(10f);
            GUILayout.Label(string.IsNullOrEmpty(_scenePath) ? "No scene selected" : _scenePath, _wrappedLabel);

            if (Ready)
            {
                GUILayout.Label($"Grid: {_session.MapView.Width} x {_session.MapView.Height} | "
                    + $"Enemies: {_units.Count} | Starts: {_startPoints.Count}", _wrappedLabel);
            }

            GUILayout.Label(_definition != null ? "Asset: " + AssetDatabase.GetAssetPath(_definition)
                : "New unsaved map", _wrappedLabel);
            GUILayout.Label("Name for new asset / copy");
            _assetName = GUILayout.TextField(_assetName);
            GUI.enabled = canInteract && Ready;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save"))
            {
                Save(false);
            }

            if (GUILayout.Button("Save new copy"))
            {
                Save(true);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate"))
            {
                _message = ValidateDraft(out string error) ? "Map placements are valid." : error;
            }

            if (GUILayout.Button("Frame map"))
            {
                FrameMap();
            }

            GUILayout.EndHorizontal();
            bool zoomControlsEnabled = GUI.enabled;
            GUI.enabled = zoomControlsEnabled && _toolCamera != null;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("-", GUILayout.Width(35f)))
            {
                Zoom(-1f, _toolCamera.pixelRect.center);
            }

            GUILayout.Label(_zoomLabel, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("+", GUILayout.Width(35f)))
            {
                Zoom(1f, _toolCamera.pixelRect.center);
            }

            GUILayout.EndHorizontal();
            GUI.enabled = zoomControlsEnabled;
            GUILayout.Label(_dirty || _selectionDirty ? "Unsaved changes" : "No unsaved changes", _wrappedLabel);
            GUILayout.Space(10f);

            if (_hasSelection)
            {
                DrawSelection();
            }
            else
            {
                GUILayout.Label("Left-click a hex to place or edit.\nMiddle-drag to pan. Scroll to zoom at the pointer.", _wrappedLabel);
            }

            _showEntries = GUILayout.Toggle(_showEntries, "Placed entries", GUI.skin.button);

            if (_showEntries)
            {
                DrawEntries();
            }

            GUI.enabled = true;
            GUILayout.Space(12f);
            GUILayout.Label(_session != null && _session.IsBusy ? _session.Status : _message, _wrappedLabel);
            GUILayout.Space(8f);
            GUILayout.Label("Save before leaving Play mode. The source scene is not saved by this tool.", _wrappedLabel);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            if (_pendingAction != null)
            {
                DrawPendingChange();
            }
        }

        void DrawLibrary()
        {
            _libraryTab = GUILayout.Toolbar(_libraryTab, _libraryTabs);
            GUILayout.Label("Filter by scene / asset path");
            _filter = GUILayout.TextField(_filter);

            if (GUILayout.Button("Refresh list"))
            {
                RefreshCatalogs();
            }

            _libraryScroll = GUILayout.BeginScrollView(_libraryScroll, GUILayout.Height(165f));
            string[] paths = _libraryTab == 0 ? _scenePaths : _definitionPaths;
            bool found = false;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];

                if (path == gameObject.scene.path
                    || (!string.IsNullOrEmpty(_filter)
                    && path.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    continue;
                }

                found = true;

                if (GUILayout.Button(Path.GetFileNameWithoutExtension(path) + "\n" + path, _listButton))
                {
                    if (_libraryTab == 0)
                    {
                        RequestChange(() => OpenScene(path, null), true);
                    }
                    else
                    {
                        RequestChange(() => OpenDefinition(path), true);
                    }
                }
            }

            if (!found)
            {
                GUILayout.Label("No matching entries.", _wrappedLabel);
            }

            GUILayout.EndScrollView();
        }

        void DrawSelection()
        {
            GUILayout.Label($"Hex ({_selectedCoordinate.Q}, {_selectedCoordinate.R})", _titleLabel);
            int kind = GUILayout.SelectionGrid(_placementKind, _placementLabels, 2);

            if (kind != _placementKind)
            {
                _placementKind = kind;
                _selectionDirty = true;
            }

            if (_placementKind == 0)
            {
                GUILayout.Label("Team: Enemy");
                int unitClass = GUILayout.SelectionGrid(_unitClass, _classLabels, 3);

                if (unitClass != _unitClass)
                {
                    _unitClass = unitClass;
                    _selectionDirty = true;
                }
            }
            else
            {
                GUILayout.Label("Player start order (ascending)");
                string order = GUILayout.TextField(_order);

                if (order != _order)
                {
                    _order = order;
                    _selectionDirty = true;
                }
            }

            GUILayout.Label("Facing");
            int facing = GUILayout.SelectionGrid(_facing, _directionLabels, 3);

            if (facing != _facing)
            {
                _facing = facing;
                _selectionDirty = true;
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply"))
            {
                ApplySelection();
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && (_selectedUnit >= 0 || _selectedStart >= 0);

            if (GUILayout.Button("Delete"))
            {
                DeleteSelection();
            }

            GUI.enabled = previousEnabled;

            if (GUILayout.Button("Cancel"))
            {
                ClearSelection();
            }

            GUILayout.EndHorizontal();
        }

        void DrawEntries()
        {
            _entryScroll = GUILayout.BeginScrollView(_entryScroll, GUILayout.Height(145f));

            for (int i = 0; i < _units.Count; i++)
            {
                BattleMapUnitPlacement unit = _units[i];
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(unit == null ? "Invalid enemy entry" : $"{unit.UnitClass} {unit.Coordinate}"))
                {
                    if (unit != null)
                    {
                        RequestChange(() => SelectCoordinate(unit.Coordinate), false);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(25f)))
                {
                    RequestChange(() =>
                    {
                        if (unit != null)
                        {
                            DeleteCoordinate(unit.Coordinate);
                        }
                        else
                        {
                            _units.Remove(null);
                            ClearSelection();
                            _dirty = true;
                        }
                    }, false);
                }

                GUILayout.EndHorizontal();
            }

            for (int i = 0; i < _startPoints.Count; i++)
            {
                BattleMapStartPoint point = _startPoints[i];
                GUILayout.BeginHorizontal();

                if (GUILayout.Button(point == null ? "Invalid start entry" : $"Start {point.Order}: {point.Coordinate}"))
                {
                    if (point != null)
                    {
                        RequestChange(() => SelectCoordinate(point.Coordinate), false);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(25f)))
                {
                    RequestChange(() =>
                    {
                        if (point != null)
                        {
                            DeleteCoordinate(point.Coordinate);
                        }
                        else
                        {
                            _startPoints.Remove(null);
                            ClearSelection();
                            _dirty = true;
                        }
                    }, false);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        void DrawPendingChange()
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
            float width = Mathf.Min(440f, Screen.width - 20f);
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - 220f) * 0.5f, width, 220f);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f));
            GUILayout.Label("Unsaved changes", _titleLabel);
            GUILayout.Label(_pendingDocumentChange ? "Save this map before opening another scene or asset?"
                : "Apply the current hex edit before changing selection?", _wrappedLabel);
            GUILayout.Label(_message, _wrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(_pendingDocumentChange ? "Save & continue" : "Apply & continue"))
            {
                if (_pendingDocumentChange ? Save(false) : ApplySelection())
                {
                    CompletePendingChange();
                }
            }

            if (GUILayout.Button("Discard"))
            {
                CompletePendingChange();
            }

            if (GUILayout.Button("Cancel"))
            {
                _pendingAction = null;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void CompletePendingChange()
        {
            Action action = _pendingAction;
            _pendingAction = null;
            _clearFocus = true;
            action();
        }
    }
}
#endif
