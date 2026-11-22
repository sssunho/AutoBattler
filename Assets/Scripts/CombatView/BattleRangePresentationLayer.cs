using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    sealed class BattleRangePresentationLayer
    {
        readonly string _name;
        readonly List<HexCoordinate> _confirmed = new List<HexCoordinate>();
        readonly List<HexCoordinate> _preview = new List<HexCoordinate>();
        BattleRangeMeshView _view;
        GameObject _viewObject;
        HexMapView _map;
        Material _material;
        int _width;
        int _height;
        float _hexSize;
        Color _confirmedColor;
        Color _previewColor;
        bool _hasCache;
        bool _hasFilledCache;

        public BattleRangeMeshView View => _view;
        public bool RequiresRebuild => !_hasCache;

        public BattleRangePresentationLayer(string name)
        {
            _name = name;
        }

        public bool Prepare(HexMapView map, Material material)
        {
            if (_map != map || map == null && !ReferenceEquals(_map, null))
            {
                Release();
            }

            if (map == null || material == null || !map.isActiveAndEnabled)
            {
                Invalidate();
                return false;
            }

            if (_view == null)
            {
                Release();
                _viewObject = new GameObject(_name);
                _viewObject.transform.SetParent(map.transform, false);
                _view = _viewObject.AddComponent<BattleRangeMeshView>();
                _map = map;
                _hasCache = false;
                _hasFilledCache = false;
                _view.SetVisible(false);
            }

            _view.gameObject.layer = map.gameObject.layer;

            if (_material != material || _width != map.Width || _height != map.Height || _hexSize != map.HexSize)
            {
                _hasCache = false;
            }

            return true;
        }

        public bool ShowFilled(HexMapView map, IReadOnlyList<HexCoordinate> confirmed,
            IReadOnlyList<HexCoordinate> preview, Material material, Color confirmedColor, Color previewColor)
        {
            if (!Prepare(map, material))
            {
                return false;
            }

            if (!_hasCache || !_hasFilledCache || _confirmedColor != confirmedColor || _previewColor != previewColor
                || !SameCells(_confirmed, confirmed) || !SameCells(_preview, preview))
            {
                if (!_view.RebuildFilledHexes(map, confirmed, preview, material, confirmedColor, previewColor))
                {
                    Invalidate();
                    return false;
                }

                CopyCells(confirmed, _confirmed);
                CopyCells(preview, _preview);
                _confirmedColor = confirmedColor;
                _previewColor = previewColor;
                _hasFilledCache = true;
                RememberGeometry(map, material);
            }

            Show();
            return true;
        }

        public bool RebuildFilled(HexMapView map, IReadOnlyList<HexCoordinate> confirmed,
            IReadOnlyList<HexCoordinate> preview, Material material)
        {
            if (!Prepare(map, material) || !_view.RebuildFilledHexes(map, confirmed, preview, material))
            {
                Invalidate();
                return false;
            }

            _hasFilledCache = false;
            RememberGeometry(map, material);
            return true;
        }

        public bool RebuildOutline(HexMapView map, IReadOnlyList<BattleHexEdge> movement,
            IReadOnlyList<BattleHexEdge> attack, IReadOnlyList<BattleGroundMarker> markers,
            bool canAttack, Material material)
        {
            if (!Prepare(map, material) || !_view.Rebuild(map, movement, attack, markers, canAttack, material))
            {
                Invalidate();
                return false;
            }

            _hasFilledCache = false;
            RememberGeometry(map, material);
            return true;
        }

        public void Show()
        {
            if (_view != null)
            {
                _view.SetVisible(_hasCache);
            }
        }

        public void Hide()
        {
            if (_view != null)
            {
                _view.SetVisible(false);
            }
        }

        public void Invalidate()
        {
            _hasCache = false;
            _hasFilledCache = false;
            _material = null;
            _confirmed.Clear();
            _preview.Clear();
            Hide();
        }

        public void Release()
        {
            Invalidate();

            if (_view != null)
            {
                _view.ReleaseMesh();
            }

            if (_viewObject != null)
            {
                _viewObject.SetActive(false);

                if (Application.isPlaying)
                {
                    Object.Destroy(_viewObject);
                }
                else
                {
                    Object.DestroyImmediate(_viewObject);
                }
            }

            _view = null;
            _viewObject = null;
            _map = null;
        }

        void RememberGeometry(HexMapView map, Material material)
        {
            _material = material;
            _width = map.Width;
            _height = map.Height;
            _hexSize = map.HexSize;
            _hasCache = true;
        }

        static bool SameCells(List<HexCoordinate> cached, IReadOnlyList<HexCoordinate> current)
        {
            int count = current != null ? current.Count : 0;

            if (cached.Count != count)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (cached[i] != current[i])
                {
                    return false;
                }
            }

            return true;
        }

        static void CopyCells(IReadOnlyList<HexCoordinate> source, List<HexCoordinate> destination)
        {
            destination.Clear();

            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                destination.Add(source[i]);
            }
        }
    }
}
