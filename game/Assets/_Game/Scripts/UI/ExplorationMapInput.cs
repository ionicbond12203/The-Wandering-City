using UnityEngine;
using UnityEngine.EventSystems;

namespace WanderingCity
{
    public sealed class ExplorationMapInput : MonoBehaviour, IScrollHandler, IDragHandler
    {
        public RectTransform Content;
        float zoom = 1;
        // Keep labels and selection targets readable at any map scale.
        void LateUpdate()
        {
            if (Content == null) return;
            foreach (Transform child in Content)
                if (child.GetComponent<TMPro.TMP_Text>() != null || child.GetComponent<UnityEngine.UI.Button>() != null || child.name == "You")
                    child.localScale = Vector3.one / zoom;
        }
        public void Focus(Vector2 point, float initialZoom)
        {
            zoom = Mathf.Clamp(initialZoom, 1, 8);
            Content.localScale = Vector3.one * zoom;
            var size = ((RectTransform)transform).rect.size;
            Content.anchoredPosition = new Vector2(size.x * .5f - point.x * zoom, -size.y * .5f + point.y * zoom);
            Clamp();
        }
        public void OnScroll(PointerEventData data)
        {
            float next = Mathf.Clamp(zoom + data.scrollDelta.y * .12f, 1, 8);
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
