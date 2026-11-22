using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(115)]
    public sealed class BattleEnvironmentView : MonoBehaviour
    {
        static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int _colorId = Shader.PropertyToID("_Color");

        sealed class CellMarkers
        {
            internal GameObject _ground;
            internal GameObject _air;
        }

        [SerializeField]
        BattleRunner _runner;

        [SerializeField]
        Material _material;

        [Header("Ground Markers")]
        [SerializeField]
        Vector3 _groundSize = new Vector3(0.65f, 0.08f, 0.65f);

        [SerializeField, Min(0f)]
        float _groundHeight = 0.06f;

        [SerializeField]
        Color _burningColor = new Color(1f, 0.3f, 0.05f);

        [SerializeField]
        Color _wetColor = new Color(0.35f, 0.75f, 1f);

        [SerializeField]
        Color _poisonedColor = new Color(0.15f, 0.5f, 0.18f);

        [SerializeField]
        Color _electrifiedColor = new Color(0.5f, 0.35f, 1f);

        [Header("Air Markers")]
        [SerializeField, Min(0.01f)]
        float _airSize = 0.3f;

        [SerializeField, Min(0f)]
        float _airHeight = 0.75f;

        [SerializeField]
        Color _smokeColor = new Color(0.4f, 0.4f, 0.4f);

        [SerializeField]
        Color _steamColor = new Color(0.9f, 0.95f, 1f);

        [SerializeField]
        Color _poisonCloudColor = new Color(0.3f, 0.65f, 0.2f);

        [SerializeField]
        Color _electricCloudColor = new Color(0.6f, 0.5f, 1f);

        [Header("Display Status")]
        [SerializeField]
        string _status;

        readonly Dictionary<HexCoordinate, CellMarkers> _markers = new Dictionary<HexCoordinate, CellMarkers>();
        readonly List<BattleEnvironmentCell> _cells = new List<BattleEnvironmentCell>();
        readonly HashSet<HexCoordinate> _seen = new HashSet<HexCoordinate>();
        readonly List<HexCoordinate> _removed = new List<HexCoordinate>();
        MaterialPropertyBlock _properties;
        BattleRunner _boundRunner;
        BattleSimulation _simulation;
        HexMapView _map;
        GameObject _root;
        Material _runtimeMaterial;
        Material _boundMaterial;
        uint _version;
        float _hexSize;
        int _materialColorId;
        bool _hasSnapshot;
        bool _settingsDirty = true;

        void OnEnable()
        {
            if (_properties == null)
            {
                _properties = new MaterialPropertyBlock();
            }

            BindRunner();
            _settingsDirty = true;
        }

        void OnValidate()
        {
            _settingsDirty = true;
        }

        void LateUpdate()
        {
            BindRunner();

            if (_runner == null || !_runner.TryGetEnvironmentContext(out BattleSimulation simulation, out HexMapView map))
            {
                ReleasePresentation();
                return;
            }

            if (_simulation != simulation || _map != map)
            {
                ReleasePresentation();
                _simulation = simulation;
                _map = map;
            }

            bool materialChanged = _boundMaterial != _material;

            if (_hasSnapshot && !_settingsDirty && !materialChanged && _hexSize == map.HexSize
                && _version == simulation.Board.EnvironmentVersion && (_markers.Count == 0 || _root != null))
            {
                return;
            }

            if (materialChanged || _root == null && _markers.Count > 0)
            {
                ReleaseResources();
            }

            ValidateSettings();
            simulation.Board.CopyEnvironment(_cells);

            if (_cells.Count == 0)
            {
                ReleaseResources();
                _status = string.Empty;
            }
            else if (EnsureMaterial())
            {
                SynchronizeMarkers(map);
                _status = string.Empty;
            }
            else
            {
                ReleaseResources();
            }

            _version = simulation.Board.EnvironmentVersion;
            _hexSize = map.HexSize;
            _boundMaterial = _material;
            _hasSnapshot = true;
            _settingsDirty = false;
        }

        void ValidateSettings()
        {
            _groundSize.x = FiniteMinimum(_groundSize.x, 0.65f, 0.01f);
            _groundSize.y = FiniteMinimum(_groundSize.y, 0.08f, 0.01f);
            _groundSize.z = FiniteMinimum(_groundSize.z, 0.65f, 0.01f);
            _groundHeight = FiniteMinimum(_groundHeight, 0.06f, 0f);
            _airSize = FiniteMinimum(_airSize, 0.3f, 0.01f);
            _airHeight = FiniteMinimum(_airHeight, 0.75f, 0f);
        }

        static float FiniteMinimum(float value, float fallback, float minimum)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(minimum, value);
        }

        bool EnsureMaterial()
        {
            if (_runtimeMaterial != null)
            {
                return true;
            }

            Shader shader = _material != null ? _material.shader : Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null || !shader.isSupported)
            {
                _status = "환경 표시에는 지원되는 Unlit 재질이 필요합니다.";
                return false;
            }

            Material candidate = _material != null ? new Material(_material) : new Material(shader);

            if (candidate.HasProperty(_baseColorId))
            {
                _materialColorId = _baseColorId;
            }
            else if (candidate.HasProperty(_colorId))
            {
                _materialColorId = _colorId;
            }
            else
            {
                DestroyOwnedObject(candidate);
                _status = "환경 표시 재질에 _BaseColor 또는 _Color 속성이 필요합니다.";
                return false;
            }

            _runtimeMaterial = candidate;
            return true;
        }

        void SynchronizeMarkers(HexMapView map)
        {
            if (_root == null)
            {
                _root = new GameObject("Battle Environment Markers");
                _root.SetActive(false);
                _root.transform.SetParent(map.transform, false);
            }

            _seen.Clear();
            _removed.Clear();

            for (int i = 0; i < _cells.Count; i++)
            {
                BattleEnvironmentCell cell = _cells[i];
                _seen.Add(cell.Coordinate);

                if (!_markers.TryGetValue(cell.Coordinate, out CellMarkers markers))
                {
                    markers = new CellMarkers();
                    _markers.Add(cell.Coordinate, markers);
                }

                Vector2 center = HexLayout.AxialToLocal(cell.Coordinate, map.HexSize);
                UpdateMarker(ref markers._ground, cell.State.Ground != BattleGroundEffect.None, PrimitiveType.Cube,
                    new Vector3(center.x, _groundHeight * map.HexSize, center.y), _groundSize * map.HexSize,
                    GroundColor(cell.State.Ground), map.gameObject.layer);
                UpdateMarker(ref markers._air, cell.State.Air != BattleAirEffect.None, PrimitiveType.Sphere,
                    new Vector3(center.x, _airHeight * map.HexSize, center.y), Vector3.one * (_airSize * map.HexSize),
                    AirColor(cell.State.Air), map.gameObject.layer);
            }

            foreach (KeyValuePair<HexCoordinate, CellMarkers> pair in _markers)
            {
                if (!_seen.Contains(pair.Key))
                {
                    DestroyMarker(pair.Value._ground);
                    DestroyMarker(pair.Value._air);
                    _removed.Add(pair.Key);
                }
            }

            for (int i = 0; i < _removed.Count; i++)
            {
                _markers.Remove(_removed[i]);
            }

            _root.SetActive(true);
        }

        void UpdateMarker(ref GameObject marker, bool visible, PrimitiveType primitive, Vector3 position,
            Vector3 scale, Color color, int layer)
        {
            if (!visible)
            {
                DestroyMarker(marker);
                marker = null;
                return;
            }

            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(primitive);
                Collider collider = marker.GetComponent<Collider>();

                if (collider != null)
                {
                    collider.enabled = false;
                    DestroyOwnedObject(collider);
                }

                marker.name = primitive == PrimitiveType.Cube ? "Ground Effect" : "Air Effect";
                marker.transform.SetParent(_root.transform, false);
            }

            marker.layer = layer;
            marker.transform.localPosition = position;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = scale;
            MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _runtimeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _properties.Clear();
            _properties.SetColor(_materialColorId, color);
            renderer.SetPropertyBlock(_properties);
        }

        Color GroundColor(BattleGroundEffect effect)
        {
            switch (effect)
            {
                case BattleGroundEffect.Burning:
                    return _burningColor;
                case BattleGroundEffect.Wet:
                    return _wetColor;
                case BattleGroundEffect.Poisoned:
                    return _poisonedColor;
                case BattleGroundEffect.Electrified:
                    return _electrifiedColor;
                default:
                    return Color.white;
            }
        }

        Color AirColor(BattleAirEffect effect)
        {
            switch (effect)
            {
                case BattleAirEffect.Smoke:
                    return _smokeColor;
                case BattleAirEffect.Steam:
                    return _steamColor;
                case BattleAirEffect.PoisonCloud:
                    return _poisonCloudColor;
                case BattleAirEffect.ElectricCloud:
                    return _electricCloudColor;
                default:
                    return Color.white;
            }
        }

        void BindRunner()
        {
            if (_boundRunner == _runner)
            {
                return;
            }

            UnbindRunner();
            ReleasePresentation();
            _boundRunner = _runner;

            if (_boundRunner != null)
            {
                _boundRunner.PresentationStopped += ReleasePresentation;
            }
        }

        void UnbindRunner()
        {
            if (_boundRunner != null)
            {
                _boundRunner.PresentationStopped -= ReleasePresentation;
            }

            _boundRunner = null;
        }

        void ReleasePresentation()
        {
            ReleaseResources();
            _simulation = null;
            _map = null;
            _hasSnapshot = false;
            _settingsDirty = true;
            _status = string.Empty;
        }

        void ReleaseResources()
        {
            DestroyMarker(_root);
            _root = null;

            if (_runtimeMaterial != null)
            {
                DestroyOwnedObject(_runtimeMaterial);
                _runtimeMaterial = null;
            }

            _markers.Clear();
            _seen.Clear();
            _removed.Clear();
        }

        static void DestroyMarker(GameObject marker)
        {
            if (marker != null)
            {
                marker.SetActive(false);
                DestroyOwnedObject(marker);
            }
        }

        static void DestroyOwnedObject(Object instance)
        {
            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        void OnDisable()
        {
            UnbindRunner();
            ReleasePresentation();
        }

        void OnDestroy()
        {
            UnbindRunner();
            ReleasePresentation();
        }
    }
}
