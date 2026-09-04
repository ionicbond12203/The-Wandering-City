using UnityEngine;
using UnityEngine.EventSystems;

namespace WanderingCity
{
    public sealed class ExplorationMapInput : MonoBehaviour, IScrollHandler, IDragHandler
    {
        public RectTransform Content;
        float zoom = 1;
        public void OnScroll(PointerEventData data)
        {
            float next = Mathf.Clamp(zoom + data.scrollDelta.y * .12f, 1, 3);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var local);
            Content.anchoredPosition = local - (local - Content.anchoredPosition) * (next / zoom);
            zoom = next; Content.localScale = Vector3.one * zoom; Clamp();
        }
        public void OnDrag(PointerEventData data)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var now);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position - data.delta, data.pressEventCamera, out var before);
            Content.anchoredPosition += now - before; Clamp();
        }
        void Clamp()
        {
            Vector2 extent = ((RectTransform)transform).rect.size * (zoom - 1);
            Content.anchoredPosition = new Vector2(Mathf.Clamp(Content.anchoredPosition.x, -extent.x, 0), Mathf.Clamp(Content.anchoredPosition.y, 0, extent.y));
        }
    }
}
