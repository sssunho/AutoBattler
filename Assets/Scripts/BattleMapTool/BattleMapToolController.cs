#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using RaggedBanner.Combat;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RaggedBanner.BattleMaps.Authoring
{
    [DisallowMultipleComponent]
    public sealed partial class BattleMapToolController : MonoBehaviour
    {
        const float _zoomFactorPerStep = 1.15f;

        [SerializeField]
        Camera _toolCamera;

        readonly List<BattleMapUnitPlacement> _units = new List<BattleMapUnitPlacement>();
        readonly List<BattleMapStartPoint> _startPoints = new List<BattleMapStartPoint>();
        readonly BattleMapToolOverlay _overlay = new BattleMapToolOverlay();
        BattleMapToolSceneSession _session;
        BattleMapDefinition _definition;
        string _assetSnapshot;
        string _scenePath;
        string _assetName = "BattleMap";
        string _message = "Choose a scene to create a battle map, or open a saved map.";
        bool _dirty;
        bool _frameOnLoad;
        bool _hasSelection;
        bool _selectionDirty;
        HexCoordinate _selectedCoordinate;
        int _selectedUnit = -1;
        int _selectedStart = -1;
        int _placementKind;
        int _unitClass;
        int _facing;
        string _order = "0";
        Action _pendingAction;
        bool _pendingDocumentChange;
        float _framedOrthographicSize;
        string _zoomLabel = "Zoom 100%";

        float PanelWidth => Mathf.Min(370f, Screen.width * 0.46f);
        bool Ready => _session != null && !_session.IsBusy && _session.MapView != null;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_toolCamera == null)
            {
                _message = "Assign the tool camera before entering Play mode.";
                return;
            }

            _session = new BattleMapToolSceneSession(gameObject.scene);
            _session.Changed += OnSessionChanged;
            RefreshCatalogs();
        }

        void OnDisable()
        {
            if (_session != null)
            {
                _session.Changed -= OnSessionChanged;
                _session.Dispose();
                _session = null;
            }

            _pendingAction = null;
        }

        void Update()
        {
            if (_toolCamera == null)
            {
                return;
            }

            _toolCamera.pixelRect = new Rect(PanelWidth, 0f, Mathf.Max(1f, Screen.width - PanelWidth), Screen.height);

            if (!Ready || _pendingAction != null || !Application.isFocused)
            {
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            Vector2 pointer = mouse.position.ReadValue();

            if (!_toolCamera.pixelRect.Contains(pointer))
            {
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;

            if (scroll != 0f)
            {
                Zoom(scroll, pointer);
            }

            if (mouse.middleButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                float scale = 2f * _toolCamera.orthographicSize / Mathf.Max(1f, _toolCamera.pixelHeight);
                _toolCamera.transform.position -= (_toolCamera.transform.right * delta.x
                    + _toolCamera.transform.up * delta.y) * scale;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                HexMapView map = _session.MapView;
                Plane plane = new Plane(map.transform.up, map.transform.position);
                Ray ray = _toolCamera.ScreenPointToRay(pointer);

                if (plane.Raycast(ray, out float distance))
                {
                    HexCoordinate coordinate = map.WorldToAxial(ray.GetPoint(distance));

                    if (map.IsValid(coordinate))
                    {
                        RequestChange(() => SelectCoordinate(coordinate), false);
                    }
                }
            }
        }

        void OnSessionChanged()
        {
            if (_session == null)
            {
                return;
            }

            _message = _session.Status;

            if (Ready && _frameOnLoad)
            {
                _frameOnLoad = false;
                FrameMap();

                if (!ValidateDraft(out string error))
                {
                    _message = error;
                }
            }
        }

        void OpenScene(string path, BattleMapDefinition definition)
        {
            if (_session == null)
            {
                _message = "The scene session is unavailable.";
                return;
            }

            if (!_session.TryLoad(path, out string error))
            {
                _message = error;
                return;
            }

            _definition = definition;
            _assetSnapshot = definition != null ? EditorJsonUtility.ToJson(definition) : null;
            _scenePath = path;
            _assetName = definition != null ? definition.name : Path.GetFileNameWithoutExtension(path) + "_BattleMap";
            _units.Clear();
            _startPoints.Clear();

            if (definition != null)
            {
                if (definition.Units != null)
                {
                    for (int i = 0; i < definition.Units.Count; i++)
                    {
                        _units.Add(definition.Units[i]);
                    }
                }

                if (definition.StartPoints != null)
                {
                    for (int i = 0; i < definition.StartPoints.Count; i++)
                    {
                        _startPoints.Add(definition.StartPoints[i]);
                    }
                }
            }

            ClearSelection();
            _dirty = false;
            _frameOnLoad = true;
            OnSessionChanged();
        }

        void OpenDefinition(string assetPath)
        {
            BattleMapDefinition definition = AssetDatabase.LoadAssetAtPath<BattleMapDefinition>(assetPath);

            if (definition == null)
            {
                _message = "The selected asset no longer exists. Refresh the list.";
                return;
            }

            string path = BattleMapToolAssetStore.ResolveScene(definition);

            if (string.IsNullOrEmpty(path))
            {
                _message = "The saved map scene is missing. Its scene GUID cannot be resolved.";
                return;
            }

            OpenScene(path, definition);
        }

        void RequestChange(Action action, bool documentChange)
        {
            if (_session == null || _session.IsBusy)
            {
                return;
            }

            if (_selectionDirty || (documentChange && _dirty))
            {
                _pendingAction = action;
                _pendingDocumentChange = documentChange;
                return;
            }

            _clearFocus = true;
            action();
        }

        void ClearSelection()
        {
            _hasSelection = false;
            _selectionDirty = false;
            _selectedUnit = -1;
            _selectedStart = -1;
        }

        void SelectCoordinate(HexCoordinate coordinate)
        {
            ClearSelection();
            _hasSelection = true;
            _selectedCoordinate = coordinate;
            _placementKind = 0;
            _unitClass = 0;
            _facing = 0;
            int nextOrder = 0;

            for (int i = 0; i < _startPoints.Count; i++)
            {
                BattleMapStartPoint point = _startPoints[i];

                if (point != null && point.Order >= nextOrder && point.Order < int.MaxValue)
                {
                    nextOrder = point.Order + 1;
                }

                if (point != null && point.Coordinate == coordinate)
                {
                    _selectedStart = i;
                }
            }

            _order = nextOrder.ToString();

            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i] != null && _units[i].Coordinate == coordinate)
                {
                    _selectedUnit = i;
                    break;
                }
            }

            if (_selectedUnit >= 0)
            {
                _unitClass = (int)_units[_selectedUnit].UnitClass;
                _facing = (int)_units[_selectedUnit].Facing;
            }
            if (_selectedUnit < 0 && _selectedStart >= 0)
            {
                _placementKind = 1;
                _order = _startPoints[_selectedStart].Order.ToString();
                _facing = (int)_startPoints[_selectedStart].Facing;
            }

            _message = "Edit the selected hex, then Apply. Save writes the whole map asset.";
        }

        bool ApplySelection()
        {
            if (!Ready || !_hasSelection)
            {
                return false;
            }

            List<BattleMapUnitPlacement> units = new List<BattleMapUnitPlacement>(_units);
            List<BattleMapStartPoint> points = new List<BattleMapStartPoint>(_startPoints);

            if (_selectedUnit >= 0)
            {
                units.RemoveAt(_selectedUnit);
            }

            if (_selectedStart >= 0)
            {
                points.RemoveAt(_selectedStart);
            }

            if (_placementKind == 0)
            {
                units.Add(new BattleMapUnitPlacement(_selectedCoordinate, (MercenaryClass)_unitClass,
                    BattleTeam.Enemy, (HexDirection)_facing));
            }
            else
            {
                if (!int.TryParse(_order, out int order) || order < 0)
                {
                    _message = "Order must be a nonnegative integer.";
                    return false;
                }

                points.Add(new BattleMapStartPoint(_selectedCoordinate, order, (HexDirection)_facing));
            }

            if (!BattleMapDefinition.ValidatePlacements(_session.MapView.Width, _session.MapView.Height,
                units, points, out string error))
            {
                _message = error;
                return false;
            }

            _units.Clear();
            _units.AddRange(units);
            _startPoints.Clear();
            _startPoints.AddRange(points);
            _dirty = true;
            SelectCoordinate(_selectedCoordinate);
            _message = "Placement applied. Save to keep it after Play mode ends.";
            return true;
        }

        void DeleteSelection()
        {
            if (_selectedUnit >= 0)
            {
                _units.RemoveAt(_selectedUnit);
            }

            if (_selectedStart >= 0)
            {
                _startPoints.RemoveAt(_selectedStart);
            }

            _dirty = true;
            ClearSelection();
            _message = "Placement deleted from the draft. Save to update the asset.";
        }

        void DeleteCoordinate(HexCoordinate coordinate)
        {
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i] != null && _units[i].Coordinate == coordinate)
                {
                    _units.RemoveAt(i);
                }
            }

            for (int i = _startPoints.Count - 1; i >= 0; i--)
            {
                if (_startPoints[i] != null && _startPoints[i].Coordinate == coordinate)
                {
                    _startPoints.RemoveAt(i);
                }
            }

            ClearSelection();
            _dirty = true;
            _message = "Placement deleted from the draft. Save to update the asset.";
        }

        bool ValidateDraft(out string error)
        {
            if (!Ready)
            {
                error = "Load a scene with exactly one active, valid HexMapView before saving.";
                return false;
            }

            return BattleMapDefinition.ValidatePlacements(_session.MapView.Width, _session.MapView.Height,
                _units, _startPoints, out error);
        }

        bool Save(bool newCopy)
        {
            if (_selectionDirty && !ApplySelection())
            {
                return false;
            }

            if (!ValidateDraft(out string error))
            {
                _message = error;
                return false;
            }

            if (!BattleMapToolAssetStore.TrySave(newCopy ? null : _definition, _assetSnapshot, _assetName,
                _scenePath, _units, _startPoints, out BattleMapDefinition saved, out error))
            {
                _message = error;
                return false;
            }

            _definition = saved;
            _assetSnapshot = EditorJsonUtility.ToJson(saved);
            _dirty = false;
            _message = "Saved: " + AssetDatabase.GetAssetPath(saved);
            RefreshCatalogs();
            return true;
        }

        void Zoom(float steps, Vector2 screenPosition)
        {
            if (!isActiveAndEnabled || !Ready || _pendingAction != null || _toolCamera == null
                || !_toolCamera.pixelRect.Contains(screenPosition) || steps == 0f
                || float.IsNaN(steps) || float.IsInfinity(steps))
            {
                return;
            }

            HexMapView map = _session.MapView;
            Plane plane = new Plane(map.transform.up, map.transform.position);
            Ray beforeRay = _toolCamera.ScreenPointToRay(screenPosition);

            if (!plane.Raycast(beforeRay, out float beforeDistance))
            {
                return;
            }

            Vector3 anchor = beforeRay.GetPoint(beforeDistance);
            float previousSize = _toolCamera.orthographicSize;
            float nextSize = Mathf.Clamp(previousSize
                / Mathf.Pow(_zoomFactorPerStep, Mathf.Clamp(steps, -100f, 100f)), 0.1f, 100000f);

            if (nextSize == previousSize)
            {
                return;
            }

            _toolCamera.orthographicSize = nextSize;
            Ray afterRay = _toolCamera.ScreenPointToRay(screenPosition);

            if (!plane.Raycast(afterRay, out float afterDistance))
            {
                _toolCamera.orthographicSize = previousSize;
                return;
            }

            _toolCamera.transform.position += anchor - afterRay.GetPoint(afterDistance);
            UpdateZoomLabel();
        }

        void UpdateZoomLabel()
        {
            _zoomLabel = "Zoom " + (_framedOrthographicSize / _toolCamera.orthographicSize * 100f).ToString("0") + "%";
        }

        void FrameMap()
        {
            if (!Ready || _toolCamera == null)
            {
                return;
            }

            HexMapView map = _session.MapView;
            Transform mapTransform = map.transform;
            Vector3 minimum = map.AxialToWorld(new HexCoordinate(0, 0));
            Vector3 maximum = map.AxialToWorld(new HexCoordinate(map.Width - 1, map.Height - 1));
            Vector3 center = (minimum + maximum) * 0.5f;
            _toolCamera.transform.rotation = Quaternion.LookRotation(-mapTransform.up, mapTransform.forward);
            Vector3 horizontal = map.AxialToWorld(new HexCoordinate(map.Width - 1, 0)) - minimum;
            Vector3 vertical = map.AxialToWorld(new HexCoordinate(0, map.Height - 1)) - minimum;
            float radius = (horizontal.magnitude + vertical.magnitude) * 0.5f
                + map.HexSize * Mathf.Max(Mathf.Abs(mapTransform.lossyScale.x), Mathf.Abs(mapTransform.lossyScale.z)) * 2f;
            float aspect = Mathf.Max(0.1f, (Screen.width - PanelWidth) / Mathf.Max(1f, Screen.height));
            _toolCamera.orthographic = true;
            _toolCamera.orthographicSize = Mathf.Max(1f, radius / Mathf.Min(1f, aspect));
            _framedOrthographicSize = _toolCamera.orthographicSize;
            UpdateZoomLabel();
            float distance = Mathf.Max(100f, radius * 4f);
            _toolCamera.transform.position = center + mapTransform.up * distance;
            _toolCamera.nearClipPlane = 0.01f;
            _toolCamera.farClipPlane = distance * 3f;
        }
    }
}
#endif
