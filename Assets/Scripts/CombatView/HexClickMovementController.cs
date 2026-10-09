using System.Collections.Generic;
using AutoBattler.HexGrid;
using AutoBattler.HexGrid.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RaggedBanner.Combat.Unity
{
    [DefaultExecutionOrder(-100)]
    public sealed class HexClickMovementController : MonoBehaviour
    {
        [SerializeField]
        Camera _inputCamera;

        [SerializeField]
        BattleRunner _runner;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        EventSystem _eventSystem;
        PointerEventData _pointerData;

        void Start()
        {
            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }

            if (_inputCamera == null || _runner == null)
            {
                Debug.LogError("Hex click movement requires a camera and BattleRunner.", this);
                enabled = false;
            }
        }

        void Update()
        {
            Mouse mouse = Mouse.current;

            if (_inputCamera == null || _runner == null || !_runner.isActiveAndEnabled
                || !_runner.IsInitialized || _runner.IsBusy || !_runner.CurrentUnitId.HasValue
                || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 mousePosition = mouse.position.ReadValue();

            if (IsPointerOverUI(mousePosition))
            {
                return;
            }

            HexMapView mapView = _runner.MapView;

            if (mapView == null || mapView.HexSize <= 0f
                || float.IsNaN(mapView.HexSize) || float.IsInfinity(mapView.HexSize))
            {
                return;
            }

            Ray ray = _inputCamera.ScreenPointToRay(mousePosition);
            Plane plane = new Plane(mapView.transform.up, mapView.transform.position);

            if (!plane.Raycast(ray, out float distance))
            {
                return;
            }

            HexCoordinate destination = mapView.WorldToAxial(ray.GetPoint(distance));
            _runner.TryMove(_runner.CurrentUnitId.Value, destination);
        }

        bool IsPointerOverUI(Vector2 position)
        {
            EventSystem current = EventSystem.current;

            if (current == null)
            {
                return false;
            }

            if (_eventSystem != current)
            {
                _eventSystem = current;
                _pointerData = new PointerEventData(current);
            }

            _pointerData.Reset();
            _pointerData.position = position;
            _uiHits.Clear();
            current.RaycastAll(_pointerData, _uiHits);
            bool overUI = false;

            for (int i = 0; i < _uiHits.Count; i++)
            {
                if (_uiHits[i].module is UnityEngine.UI.GraphicRaycaster)
                {
                    overUI = true;
                    break;
                }
            }

            _uiHits.Clear();
            return overUI;
        }
    }
}
