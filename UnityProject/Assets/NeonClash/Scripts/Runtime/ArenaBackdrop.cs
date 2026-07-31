using UnityEngine;

namespace NeonClash
{
    /// <summary>Loads the project-owned painted arena layer and keeps vector stage motifs in front of it.</summary>
    public static class ArenaBackdrop
    {
        public const string ResourcePath = "NeonClash/Backgrounds/neon-rooftop-arena-v2";

        public static SpriteRenderer Create(Transform parent, Color primary, Color accent)
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                Debug.LogError("Painted arena background is missing at Resources/" + ResourcePath + ".");
                return null;
            }
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Neon Rooftop Arena V2";
            GameObject item = new GameObject("Painted Arena Background");
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(0f, 1.18f, 3f);
            float worldWidth = 18.7f;
            float scale = worldWidth / (texture.width / 100f);
            item.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -80;
            renderer.color = Color.Lerp(Color.white, Color.Lerp(primary, accent, 0.5f), 0.08f);
            return renderer;
        }
    }
}
