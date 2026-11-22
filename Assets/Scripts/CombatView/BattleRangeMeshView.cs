using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaggedBanner.Combat.Unity
{
    internal readonly struct BattleGroundMarker
    {
        internal HexCoordinate Coordinate { get; }
        internal bool RequiresMovement { get; }

        internal BattleGroundMarker(HexCoordinate coordinate, bool requiresMovement)
        {
            Coordinate = coordinate;
            RequiresMovement = requiresMovement;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BattleRangeMeshView : MonoBehaviour
    {
        const float _height = 0.02f;
        const float _fillHeight = 0.01f;
        const float _movementWidth = 0.05f;
        const float _attackWidth = 0.02f;
        const float _markerWidth = 0.04f;
        static readonly Color _movementColor = new Color(0.25f, 0.9f, 0.9f, 1f);
        static readonly Color _attackColor = new Color(1f, 0.44f, 0.44f, 1f);
        static readonly Color _unavailableColor = new Color(0.67f, 0.67f, 0.67f, 1f);
        static readonly Color _directColor = new Color(1f, 0.31f, 0.31f, 1f);
        static readonly Color _approachColor = new Color(1f, 0.7f, 0.25f, 1f);
        static readonly Color _overwatchColor = new Color(1f, 0.5f, 0.12f, 0.32f);
        static readonly Color _previewColor = new Color(1f, 0.9f, 0.2f, 0.48f);

        readonly List<Vector3> _vertices = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _indices = new List<int>();
        readonly HashSet<(HexCoordinate, HexDirection)> _edges = new HashSet<(HexCoordinate, HexDirection)>();
        readonly HashSet<HexCoordinate> _filledCells = new HashSet<HexCoordinate>();

        MeshFilter _filter;
        MeshRenderer _renderer;
        Mesh _mesh;
        bool _hasGeometry;

        void Awake()
        {
            EnsureComponents();
        }

        internal bool Rebuild(HexMapView map, IReadOnlyList<BattleHexEdge> movementEdges,
            IReadOnlyList<BattleHexEdge> attackEdges, IReadOnlyList<BattleGroundMarker> markers,
            bool canAttack, Material material)
        {
            EnsureComponents();
            _renderer.enabled = false;
            _hasGeometry = false;
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
            _edges.Clear();

            if (map == null || material == null || map.Width <= 0 || map.Height <= 0
                || !IsFinite(map.HexSize) || map.HexSize <= 0f)
            {
                return InvalidateGeometry();
            }

            long maxSegments = (long)(movementEdges?.Count ?? 0) + (attackEdges?.Count ?? 0)
                + (canAttack ? (long)(markers?.Count ?? 0) * 12 : 0);

            if (maxSegments > int.MaxValue / 6)
            {
                return InvalidateGeometry();
            }

            if (!AddEdges(map, movementEdges, _movementWidth * map.HexSize, _movementColor)
                || !AddEdges(map, attackEdges, _attackWidth * map.HexSize,
                    canAttack ? _attackColor : _unavailableColor))
            {
                return InvalidateGeometry();
            }

            if (canAttack && markers != null)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    BattleGroundMarker marker = markers[i];

                    if (!map.IsValid(marker.Coordinate))
                    {
                        return InvalidateGeometry();
                    }

                    Color markerColor = marker.RequiresMovement ? _approachColor : _directColor;

                    if (!AddMarkerRing(marker.Coordinate, map.HexSize, 0.5f, markerColor)
                        || marker.RequiresMovement
                        && !AddMarkerRing(marker.Coordinate, map.HexSize, 0.35f, markerColor))
                    {
                        return InvalidateGeometry();
                    }
                }
            }

            return ApplyGeometry(material);
        }

        internal bool RebuildFilledHexes(HexMapView map, IReadOnlyList<HexCoordinate> confirmed,
            IReadOnlyList<HexCoordinate> preview, Material material)
        {
            return RebuildFilledHexes(map, confirmed, preview, material, _overwatchColor, _previewColor);
        }

        internal bool RebuildFilledHexes(HexMapView map, IReadOnlyList<HexCoordinate> confirmed,
            IReadOnlyList<HexCoordinate> preview, Material material, Color confirmedColor, Color previewColor)
        {
            EnsureComponents();
            _renderer.enabled = false;
            _hasGeometry = false;
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();
            _filledCells.Clear();

            if (map == null || material == null || map.Width <= 0 || map.Height <= 0
                || !IsFinite(map.HexSize) || map.HexSize <= 0f
                || (long)(confirmed?.Count ?? 0) + (preview?.Count ?? 0) > int.MaxValue / 18)
            {
                return InvalidateGeometry();
            }

            if (!AddFilledHexes(map, preview, previewColor)
                || !AddFilledHexes(map, confirmed, confirmedColor))
            {
                return InvalidateGeometry();
            }

            return ApplyGeometry(material);
        }

        bool ApplyGeometry(Material material)
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "Battle Ground Ranges" };
                _mesh.MarkDynamic();
            }

            _mesh.Clear();
            _mesh.indexFormat = _vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0);
            _mesh.RecalculateBounds();
            _filter.sharedMesh = _mesh;
            _renderer.sharedMaterial = material;
            _hasGeometry = _vertices.Count > 0;
            return true;
        }

        bool AddFilledHexes(HexMapView map, IReadOnlyList<HexCoordinate> cells, Color color)
        {
            if (cells == null)
            {
                return true;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                HexCoordinate coordinate = cells[i];

                if (!map.IsValid(coordinate))
                {
                    return false;
                }

                if (!_filledCells.Add(coordinate))
                {
                    continue;
                }

                Vector2 centerXZ = HexLayout.AxialToLocal(coordinate, map.HexSize);
                Vector3 center = new Vector3(centerXZ.x, _fillHeight, centerXZ.y);

                if (!IsFinite(center))
                {
                    return false;
                }

                for (int direction = 0; direction < 6; direction++)
                {
                    BattleRangeOutline.GetLocalEdge(new BattleHexEdge(coordinate, (HexDirection)direction),
                        map.HexSize, out Vector3 start, out Vector3 end);
                    start.y = _fillHeight;
                    end.y = _fillHeight;

                    if (!IsFinite(start) || !IsFinite(end))
                    {
                        return false;
                    }

                    int index = _vertices.Count;
                    _vertices.Add(center);
                    _vertices.Add(start);
                    _vertices.Add(end);
                    _colors.Add(color);
                    _colors.Add(color);
                    _colors.Add(color);
                    _indices.Add(index);
                    _indices.Add(index + 1);
                    _indices.Add(index + 2);
                }
            }

            return true;
        }

        internal void SetVisible(bool visible)
        {
            if (_renderer != null)
            {
                _renderer.enabled = visible && isActiveAndEnabled && _hasGeometry
                    && _mesh != null && _renderer.sharedMaterial != null;
            }
        }

        void EnsureComponents()
        {
            if (_filter == null)
            {
                _filter = GetComponent<MeshFilter>();
            }

            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
                _renderer.enabled = false;
                _renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                _renderer.lightProbeUsage = LightProbeUsage.Off;
                _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                _renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
        }

        bool AddEdges(HexMapView map, IReadOnlyList<BattleHexEdge> edges, float width, Color color)
        {
            if (edges == null)
            {
                return true;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                BattleHexEdge edge = edges[i];

                if (!map.IsValid(edge.Coordinate) || (uint)edge.Direction >= 6)
                {
                    return false;
                }

                HexCoordinate neighbor = edge.Coordinate.GetNeighbor(edge.Direction);
                bool useNeighbor = neighbor.R < edge.Coordinate.R
                    || neighbor.R == edge.Coordinate.R && neighbor.Q < edge.Coordinate.Q;
                HexCoordinate keyCoordinate = useNeighbor ? neighbor : edge.Coordinate;
                HexDirection keyDirection = useNeighbor ? (HexDirection)(((int)edge.Direction + 3) % 6) : edge.Direction;

                if (!_edges.Add((keyCoordinate, keyDirection)))
                {
                    continue;
                }

                BattleRangeOutline.GetLocalEdge(edge, map.HexSize, out Vector3 start, out Vector3 end);

                if (!AddStroke(start, end, width, color))
                {
                    return false;
                }
            }

            return true;
        }

        bool AddMarkerRing(HexCoordinate coordinate, float hexSize, float radiusScale, Color color)
        {
            Vector2 centerXZ = HexLayout.AxialToLocal(coordinate, hexSize);
            Vector3 center = new Vector3(centerXZ.x, 0f, centerXZ.y);

            for (int direction = 0; direction < 6; direction++)
            {
                BattleRangeOutline.GetLocalEdge(new BattleHexEdge(coordinate, (HexDirection)direction),
                    hexSize, out Vector3 start, out Vector3 end);
                start = center + (start - center) * radiusScale;
                end = center + (end - center) * radiusScale;

                if (!AddStroke(start, end, _markerWidth * hexSize, color))
                {
                    return false;
                }
            }

            return true;
        }

        bool AddStroke(Vector3 start, Vector3 end, float width, Color color)
        {
            if (!IsFinite(start) || !IsFinite(end) || !IsFinite(width) || width <= 0f)
            {
                return false;
            }

            Vector3 delta = end - start;
            float length = delta.magnitude;

            if (!IsFinite(length) || length <= 0f)
            {
                return false;
            }

            Vector3 direction = delta / length;
            Vector3 offset = new Vector3(-direction.z, 0f, direction.x) * (width * 0.5f);
            start -= direction * (width * 0.5f);
            end += direction * (width * 0.5f);
            start.y = _height;
            end.y = _height;
            Vector3 first = start + offset;
            Vector3 second = end + offset;
            Vector3 third = end - offset;
            Vector3 fourth = start - offset;

            if (!IsFinite(first) || !IsFinite(second) || !IsFinite(third) || !IsFinite(fourth))
            {
                return false;
            }

            int index = _vertices.Count;
            _vertices.Add(first);
            _vertices.Add(second);
            _vertices.Add(third);
            _vertices.Add(fourth);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _indices.Add(index);
            _indices.Add(index + 1);
            _indices.Add(index + 2);
            _indices.Add(index);
            _indices.Add(index + 2);
            _indices.Add(index + 3);
            return true;
        }

        bool InvalidateGeometry()
        {
            _hasGeometry = false;
            _renderer.enabled = false;

            if (_mesh != null)
            {
                _mesh.Clear();
            }

            return false;
        }

        void OnDisable()
        {
            SetVisible(false);
        }

        void OnDestroy()
        {
            ReleaseMesh();
        }

        internal void ReleaseMesh()
        {
            if (_mesh == null)
            {
                return;
            }

            if (_filter != null && _filter.sharedMesh == _mesh)
            {
                _filter.sharedMesh = null;
            }

            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }

            _mesh = null;
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
