#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace RaggedBanner.WorldMap.Unity
{
    public sealed class MapToolRoadDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler
    {
        MapToolController _tool;
        int _handle;

        internal void Initialize(MapToolController tool, int handle)
        {
            _tool = tool;
            _handle = handle;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                eventData.eligibleForClick = false;
                _tool.BeginRoadDrag(_handle, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _tool.MoveRoadDrag(_handle, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            eventData.eligibleForClick = false;
            _tool.EndRoadDrag(_handle, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
        }

        void OnDisable()
        {
            if (_tool != null)
            {
                _tool.CancelRoadDrag();
            }
        }
    }
}
#endif
