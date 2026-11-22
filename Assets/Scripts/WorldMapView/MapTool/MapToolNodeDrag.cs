#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace RaggedBanner.WorldMap.Unity
{
    public sealed class MapToolNodeDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        MapToolController _tool;
        int _index;

        internal void Initialize(MapToolController tool, int index)
        {
            _tool = tool;
            _index = index;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                eventData.eligibleForClick = false;
                _tool.BeginNodeDrag((RectTransform)transform, _index, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _tool.MoveNodeDrag((RectTransform)transform, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            eventData.eligibleForClick = false;
            _tool.EndNodeDrag((RectTransform)transform, eventData);
        }

        void OnDisable()
        {
            if (_tool != null)
            {
                _tool.CancelNodeDrag((RectTransform)transform);
            }
        }
    }
}
#endif
