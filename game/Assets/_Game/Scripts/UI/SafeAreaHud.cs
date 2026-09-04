using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    public sealed class SafeAreaHud : MonoBehaviour
    {
        public readonly List<RectTransform> Critical = new List<RectTransform>();
        Rect lastSafe; Vector2 lastScreen;
        public static void Apply(RectTransform root, Rect safe, Vector2 screen)
        {
            root.anchorMin = safe.min / screen; root.anchorMax = safe.max / screen;
            root.offsetMin = root.offsetMax = Vector2.zero;
        }
        public static RectTransform Group(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size; rect.anchoredPosition = offset; return rect;
        }
        public static bool Inside(RectTransform rect, Rect safe, Camera camera = null)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var p = RectTransformUtility.WorldToScreenPoint(camera, corner);
                if (p.x < safe.xMin - 1 || p.x > safe.xMax + 1 || p.y < safe.yMin - 1 || p.y > safe.yMax + 1) return false;
            }
            return true;
        }
        void LateUpdate()
        {
            var screen = new Vector2(Screen.width, Screen.height); var safe = Screen.safeArea;
            if (lastSafe == safe && lastScreen == screen) return;
            Apply((RectTransform)transform, safe, screen); lastSafe = safe; lastScreen = screen;
            Canvas.ForceUpdateCanvases();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var canvas = GetComponentInParent<Canvas>();
            foreach (var rect in Critical) Debug.Assert(Inside(rect, safe, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera), "HUD outside safe area: " + rect.name);
#endif
        }
    }
}
