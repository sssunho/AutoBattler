#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaggedBanner.WorldMap.Unity
{
    public sealed class MapToolController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        WorldMapDefinition _definition;

        [SerializeField]
        string _catalogPath = "Docs/Data/Locations.csv";

        [SerializeField]
        Vector2 _defaultIconSize = new Vector2(92f, 92f);

        [SerializeField]
        Color _selectionColor = Color.cyan;

        readonly List<RectTransform> _nodes = new List<RectTransform>();
        readonly List<WorldMapRoadGraphic> _roads = new List<WorldMapRoadGraphic>();
        readonly List<WorldMapRoadPath> _paths = new List<WorldMapRoadPath>();
        readonly List<RectTransform> _roadHandles = new List<RectTransform>();
        readonly WorldMapRoadDefinition _roadDraft = new WorldMapRoadDefinition();
        readonly WorldMapRoadDefinition _roadOriginal = new WorldMapRoadDefinition();
        WorldMapViewportController _viewport;
        RectTransform _map;
        RectTransform _roadsRoot;
        MapToolWindow _window;
        Dictionary<int, string> _catalog;
        RectTransform _draft;
        int _selectedIndex = -1;
        int _selectedOriginalId;
        Vector2 _position;
        bool _ready;
        RectTransform _dragNode;
        int _dragIndex;
        int _dragOriginalId;
        int _dragPointerId;
        Vector2 _dragStart;
        Vector2 _dragPointerStart;
        Color _dragOutlineColor;
        string[] _roadNames = { "Select a road" };
        int _selectedRoadIndex = -1;
        int _selectedRoadId;
        int _roadLocationAId;
        int _roadLocationBId;
        int _dragRoadHandle = -1;
        int _roadPointerId;
        Vector2 _roadPointerStart;
        Vector2 _roadOffsetStart;

        internal WorldMapDefinition Definition => _definition;
        internal string CatalogPath => _catalogPath;
        internal bool HasSelectedNode => _selectedIndex >= 0;
        internal string[] RoadNames => _roadNames;

        void Start()
        {
            _map = (RectTransform)transform;
            _viewport = GetComponentInParent<WorldMapViewportController>();
            _viewport.ZoomChanged += UpdateScale;
            _window = ScriptableObject.CreateInstance<MapToolWindow>();
            _window.Initialize(this);
            _window.Show();
            try
            {
                _catalog = MapToolData.LoadCatalog(_catalogPath);
                int removed = MapToolData.Validate(_definition, _catalog);
                Rebuild();
                _ready = true;
                _window.SetStatus(MapToolData.Status(_definition, removed));
            }
            catch (Exception exception)
            {
                _window.SetStatus("Editing stopped: " + exception.Message);
            }
        }

        void Update()
        {
            if (_viewport != null)
            {
                bool editing = EditorWindow.focusedWindow == _window;
                bool picking = EditorWindow.focusedWindow != null
                    && EditorWindow.focusedWindow.GetType().Name == "ObjectSelector";
                if ((_dragNode != null || _dragRoadHandle >= 0)
                    && (!Application.isFocused || EditorWindow.focusedWindow == null
                    || EditorWindow.focusedWindow.GetType().Name != "GameView"))
                {
                    CancelNodeDrag(_dragNode);
                    CancelRoadDrag();
                }
                _viewport.enabled = _dragNode == null && _dragRoadHandle < 0 && !editing && !picking;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_ready || eventData.button != PointerEventData.InputButton.Left || eventData.dragging)
            {
                return;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_map, eventData.position,
                eventData.pressEventCamera, out Vector2 local) && _map.rect.Contains(local))
            {
                CancelSelection();
                _position = new Vector2((local.x - _map.rect.xMin) / _map.rect.width,
                    (local.y - _map.rect.yMin) / _map.rect.height);
                _draft = CreateNode(_position, null, _defaultIconSize, "New location", -1);
                OpenSelection(-1, null, _defaultIconSize);
            }
        }

        void Select(int index)
        {
            CancelSelection();
            _selectedIndex = index;
            WorldMapLocationDefinition node = _definition.Locations[index];
            _selectedOriginalId = node.Id;
            _position = node.Position;
            OpenSelection(node.Id, node.Icon, node.IconSize);
        }

        void OpenSelection(int id, Sprite icon, Vector2 size)
        {
            if (_window == null)
            {
                _window = ScriptableObject.CreateInstance<MapToolWindow>();
                _window.Initialize(this);
            }
            List<int> ids = new List<int>();
            List<string> names = new List<string>();
            foreach (KeyValuePair<int, string> item in _catalog)
            {
                bool used = false;
                for (int i = 0; i < _definition.Locations.Count; i++)
                {
                    if (i != _selectedIndex && _definition.Locations[i].Id == item.Key)
                    {
                        used = true;
                        break;
                    }
                }
                if (!used)
                {
                    ids.Add(item.Key);
                    names.Add(item.Key + " - " + item.Value);
                }
            }
            _window.Edit(ids.ToArray(), names.ToArray(), id, icon, size);
            _window.Show();
            _window.Focus();
            Preview(icon, size);
        }

        internal void Preview(Sprite sprite, Vector2 size)
        {
            RectTransform rect = _selectedIndex >= 0 ? _nodes[_selectedIndex] : _draft;
            if (rect == null)
            {
                return;
            }
            rect.sizeDelta = size;
            Image icon = rect.Find("Icon").GetComponent<Image>();
            icon.sprite = sprite;
            icon.color = sprite == null ? new Color(1f, 0.76f, 0.25f) : Color.white;
            Outline outline = icon.GetComponent<Outline>();
            outline.enabled = sprite == null;
            outline.effectColor = _selectionColor;
            UpdateNodeScale(rect, _map.localScale.x);
        }

        internal bool Apply(int id, Sprite icon, Vector2 size, out string message)
        {
            try
            {
                Dictionary<int, string> catalog = MapToolData.LoadCatalog(_catalogPath);
                if (!catalog.TryGetValue(id, out string name))
                {
                    throw new InvalidOperationException("Select an ID that exists in the catalog.");
                }
                if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x) || float.IsNaN(size.y)
                    || float.IsInfinity(size.x) || float.IsInfinity(size.y))
                {
                    throw new InvalidOperationException("Icon width and height must be finite and greater than zero.");
                }
                if (_selectedIndex >= 0 && (_selectedIndex >= _definition.Locations.Count
                    || _definition.Locations[_selectedIndex].Id != _selectedOriginalId))
                {
                    throw new InvalidOperationException("The asset changed externally. Select the node again.");
                }
                for (int i = 0; i < _definition.Locations.Count; i++)
                {
                    if (i != _selectedIndex && _definition.Locations[i].Id == id)
                    {
                        throw new InvalidOperationException("This ID is already placed.");
                    }
                }
                MapToolData.SaveNode(_definition, _selectedIndex, id, name, _position, icon, size);
                _catalog = catalog;
                CancelSelection();
                message = "Saved. " + MapToolData.Status(_definition, 0);
                return true;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }
        }

        internal bool DeleteSelectedNode(out string message)
        {
            if (!_ready || !HasSelectedNode)
            {
                message = "Select a saved node first.";
                return false;
            }

            try
            {
                MapToolData.DeleteNode(_definition, _selectedIndex, _selectedOriginalId);
                CancelSelection();
                message = "Node and connected roads deleted and saved."
                    + (MapToolData.HasStart(_definition) ? ""
                        : "\nStarting location is missing. Restore that ID or set Start Location Id on the asset.");
                return true;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }
        }

        internal void CancelSelection()
        {
            CancelNodeDrag(_dragNode);
            CancelRoadDrag();
            _selectedIndex = -1;
            _selectedRoadIndex = -1;
            ClearRoadHandles();
            if (_window != null)
            {
                _window.ClearSelection();
            }
            if (_draft != null)
            {
                _draft.gameObject.SetActive(false);
                Destroy(_draft.gameObject);
                _draft = null;
            }
            if (_definition != null && _map != null)
            {
                Rebuild();
            }
        }

        void Rebuild()
        {
            if (_roadsRoot != null)
            {
                _roadsRoot.gameObject.SetActive(false);
                Destroy(_roadsRoot.gameObject);
            }
            _roads.Clear();
            _paths.Clear();
            _roadsRoot = WorldMapView.CreateRect("Roads", _map, Vector2.zero);
            _roadsRoot.anchorMin = Vector2.zero;
            _roadsRoot.anchorMax = Vector2.one;
            _roadsRoot.SetAsFirstSibling();
            _roadNames = new string[_definition.Roads.Count + 1];
            _roadNames[0] = "Select a road";
            for (int i = 0; i < _definition.Roads.Count; i++)
            {
                WorldMapRoadDefinition road = _definition.Roads[i];
                RectTransform rect = WorldMapView.CreateRect("Road " + road.Id, _roadsRoot, Vector2.zero);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                WorldMapRoadGraphic graphic = rect.gameObject.AddComponent<WorldMapRoadGraphic>();
                graphic.raycastTarget = false;
                _roads.Add(graphic);
                _paths.Add(new WorldMapRoadPath());
                _roadNames[i + 1] = road.Id + " - " + road.LocationAId + " / " + road.LocationBId;
            }
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].gameObject.SetActive(false);
                Destroy(_nodes[i].gameObject);
            }
            _nodes.Clear();
            for (int i = 0; i < _definition.Locations.Count; i++)
            {
                WorldMapLocationDefinition node = _definition.Locations[i];
                _nodes.Add(CreateNode(node.Position, node.Icon, node.IconSize,
                    node.Id + " - " + node.DisplayName, i));
            }
            RefreshRoads();
            UpdateScale(_map.localScale.x);
        }

        RectTransform CreateNode(Vector2 position, Sprite sprite, Vector2 size, string name, int index)
        {
            RectTransform rect = WorldMapView.CreateRect(name, _map, size);
            rect.anchorMin = position;
            rect.anchorMax = position;
            RectTransform iconRect = WorldMapView.CreateRect("Icon", rect, Vector2.zero);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.color = sprite == null ? new Color(1f, 0.76f, 0.25f) : Color.white;
            Outline outline = iconRect.gameObject.AddComponent<Outline>();
            outline.enabled = sprite == null;
            outline.effectColor = index < 0 ? _selectionColor : Color.black;
            outline.effectDistance = new Vector2(4f, -4f);
            MapToolNodeDrag drag = rect.gameObject.AddComponent<MapToolNodeDrag>();
            drag.Initialize(this, index);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                if (index >= 0)
                {
                    Select(index);
                }
            });
            RectTransform labelRect = WorldMapView.CreateRect("Label", rect, new Vector2(400f, 40f));
            labelRect.anchoredPosition = new Vector2(0f, -size.y * 0.5f - 24f);
            Text label = labelRect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = name;
            label.raycastTarget = false;
            Outline textOutline = labelRect.gameObject.AddComponent<Outline>();
            textOutline.effectColor = Color.black;
            UpdateNodeScale(rect, _map.localScale.x);
            return rect;
        }

        void UpdateScale(float zoom)
        {
            if (zoom <= 0f)
            {
                return;
            }
            Vector3 scale = new Vector3(1f / zoom, 1f / zoom, 1f);
            for (int i = 0; i < _nodes.Count; i++)
            {
                UpdateNodeScale(_nodes[i], zoom);
            }
            if (_draft != null)
            {
                UpdateNodeScale(_draft, zoom);
            }
            for (int i = 0; i < _roadHandles.Count; i++)
            {
                _roadHandles[i].localScale = scale;
            }
        }

        static void UpdateNodeScale(RectTransform node, float zoom)
        {
            float inverseZoom = 1f / Mathf.Max(zoom, 0.0001f);
            RectTransform label = node.Find("Label") as RectTransform;
            if (label != null)
            {
                label.localScale = new Vector3(inverseZoom, inverseZoom, 1f);
                label.anchoredPosition = new Vector2(0f, -node.sizeDelta.y * 0.5f - 24f * inverseZoom);
            }
        }

        internal void BeginNodeDrag(RectTransform node, int index, PointerEventData eventData)
        {
            if (!_ready || !isActiveAndEnabled || _dragNode != null || _dragRoadHandle >= 0
                || index >= _definition.Locations.Count
                || _map.rect.width <= 0f || _map.rect.height <= 0f
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_map, eventData.pressPosition,
                    eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }
            _viewport.enabled = false;
            _dragNode = node;
            _dragIndex = index;
            _dragOriginalId = index >= 0 ? _definition.Locations[index].Id : 0;
            _dragPointerId = eventData.pointerId;
            _dragStart = node.anchorMin;
            _dragPointerStart = local;
            Outline outline = node.Find("Icon").GetComponent<Outline>();
            _dragOutlineColor = outline.effectColor;
            outline.effectColor = _selectionColor;
            MoveNodeDrag(node, eventData);
        }

        internal void MoveNodeDrag(RectTransform node, PointerEventData eventData)
        {
            if (_dragNode != node || _dragNode == null || eventData.pointerId != _dragPointerId
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_map, eventData.position,
                eventData.pressEventCamera, out Vector2 local))
            {
                Vector2 delta = local - _dragPointerStart;
                Vector2 position = _dragStart + new Vector2(delta.x / _map.rect.width, delta.y / _map.rect.height);
                position.x = Mathf.Clamp01(position.x);
                position.y = Mathf.Clamp01(position.y);
                node.anchorMin = position;
                node.anchorMax = position;
                RefreshRoads();
            }
        }

        internal void EndNodeDrag(RectTransform node, PointerEventData eventData)
        {
            if (_dragNode != node || _dragNode == null || eventData.pointerId != _dragPointerId)
            {
                return;
            }
            MoveNodeDrag(node, eventData);
            Vector2 position = node.anchorMin;
            try
            {
                if (_dragIndex >= 0 && position != _dragStart)
                {
                    MapToolData.SavePosition(_definition, _dragIndex, _dragOriginalId, position);
                }
                if (_dragIndex == _selectedIndex)
                {
                    _position = position;
                }
                if (_window != null)
                {
                    _window.SetStatus(_dragIndex >= 0 ? "Position saved. Unapplied icon and ID edits were not saved."
                        : "Draft moved. Apply and Save to keep this node.");
                }
                FinishNodeDrag();
            }
            catch (Exception exception)
            {
                CancelNodeDrag(node);
                if (_window != null)
                {
                    _window.SetStatus(exception.Message);
                }
            }
        }

        internal void CancelNodeDrag(RectTransform node)
        {
            if (_dragNode == null || _dragNode != node)
            {
                return;
            }
            _dragNode.anchorMin = _dragStart;
            _dragNode.anchorMax = _dragStart;
            FinishNodeDrag();
        }

        void FinishNodeDrag()
        {
            _dragNode.Find("Icon").GetComponent<Outline>().effectColor = _dragOutlineColor;
            _dragNode = null;
            RefreshRoads();
            if (_viewport != null)
            {
                _viewport.enabled = true;
            }
        }

        internal void SelectRoad(int index)
        {
            CancelSelection();
            if (!_ready || index < 0 || index >= _definition.Roads.Count)
            {
                return;
            }
            WorldMapRoadDefinition road = _definition.Roads[index];
            _selectedRoadIndex = index;
            _selectedRoadId = road.Id;
            _roadLocationAId = road.LocationAId;
            _roadLocationBId = road.LocationBId;
            CopyRoadAppearance(road, _roadOriginal);
            CopyRoadAppearance(road, _roadDraft);
            string[] labels = { "Start", "Control A", "Control B", "End" };
            for (int i = 0; i < labels.Length; i++)
            {
                RectTransform handle = WorldMapView.CreateRect(labels[i], _map, new Vector2(22f, 22f));
                Image image = handle.gameObject.AddComponent<Image>();
                image.color = i == 0 || i == 3 ? new Color(1f, 0.7f, 0.2f) : _selectionColor;
                Outline outline = handle.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                MapToolRoadDrag drag = handle.gameObject.AddComponent<MapToolRoadDrag>();
                drag.Initialize(this, i);
                RectTransform labelRect = WorldMapView.CreateRect("Label", handle, new Vector2(120f, 28f));
                labelRect.anchoredPosition = new Vector2(0f, 26f);
                Text label = labelRect.gameObject.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 18;
                label.alignment = TextAnchor.MiddleCenter;
                label.text = labels[i];
                label.raycastTarget = false;
                label.gameObject.AddComponent<Outline>().effectColor = Color.black;
                _roadHandles.Add(handle);
            }
            _window.EditRoad(index, road);
            RefreshRoads();
            UpdateScale(_map.localScale.x);
        }

        internal bool PreviewRoad(Vector2 startOffset, Vector2 endOffset, Vector2 controlOffsetA,
            Vector2 controlOffsetB, float width)
        {
            if (_selectedRoadIndex < 0 || !MapToolData.IsValidRoadAppearance(startOffset, endOffset,
                controlOffsetA, controlOffsetB, width))
            {
                return false;
            }
            _roadDraft.SetAppearance(startOffset, endOffset, controlOffsetA, controlOffsetB, width);
            RefreshRoads();
            return true;
        }

        internal bool ApplyRoad(out string message)
        {
            try
            {
                MapToolData.SaveRoad(_definition, _selectedRoadIndex, _selectedRoadId,
                    _roadLocationAId, _roadLocationBId, _roadOriginal, _roadDraft);
                CancelSelection();
                message = "Road appearance saved.";
                return true;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }
        }

        static void CopyRoadAppearance(WorldMapRoadDefinition source, WorldMapRoadDefinition target)
        {
            target.SetAppearance(source.StartOffset, source.EndOffset, source.ControlOffsetA,
                source.ControlOffsetB, source.Width);
        }

        bool TryGetNodePosition(int id, out Vector2 position)
        {
            for (int i = 0; i < _definition.Locations.Count; i++)
            {
                if (_definition.Locations[i].Id == id)
                {
                    position = i < _nodes.Count ? _nodes[i].anchorMin : _definition.Locations[i].Position;
                    return true;
                }
            }
            position = Vector2.zero;
            return false;
        }

        void RefreshRoads()
        {
            if (_map == null || _definition == null || _map.rect.width <= 0f || _map.rect.height <= 0f)
            {
                return;
            }
            for (int i = 0; i < _roads.Count; i++)
            {
                if (i >= _definition.Roads.Count)
                {
                    _roads[i].gameObject.SetActive(false);
                    continue;
                }
                WorldMapRoadDefinition road = _definition.Roads[i];
                bool hasA = TryGetNodePosition(road.LocationAId, out Vector2 a);
                bool hasB = TryGetNodePosition(road.LocationBId, out Vector2 b);
                bool visible = hasA && hasB;
                _roads[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                bool selected = i == _selectedRoadIndex && road.Id == _selectedRoadId;
                WorldMapRoadDefinition appearance = selected ? _roadDraft : road;
                _paths[i].Rebuild(appearance, a, b, _map.rect.size);
                _roads[i].Configure(_paths[i], appearance.Width);
                _roads[i].color = selected ? new Color(1f, 0.76f, 0.25f, 1f)
                    : new Color(0.38f, 0.29f, 0.17f, 0.85f);
            }
            RefreshRoadHandles();
        }

        void RefreshRoadHandles()
        {
            if (_selectedRoadIndex < 0 || _roadHandles.Count != 4
                || !TryGetNodePosition(_roadLocationAId, out Vector2 a)
                || !TryGetNodePosition(_roadLocationBId, out Vector2 b))
            {
                return;
            }
            Vector2 start = a + _roadDraft.StartOffset;
            Vector2 end = b + _roadDraft.EndOffset;
            SetHandlePosition(0, start);
            SetHandlePosition(1, Vector2.Lerp(start, end, 1f / 3f) + _roadDraft.ControlOffsetA);
            SetHandlePosition(2, Vector2.Lerp(start, end, 2f / 3f) + _roadDraft.ControlOffsetB);
            SetHandlePosition(3, end);
        }

        void SetHandlePosition(int index, Vector2 position)
        {
            _roadHandles[index].anchorMin = position;
            _roadHandles[index].anchorMax = position;
        }

        void ClearRoadHandles()
        {
            for (int i = 0; i < _roadHandles.Count; i++)
            {
                if (_roadHandles[i] != null)
                {
                    _roadHandles[i].gameObject.SetActive(false);
                    Destroy(_roadHandles[i].gameObject);
                }
            }
            _roadHandles.Clear();
        }

        internal void BeginRoadDrag(int handle, PointerEventData eventData)
        {
            if (!_ready || !isActiveAndEnabled || _selectedRoadIndex < 0 || _dragNode != null
                || _dragRoadHandle >= 0 || _map.rect.width <= 0f || _map.rect.height <= 0f
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_map, eventData.pressPosition,
                    eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }
            _dragRoadHandle = handle;
            _roadPointerId = eventData.pointerId;
            _roadPointerStart = local;
            _roadOffsetStart = handle == 0 ? _roadDraft.StartOffset
                : handle == 1 ? _roadDraft.ControlOffsetA
                : handle == 2 ? _roadDraft.ControlOffsetB : _roadDraft.EndOffset;
            _viewport.enabled = false;
            MoveRoadDrag(handle, eventData);
        }

        internal void MoveRoadDrag(int handle, PointerEventData eventData)
        {
            if (_dragRoadHandle != handle || _dragRoadHandle < 0 || eventData.pointerId != _roadPointerId
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_map, eventData.position,
                eventData.pressEventCamera, out Vector2 local))
            {
                Vector2 delta = local - _roadPointerStart;
                SetRoadOffset(handle, _roadOffsetStart + new Vector2(delta.x / _map.rect.width,
                    delta.y / _map.rect.height));
            }
        }

        void SetRoadOffset(int handle, Vector2 offset)
        {
            _roadDraft.SetAppearance(handle == 0 ? offset : _roadDraft.StartOffset,
                handle == 3 ? offset : _roadDraft.EndOffset,
                handle == 1 ? offset : _roadDraft.ControlOffsetA,
                handle == 2 ? offset : _roadDraft.ControlOffsetB,
                _roadDraft.Width);
            RefreshRoads();
            if (_window != null)
            {
                _window.UpdateRoadAppearance(_roadDraft);
            }
        }

        internal void EndRoadDrag(int handle, PointerEventData eventData)
        {
            if (_dragRoadHandle != handle || _dragRoadHandle < 0 || eventData.pointerId != _roadPointerId)
            {
                return;
            }
            MoveRoadDrag(handle, eventData);
            _dragRoadHandle = -1;
            _viewport.enabled = true;
            if (_window != null)
            {
                _window.SetStatus("Road preview updated. Apply and Save to keep these changes.");
            }
        }

        internal void CancelRoadDrag()
        {
            if (_dragRoadHandle < 0)
            {
                return;
            }
            int handle = _dragRoadHandle;
            _dragRoadHandle = -1;
            SetRoadOffset(handle, _roadOffsetStart);
            if (_viewport != null)
            {
                _viewport.enabled = true;
            }
        }

        void OnRectTransformDimensionsChange()
        {
            RefreshRoads();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelNodeDrag(_dragNode);
                CancelRoadDrag();
            }
        }

        void OnDisable()
        {
            CancelNodeDrag(_dragNode);
            CancelRoadDrag();
            if (_viewport != null)
            {
                _viewport.enabled = true;
            }
        }

        void OnDestroy()
        {
            _ready = false;
            ClearRoadHandles();
            if (_roadsRoot != null)
            {
                Destroy(_roadsRoot.gameObject);
            }
            if (_viewport != null)
            {
                _viewport.ZoomChanged -= UpdateScale;
                _viewport.enabled = true;
            }
            if (_window != null)
            {
                _window.Detach();
                _window.Close();
            }
        }
    }
}
#endif
