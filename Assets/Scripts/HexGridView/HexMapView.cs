using UnityEngine;
using UnityEngine.Rendering;

namespace RaggedBanner.HexGrid.Unity
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HexMapView : MonoBehaviour
    {
        const float _halfSqrt3 = 0.8660254037844386f;

        static readonly Vector3[] _cornerDirections =
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(-_halfSqrt3, 0f, 0.5f),
            new Vector3(-_halfSqrt3, 0f, -0.5f),
            new Vector3(0f, 0f, -1f),
            new Vector3(_halfSqrt3, 0f, -0.5f),
            new Vector3(_halfSqrt3, 0f, 0.5f)
        };

        [SerializeField]
        [Min(0)]
        int _width = 8;

        [SerializeField]
        [Min(0)]
        int _height = 8;

        [SerializeField]
        [Min(0.0001f)]
        float _hexSize = 1f;

        [SerializeField]
        Material _lineMaterial;

        Mesh _gridMesh;
        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;

        public int Width => _width;
        public int Height => _height;
        public float HexSize => _hexSize;

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _gridMesh = CreateGridMesh(_width, _height);
            _meshFilter.sharedMesh = _gridMesh;
            _meshRenderer.sharedMaterial = _lineMaterial;
        }

        void OnDestroy()
        {
            if (_gridMesh == null)
            {
                return;
            }

            if (_meshFilter != null && _meshFilter.sharedMesh == _gridMesh)
            {
                _meshFilter.sharedMesh = null;
            }

            if (Application.isPlaying)
            {
                Destroy(_gridMesh);
            }
            else
            {
                DestroyImmediate(_gridMesh);
            }
        }

        public HexCoordinate WorldToAxial(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            Vector2 positionXZ = new Vector2(localPosition.x, localPosition.z);
            return HexLayout.LocalToAxial(positionXZ, _hexSize);
        }

        public bool TryResize(int newWidth, int newHeight)
        {
            if (newWidth < 0 || newHeight < 0
                || (long)newWidth * newHeight > int.MaxValue / (_cornerDirections.Length * 2)
                || _hexSize <= 0f || float.IsNaN(_hexSize) || float.IsInfinity(_hexSize)
                || _meshFilter == null)
            {
                return false;
            }

            if (_width == newWidth && _height == newHeight)
            {
                return true;
            }

            Mesh nextMesh = CreateGridMesh(newWidth, newHeight);
            Mesh previousMesh = _gridMesh;
            _meshFilter.sharedMesh = nextMesh;
            _gridMesh = nextMesh;
            _width = newWidth;
            _height = newHeight;

            if (previousMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(previousMesh);
                }
                else
                {
                    DestroyImmediate(previousMesh);
                }
            }

            return true;
        }

        public bool IsValid(HexCoordinate coordinate)
        {
            return coordinate.Q >= 0
                && coordinate.Q < _width
                && coordinate.R >= 0
                && coordinate.R < _height;
        }

        public Vector3 AxialToWorld(HexCoordinate coordinate)
        {
            Vector2 positionXZ = HexLayout.AxialToLocal(coordinate, _hexSize);
            Vector3 localPosition = new Vector3(positionXZ.x, 0f, positionXZ.y);
            return transform.TransformPoint(localPosition);
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying || _width <= 0 || _height <= 0 || _hexSize <= 0f)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            try
            {
                for (int r = 0; r < _height; r++)
                {
                    for (int q = 0; q < _width; q++)
                    {
                        DrawCell(new HexCoordinate(q, r));
                    }
                }
            }
            finally
            {
                Gizmos.matrix = previousMatrix;
                Gizmos.color = previousColor;
            }
        }

        void DrawCell(HexCoordinate coordinate)
        {
            Vector2 centerXZ = HexLayout.AxialToLocal(coordinate, _hexSize);
            Vector3 center = new Vector3(centerXZ.x, 0.01f, centerXZ.y);

            for (int index = 0; index < _cornerDirections.Length; index++)
            {
                int nextIndex = (index + 1) % _cornerDirections.Length;
                Vector3 start = center + _cornerDirections[index] * _hexSize;
                Vector3 end = center + _cornerDirections[nextIndex] * _hexSize;
                Gizmos.DrawLine(start, end);
            }
        }

        Mesh CreateGridMesh(int width, int height)
        {
            int cellCount = checked(width * height);
            int vertexCount = checked(cellCount * _cornerDirections.Length);
            Vector3[] vertices = new Vector3[vertexCount];
            int[] indices = new int[checked(vertexCount * 2)];
            int cellIndex = 0;

            for (int r = 0; r < height && width > 0; r++)
            {
                for (int q = 0; q < width; q++)
                {
                    Vector2 centerXZ = HexLayout.AxialToLocal(new HexCoordinate(q, r), _hexSize);
                    Vector3 center = new Vector3(centerXZ.x, 0.01f, centerXZ.y);
                    int vertexStart = cellIndex * _cornerDirections.Length;
                    int indexStart = vertexStart * 2;

                    for (int cornerIndex = 0; cornerIndex < _cornerDirections.Length; cornerIndex++)
                    {
                        int nextCornerIndex = (cornerIndex + 1) % _cornerDirections.Length;
                        vertices[vertexStart + cornerIndex] = center + _cornerDirections[cornerIndex] * _hexSize;
                        indices[indexStart + cornerIndex * 2] = vertexStart + cornerIndex;
                        indices[indexStart + cornerIndex * 2 + 1] = vertexStart + nextCornerIndex;
                    }

                    cellIndex++;
                }
            }

            Mesh mesh = new Mesh
            {
                name = "Hex Grid Lines"
            };

            try
            {
                if (vertexCount > ushort.MaxValue)
                {
                    mesh.indexFormat = IndexFormat.UInt32;
                }

                mesh.vertices = vertices;
                mesh.SetIndices(indices, MeshTopology.Lines, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
            catch
            {
                if (Application.isPlaying)
                {
                    Destroy(mesh);
                }
                else
                {
                    DestroyImmediate(mesh);
                }

                throw;
            }
        }
    }
}
