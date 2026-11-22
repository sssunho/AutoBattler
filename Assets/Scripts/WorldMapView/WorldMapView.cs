using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.WorldMap.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapView : MonoBehaviour
    {
        [SerializeField]
        Sprite _partySprite;

        [SerializeField]
        Color _roadColor = new Color(0.38f, 0.29f, 0.17f, 0.85f);

        [SerializeField]
        Color _availableRoadColor = new Color(1f, 0.76f, 0.25f, 1f);

        [SerializeField]
        Color _activeRoadColor = new Color(1f, 0.88f, 0.52f, 1f);

        [SerializeField, Min(1f)]
        float _timeBarWidth = 520f;

        [SerializeField, Min(1f)]
        float _timeBarHeight = 32f;

        [SerializeField, Min(0f)]
        float _timeBarBottomMargin = 32f;

        [SerializeField]
        Color _timeBarBackgroundColor = new Color(0.12f, 0.09f, 0.06f, 0.9f);

        [SerializeField]
        Color _timeBarFillColor = new Color(1f, 0.76f, 0.25f, 1f);

        [SerializeField]
        Color _timeBarTextColor = Color.white;

        readonly Dictionary<int, Vector2> _positions = new Dictionary<int, Vector2>();
        WorldMapViewportController _viewportController;
        WorldMapDefinition _definition;
        WorldMapNodeView[] _nodes;
        WorldMapRoadGraphic[] _roads;
        WorldMapRoadPath[] _roadPaths;
        RectTransform _mapRect;
        RectTransform _roadsRoot;
        RectTransform _nodesRoot;
        RectTransform _partyMarker;
        Text _expeditionTimeLabel;
        RectTransform _timeBarRoot;
        Slider _timeSlider;
        int _initialExpeditionTime;
        int _activeRoadIndex = -1;
        int _partyFromLocationId;
        int _partyToLocationId;
        float _partyProgress;
        bool _hasPartyPosition;

        internal event Action<int> NodeClicked;

        void OnEnable()
        {
            _viewportController = GetComponentInParent<WorldMapViewportController>();

            if (_viewportController != null)
            {
                _viewportController.ZoomChanged += UpdateNodeScale;
            }

            if (_mapRect != null)
            {
                UpdateNodeScale(_mapRect.localScale.x);
            }

            if (_timeBarRoot != null)
            {
                _timeBarRoot.gameObject.SetActive(true);
            }
        }

        void OnDisable()
        {
            if (_timeBarRoot != null)
            {
                _timeBarRoot.gameObject.SetActive(false);
            }

            if (_viewportController != null)
            {
                _viewportController.ZoomChanged -= UpdateNodeScale;
            }
        }

        internal void Initialize(WorldMapDefinition definition)
        {
            if (_definition != null)
            {
                return;
            }

            _definition = definition;
            _mapRect = (RectTransform)transform;
            _roadsRoot = CreateLayer("Roads");
            _nodesRoot = CreateLayer("Nodes");
            _nodes = new WorldMapNodeView[definition.Locations.Count];
            _roads = new WorldMapRoadGraphic[definition.Roads.Count];
            _roadPaths = new WorldMapRoadPath[definition.Roads.Count];
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = GetComponentInParent<Canvas>();
            _initialExpeditionTime = definition.InitialExpeditionTime;
            _timeBarRoot = CreateRect("ExpeditionTime", (RectTransform)canvas.transform,
                new Vector2(Mathf.Max(1f, _timeBarWidth), Mathf.Max(1f, _timeBarHeight)));
            _timeBarRoot.anchorMin = new Vector2(0.5f, 0f);
            _timeBarRoot.anchorMax = new Vector2(0.5f, 0f);
            _timeBarRoot.pivot = new Vector2(0.5f, 0f);
            _timeBarRoot.anchoredPosition = new Vector2(0f, Mathf.Max(0f, _timeBarBottomMargin));
            Image background = _timeBarRoot.gameObject.AddComponent<Image>();
            background.color = _timeBarBackgroundColor;
            background.raycastTarget = false;

            RectTransform fillRect = CreateRect("Fill", _timeBarRoot, Vector2.zero);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = _timeBarFillColor;
            fill.raycastTarget = false;

            _timeSlider = _timeBarRoot.gameObject.AddComponent<Slider>();
            _timeSlider.transition = Selectable.Transition.None;
            _timeSlider.navigation = new Navigation { mode = Navigation.Mode.None };
            _timeSlider.interactable = false;
            _timeSlider.minValue = 0f;
            _timeSlider.maxValue = 1f;
            _timeSlider.direction = Slider.Direction.LeftToRight;
            _timeSlider.fillRect = fillRect;

            RectTransform timeRect = CreateRect("Label", _timeBarRoot, Vector2.zero);
            timeRect.anchorMin = Vector2.zero;
            timeRect.anchorMax = Vector2.one;
            _expeditionTimeLabel = timeRect.gameObject.AddComponent<Text>();
            _expeditionTimeLabel.font = font;
            _expeditionTimeLabel.fontSize = 28;
            _expeditionTimeLabel.alignment = TextAnchor.MiddleCenter;
            _expeditionTimeLabel.resizeTextForBestFit = true;
            _expeditionTimeLabel.color = _timeBarTextColor;
            _expeditionTimeLabel.raycastTarget = false;
            Outline timeOutline = timeRect.gameObject.AddComponent<Outline>();
            timeOutline.effectColor = Color.black;
            timeOutline.effectDistance = new Vector2(2f, -2f);
            _timeBarRoot.gameObject.SetActive(isActiveAndEnabled);

            for (int i = 0; i < definition.Locations.Count; i++)
            {
                WorldMapLocationDefinition location = definition.Locations[i];
                _positions.Add(location.Id, location.Position);
                RectTransform nodeRect = CreateRect(location.DisplayName, _nodesRoot, Vector2.zero);
                WorldMapNodeView node = nodeRect.gameObject.AddComponent<WorldMapNodeView>();
                node.Initialize(location, location.Id == definition.StartLocationId, font);
                node.Clicked += OnNodeClicked;
                _nodes[i] = node;
            }

            for (int i = 0; i < definition.Roads.Count; i++)
            {
                _roadPaths[i] = new WorldMapRoadPath();
                _roads[i] = CreateRoadGraphic("Road " + definition.Roads[i].Id);
                _roads[i].color = _roadColor;
            }

            _partyMarker = CreateRect("PartyMarker", _mapRect, new Vector2(64f, 64f));
            Image markerImage = _partyMarker.gameObject.AddComponent<Image>();
            markerImage.sprite = _partySprite;
            markerImage.preserveAspect = true;
            markerImage.color = new Color(0.3f, 1f, 1f, 1f);
            markerImage.raycastTarget = false;
            _partyMarker.pivot = new Vector2(0.5f, 0f);
            RefreshRoadLayout();
            UpdateNodeScale(_mapRect.localScale.x);
        }

        internal void Refresh(WorldMapTravel travel, bool travelAllowed)
        {
            _timeSlider.SetValueWithoutNotify(_initialExpeditionTime > 0
                ? Mathf.Clamp01((float)travel.RemainingTime / _initialExpeditionTime) : 0f);
            _expeditionTimeLabel.text = "Expedition Time: " + travel.RemainingTime + " / " + _initialExpeditionTime;
            for (int i = 0; i < _nodes.Length; i++)
            {
                int id = _definition.Locations[i].Id;
                _nodes[i].SetState(id == travel.CurrentLocationId, travelAllowed && travel.CanTravelTo(id),
                    travel.IsMoving && id == travel.DestinationLocationId);
            }

            _activeRoadIndex = -1;
            for (int i = 0; i < _roads.Length; i++)
            {
                WorldMapRoadDefinition road = _definition.Roads[i];
                bool isAvailable = travelAllowed && !travel.IsMoving && travel.RemainingTime > 0
                    && (road.LocationAId == travel.CurrentLocationId || road.LocationBId == travel.CurrentLocationId);
                bool isActive = travel.IsMoving && road.Id == travel.ActiveRoad.Id;
                _roads[i].color = isActive ? _activeRoadColor : isAvailable ? _availableRoadColor : _roadColor;
                if (isActive)
                {
                    _activeRoadIndex = i;
                }
            }
        }

        internal void SetPartyPosition(int fromLocationId, int toLocationId, float progress)
        {
            _partyFromLocationId = fromLocationId;
            _partyToLocationId = toLocationId;
            _partyProgress = progress;
            _hasPartyPosition = true;
            Vector2 position = Vector2.Scale(_positions[fromLocationId] - Vector2.one * 0.5f, _mapRect.rect.size);
            if (fromLocationId != toLocationId)
            {
                if (!MatchesRoad(_activeRoadIndex, fromLocationId, toLocationId))
                {
                    _activeRoadIndex = -1;
                    for (int i = 0; i < _definition.Roads.Count; i++)
                    {
                        if (MatchesRoad(i, fromLocationId, toLocationId))
                        {
                            _activeRoadIndex = i;
                            break;
                        }
                    }
                }
                if (_activeRoadIndex >= 0)
                {
                    WorldMapRoadDefinition road = _definition.Roads[_activeRoadIndex];
                    position = EvaluatePartyPosition(road, _roadPaths[_activeRoadIndex],
                        road.LocationAId == fromLocationId ? progress : 1f - progress);
                }
            }
            _partyMarker.anchorMin = Vector2.one * 0.5f;
            _partyMarker.anchorMax = Vector2.one * 0.5f;
            _partyMarker.anchoredPosition = position;
        }

        bool MatchesRoad(int index, int fromLocationId, int toLocationId)
        {
            if (index < 0 || index >= _definition.Roads.Count)
            {
                return false;
            }
            WorldMapRoadDefinition road = _definition.Roads[index];
            return road.LocationAId == fromLocationId && road.LocationBId == toLocationId
                || road.LocationBId == fromLocationId && road.LocationAId == toLocationId;
        }

        Vector2 EvaluatePartyPosition(WorldMapRoadDefinition road, WorldMapRoadPath path, float progress)
        {
            Vector2 mapSize = _mapRect.rect.size;
            Vector2 start = Vector2.Scale(_positions[road.LocationAId] - Vector2.one * 0.5f, mapSize);
            Vector2 end = Vector2.Scale(_positions[road.LocationBId] - Vector2.one * 0.5f, mapSize);
            if (path.Count == 0)
            {
                return Vector2.Lerp(start, end, progress);
            }
            Vector2 entrance = path.GetPoint(0);
            Vector2 exit = path.GetPoint(path.Count - 1);
            float entranceLength = Vector2.Distance(start, entrance);
            float exitLength = Vector2.Distance(exit, end);
            float distance = Mathf.Clamp01(progress) * (entranceLength + path.Length + exitLength);
            if (distance < entranceLength)
            {
                return Vector2.Lerp(start, entrance, distance / entranceLength);
            }
            distance -= entranceLength;
            if (distance < path.Length)
            {
                return path.Evaluate(distance / path.Length);
            }
            return exitLength > 0f ? Vector2.Lerp(exit, end, (distance - path.Length) / exitLength) : end;
        }

        internal void DisableInput()
        {
            if (_nodes == null)
            {
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].DisableInput();
            }
        }

        internal static RectTransform CreateRect(string objectName, RectTransform parent, Vector2 size)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return rect;
        }

        RectTransform CreateLayer(string objectName)
        {
            RectTransform layer = CreateRect(objectName, _mapRect, Vector2.zero);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            return layer;
        }

        void UpdateNodeScale(float zoom)
        {
            if (_nodes == null || zoom <= 0f)
            {
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].SetZoom(zoom);
                }
            }
        }

        void OnRectTransformDimensionsChange()
        {
            if (_roads != null)
            {
                RefreshRoadLayout();
            }
        }

        void RefreshRoadLayout()
        {
            Vector2 size = _mapRect.rect.size;

            for (int i = 0; i < _roads.Length; i++)
            {
                if (_roads[i] == null)
                {
                    continue;
                }

                WorldMapRoadDefinition road = _definition.Roads[i];
                _roadPaths[i].Rebuild(road, _positions[road.LocationAId], _positions[road.LocationBId], size);
                _roads[i].Configure(_roadPaths[i], road.Width);
            }
            if (_hasPartyPosition)
            {
                SetPartyPosition(_partyFromLocationId, _partyToLocationId, _partyProgress);
            }
        }

        WorldMapRoadGraphic CreateRoadGraphic(string objectName)
        {
            RectTransform rect = CreateRect(objectName, _roadsRoot, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            return rect.gameObject.AddComponent<WorldMapRoadGraphic>();
        }

        void OnNodeClicked(int locationId)
        {
            NodeClicked?.Invoke(locationId);
        }

        void OnDestroy()
        {
            if (_timeBarRoot != null)
            {
                Destroy(_timeBarRoot.gameObject);
            }

            if (_nodes == null)
            {
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].Clicked -= OnNodeClicked;
                }
            }

            if (_roadsRoot != null)
            {
                Destroy(_roadsRoot.gameObject);
            }

            if (_nodesRoot != null)
            {
                Destroy(_nodesRoot.gameObject);
            }

            if (_partyMarker != null)
            {
                Destroy(_partyMarker.gameObject);
            }
        }
    }
}
