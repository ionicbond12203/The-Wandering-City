using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WanderingCity
{
    public enum MinimapOrientation { NorthUp, RotateWithPlayer }
    public sealed class CircularMinimap : MonoBehaviour
    {
        public MinimapOrientation Orientation = MinimapOrientation.NorthUp;
        [Min(10)] public float ViewRadiusMeters = 85;
        public RectTransform PlayerMarker { get; private set; }
        public RectTransform ObjectiveMarker { get; private set; }
        GameSession game; WorldMapData data; RectTransform background, north;
        TMP_Text distance;
        readonly Dictionary<string, RectTransform> markers = new Dictionary<string, RectTransform>();
        Sprite circle, arrow; Texture2D circleTexture, arrowTexture;
        const float Radius = 100, InnerRadius = 86;
        RectTransform Center(Transform parent, string name, Vector2 size)
            => SafeAreaHud.Group(parent, name, Vector2.one * .5f, size, Vector2.zero);
        Image Icon(Transform parent, string name, float size, Color color, Sprite sprite)
        {
            var r = Center(parent, name, Vector2.one * size); var image = r.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false; return image;
        }
        Sprite MakeSprite(bool triangle, out Texture2D texture)
        {
            const int resolution = 256;
            texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            var pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++) for (int x = 0; x < resolution; x++)
            {
                float px = (x + .5f - resolution / 2f) / (resolution / 2f), py = (y + .5f - resolution / 2f) / (resolution / 2f);
                bool inside = triangle ? py > -.65f && py < .9f && Mathf.Abs(px) < (.9f - py) * .45f : px * px + py * py <= .98f;
                pixels[y * resolution + x] = inside ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), Vector2.one * .5f);
        }
        public void Create(GameSession owner, TMP_FontAsset font)
        {
            game = owner; data = WorldMapData.Load();
            circle = MakeSprite(false, out circleTexture); arrow = MakeSprite(true, out arrowTexture);
            Icon(transform, "Compass rim", 210, new Color(.83f, .72f, .46f), circle);
            var clip = Icon(transform, "Circular clip", 200, new Color(.08f, .16f, .18f), circle);
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            background = Center(clip.transform, "Baked terrain", Vector2.one);
            var image = background.gameObject.AddComponent<RawImage>(); image.texture = data.Texture; image.raycastTarget = false;
            foreach (var poi in game.Exploration.Points.Values)
                markers[poi.Id] = Icon(clip.transform, poi.Id, poi.Type == PoiType.TeleportPoint ? 12 : 8, Color.white, circle).rectTransform;
            ObjectiveMarker = Icon(clip.transform, "Active objective", 16, new Color(1, .75f, .25f), arrow).rectTransform;
            PlayerMarker = Icon(clip.transform, "Player arrow", 20, Color.white, arrow).rectTransform;
            north = Center(transform, "North", new Vector2(24, 24));
            var label = north.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = "N"; label.fontSize = 18; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            var footer = Center(transform, "Objective distance", new Vector2(240, 30)); footer.anchoredPosition = new Vector2(0, -125);
            distance = footer.gameObject.AddComponent<TextMeshProUGUI>(); distance.font = font; distance.fontSize = 17; distance.alignment = TextAlignmentOptions.Center; distance.raycastTarget = false;
            Refresh();
        }
        public void Refresh()
        {
            var player = game.Player.transform.position;
            float yaw = Orientation == MinimapOrientation.RotateWithPlayer ? game.Player.transform.eulerAngles.y : 0;
            float scale = Radius / Mathf.Max(10, ViewRadiusMeters);
            var uv = data.WorldToUV(player); var size = new Vector2(data.WorldBounds.size.x, data.WorldBounds.size.z) * scale;
            background.sizeDelta = size; background.localEulerAngles = new Vector3(0, 0, yaw);
            background.anchoredPosition = Quaternion.Euler(0, 0, yaw) * Vector2.Scale(Vector2.one * .5f - uv, size);
            PlayerMarker.anchoredPosition = Vector2.zero; PlayerMarker.localEulerAngles = new Vector3(0, 0, yaw - game.Player.transform.eulerAngles.y);
            north.anchoredPosition = Quaternion.Euler(0, 0, yaw) * new Vector2(0, 113);
            foreach (var poi in game.Exploration.Points.Values)
            {
                var marker = markers[poi.Id]; marker.gameObject.SetActive(WorldMapData.Visible(game.State, poi.Id));
                marker.anchoredPosition = WorldMapData.MinimapOffset(poi.transform.position, player, yaw, scale, InnerRadius);
                marker.GetComponent<Image>().color = poi.Completed || game.State.activatedTeleportIds.Contains(poi.Id) ? new Color(.3f, .9f, .85f) : new Color(.9f, .75f, .5f);
            }
            var objective = WorldMapData.ObjectivePosition(game); ObjectiveMarker.gameObject.SetActive(objective.HasValue);
            if (objective.HasValue)
            {
                var offset = WorldMapData.MinimapOffset(objective.Value, player, yaw, scale, InnerRadius);
                ObjectiveMarker.anchoredPosition = offset;
                ObjectiveMarker.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg);
                var delta = objective.Value - player; delta.y = 0;
                distance.text = "目标 " + Mathf.RoundToInt(delta.magnitude) + " m  ·  M 全图";
            }
            else distance.text = "自由探索  ·  M 全图";
        }
        void LateUpdate() { if (game != null) Refresh(); }
        void OnDestroy() { Destroy(circle); Destroy(arrow); Destroy(circleTexture); Destroy(arrowTexture); }
    }
}
