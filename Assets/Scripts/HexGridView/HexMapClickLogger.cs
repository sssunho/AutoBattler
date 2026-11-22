using UnityEngine;

namespace RaggedBanner.HexGrid.Unity
{
    [RequireComponent(typeof(HexMapView))]
    public sealed class HexMapClickLogger : MonoBehaviour
    {
        [SerializeField]
        Camera _inputCamera;

        HexMapView _mapView;

        void Awake()
        {
            _mapView = GetComponent<HexMapView>();

            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }
        }

        void Update()
        {
            if (!Input.GetMouseButtonDown(0) || _inputCamera == null)
            {
                return;
            }

            Ray ray = _inputCamera.ScreenPointToRay(Input.mousePosition);
            Plane mapPlane = new Plane(_mapView.transform.up, _mapView.transform.position);

            if (!mapPlane.Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 hitPosition = ray.GetPoint(distance);
            HexCoordinate coordinate = _mapView.WorldToAxial(hitPosition);
            bool isValid = _mapView.IsValid(coordinate);
            Debug.Log($"Hit {hitPosition} -> Hex {coordinate}, Valid: {isValid}", this);
        }
    }
}
