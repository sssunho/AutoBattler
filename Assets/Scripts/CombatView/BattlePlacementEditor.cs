using System.Collections.Generic;
using AutoBattler.HexGrid;
using AutoBattler.HexGrid.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AutoBattler.Combat.Unity
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(BattleRunner))]
    public sealed class BattlePlacementEditor : MonoBehaviour
    {
        static readonly Rect _panelRect = new Rect(10f, 10f, 270f, 210f);

        [SerializeField]
        Camera _inputCamera;

        readonly List<BattlePlacement> _placements = new List<BattlePlacement>();
        readonly List<BattleUnitView> _placementViews = new List<BattleUnitView>();
        readonly Dictionary<HexCoordinate, int> _placementIndices = new Dictionary<HexCoordinate, int>();

        BattleRunner _runner;
        HexMapView _mapView;
        Team _selectedTeam = Team.Player;
        string _widthInput;
        string _heightInput;
        string _status = string.Empty;
        bool _battleMode;

        void Awake()
        {
            _runner = GetComponent<BattleRunner>();
            _mapView = _runner.MapView;

            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }
        }

        void Start()
        {
            if (_mapView == null)
            {
                enabled = false;
                return;
            }

            _widthInput = _mapView.Width.ToString();
            _heightInput = _mapView.Height.ToString();
        }

        void OnEnable()
        {
            if (_mapView != null && _placements.Count > 0 && _placementViews.Count == 0)
            {
                RestorePlacementViews();
            }
        }

        void Update()
        {
            if (_mapView == null || _runner == null)
            {
                return;
            }

            if (_battleMode)
            {
                if (Input.GetKeyDown(KeyCode.F2) || !_runner.IsRunning)
                {
                    _runner.StopBattle();
                    _battleMode = false;
                    RestorePlacementViews();
                    _status = string.Empty;
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (_runner.TryStartBattle(_placements))
                {
                    ClearPlacementViews();
                    _battleMode = true;
                    _status = string.Empty;
                    GUIUtility.keyboardControl = 0;
                }
                else
                {
                    _status = "Cannot start: check map and runner settings.";
                }

                return;
            }

            if (GUIUtility.keyboardControl == 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    _selectedTeam = Team.Enemy;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    _selectedTeam = Team.Player;
                }
            }

            bool place = Input.GetMouseButtonDown(0);
            bool remove = Input.GetMouseButtonDown(1);

            if ((!place && !remove) || _inputCamera == null || IsPointerOverUI())
            {
                return;
            }

            GUIUtility.keyboardControl = 0;
            Ray ray = _inputCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(_mapView.transform.up, _mapView.transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                return;
            }

            HexCoordinate coordinate = _mapView.WorldToAxial(ray.GetPoint(distance));

            if (!_mapView.IsValid(coordinate))
            {
                return;
            }

            if (remove)
            {
                if (_placementIndices.TryGetValue(coordinate, out int index))
                {
                    RemovePlacement(index);
                }
            }
            else if (!_placementIndices.ContainsKey(coordinate)
                && _runner.TryCreatePlacement(_selectedTeam, coordinate, out BattlePlacement placement))
            {
                BattleUnitView view = _runner.CreatePlacementView(placement);

                if (view != null)
                {
                    _placementIndices.Add(coordinate, _placements.Count);
                    _placements.Add(placement);
                    _placementViews.Add(view);
                }
            }
        }

        void OnGUI()
        {
            if (_mapView == null || _runner == null || _widthInput == null)
            {
                return;
            }

            GUILayout.BeginArea(_panelRect, GUI.skin.box);
            GUILayout.Label(_battleMode ? "Battle | F2: Restore placement" : "Placement | F1: Start battle");
            GUILayout.Label(_selectedTeam == Team.Player ? "Team: Player (2)" : "Team: Enemy (1)");
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !_battleMode;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Width", GUILayout.Width(50f));
            _widthInput = GUILayout.TextField(_widthInput, 10);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Height", GUILayout.Width(50f));
            _heightInput = GUILayout.TextField(_heightInput, 10);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Apply map size"))
            {
                ApplyMapSize();
                GUIUtility.keyboardControl = 0;
            }

            GUI.enabled = previousEnabled;
            GUILayout.Label("Left: Place | Right: Remove");
            GUILayout.Label(_status);
            GUILayout.EndArea();

            Event current = Event.current;

            if (current.type == EventType.MouseDown && !_panelRect.Contains(current.mousePosition))
            {
                GUIUtility.keyboardControl = 0;
            }
        }

        void OnDisable()
        {
            if (_runner != null)
            {
                _runner.StopBattle();
            }

            _battleMode = false;
            ClearPlacementViews();
        }

        void ApplyMapSize()
        {
            if (_battleMode)
            {
                return;
            }

            if (!int.TryParse(_widthInput, out int width)
                || !int.TryParse(_heightInput, out int height)
                || !_mapView.TryResize(width, height))
            {
                _status = "Invalid map size.";
                return;
            }

            for (int i = _placements.Count - 1; i >= 0; i--)
            {
                if (!_mapView.IsValid(_placements[i].Coordinate))
                {
                    RemovePlacement(i);
                }
            }

            _status = string.Empty;
        }

        bool IsPointerOverUI()
        {
            Vector3 mouse = Input.mousePosition;
            Vector2 guiPosition = new Vector2(mouse.x, Screen.height - mouse.y);
            return _panelRect.Contains(guiPosition)
                || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        }

        void RemovePlacement(int index)
        {
            DestroyPlacementView(_placementViews[index]);
            _placementIndices.Remove(_placements[index].Coordinate);
            int lastIndex = _placements.Count - 1;

            if (index != lastIndex)
            {
                _placements[index] = _placements[lastIndex];
                _placementViews[index] = _placementViews[lastIndex];
                _placementIndices[_placements[index].Coordinate] = index;
            }

            _placements.RemoveAt(lastIndex);
            _placementViews.RemoveAt(lastIndex);
        }

        void RestorePlacementViews()
        {
            for (int i = 0; i < _placements.Count; i++)
            {
                _placementViews.Add(_runner.CreatePlacementView(_placements[i]));
            }
        }

        void ClearPlacementViews()
        {
            for (int i = 0; i < _placementViews.Count; i++)
            {
                DestroyPlacementView(_placementViews[i]);
            }

            _placementViews.Clear();
        }

        void DestroyPlacementView(BattleUnitView view)
        {
            if (view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
        }
    }
}
