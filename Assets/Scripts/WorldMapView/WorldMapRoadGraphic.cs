using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.WorldMap.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer), typeof(LineRenderer))]
    public sealed class WorldMapRoadGraphic : MaskableGraphic
    {
        WorldMapRoadPath _path;
        LineRenderer _lineRenderer;
        Mesh _mesh;
        float _width;

        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void Awake()
        {
            base.Awake();
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.loop = false;
            _lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            _lineRenderer.startColor = Color.white;
            _lineRenderer.endColor = Color.white;
            _lineRenderer.numCornerVertices = 2;
            _lineRenderer.numCapVertices = 2;
            raycastTarget = false;
        }

        internal void Configure(WorldMapRoadPath path, float width)
        {
            _path = path;
            _width = float.IsNaN(width) || float.IsInfinity(width) ? 0f : Mathf.Max(0f, width);
            SetVerticesDirty();
        }

        protected override void UpdateGeometry()
        {
            canvasRenderer.SetColor(color);

            if (_lineRenderer == null || _path == null || _path.Count < 2 || _width <= 0f || _path.Length <= 0f)
            {
                canvasRenderer.SetMesh(null);
                return;
            }

            Camera bakeCamera = Camera.main;

            if (bakeCamera == null)
            {
                canvasRenderer.SetMesh(null);
                return;
            }

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.MarkDynamic();
            }

            _lineRenderer.widthMultiplier = _width;
            _lineRenderer.positionCount = _path.Count;

            for (int i = 0; i < _path.Count; i++)
            {
                _lineRenderer.SetPosition(i, _path.GetPoint(i));
            }

            _lineRenderer.BakeMesh(_mesh, bakeCamera, false);
            canvasRenderer.SetMesh(_mesh);
        }

        protected override void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_mesh);
                }
                else
                {
                    DestroyImmediate(_mesh);
                }
            }

            base.OnDestroy();
        }
    }
}
